using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.ComponentModel;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// Burbuja de chat estilo moderno (WhatsApp/Telegram-inspired).
    /// Soporta texto enriquecido, rutas/URLs clicables, preview de imágenes
    /// y avatar circular con borde de color según el remitente.
    /// </summary>
    public class BurbujaChat : Control
    {
        // ─────────────────────────────────────────────────────────────
        //  Constantes de diseño
        // ─────────────────────────────────────────────────────────────
        private const int TamañoAvatar = 42;
        private const int RadioBurbuja = 18;
        private const int PaddingBurbuja = 14;
        private const int MargenAvatar = 6;
        private const int PreviewSize = 110;
        private const int PreviewGap = 6;
        private const int SombraAlfa = 45;
        private const int SombraDesplaz = 3;

        // ─────────────────────────────────────────────────────────────
        //  Paleta de colores
        // ─────────────────────────────────────────────────────────────
        private static readonly Color ColorUsuario = Color.FromArgb(0, 102, 161);
        private static readonly Color ColorUsuarioClaro = Color.FromArgb(0, 122, 185);
        private static readonly Color ColorIA = Color.FromArgb(32, 41, 46);
        private static readonly Color ColorIAClaro = Color.FromArgb(45, 56, 63);
        private static readonly Color ColorTexto = Color.FromArgb(240, 240, 240);
        private static readonly Color ColorEnlace = Color.FromArgb(255, 180, 72);
        private static readonly Color ColorAvatarBordeU = Color.FromArgb(0, 140, 210);
        private static readonly Color ColorAvatarBordeIA = Color.FromArgb(80, 100, 110);

        // ─────────────────────────────────────────────────────────────
        //  Expresión regular para rutas y URLs
        // ─────────────────────────────────────────────────────────────
        private static readonly Regex RegexRuta = new Regex(
            @"([A-Za-z]:\\(?:[^\n""'<>|*?\r\\/:]+\\)*[^\n""'<>|*?\r\/:\\]*)|(https?:\/\/[^\s\n""'<>]+)",
            RegexOptions.Compiled);

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
        private RichTextBox _txtMensaje;
        private Panel _avatarPanel;
        private ToolTip _tooltip;
        private List<Panel> _previews = new List<Panel>();

        // ─────────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────────
        public BurbujaChat(string texto, bool esUsuario, int anchoMaximo, Image avatarImg)
        {
            EsUsuario = esUsuario;
            Text = texto;
            _anchoMaximo = anchoMaximo;

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;
            Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(15, 23, 42);
            Margin = new Padding(4, 3, 4, 3);

            _tooltip = new ToolTip { AutomaticDelay = 400, ShowAlways = true };

            Color colorFondo = EsUsuario ? ColorUsuario : ColorIA;

            // ── RichTextBox ──────────────────────────────────────────
            _txtMensaje = new RichTextBox
            {
                Text = texto,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                Multiline = true,
                TabStop = false,
                Font = Font,
                ForeColor = ColorTexto,
                BackColor = colorFondo,
                Cursor = Cursors.IBeam,
                WordWrap = true
            };

            _txtMensaje.MouseMove += OnTxtMouseMove;
            _txtMensaje.MouseClick += OnTxtMouseClick;

            AplicarFormatoEnlaces();

            // ── Avatar ───────────────────────────────────────────────
            if (avatarImg != null)
            {
                ImagenPerfil = avatarImg;
                _avatarPanel = new Panel
                {
                    Size = new Size(TamañoAvatar, TamañoAvatar),
                    BackColor = Color.Transparent
                };

                var pb = new PictureBox
                {
                    Image = avatarImg,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };

                _avatarPanel.Controls.Add(pb);
                Controls.Add(_avatarPanel);
            }

            Controls.Add(_txtMensaje);

            // ── Previews de imágenes ─────────────────────────────────
            CargarPreviewImagenes(texto, colorFondo);
        }

        // ─────────────────────────────────────────────────────────────
        //  ENLACES — formato y eventos
        // ─────────────────────────────────────────────────────────────
        private void AplicarFormatoEnlaces()
        {
            foreach (Match m in RegexRuta.Matches(_txtMensaje.Text))
            {
                _txtMensaje.Select(m.Index, m.Length);
                _txtMensaje.SelectionColor = ColorEnlace;
                _txtMensaje.SelectionFont =
                    new Font(_txtMensaje.Font, FontStyle.Underline);
            }
            _txtMensaje.SelectionStart = 0;
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
            ExtensionesImagen.Any(ext =>
                ruta.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

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
                    Image imagen = Image.FromFile(rutaLocal);

                    Panel contenedor = new Panel
                    {
                        BackColor = colorFondo,
                        Cursor = Cursors.Hand,
                        Tag = rutaLocal,
                        Size = new Size(PreviewSize, PreviewSize)
                    };

                    // Borde redondeado simulado con padding
                    contenedor.Padding = new Padding(3);

                    var pb = new PictureBox
                    {
                        Image = imagen,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Dock = DockStyle.Fill,
                        Cursor = Cursors.Hand,
                        BackColor = Color.Transparent
                    };

                    pb.Click += (s, ev) => MostrarPreviewPopup(rutaLocal);
                    contenedor.Click += (s, ev) => MostrarPreviewPopup(rutaLocal);
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
                    Text = Path.GetFileName(rutaImagen),
                    Size = new Size(800, 580),
                    MinimumSize = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = Color.FromArgb(18, 18, 20),
                    FormBorderStyle = FormBorderStyle.Sizable,
                    Icon = SystemIcons.Application
                };

                var pb = new PictureBox
                {
                    Image = imagen,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };

                Panel barraInferior = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 44,
                    BackColor = Color.FromArgb(28, 28, 32),
                    Padding = new Padding(8, 5, 8, 5)
                };

                var btnAbrir = new Button
                {
                    Text = "📂  Abrir en visor",
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    BackColor = ColorUsuario,
                    Height = 32,
                    Width = 150,
                    Left = 8,
                    Top = 6,
                    Cursor = Cursors.Hand
                };
                btnAbrir.FlatAppearance.BorderSize = 0;
                btnAbrir.Click += (s, e) => AbrirRuta(rutaImagen);

                var lblInfo = new Label
                {
                    Text = $"{imagen.Width} × {imagen.Height} px",
                    ForeColor = Color.FromArgb(160, 160, 160),
                    AutoSize = true,
                    Top = 13,
                    Left = 170
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
        /// Actualiza el texto de la burbuja en tiempo real (usado para streaming de salida de scripts).
        /// Seguro para llamar desde cualquier hilo — hace InvokeRequired internamente.
        /// </summary>
        public void ActualizarTexto(string nuevoTexto)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(() => ActualizarTexto(nuevoTexto)); return; }

            if (nuevoTexto == Text) return;
            Text = nuevoTexto;
            _txtMensaje.Text = nuevoTexto;
            AplicarFormatoEnlaces();
            CalcularTamaño();
            // NO llamar Parent.PerformLayout() aquí — cambiar Width/Height ya
            // dispara el re-layout del FlowLayoutPanel automáticamente.
            // Llamarlo explícitamente en cada token causaba redibujado en cascada.
            Invalidate();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            CalcularTamaño();
        }

        private void CalcularTamaño()
        {
            if (!IsHandleCreated) return;

            // Ancho disponible para el texto (descuenta avatar + paddings)
            int anchoTexto = Math.Min(
                _anchoMaximo - TamañoAvatar - MargenAvatar * 2 - PaddingBurbuja * 2,
                _anchoMaximo - TamañoAvatar - 30
            );

            Size tamTexto = TextRenderer.MeasureText(
                Text,
                Font,
                new Size(anchoTexto, int.MaxValue),
                TextFormatFlags.WordBreak
            );

            // Calcular espacio para previews en grilla
            int previewsAlto = 0;
            if (_previews.Count > 0)
            {
                int cols = Math.Max(1, anchoTexto / (PreviewSize + PreviewGap));
                int rows = (int)Math.Ceiling(_previews.Count / (double)cols);
                previewsAlto = rows * (PreviewSize + PreviewGap) + PreviewGap;
            }

            int burbujaAncho = tamTexto.Width + PaddingBurbuja * 2 + TamañoAvatar + MargenAvatar;
            int burbujaAlto = Math.Max(tamTexto.Height + PaddingBurbuja * 2, TamañoAvatar + 10)
                             + previewsAlto;

            Width = Math.Min(burbujaAncho, _anchoMaximo);
            Height = burbujaAlto + SombraDesplaz + 4; // espacio para sombra

            // ── Posición horizontal del bloque de contenido ──────────
            int xContenido = EsUsuario
                ? MargenAvatar
                : TamañoAvatar + MargenAvatar;

            int anchoContenido = Width - TamañoAvatar - MargenAvatar * 2;

            // ── Posicionar RichTextBox ───────────────────────────────
            _txtMensaje.SetBounds(
                xContenido + PaddingBurbuja,
                PaddingBurbuja,
                anchoContenido - PaddingBurbuja * 2,
                tamTexto.Height + 4
            );

            // ── Posicionar avatar ────────────────────────────────────
            if (_avatarPanel != null)
            {
                int xAvatar = EsUsuario
                    ? Width - TamañoAvatar - MargenAvatar
                    : MargenAvatar;
                int yAvatar = Height - TamañoAvatar - SombraDesplaz - 2;
                _avatarPanel.SetBounds(xAvatar, yAvatar, TamañoAvatar, TamañoAvatar);
            }

            // ── Posicionar previews en grilla ────────────────────────
            if (_previews.Count > 0)
            {
                int cols = Math.Max(1, anchoContenido / (PreviewSize + PreviewGap));
                int xStart = xContenido + PaddingBurbuja;
                int yStart = PaddingBurbuja + tamTexto.Height + 10;
                int col = 0, row = 0;

                foreach (var panel in _previews)
                {
                    int px = xStart + col * (PreviewSize + PreviewGap);
                    int py = yStart + row * (PreviewSize + PreviewGap);
                    panel.SetBounds(px, py, PreviewSize, PreviewSize);

                    col++;
                    if (col >= cols) { col = 0; row++; }
                }
            }
        }

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
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            Color colorFondo = EsUsuario ? ColorUsuario : ColorIA;

            // ── Calcular rectángulo de burbuja ───────────────────────
            int xBurbuja = EsUsuario
                ? MargenAvatar
                : TamañoAvatar + MargenAvatar;

            int anchoBurbuja = Width - TamañoAvatar - MargenAvatar * 2;
            int altoBurbuja = Height - SombraDesplaz - 4;

            Rectangle rectBurbuja = new Rectangle(
                xBurbuja, 0, anchoBurbuja - 1, altoBurbuja - 1);

            // ── Sombra suave ─────────────────────────────────────────
            DibujarSombra(g, rectBurbuja);

            // ── Degradado de fondo ───────────────────────────────────
            using (var path = CrearPathRedondeado(rectBurbuja, RadioBurbuja))
            {
                Color colorClaro = EsUsuario ? ColorUsuarioClaro : ColorIAClaro;

                using (var brush = new LinearGradientBrush(
                    rectBurbuja, colorClaro, colorFondo,
                    EsUsuario ? LinearGradientMode.BackwardDiagonal
                              : LinearGradientMode.ForwardDiagonal))
                {
                    g.FillPath(brush, path);
                }

                // Borde sutil
                using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
                    g.DrawPath(pen, path);
            }

            // ── Cola de burbuja ──────────────────────────────────────
            DibujarCola(g, rectBurbuja, colorFondo);

            // ── Avatar circular con borde ────────────────────────────
            if (ImagenPerfil != null)
            {
                int xAvatar = EsUsuario
                    ? Width - TamañoAvatar - MargenAvatar
                    : MargenAvatar;
                int yAvatar = Height - TamañoAvatar - SombraDesplaz - 2;

                DibujarAvatar(g, ImagenPerfil, xAvatar, yAvatar,
                    EsUsuario ? ColorAvatarBordeU : ColorAvatarBordeIA);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS DE PINTADO
        // ─────────────────────────────────────────────────────────────
        private void DibujarSombra(Graphics g, Rectangle rect)
        {
            for (int i = SombraDesplaz; i >= 1; i--)
            {
                int alfa = (int)(SombraAlfa * (1.0 - (i - 1.0) / SombraDesplaz));
                var r = new Rectangle(
                    rect.X + i, rect.Y + i,
                    rect.Width - 1, rect.Height - 1);

                using (var path = CrearPathRedondeado(r, RadioBurbuja))
                using (var brush = new SolidBrush(Color.FromArgb(alfa, 0, 0, 0)))
                    g.FillPath(brush, path);
            }
        }

        private void DibujarCola(Graphics g, Rectangle rect, Color color)
        {
            // Cola triangular pequeña en la esquina inferior
            const int sz = 10;
            Point tip, p1, p2;

            if (EsUsuario)
            {
                int x = rect.Right;
                int y = rect.Bottom - RadioBurbuja;
                tip = new Point(x + sz, y + sz);
                p1 = new Point(x, y - 4);
                p2 = new Point(x, y + 4);
            }
            else
            {
                int x = rect.Left;
                int y = rect.Bottom - RadioBurbuja;
                tip = new Point(x - sz, y + sz);
                p1 = new Point(x, y - 4);
                p2 = new Point(x, y + 4);
            }

            using (var brush = new SolidBrush(color))
                g.FillPolygon(brush, new[] { tip, p1, p2 });
        }

        private static void DibujarAvatar(Graphics g, Image img, int x, int y, Color colorBorde)
        {
            var rect = new Rectangle(x, y, TamañoAvatar, TamañoAvatar);

            // Sombra del avatar
            using (var shadowPath = new GraphicsPath())
            {
                shadowPath.AddEllipse(x + 2, y + 2, TamañoAvatar, TamañoAvatar);
                using (var b = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                    g.FillPath(b, shadowPath);
            }

            // Clip circular para la imagen
            using (var clipPath = new GraphicsPath())
            {
                clipPath.AddEllipse(rect);
                g.SetClip(clipPath);
                g.DrawImage(img, rect);
                g.ResetClip();
            }

            // Borde de color
            using (var pen = new Pen(colorBorde, 2.5f))
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
                foreach (var p in _previews) p.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}