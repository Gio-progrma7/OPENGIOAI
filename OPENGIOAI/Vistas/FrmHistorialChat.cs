using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    /// <summary>
    /// Ventana flotante que muestra el historial de conversaciones guardadas.
    /// Cuando el usuario quiere cargar una sesión, dispara OnSesionSeleccionada.
    /// </summary>
    public partial class FrmHistorialChat : Form
    {
        // ── Paleta ────────────────────────────────────────────────────────────
        private static Color BgDeep    => EmeraldTheme.BgDeep;
        private static Color BgCard    => EmeraldTheme.BgCard;
        private static Color BgSurface => EmeraldTheme.BgSurface;
        private static Color Accent    => EmeraldTheme.Emerald400;
        private static Color TextMain  => EmeraldTheme.TextPrimary;
        private static Color TextSub   => EmeraldTheme.TextSecondary;
        private static Color TextMuted => EmeraldTheme.TextMuted;
        private static Color Border    => EmeraldTheme.IsDark
                                           ? ColorTranslator.FromHtml("#1a3a5c")
                                           : ColorTranslator.FromHtml("#C5D8F0");

        // ── Evento que escucha FrmMandos ──────────────────────────────────────
        public event Action<SesionConversacion>? OnSesionSeleccionada;

        // ── Estado ────────────────────────────────────────────────────────────
        private static FrmHistorialChat? _instancia;
        private SesionIndiceItem? _itemSeleccionado;
        private DateTime _fechaSeleccionada = DateTime.Today;

        // ── Controles ─────────────────────────────────────────────────────────
        private Panel            _pnlIzq     = null!;
        private Panel            _pnlDer     = null!;
        private FlowLayoutPanel  _flpDias    = null!;
        private FlowLayoutPanel  _flpSesiones = null!;
        private Panel            _pnlPreview = null!;
        private Label            _lblPreview = null!;
        private Button           _btnCargar  = null!;
        private Button           _btnEliminar = null!;
        private TextBox          _txtBuscar  = null!;

        // ═════════════════════════════════════════════════════════════════════
        //  Singleton flotante
        // ═════════════════════════════════════════════════════════════════════

        public static FrmHistorialChat ObtenerInstancia(Form owner)
        {
            if (_instancia == null || _instancia.IsDisposed)
            {
                _instancia = new FrmHistorialChat();
                _instancia.Owner = owner;
            }
            return _instancia;
        }

        public static void MostrarOTraerAlFrente(Form owner,
            Action<SesionConversacion>? onSeleccionada = null)
        {
            // Owner siempre debe ser un form top-level.
            // FrmMandos es embebido (TopLevel=false), así que subimos al ancestro real.
            Form topOwner = (owner.TopLevelControl as Form) ?? owner;

            var f = ObtenerInstancia(topOwner);
            if (onSeleccionada != null)
                f.OnSesionSeleccionada = onSeleccionada;

            if (!f.Visible) f.Show(topOwner);
            else            f.BringToFront();
            f.Refrescar();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Constructor
        // ═════════════════════════════════════════════════════════════════════

        public FrmHistorialChat()
        {
            InitializeComponent();
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize     = new Size(700, 500);
            ConstruirUI();

            EmeraldTheme.ThemeChanged += RefrescarTema;
            FormClosed += (_, __) => EmeraldTheme.ThemeChanged -= RefrescarTema;

            Refrescar();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  UI
        // ═════════════════════════════════════════════════════════════════════

        private void ConstruirUI()
        {
            BackColor = BgDeep;

            // ── Header ──
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = BgSurface
            };
            var lblTitulo = new Label
            {
                Text      = "📚  Historial de Conversaciones",
                Font      = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize  = true,
                Location  = new Point(16, 14),
                BackColor = Color.Transparent
            };
            _txtBuscar = new TextBox
            {
                PlaceholderText = "🔍  Buscar…",
                Font            = new Font("Segoe UI", 9f),
                BackColor       = BgCard,
                ForeColor       = TextMain,
                BorderStyle     = BorderStyle.FixedSingle,
                Size            = new Size(200, 26),
                Anchor          = AnchorStyles.Top | AnchorStyles.Right,
                Location        = new Point(pnlHeader.Width - 220, 13)
            };
            _txtBuscar.TextChanged += (_, __) => FiltrarSesiones();
            pnlHeader.Resize += (_, __) =>
                _txtBuscar.Location = new Point(pnlHeader.Width - 220, 13);
            var hairBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Border };

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(_txtBuscar);
            pnlHeader.Controls.Add(hairBottom);

            // ── Split: izquierda (días + sesiones) | derecha (preview) ──
            // Panel1MinSize/Panel2MinSize y SplitterDistance se aplican en Load,
            // cuando el control ya tiene su tamaño real (evita InvalidOperationException).
            var split = new SplitContainer
            {
                Dock          = DockStyle.Fill,
                SplitterWidth = 4,
                BackColor     = Border
            };

            // Panel izquierdo
            _pnlIzq = new Panel { Dock = DockStyle.Fill, BackColor = BgDeep };
            var lblDias = new Label
            {
                Text      = "FECHAS",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Border,
                AutoSize  = true,
                Location  = new Point(12, 10),
                BackColor = Color.Transparent
            };
            _flpDias = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = BgDeep,
                Padding       = new Padding(8, 40, 8, 8)
            };
            _pnlIzq.Controls.Add(_flpDias);
            _pnlIzq.Controls.Add(lblDias);

            // Panel derecho
            _pnlDer = new Panel { Dock = DockStyle.Fill, BackColor = BgDeep };
            var lblSes = new Label
            {
                Text      = "SESIONES",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Border,
                AutoSize  = true,
                Location  = new Point(12, 10),
                BackColor = Color.Transparent
            };
            _flpSesiones = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = BgDeep,
                Padding       = new Padding(8, 40, 8, 100)
            };

            // Preview en la parte inferior del panel derecho
            _pnlPreview = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 130,
                BackColor = BgSurface,
                Padding   = new Padding(12, 8, 12, 8)
            };
            var hairPreview = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Border };
            _lblPreview = new Label
            {
                Dock      = DockStyle.Fill,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = TextMuted,
                Text      = "Selecciona una sesión para ver la vista previa",
                AutoSize  = false,
                BackColor = Color.Transparent
            };

            var pnlBotones = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 38,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0)
            };
            _btnCargar   = CrearBtn("📂  Cargar sesión", Accent);
            _btnEliminar = CrearBtn("🗑️  Eliminar", EmeraldTheme.Error);
            _btnCargar.Click   += BtnCargar_Click;
            _btnEliminar.Click += BtnEliminar_Click;
            _btnCargar.Enabled   = false;
            _btnEliminar.Enabled = false;
            pnlBotones.Controls.Add(_btnCargar);
            pnlBotones.Controls.Add(_btnEliminar);

            _pnlPreview.Controls.Add(_lblPreview);
            _pnlPreview.Controls.Add(hairPreview);
            _pnlPreview.Controls.Add(pnlBotones);

            _pnlDer.Controls.Add(_flpSesiones);
            _pnlDer.Controls.Add(lblSes);
            _pnlDer.Controls.Add(_pnlPreview);

            // Ensamblar split
            split.Panel1.Controls.Add(_pnlIzq);
            split.Panel2.Controls.Add(_pnlDer);

            // MinSizes y SplitterDistance se aplican en Load cuando el control
            // ya tiene tamaño real — antes lanzaría InvalidOperationException.
            Load += (_, __) =>
            {
                try
                {
                    split.Panel1MinSize  = 160;
                    split.Panel2MinSize  = 240;
                    split.SplitterDistance = 200;
                }
                catch { }
            };

            Controls.Add(split);
            Controls.Add(pnlHeader);

            RefrescarTema();
        }

        private static Button CrearBtn(string text, Color fore) => new Button
        {
            Text      = text,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = fore,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            AutoSize  = true,
            Margin    = new Padding(4, 4, 4, 0),
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderColor = fore, BorderSize = 1 }
        };

        // ═════════════════════════════════════════════════════════════════════
        //  Carga de datos
        // ═════════════════════════════════════════════════════════════════════

        public void Refrescar()
        {
            var fechas = ConversationStorage.FechasDisponibles();

            _flpDias.SuspendLayout();
            _flpDias.Controls.Clear();

            foreach (var fecha in fechas)
            {
                var f = fecha;
                bool esHoy = f.Date == DateTime.Today;
                var btn = new Button
                {
                    Text      = esHoy ? $"📅  Hoy ({f:dd MMM})" : $"   {f:dd MMM yyyy}",
                    Font      = new Font("Segoe UI", 8.5f, f == _fechaSeleccionada ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = f == _fechaSeleccionada ? Accent : TextMain,
                    BackColor = f == _fechaSeleccionada ? BgCard : Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    AutoSize  = false,
                    Size      = new Size(164, 30),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor    = Cursors.Hand,
                    Padding   = new Padding(8, 0, 0, 0),
                    Margin    = new Padding(0, 1, 0, 1),
                    Tag       = f
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = BgCard;
                btn.Click += (_, __) =>
                {
                    _fechaSeleccionada = f;
                    Refrescar();
                };
                _flpDias.Controls.Add(btn);
            }

            if (fechas.Count == 0)
            {
                _flpDias.Controls.Add(new Label
                {
                    Text      = "Sin conversaciones guardadas",
                    Font      = new Font("Segoe UI", 8.5f),
                    ForeColor = TextMuted,
                    AutoSize  = false,
                    Size      = new Size(160, 50),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                });
            }

            _flpDias.ResumeLayout();
            CargarSesionesDia(_fechaSeleccionada);
        }

        private void CargarSesionesDia(DateTime fecha)
        {
            var items = ConversationStorage.LeerIndice(fecha)
                        .OrderByDescending(i => i.Inicio)
                        .ToList();
            MostrarItemsSesiones(items);
        }

        private List<SesionIndiceItem> _itemsActuales = new();

        private void MostrarItemsSesiones(List<SesionIndiceItem> items)
        {
            _itemsActuales = items;
            _flpSesiones.SuspendLayout();
            _flpSesiones.Controls.Clear();

            if (items.Count == 0)
            {
                _flpSesiones.Controls.Add(new Label
                {
                    Text      = "No hay sesiones en esta fecha",
                    Font      = new Font("Segoe UI", 8.5f),
                    ForeColor = TextMuted,
                    AutoSize  = false,
                    Size      = new Size(320, 50),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                });
                _flpSesiones.ResumeLayout();
                return;
            }

            foreach (var item in items)
            {
                var card = CrearCardSesion(item);
                _flpSesiones.Controls.Add(card);
            }
            _flpSesiones.ResumeLayout();
        }

        private void FiltrarSesiones()
        {
            string q = _txtBuscar.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(q)) { MostrarItemsSesiones(_itemsActuales); return; }
            var filtrado = _itemsActuales
                .Where(i => i.Titulo.ToLower().Contains(q) || i.Modelo.ToLower().Contains(q))
                .ToList();
            MostrarItemsSesiones(filtrado);
        }

        private Panel CrearCardSesion(SesionIndiceItem item)
        {
            bool esSeleccionada = _itemSeleccionado?.SesionId == item.SesionId;

            var pnl = new Panel
            {
                Size      = new Size(_flpSesiones.Width - 20, 62),
                BackColor = esSeleccionada ? BgCard : BgDeep,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 0, 4),
                Padding   = new Padding(10, 6, 10, 6),
                Tag       = item
            };

            var lblTitulo = new Label
            {
                Text      = item.Titulo,
                Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = esSeleccionada ? Accent : TextMain,
                AutoSize  = false,
                Size      = new Size(pnl.Width - 20, 20),
                Location  = new Point(10, 8),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand
            };
            var lblMeta = new Label
            {
                Text      = $"{item.Inicio:HH:mm}  ·  {item.TotalTurnos} turnos  ·  {item.Modelo}",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = TextMuted,
                AutoSize  = false,
                Size      = new Size(pnl.Width - 20, 16),
                Location  = new Point(10, 30),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand
            };

            if (esSeleccionada)
            {
                using var g = pnl.CreateGraphics();
                pnl.Paint += (_, pe) =>
                {
                    using var pen = new Pen(Accent, 2f);
                    pe.Graphics.DrawLine(pen, 0, 0, 0, pnl.Height);
                };
            }

            Action seleccionar = () =>
            {
                _itemSeleccionado = item;
                MostrarPreview(item);
                // Redibujar solo esta fecha para actualizar selección visual
                CargarSesionesDia(_fechaSeleccionada);
            };

            pnl.Click      += (_, __) => seleccionar();
            lblTitulo.Click += (_, __) => seleccionar();
            lblMeta.Click   += (_, __) => seleccionar();

            pnl.Controls.Add(lblTitulo);
            pnl.Controls.Add(lblMeta);
            return pnl;
        }

        private void MostrarPreview(SesionIndiceItem item)
        {
            var ses = ConversationStorage.LeerSesion(item.SesionId);
            if (ses == null) { _lblPreview.Text = "(no se pudo cargar)"; return; }

            string durStr = ses.Duracion.TotalMinutes >= 1
                ? $"{(int)ses.Duracion.TotalMinutes}m"
                : $"{(int)ses.Duracion.TotalSeconds}s";

            string primer = ses.Turnos.Count > 0
                ? Acortar(ses.Turnos[0].Instruccion, 120)
                : "(sin turnos)";

            _lblPreview.Text =
                $"🕐 {item.Inicio:dd/MM/yyyy HH:mm}  ·  {durStr}  ·  {item.TotalTurnos} turnos\n" +
                $"🤖 {item.Modelo}  ·  ~{item.TotalTokens:N0} tokens\n\n" +
                $"❝ {primer}";

            _btnCargar.Enabled   = true;
            _btnEliminar.Enabled = true;
        }

        // ── Botones ───────────────────────────────────────────────────────────

        private void BtnCargar_Click(object? sender, EventArgs e)
        {
            if (_itemSeleccionado == null) return;
            var ses = ConversationStorage.LeerSesion(_itemSeleccionado.SesionId);
            if (ses == null)
            {
                MessageBox.Show("No se pudo cargar la sesión.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            OnSesionSeleccionada?.Invoke(ses);
            Close();
        }

        private void BtnEliminar_Click(object? sender, EventArgs e)
        {
            if (_itemSeleccionado == null) return;
            if (MessageBox.Show(
                    $"¿Eliminar la sesión \"{_itemSeleccionado.Titulo}\"?\nEsta acción no se puede deshacer.",
                    "Confirmar eliminación",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ConversationStorage.Eliminar(_itemSeleccionado.SesionId, _fechaSeleccionada);
            _itemSeleccionado    = null;
            _lblPreview.Text     = "Selecciona una sesión para ver la vista previa";
            _btnCargar.Enabled   = false;
            _btnEliminar.Enabled = false;
            Refrescar();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Tema
        // ═════════════════════════════════════════════════════════════════════

        private void RefrescarTema()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { Invoke(RefrescarTema); return; }
            BackColor = BgDeep;
            Refrescar();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Helper
        // ═════════════════════════════════════════════════════════════════════

        private static string Acortar(string s, int max) =>
            s.Length <= max ? s : s[..max] + "…";
    }
}
