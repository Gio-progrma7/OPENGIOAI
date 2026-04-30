using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// Burbuja de chat estilo moderno con paleta Emerald.
    /// Soporta texto enriquecido, rutas/URLs clicables, preview de imágenes,
    /// avatar circular y un toggle "Mostrar más / Mostrar menos" cuando la
    /// respuesta es larga (típico del Constructor que devuelve JSON crudo).
    /// </summary>
    public class BurbujaChat : Control
    {
        // ─────────────────────────────────────────────────────────────
        //  Constantes de diseño
        // ─────────────────────────────────────────────────────────────
        private const int TamañoAvatar    = 36;
        private const int RadioBurbuja    = 14;
        private const int PaddingBurbuja  = 14;
        private const int MargenAvatar    = 8;
        private const int GapAvatarTexto  = 10;
        private const int PreviewSize     = 110;
        private const int PreviewGap      = 6;

        // Toggle "ver más / ver menos"
        private const int LimiteCaracteres = 420;   // a partir de aquí se colapsa
        private const int LineasColapsado  = 6;     // líneas visibles en colapso
        private const int AltoToggle       = 26;    // alto del botón de toggle

        // Timestamp + botón copiar
        private const int AltoMeta         = 16;    // alto del pie con hora
        private const int TamañoBtnCopiar  = 22;

        // ─────────────────────────────────────────────────────────────
        //  Paleta dinámica (sigue a EmeraldTheme.IsDark)
        // ─────────────────────────────────────────────────────────────
        private static Color BgDeep   => EmeraldTheme.BgDeep;
        private static Color BgCard   => EmeraldTheme.BgCard;
        private static Color BgCardHi => EmeraldTheme.IsDark
                                           ? ColorTranslator.FromHtml("#003d73")
                                           : ColorTranslator.FromHtml("#D6E8FF");
        private static Color Emerald  => EmeraldTheme.Emerald500;
        private static Color Emerald4 => EmeraldTheme.Emerald400;
        private static Color Emerald9 => EmeraldTheme.Emerald900;
        // Bubble usuario: siempre teal oscuro para contraste
        private static readonly Color Emerald12 = ColorTranslator.FromHtml("#0f2a2a");
        private static Color TextMain  => EmeraldTheme.TextPrimary;
        private static Color TextSub   => EmeraldTheme.TextSecondary;
        private static Color BorderCol => EmeraldTheme.IsDark
                                            ? ColorTranslator.FromHtml("#1a3a5c")
                                            : ColorTranslator.FromHtml("#C5D8F0");

        // Fondos por remitente (calculados desde las propiedades anteriores)
        private static Color BgUsuario  => Emerald9;    // teal profundo (contrasta en ambos modos)
        private static Color BgUsuario2 => Emerald12;
        private static Color BgIA       => BgCard;
        private static Color BgIA2      => BgCardHi;

        // ─────────────────────────────────────────────────────────────
        //  Regex para rutas, URLs y bloques de código
        // ─────────────────────────────────────────────────────────────
        private static readonly Regex RegexRuta = new Regex(
            @"([A-Za-z]:\\(?:[^\n""'<>|*?\r\\/:]+\\)*[^\n""'<>|*?\r\/:\\]*)|(https?:\/\/[^\s\n""'<>]+)",
            RegexOptions.Compiled);

        // Bloques cercados con triple backtick: ``` ... ``` (multiline)
        private static readonly Regex RegexCodeBlock = new Regex(
            @"```[a-zA-Z0-9_\-]*\n([\s\S]*?)\n?```",
            RegexOptions.Compiled);

        // Inline code con backtick simple: `texto`
        private static readonly Regex RegexInlineCode = new Regex(
            @"`([^`\n]+)`",
            RegexOptions.Compiled);

        // Fuente monoespaciada para bloques de código
        private static readonly Color BgCode = ColorTranslator.FromHtml("#020617");

        private static readonly string[] ExtensionesImagen =
            { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

        // ─────────────────────────────────────────────────────────────
        //  Propiedades públicas
        // ─────────────────────────────────────────────────────────────
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EsUsuario { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image ImagenPerfil { get; set; }

        // ─────────────────────────────────────────────────────────────
        //  Campos privados
        // ─────────────────────────────────────────────────────────────
        private int _anchoMaximo;
        private string _textoCompleto;
        private bool _expandido = true;          // por defecto expandido; se colapsa si excede
        private bool _puedeColapsar;             // true si el texto supera el límite
        private RichTextBox _txtMensaje;
        private Panel _avatarPanel;
        private Label _btnToggle;                // "Mostrar más ⌄ / Mostrar menos ⌃"
        private Label _lblHora;                  // timestamp HH:mm
        private Label _btnCopiar;                // 📋 al hover
        private DateTime _timestamp = DateTime.Now;
        private ToolTip _tooltip;
        private List<Panel> _previews = new List<Panel>();

        // Animación "está pensando..." (cuando el texto termina en "...")
        private System.Windows.Forms.Timer _timerTyping;
        private int _typingTick;

        // Debounce del formateo durante streaming (rutas/code blocks)
        private System.Windows.Forms.Timer _timerFormatoDebounce;

        // Zoom global compartido por todas las burbujas (controlado desde FrmMandos)
        public static float ZoomGlobal { get; set; } = 1.0f;
        private const float FontSizeBase = 9.5F;

        // ─────────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────────
        public BurbujaChat(string texto, bool esUsuario, int anchoMaximo, Image avatarImg)
        {
            EsUsuario      = esUsuario;
            Text           = texto;
            _textoCompleto = texto ?? string.Empty;
            _anchoMaximo   = anchoMaximo;

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            DoubleBuffered = true;
            Font           = new Font("Segoe UI Emoji", FontSizeBase * ZoomGlobal, FontStyle.Regular, GraphicsUnit.Point);
            BackColor      = BgDeep;
            Margin         = new Padding(0, 4, 0, 4);

            _tooltip = new ToolTip { AutomaticDelay = 400, ShowAlways = true };

            Color colorFondo = EsUsuario ? BgUsuario : BgIA;

            // ── Determinar si el contenido debe poder colapsarse ─────
            _puedeColapsar = !EsUsuario && _textoCompleto.Length > LimiteCaracteres;
            _expandido     = !_puedeColapsar; // si es largo arranca colapsado

            // ── RichTextBox ──────────────────────────────────────────
            _txtMensaje = new RichTextBox
            {
                Text        = TextoVisible(),
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.None,
                Multiline   = true,
                TabStop     = false,
                Font        = Font,
                ForeColor   = TextMain,
                BackColor   = colorFondo,
                Cursor      = Cursors.IBeam,
                WordWrap    = true
            };

            _txtMensaje.MouseMove  += OnTxtMouseMove;
            _txtMensaje.MouseClick += OnTxtMouseClick;

            AplicarFormatoEnlaces();

            // ── Avatar ───────────────────────────────────────────────
            if (avatarImg != null)
            {
                ImagenPerfil = avatarImg;
                _avatarPanel = new Panel
                {
                    Size      = new Size(TamañoAvatar, TamañoAvatar),
                    BackColor = Color.Transparent
                };

                var pb = new PictureBox
                {
                    Image     = avatarImg,
                    SizeMode  = PictureBoxSizeMode.Zoom,
                    Dock      = DockStyle.Fill,
                    BackColor = Color.Transparent
                };

                _avatarPanel.Controls.Add(pb);
                Controls.Add(_avatarPanel);
            }

            Controls.Add(_txtMensaje);

            // ── Timestamp ────────────────────────────────────────────
            _lblHora = new Label
            {
                AutoSize  = true,
                Text      = _timestamp.ToString("HH:mm"),
                ForeColor = Color.FromArgb(110, 130, 145),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 7.5F, FontStyle.Regular)
            };
            Controls.Add(_lblHora);

            // ── Botón copiar (visible al hover) ──────────────────────
            _btnCopiar = new Label
            {
                AutoSize    = false,
                Size        = new Size(TamañoBtnCopiar, TamañoBtnCopiar),
                Text        = "\U0001F4CB",        // 📋
                TextAlign   = ContentAlignment.MiddleCenter,
                ForeColor   = TextSub,
                BackColor   = Color.Transparent,
                Cursor      = Cursors.Hand,
                Font        = new Font("Segoe UI Emoji", 9F),
                Visible     = false
            };
            _btnCopiar.Click       += OnCopiarClick;
            _btnCopiar.MouseEnter  += (_, __) => _btnCopiar.ForeColor = Emerald4;
            _btnCopiar.MouseLeave  += (_, __) => _btnCopiar.ForeColor = TextSub;
            _tooltip.SetToolTip(_btnCopiar, "Copiar mensaje");
            Controls.Add(_btnCopiar);

            // Hover: mostrar/ocultar el botón copiar en toda la burbuja
            this.MouseEnter        += MostrarCopiar;
            _txtMensaje.MouseEnter += MostrarCopiar;
            _txtMensaje.MouseLeave += OcultarSiCorresponde;
            this.MouseLeave        += OcultarSiCorresponde;

            // ── Botón "Mostrar más / menos" (solo si aplica) ─────────
            if (_puedeColapsar)
            {
                _btnToggle = new Label
                {
                    AutoSize    = false,
                    TextAlign   = ContentAlignment.MiddleCenter,
                    ForeColor   = Emerald4,
                    BackColor   = Color.Transparent,
                    Cursor      = Cursors.Hand,
                    Font        = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                    Height      = AltoToggle,
                    Text        = "  Mostrar más  ⌄"
                };
                _btnToggle.Click       += OnToggleClick;
                _btnToggle.MouseEnter  += (_, __) => _btnToggle.ForeColor = Emerald;
                _btnToggle.MouseLeave  += (_, __) => _btnToggle.ForeColor = Emerald4;
                Controls.Add(_btnToggle);
            }

            // ── Previews de imágenes ─────────────────────────────────
            CargarPreviewImagenes(_textoCompleto, colorFondo);

            // ── Animación "pensando" cuando texto termina en "..." ───
            ActualizarAnimacionTyping();
        }

        private void ActualizarAnimacionTyping()
        {
            bool debeAnimar = !EsUsuario && (_textoCompleto?.TrimEnd().EndsWith("...") == true);

            if (debeAnimar && _timerTyping == null)
            {
                _timerTyping = new System.Windows.Forms.Timer { Interval = 380 };
                _timerTyping.Tick += (_, __) =>
                {
                    _typingTick = (_typingTick + 1) % 4;
                    string sufijo = new string('.', _typingTick);
                    string baseTxt = (_textoCompleto ?? "").TrimEnd('.', ' ');
                    _txtMensaje.Text = baseTxt + (sufijo.Length > 0 ? "  " + sufijo : "");
                    AplicarFormatoEnlaces();
                };
                _timerTyping.Start();
            }
            else if (!debeAnimar && _timerTyping != null)
            {
                _timerTyping.Stop();
                _timerTyping.Dispose();
                _timerTyping = null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  TOGGLE Mostrar más / menos
        // ─────────────────────────────────────────────────────────────
        private string TextoVisible()
        {
            if (_expandido || !_puedeColapsar) return _textoCompleto;

            var lineas = _textoCompleto.Replace("\r\n", "\n").Split('\n');
            string corte = lineas.Length > LineasColapsado
                ? string.Join("\n", lineas.Take(LineasColapsado))
                : _textoCompleto.Substring(0, Math.Min(LimiteCaracteres, _textoCompleto.Length));

            return corte.TrimEnd() + " …";
        }

        // ─────────────────────────────────────────────────────────────
        //  COPIAR + HOVER
        // ─────────────────────────────────────────────────────────────
        private void MostrarCopiar(object sender, EventArgs e)
        {
            if (_btnCopiar != null) _btnCopiar.Visible = true;
        }

        private void OcultarSiCorresponde(object sender, EventArgs e)
        {
            if (_btnCopiar == null) return;
            // Solo ocultar si el cursor salió de toda la burbuja
            var pos = PointToClient(MousePosition);
            if (!ClientRectangle.Contains(pos)) _btnCopiar.Visible = false;
        }

        private async void OnCopiarClick(object sender, EventArgs e)
        {
            try { Clipboard.SetText(_textoCompleto ?? string.Empty); }
            catch { return; }

            string original = _btnCopiar.Text;
            _btnCopiar.Text      = "✓";   // ✓
            _btnCopiar.ForeColor = Emerald;
            await Task.Delay(900);
            if (_btnCopiar == null || _btnCopiar.IsDisposed) return;
            _btnCopiar.Text      = original;
            _btnCopiar.ForeColor = TextSub;
        }

        private void OnToggleClick(object sender, EventArgs e)
        {
            _expandido = !_expandido;
            _btnToggle.Text = _expandido
                ? "  Mostrar menos  ⌃"
                : "  Mostrar más  ⌄";
            _txtMensaje.Text = TextoVisible();
            AplicarFormatoEnlaces();
            CalcularTamaño();
            Invalidate();
            Parent?.PerformLayout();
        }

        // ─────────────────────────────────────────────────────────────
        //  ENLACES
        // ─────────────────────────────────────────────────────────────
        private void AplicarFormatoEnlaces()
        {
            // Reset previo: color base y back base (por si era código antes)
            int oldStart  = _txtMensaje.SelectionStart;
            int oldLength = _txtMensaje.SelectionLength;
            _txtMensaje.SelectAll();
            _txtMensaje.SelectionColor    = TextMain;
            _txtMensaje.SelectionBackColor = _txtMensaje.BackColor;
            _txtMensaje.SelectionFont     = Font;

            // 1) Bloques de código triple backtick (ej. ```py\nprint()\n```)
            var fuenteMono = new Font("Cascadia Mono", 9F, FontStyle.Regular);
            foreach (Match m in RegexCodeBlock.Matches(_txtMensaje.Text))
            {
                _txtMensaje.Select(m.Index, m.Length);
                _txtMensaje.SelectionColor     = Emerald4;
                _txtMensaje.SelectionBackColor = BgCode;
                _txtMensaje.SelectionFont      = fuenteMono;
            }

            // 2) Inline code `palabra`
            foreach (Match m in RegexInlineCode.Matches(_txtMensaje.Text))
            {
                // No re-aplicar dentro de un code block ya pintado (su back ya es BgCode)
                _txtMensaje.Select(m.Index, 1);
                if (_txtMensaje.SelectionBackColor.ToArgb() == BgCode.ToArgb()) continue;

                _txtMensaje.Select(m.Index, m.Length);
                _txtMensaje.SelectionColor     = Emerald4;
                _txtMensaje.SelectionBackColor = BgCode;
                _txtMensaje.SelectionFont      = fuenteMono;
            }

            // 3) Rutas y URLs (no pisar si ya es código)
            foreach (Match m in RegexRuta.Matches(_txtMensaje.Text))
            {
                _txtMensaje.Select(m.Index, 1);
                if (_txtMensaje.SelectionBackColor.ToArgb() == BgCode.ToArgb()) continue;

                _txtMensaje.Select(m.Index, m.Length);
                _txtMensaje.SelectionColor = Emerald4;
                _txtMensaje.SelectionFont  = new Font(_txtMensaje.Font, FontStyle.Underline);
            }

            _txtMensaje.SelectionStart  = oldStart;
            _txtMensaje.SelectionLength = 0;
        }

        private void OnTxtMouseMove(object sender, MouseEventArgs e)
        {
            string ruta = ObtenerRutaBajoCursor(e.Location);
            _txtMensaje.Cursor = ruta != null ? Cursors.Hand : Cursors.IBeam;

            if (ruta != null)
                _tooltip.SetToolTip(_txtMensaje, ruta.Length > 80 ? ruta.Substring(0, 77) + "..." : ruta);
            else
                _tooltip.SetToolTip(_txtMensaje, string.Empty);
        }

        private void OnTxtMouseClick(object sender, MouseEventArgs e)
        {
            string ruta = ObtenerRutaBajoCursor(e.Location);
            if (ruta == null) return;

            if (EsImagen(ruta) && File.Exists(ruta))
                MostrarPreviewPopup(ruta);
            else
                AbrirRuta(ruta);
        }

        private string ObtenerRutaBajoCursor(Point pos)
        {
            int idx = _txtMensaje.GetCharIndexFromPosition(pos);
            foreach (Match m in RegexRuta.Matches(_txtMensaje.Text))
                if (idx >= m.Index && idx <= m.Index + m.Length)
                    return m.Value.Trim();
            return null;
        }

        private static void AbrirRuta(string ruta)
        {
            try { Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }); }
            catch (Exception ex) { Debug.WriteLine($"[BurbujaChat] AbrirRuta: {ex.Message}"); }
        }

        // ─────────────────────────────────────────────────────────────
        //  PREVIEW DE IMÁGENES
        // ─────────────────────────────────────────────────────────────
        private static bool EsImagen(string ruta) =>
            ExtensionesImagen.Any(ext => ruta.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        private void CargarPreviewImagenes(string texto, Color colorFondo)
        {
            var rutas = RegexRuta.Matches(texto)
                .Cast<Match>()
                .Select(m => m.Value.Trim())
                .Where(r => EsImagen(r) && File.Exists(r))
                .ToList();

            foreach (string ruta in rutas)
            {
                try
                {
                    string rutaLocal = ruta;
                    Image imagen     = Image.FromFile(rutaLocal);

                    Panel contenedor = new Panel
                    {
                        BackColor = colorFondo,
                        Cursor    = Cursors.Hand,
                        Tag       = rutaLocal,
                        Size      = new Size(PreviewSize, PreviewSize),
                        Padding   = new Padding(3)
                    };

                    var pb = new PictureBox
                    {
                        Image     = imagen,
                        SizeMode  = PictureBoxSizeMode.Zoom,
                        Dock      = DockStyle.Fill,
                        Cursor    = Cursors.Hand,
                        BackColor = Color.Transparent
                    };

                    pb.Click          += (s, ev) => MostrarPreviewPopup(rutaLocal);
                    contenedor.Click  += (s, ev) => MostrarPreviewPopup(rutaLocal);
                    contenedor.Controls.Add(pb);

                    _tooltip.SetToolTip(contenedor,
                        $"🖼 {Path.GetFileName(rutaLocal)}\nClic para ampliar");

                    _previews.Add(contenedor);
                    Controls.Add(contenedor);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BurbujaChat] Preview: {ruta} → {ex.Message}");
                }
            }
        }

        private void MostrarPreviewPopup(string rutaImagen)
        {
            try
            {
                Image imagen = Image.FromFile(rutaImagen);

                Form popup = new Form
                {
                    Text            = Path.GetFileName(rutaImagen),
                    Size            = new Size(800, 580),
                    MinimumSize     = new Size(400, 300),
                    StartPosition   = FormStartPosition.CenterScreen,
                    BackColor       = BgDeep,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    Icon            = SystemIcons.Application
                };

                var pb = new PictureBox
                {
                    Image     = imagen,
                    SizeMode  = PictureBoxSizeMode.Zoom,
                    Dock      = DockStyle.Fill,
                    BackColor = Color.Transparent
                };

                Panel barraInferior = new Panel
                {
                    Dock      = DockStyle.Bottom,
                    Height    = 44,
                    BackColor = BgCard,
                    Padding   = new Padding(8, 5, 8, 5)
                };

                var btnAbrir = new Button
                {
                    Text      = "📂  Abrir en visor",
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = TextMain,
                    BackColor = Emerald9,
                    Height    = 32,
                    Width     = 150,
                    Left      = 8,
                    Top       = 6,
                    Cursor    = Cursors.Hand
                };
                btnAbrir.FlatAppearance.BorderColor       = Emerald;
                btnAbrir.FlatAppearance.BorderSize        = 1;
                btnAbrir.FlatAppearance.MouseOverBackColor = Emerald;
                btnAbrir.Click += (s, e) => AbrirRuta(rutaImagen);

                var lblInfo = new Label
                {
                    Text      = $"{imagen.Width} × {imagen.Height} px",
                    ForeColor = TextSub,
                    AutoSize  = true,
                    Top       = 13,
                    Left      = 170
                };

                barraInferior.Controls.Add(btnAbrir);
                barraInferior.Controls.Add(lblInfo);
                popup.Controls.Add(pb);
                popup.Controls.Add(barraInferior);
                popup.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BurbujaChat] PopupPreview: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  TAMAÑO Y LAYOUT
        // ─────────────────────────────────────────────────────────────
        public void ActualizarAncho(int nuevoAncho)
        {
            _anchoMaximo = nuevoAncho;
            CalcularTamaño();
            Invalidate();
        }

        /// <summary>
        /// Aplica un factor de zoom (ej. 1.0, 1.25, 0.85) a la burbuja:
        /// recalcula la fuente del control y del RichTextBox y dispara el layout.
        /// </summary>
        public void AplicarZoom(float factor)
        {
            float size = FontSizeBase * factor;
            var nuevaFont = new Font("Segoe UI Emoji", size, FontStyle.Regular, GraphicsUnit.Point);
            Font = nuevaFont;
            if (_txtMensaje != null) _txtMensaje.Font = nuevaFont;
            // El timestamp y el botón "ver más" mantienen tamaño fijo (no zoom).
            CalcularTamaño();
            AplicarFormatoEnlaces();  // re-pinta links/code con la nueva fuente
            Invalidate();
        }

        public void ActualizarTexto(string nuevoTexto)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(() => ActualizarTexto(nuevoTexto)); return; }

            if (nuevoTexto == _textoCompleto) return;
            string textoPrevio = _textoCompleto ?? string.Empty;
            _textoCompleto = nuevoTexto ?? string.Empty;
            Text           = _textoCompleto;

            // Reevaluar si ahora se puede colapsar
            bool podiaColapsar = _puedeColapsar;
            _puedeColapsar = !EsUsuario && _textoCompleto.Length > LimiteCaracteres;

            if (_puedeColapsar && _btnToggle == null)
            {
                _btnToggle = new Label
                {
                    AutoSize    = false,
                    TextAlign   = ContentAlignment.MiddleCenter,
                    ForeColor   = Emerald4,
                    BackColor   = Color.Transparent,
                    Cursor      = Cursors.Hand,
                    Font        = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                    Height      = AltoToggle,
                    Text        = "  Mostrar más  ⌄"
                };
                _btnToggle.Click += OnToggleClick;
                Controls.Add(_btnToggle);
                _expandido = false;
            }
            else if (!_puedeColapsar && podiaColapsar && _btnToggle != null)
            {
                Controls.Remove(_btnToggle);
                _btnToggle.Dispose();
                _btnToggle = null;
                _expandido = true;
            }

            // ── Streaming: si el nuevo texto es una extensión del anterior ──
            //    sólo agregamos el delta con AppendText (no reemplaza todo
            //    el contenido, conserva la posición del cursor y NO llama
            //    a SelectAll que es lo que hacía parpadear el caret).
            bool esStreaming = !_puedeColapsar
                            && !string.IsNullOrEmpty(textoPrevio)
                            && _textoCompleto.StartsWith(textoPrevio, StringComparison.Ordinal)
                            && _textoCompleto.Length > textoPrevio.Length;

            if (esStreaming)
            {
                string delta = _textoCompleto.Substring(textoPrevio.Length);

                // Suspender redraw del RichTextBox para que el AppendText no
                // dispare repaint visible (cierre y reapertura del caret +
                // scroll interno). Es la causa real del parpadeo.
                SuspendDrawing(_txtMensaje);
                try
                {
                    _txtMensaje.AppendText(delta);
                    // No movemos SelectionStart aquí — AppendText ya lo deja
                    // al final; tocarlo dispara más repaint sin valor.
                }
                finally
                {
                    ResumeDrawing(_txtMensaje);
                }

                // El gradiente del control padre solo se redibuja cada 80 ms
                // (debounce) — corta el parpadeo de fondo / sombra / borde.
                ProgramarFormatoDebounce();
                ProgramarLayoutDebounce();
                ActualizarAnimacionTyping();
                return;
            }

            _txtMensaje.Text = TextoVisible();
            AplicarFormatoEnlaces();
            CalcularTamaño();
            ActualizarAnimacionTyping();
            Invalidate();
        }

        // ─────────────────────────────────────────────────────────────
        //  WIN32: suspender/reanudar redraw de un control
        //  Es la única forma de evitar que un RichTextBox parpadee al
        //  recibir AppendText() en streaming — sin esto, cada llamada
        //  dispara repaint del scroll interno + caret.
        // ─────────────────────────────────────────────────────────────
        private const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private static void SuspendDrawing(Control c)
        {
            if (c == null || !c.IsHandleCreated) return;
            SendMessage(c.Handle, WM_SETREDRAW, 0, 0);
        }

        private static void ResumeDrawing(Control c)
        {
            if (c == null || !c.IsHandleCreated) return;
            SendMessage(c.Handle, WM_SETREDRAW, 1, 0);
            c.Invalidate();
        }

        /// <summary>
        /// Debounce 80 ms: una sola llamada a CalcularTamaño + Invalidate
        /// agrupando ráfagas de tokens. Evita el parpadeo del gradiente.
        /// </summary>
        private System.Windows.Forms.Timer _timerLayoutDebounce;
        private void ProgramarLayoutDebounce()
        {
            if (_timerLayoutDebounce == null)
            {
                _timerLayoutDebounce = new System.Windows.Forms.Timer { Interval = 80 };
                _timerLayoutDebounce.Tick += (_, __) =>
                {
                    _timerLayoutDebounce.Stop();
                    if (IsDisposed) return;
                    CalcularTamaño();
                    Invalidate();
                };
            }
            _timerLayoutDebounce.Stop();
            _timerLayoutDebounce.Start();
        }

        /// <summary>
        /// Durante streaming, agrupar el reformateo de enlaces/código en
        /// un único pase 250 ms después del último token. Evita N llamadas
        /// a SelectAll por cada token (causa principal del parpadeo).
        /// </summary>
        private void ProgramarFormatoDebounce()
        {
            if (_timerFormatoDebounce == null)
            {
                _timerFormatoDebounce = new System.Windows.Forms.Timer { Interval = 250 };
                _timerFormatoDebounce.Tick += (_, __) =>
                {
                    _timerFormatoDebounce.Stop();
                    if (IsDisposed) return;
                    AplicarFormatoEnlaces();
                };
            }
            _timerFormatoDebounce.Stop();
            _timerFormatoDebounce.Start();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            CalcularTamaño();
        }

        private void CalcularTamaño()
        {
            if (!IsHandleCreated) return;

            // El control siempre ocupa el ancho del panel; la burbuja se alinea
            // adentro a izquierda (IA) o derecha (usuario).
            int anchoControl = Math.Max(_anchoMaximo, 100);

            // Ancho máximo de la burbuja: ~78% del ancho del panel
            int anchoMaxBurbuja = (int)(anchoControl * 0.78);
            int anchoTextoMax   = anchoMaxBurbuja - PaddingBurbuja * 2;

            Size tamTexto = TextRenderer.MeasureText(
                _txtMensaje.Text,
                Font,
                new Size(anchoTextoMax, int.MaxValue),
                TextFormatFlags.WordBreak);

            // Ancho real de la burbuja (se ajusta al contenido si es corto)
            int anchoBurbuja = Math.Min(
                tamTexto.Width + PaddingBurbuja * 2 + 8,
                anchoMaxBurbuja);

            // Espacio para previews
            int previewsAlto = 0;
            int colsPrev = 1;
            if (_previews.Count > 0)
            {
                colsPrev     = Math.Max(1, (anchoBurbuja - PaddingBurbuja * 2) / (PreviewSize + PreviewGap));
                int rows     = (int)Math.Ceiling(_previews.Count / (double)colsPrev);
                previewsAlto = rows * (PreviewSize + PreviewGap) + PreviewGap;
            }

            int altoToggleEspacio = (_btnToggle != null) ? AltoToggle + 4 : 0;
            int altoMetaEspacio   = AltoMeta + 2;  // siempre reservamos espacio para el timestamp
            int altoBurbuja = tamTexto.Height + PaddingBurbuja * 2 + previewsAlto + altoToggleEspacio + altoMetaEspacio;

            // Alto del control = max(burbuja, avatar)
            int altoControl = Math.Max(altoBurbuja, TamañoAvatar) + 6;

            Width  = anchoControl;
            Height = altoControl;

            // ── Posición horizontal ──────────────────────────────────
            // Avatar IA → izquierda. Avatar usuario → derecha. Burbuja al lado opuesto.
            int xAvatar, xBurbuja;
            if (EsUsuario)
            {
                xAvatar  = anchoControl - TamañoAvatar - MargenAvatar;
                xBurbuja = xAvatar - GapAvatarTexto - anchoBurbuja;
            }
            else
            {
                xAvatar  = MargenAvatar;
                xBurbuja = xAvatar + TamañoAvatar + GapAvatarTexto;
            }

            // Avatar arriba, alineado al inicio de la burbuja
            if (_avatarPanel != null)
                _avatarPanel.SetBounds(xAvatar, 2, TamañoAvatar, TamañoAvatar);

            // ── Posicionar RichTextBox ───────────────────────────────
            _txtMensaje.SetBounds(
                xBurbuja + PaddingBurbuja,
                PaddingBurbuja,
                anchoBurbuja - PaddingBurbuja * 2,
                tamTexto.Height + 4);

            // ── Posicionar previews ──────────────────────────────────
            if (_previews.Count > 0)
            {
                int xStart = xBurbuja + PaddingBurbuja;
                int yStart = PaddingBurbuja + tamTexto.Height + 8;
                int col = 0, row = 0;

                foreach (var panel in _previews)
                {
                    int px = xStart + col * (PreviewSize + PreviewGap);
                    int py = yStart + row * (PreviewSize + PreviewGap);
                    panel.SetBounds(px, py, PreviewSize, PreviewSize);

                    col++;
                    if (col >= colsPrev) { col = 0; row++; }
                }
            }

            // ── Posicionar toggle "Mostrar más/menos" ────────────────
            if (_btnToggle != null)
            {
                int yToggle = PaddingBurbuja + tamTexto.Height + previewsAlto;
                _btnToggle.SetBounds(
                    xBurbuja + PaddingBurbuja - 2,
                    yToggle,
                    anchoBurbuja - PaddingBurbuja * 2 + 4,
                    AltoToggle);
            }

            // ── Posicionar timestamp (al pie, alineado con la burbuja) ──
            if (_lblHora != null)
            {
                int yMeta = altoBurbuja - AltoMeta + 2;
                int xMeta = EsUsuario
                    ? xBurbuja + anchoBurbuja - _lblHora.PreferredWidth - 6
                    : xBurbuja + PaddingBurbuja - 2;
                _lblHora.SetBounds(xMeta, yMeta, _lblHora.PreferredWidth, AltoMeta);
            }

            // ── Posicionar botón copiar (esquina superior opuesta al avatar) ──
            if (_btnCopiar != null)
            {
                int xCopy = EsUsuario
                    ? xBurbuja + 4
                    : xBurbuja + anchoBurbuja - TamañoBtnCopiar - 4;
                int yCopy = 4;
                _btnCopiar.SetBounds(xCopy, yCopy, TamañoBtnCopiar, TamañoBtnCopiar);
                _btnCopiar.BringToFront();
            }

            // Guardar el rect de la burbuja para OnPaint
            _rectBurbuja = new Rectangle(xBurbuja, 0, anchoBurbuja, altoBurbuja);
        }

        private Rectangle _rectBurbuja;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CalcularTamaño();
        }

        // ─────────────────────────────────────────────────────────────
        //  PINTADO
        // ─────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            if (_rectBurbuja.Width <= 0) return;

            Color bg1 = EsUsuario ? BgUsuario  : BgIA;
            Color bg2 = EsUsuario ? BgUsuario2 : BgIA2;

            var rect = new Rectangle(
                _rectBurbuja.X,
                _rectBurbuja.Y,
                _rectBurbuja.Width - 1,
                _rectBurbuja.Height - 1);

            // ── Fondo con gradiente sutil ────────────────────────────
            using (var path = CrearPathRedondeado(rect, RadioBurbuja))
            using (var brush = new LinearGradientBrush(
                rect, bg1, bg2,
                EsUsuario ? LinearGradientMode.BackwardDiagonal
                          : LinearGradientMode.ForwardDiagonal))
            {
                g.FillPath(brush, path);

                // Borde fino — esmeralda translúcido en usuario, gris en IA
                Color colorBorde = EsUsuario
                    ? Color.FromArgb(120, Emerald.R, Emerald.G, Emerald.B)
                    : Color.FromArgb(180, BorderCol.R, BorderCol.G, BorderCol.B);

                using (var pen = new Pen(colorBorde, 1f))
                    g.DrawPath(pen, path);
            }

            // ── Barra de acento lateral (sólo IA) ────────────────────
            if (!EsUsuario)
            {
                var rectAcento = new Rectangle(
                    _rectBurbuja.X, _rectBurbuja.Y + 8, 3, _rectBurbuja.Height - 16);
                using (var b = new SolidBrush(Emerald))
                    g.FillRectangle(b, rectAcento);
            }

            // ── Avatar circular con borde ────────────────────────────
            if (ImagenPerfil != null && _avatarPanel != null)
            {
                DibujarAvatar(g, ImagenPerfil,
                    _avatarPanel.Left, _avatarPanel.Top,
                    EsUsuario ? Emerald : BorderCol);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS DE PINTADO
        // ─────────────────────────────────────────────────────────────
        private static void DibujarAvatar(Graphics g, Image img, int x, int y, Color colorBorde)
        {
            var rect = new Rectangle(x, y, TamañoAvatar, TamañoAvatar);

            using (var clipPath = new GraphicsPath())
            {
                clipPath.AddEllipse(rect);
                g.SetClip(clipPath);
                g.DrawImage(img, rect);
                g.ResetClip();
            }

            using (var pen = new Pen(colorBorde, 2f))
                g.DrawEllipse(pen, rect);
        }

        private static GraphicsPath CrearPathRedondeado(Rectangle rect, int radio)
        {
            int d = radio * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ─────────────────────────────────────────────────────────────
        //  DISPOSE
        // ─────────────────────────────────────────────────────────────
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tooltip?.Dispose();
                _txtMensaje?.Dispose();
                _avatarPanel?.Dispose();
                _btnToggle?.Dispose();
                _lblHora?.Dispose();
                _btnCopiar?.Dispose();
                _timerTyping?.Stop();
                _timerTyping?.Dispose();
                _timerFormatoDebounce?.Stop();
                _timerFormatoDebounce?.Dispose();
                _timerLayoutDebounce?.Stop();
                _timerLayoutDebounce?.Dispose();
                foreach (var p in _previews) p.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
