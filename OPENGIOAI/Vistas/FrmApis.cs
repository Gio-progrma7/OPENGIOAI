using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmApis : Form
    {
        // ── Paleta dinámica ───────────────────────────────────────────────────
        private static Color BgDeep    => EmeraldTheme.BgDeep;
        private static Color BgSurface => EmeraldTheme.BgSurface;
        private static Color BgCard    => EmeraldTheme.BgCard;
        private static Color BgCardHi  => EmeraldTheme.IsDark
                                            ? ColorTranslator.FromHtml("#003d73")
                                            : ColorTranslator.FromHtml("#D6E8FF");
        private static Color BgInput   => EmeraldTheme.BgDeep;
        private static Color Emerald   => EmeraldTheme.Emerald500;
        private static Color Emerald4  => EmeraldTheme.Emerald400;
        private static Color Emerald9  => EmeraldTheme.Emerald900;
        private static Color TextMain  => EmeraldTheme.TextPrimary;
        private static Color TextSub   => EmeraldTheme.TextSecondary;
        private static Color TextMuted => EmeraldTheme.TextMuted;
        private static Color Border    => EmeraldTheme.IsDark
                                            ? ColorTranslator.FromHtml("#1a3a5c")
                                            : ColorTranslator.FromHtml("#C5D8F0");
        private static Color DangerCol => EmeraldTheme.Error;
        private static readonly Color WarnCol = ColorTranslator.FromHtml("#fbbf24");

        // ── Datos ─────────────────────────────────────────────────────────────
        private List<Api> _misApis = new();
        private bool _esNueva = true;
        private string _apiSeleccionada = "";

        // ── Layout principal ──────────────────────────────────────────────────
        private Panel pnlHeader = null!;
        private Label lblTitulo = null!, lblSubtitulo = null!;
        private TextBox txtFiltro = null!;
        private Button btnNueva = null!;

        private FlowLayoutPanelSuave pnlApis = null!;
        private Panel pnlEmpty = null!;

        // ── Editor lateral (slide-in) ─────────────────────────────────────────
        private Panel pnlEditor = null!;
        private Label lblModoEdicion = null!;
        private TextBox txtNombre = null!, txtKey = null!, txtDescripcion = null!;
        private Button btnGuardar = null!, btnCancelar = null!, btnCerrarEditor = null!;
        private CheckBox chkRevelar = null!;

        // ── Toast ─────────────────────────────────────────────────────────────
        private Panel pnlToast = null!;
        private Label lblToast = null!;
        private System.Windows.Forms.Timer _timerToast = null!;

        // ── Cache de tarjetas para filtrar sin recrear ────────────────────────
        private readonly Dictionary<string, CardApi> _cards = new();

        public FrmApis()
        {
            InitializeComponent();
            EmeraldTheme.ThemeChanged += OnTemaChanged;
            Disposed += (_, __) => EmeraldTheme.ThemeChanged -= OnTemaChanged;
            ConstruirUI();
        }

        private void OnTemaChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(OnTemaChanged); return; }
            ConstruirUI();
            Invalidate(true);
        }

        private void FrmApis_Load(object sender, EventArgs e)
        {
            CargarApis();
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCCIÓN DE UI
        // ════════════════════════════════════════════════════════════════════

        private void ConstruirUI()
        {
            // ── Header (top) ──────────────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = BgSurface,
                Padding = new Padding(28, 16, 28, 12)
            };

            lblTitulo = new Label
            {
                Text = "🔑  Credenciales",
                Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                ForeColor = Emerald4,
                AutoSize = true,
                Location = new Point(28, 14),
                BackColor = Color.Transparent
            };

            lblSubtitulo = new Label
            {
                Text = "Administra tus claves de API y tokens de acceso",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(30, 50),
                BackColor = Color.Transparent
            };

            txtFiltro = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                BackColor = BgInput,
                ForeColor = TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Width = 280,
                Location = new Point(pnlHeader.Width - 470, 28)
            };
            txtFiltro.GotFocus += (_, __) => txtFiltro.Invalidate();
            ConfigurarPlaceholder(txtFiltro, "🔍  Buscar credencial...");
            txtFiltro.TextChanged += (_, __) =>
            {
                if (txtFiltro.ForeColor == TextMuted) return; // placeholder activo
                AplicarFiltro(txtFiltro.Text);
            };

            btnNueva = new Button
            {
                Text = "+  Nueva",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = TextMain,
                BackColor = Emerald9,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(140, 32),
                Location = new Point(pnlHeader.Width - 168, 26),
                Cursor = Cursors.Hand
            };
            btnNueva.FlatAppearance.BorderColor = Emerald;
            btnNueva.FlatAppearance.BorderSize = 1;
            btnNueva.FlatAppearance.MouseOverBackColor = Emerald;
            btnNueva.Click += (_, __) => AbrirEditorNuevo();

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(txtFiltro);
            pnlHeader.Controls.Add(btnNueva);
            pnlHeader.Resize += (_, __) =>
            {
                txtFiltro.Location = new Point(pnlHeader.Width - 470, 28);
                btnNueva.Location  = new Point(pnlHeader.Width - 168, 26);
            };

            // Línea inferior del header
            var hairline = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Border
            };
            pnlHeader.Controls.Add(hairline);

            // ── Editor lateral (right, slide-in) ──────────────────────────────
            ConstruirEditor();

            // ── Empty state ───────────────────────────────────────────────────
            ConstruirEmptyState();

            // ── Grid de tarjetas ──────────────────────────────────────────────
            pnlApis = new FlowLayoutPanelSuave
            {
                Dock = DockStyle.Fill,
                BackColor = BgDeep,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(20, 20, 20, 20)
            };
            pnlApis.MouseWheel += PnlApis_MouseWheel;

            // ── Toast ─────────────────────────────────────────────────────────
            ConstruirToast();

            // ── Orden de docking (último agregado va detrás del Fill) ─────────
            Controls.Add(pnlApis);        // Fill
            Controls.Add(pnlEditor);      // Right
            Controls.Add(pnlHeader);      // Top
            Controls.Add(pnlEmpty);       // Centered (manual)
            Controls.Add(pnlToast);       // Manual

            pnlEmpty.BringToFront();
            pnlToast.BringToFront();
        }

        private void ConstruirEditor()
        {
            pnlEditor = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                BackColor = BgSurface,
                Padding = new Padding(24, 22, 24, 22),
                Visible = false
            };

            var hairLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 1,
                BackColor = Border
            };
            pnlEditor.Controls.Add(hairLeft);

            btnCerrarEditor = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(28, 28),
                Location = new Point(pnlEditor.Width - 52, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnCerrarEditor.FlatAppearance.BorderSize = 0;
            btnCerrarEditor.FlatAppearance.MouseOverBackColor = BgCardHi;
            btnCerrarEditor.Click += (_, __) => CerrarEditor();

            var lblTituloEditor = new Label
            {
                Text = "Credencial",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = Emerald4,
                AutoSize = true,
                Location = new Point(24, 22),
                BackColor = Color.Transparent
            };

            lblModoEdicion = new Label
            {
                Text = "Nueva credencial",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(25, 50),
                BackColor = Color.Transparent
            };

            // Campos
            int x = 24, y = 90, ancho = pnlEditor.Width - 48;

            var lblNombre = LabelCampo("Nombre", x, y);
            txtNombre = TextBoxOscuro(x, y + 22, ancho);

            y += 76;
            var lblKey = LabelCampo("Token / Key", x, y);
            txtKey = TextBoxOscuro(x, y + 22, ancho);
            txtKey.UseSystemPasswordChar = true;

            chkRevelar = new CheckBox
            {
                Text = "Mostrar valor",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(x, y + 56),
                Cursor = Cursors.Hand
            };
            chkRevelar.CheckedChanged += (_, __) => txtKey.UseSystemPasswordChar = !chkRevelar.Checked;

            y += 96;
            var lblDesc = LabelCampo("Descripción", x, y);
            txtDescripcion = TextBoxOscuro(x, y + 22, ancho, 90, multiline: true);

            y += 132;
            btnGuardar = new Button
            {
                Text = "💾  Guardar",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = TextMain,
                BackColor = Emerald9,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(170, 36),
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderColor = Emerald;
            btnGuardar.FlatAppearance.BorderSize = 1;
            btnGuardar.FlatAppearance.MouseOverBackColor = Emerald;
            btnGuardar.Click += (_, __) => GuardarOModificar();

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 36),
                Location = new Point(x + 180, y),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Border;
            btnCancelar.FlatAppearance.BorderSize = 1;
            btnCancelar.FlatAppearance.MouseOverBackColor = BgCardHi;
            btnCancelar.Click += (_, __) => CerrarEditor();

            pnlEditor.Controls.Add(lblTituloEditor);
            pnlEditor.Controls.Add(lblModoEdicion);
            pnlEditor.Controls.Add(lblNombre);
            pnlEditor.Controls.Add(txtNombre);
            pnlEditor.Controls.Add(lblKey);
            pnlEditor.Controls.Add(txtKey);
            pnlEditor.Controls.Add(chkRevelar);
            pnlEditor.Controls.Add(lblDesc);
            pnlEditor.Controls.Add(txtDescripcion);
            pnlEditor.Controls.Add(btnGuardar);
            pnlEditor.Controls.Add(btnCancelar);
            pnlEditor.Controls.Add(btnCerrarEditor);
        }

        private void ConstruirEmptyState()
        {
            pnlEmpty = new Panel
            {
                Size = new Size(420, 200),
                BackColor = Color.Transparent,
                Visible = false
            };

            var lblIcon = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI Emoji", 36f),
                ForeColor = Emerald9,
                AutoSize = true,
                Location = new Point(180, 0),
                BackColor = Color.Transparent
            };
            var lblMsg = new Label
            {
                Text = "Sin credenciales todavía",
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = TextSub,
                AutoSize = true,
                Location = new Point(120, 80),
                BackColor = Color.Transparent
            };
            var lblHint = new Label
            {
                Text = "Agrega tu primera API key para empezar",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(105, 110),
                BackColor = Color.Transparent
            };
            var btnPrimera = new Button
            {
                Text = "+  Agregar credencial",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = TextMain,
                BackColor = Emerald9,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 36),
                Location = new Point(110, 145),
                Cursor = Cursors.Hand
            };
            btnPrimera.FlatAppearance.BorderColor = Emerald;
            btnPrimera.FlatAppearance.BorderSize = 1;
            btnPrimera.FlatAppearance.MouseOverBackColor = Emerald;
            btnPrimera.Click += (_, __) => AbrirEditorNuevo();

            pnlEmpty.Controls.Add(lblIcon);
            pnlEmpty.Controls.Add(lblMsg);
            pnlEmpty.Controls.Add(lblHint);
            pnlEmpty.Controls.Add(btnPrimera);

            Resize += (_, __) => CentrarEmpty();
            CentrarEmpty();
        }

        private void CentrarEmpty()
        {
            if (pnlEmpty == null) return;
            int areaW = ClientSize.Width - (pnlEditor != null && pnlEditor.Visible ? pnlEditor.Width : 0);
            int areaH = ClientSize.Height - (pnlHeader?.Height ?? 0);
            pnlEmpty.Location = new Point(
                (areaW - pnlEmpty.Width) / 2,
                (pnlHeader?.Height ?? 0) + (areaH - pnlEmpty.Height) / 2);
        }

        private void ConstruirToast()
        {
            pnlToast = new Panel
            {
                Size = new Size(320, 50),
                BackColor = Emerald9,
                Visible = false,
                Padding = new Padding(16, 8, 16, 8)
            };

            lblToast = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = TextMain,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            pnlToast.Controls.Add(lblToast);

            _timerToast = new System.Windows.Forms.Timer { Interval = 2200 };
            _timerToast.Tick += (_, __) =>
            {
                _timerToast.Stop();
                pnlToast.Visible = false;
            };

            Resize += (_, __) => UbicarToast();
            UbicarToast();
        }

        private void UbicarToast()
        {
            if (pnlToast == null) return;
            int rightPad = (pnlEditor != null && pnlEditor.Visible ? pnlEditor.Width : 0) + 24;
            pnlToast.Location = new Point(
                ClientSize.Width - pnlToast.Width - rightPad,
                ClientSize.Height - pnlToast.Height - 24);
        }

        private void MostrarToast(string mensaje, Color? color = null)
        {
            lblToast.Text = "  " + mensaje;
            pnlToast.BackColor = color ?? Emerald9;
            UbicarToast();
            pnlToast.Visible = true;
            pnlToast.BringToFront();
            _timerToast.Stop();
            _timerToast.Start();
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERS DE UI
        // ════════════════════════════════════════════════════════════════════

        private static Label LabelCampo(string txt, int x, int y) => new Label
        {
            Text = txt,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(x, y),
            BackColor = Color.Transparent
        };

        private TextBox TextBoxOscuro(int x, int y, int w, int h = 30, bool multiline = false)
        {
            var tb = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                BackColor = BgInput,
                ForeColor = TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Multiline = multiline
            };
            return tb;
        }

        private void ConfigurarPlaceholder(TextBox tb, string placeholder)
        {
            tb.Text = placeholder;
            tb.ForeColor = TextMuted;

            tb.Enter += (_, __) =>
            {
                if (tb.Text == placeholder)
                {
                    tb.Text = "";
                    tb.ForeColor = TextMain;
                }
            };
            tb.Leave += (_, __) =>
            {
                if (string.IsNullOrEmpty(tb.Text))
                {
                    tb.Text = placeholder;
                    tb.ForeColor = TextMuted;
                }
            };
        }

        private void PnlApis_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (sender is not Panel panel) return;
            int amount = 30;
            int newValue = panel.VerticalScroll.Value - (e.Delta / 120) * amount;
            newValue = Math.Max(panel.VerticalScroll.Minimum, newValue);
            newValue = Math.Min(panel.VerticalScroll.Maximum, newValue);
            panel.VerticalScroll.Value = newValue;
        }

        // ════════════════════════════════════════════════════════════════════
        //  DATOS
        // ════════════════════════════════════════════════════════════════════

        private void CargarApis()
        {
            string ruta = RutasProyecto.ObtenerRutaListApis();
            _misApis = JsonManager.Leer<Api>(ruta) ?? new List<Api>();
            RenderizarTarjetas();
        }

        private void RenderizarTarjetas()
        {
            pnlApis.SuspendLayout();
            pnlApis.Controls.Clear();
            _cards.Clear();

            foreach (var api in _misApis)
            {
                var card = new CardApi(api, this);
                _cards[api.Nombre ?? Guid.NewGuid().ToString()] = card;
                pnlApis.Controls.Add(card);
            }

            pnlApis.ResumeLayout(true);
            ActualizarEmpty();
        }

        private void ActualizarEmpty()
        {
            bool vacio = _misApis.Count == 0;
            bool sinResultados = _misApis.Count > 0 && _cards.Values.All(c => !c.Visible);
            pnlEmpty.Visible = vacio || sinResultados;
            if (sinResultados)
            {
                ((Label)pnlEmpty.Controls[1]).Text = "Sin resultados";
                ((Label)pnlEmpty.Controls[2]).Text = "Prueba con otra búsqueda";
            }
            else
            {
                ((Label)pnlEmpty.Controls[1]).Text = "Sin credenciales todavía";
                ((Label)pnlEmpty.Controls[2]).Text = "Agrega tu primera API key para empezar";
            }
            CentrarEmpty();
        }

        private void AplicarFiltro(string filtro)
        {
            pnlApis.SuspendLayout();
            string f = (filtro ?? "").Trim();
            foreach (var card in _cards.Values)
            {
                bool match = string.IsNullOrEmpty(f) ||
                             (card.Api.Nombre?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (card.Api.Descripcion?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false);
                card.Visible = match;
            }
            pnlApis.ResumeLayout(true);
            ActualizarEmpty();
        }

        // ════════════════════════════════════════════════════════════════════
        //  EDITOR
        // ════════════════════════════════════════════════════════════════════

        private void AbrirEditorNuevo()
        {
            _esNueva = true;
            _apiSeleccionada = "";
            lblModoEdicion.Text = "Nueva credencial";
            lblModoEdicion.ForeColor = TextMuted;
            txtNombre.Text = "";
            txtKey.Text = "";
            txtDescripcion.Text = "";
            chkRevelar.Checked = false;
            txtKey.UseSystemPasswordChar = true;
            btnGuardar.Text = "💾  Guardar";
            MostrarEditor();
            txtNombre.Focus();
        }

        private void AbrirEditorModificar(Api api)
        {
            _esNueva = false;
            _apiSeleccionada = api.Nombre ?? "";
            lblModoEdicion.Text = $"Editando: {api.Nombre}";
            lblModoEdicion.ForeColor = Emerald4;
            txtNombre.Text = api.Nombre ?? "";
            txtKey.Text = api.key ?? "";
            txtDescripcion.Text = api.Descripcion ?? "";
            chkRevelar.Checked = false;
            txtKey.UseSystemPasswordChar = true;
            btnGuardar.Text = "💾  Modificar";
            MostrarEditor();
            txtNombre.Focus();
            txtNombre.SelectAll();
        }

        private void MostrarEditor()
        {
            pnlEditor.Visible = true;
            UbicarToast();
            CentrarEmpty();
        }

        private void CerrarEditor()
        {
            pnlEditor.Visible = false;
            UbicarToast();
            CentrarEmpty();
        }

        private void GuardarOModificar()
        {
            string nombre = (txtNombre.Text ?? "").Trim();
            string key    = (txtKey.Text ?? "").Trim();
            string desc   = (txtDescripcion.Text ?? "").Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(key))
            {
                MostrarToast("⚠ Nombre y key son obligatorios", DangerCol);
                return;
            }

            if (_esNueva)
            {
                if (_misApis.Any(a => string.Equals(a.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
                {
                    MostrarToast("⚠ Ya existe una credencial con ese nombre", DangerCol);
                    return;
                }

                JsonManager.Agregar(RutasProyecto.ObtenerRutaListApis(), new Api
                {
                    Nombre = nombre,
                    key = key,
                    Descripcion = desc
                });
                MostrarToast("✔ Credencial agregada");
            }
            else
            {
                JsonManager.Modificar<Api>(
                    RutasProyecto.ObtenerRutaListApis(),
                    u => u.Nombre == _apiSeleccionada,
                    u => { u.Nombre = nombre; u.key = key; u.Descripcion = desc; });
                MostrarToast("✔ Credencial actualizada");
            }

            CargarApis();
            CerrarEditor();
        }

        private void EliminarApi(Api api)
        {
            var dr = MessageBox.Show(
                $"¿Eliminar la credencial \"{api.Nombre}\"?\nEsta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (dr != DialogResult.Yes) return;

            JsonManager.Eliminar<Api>(RutasProyecto.ObtenerRutaListApis(),
                u => u.Nombre == api.Nombre);

            CargarApis();
            MostrarToast($"🗑 \"{api.Nombre}\" eliminada");
        }

        // ════════════════════════════════════════════════════════════════════
        //  CARD
        // ════════════════════════════════════════════════════════════════════

        private sealed class CardApi : PanelSuave
        {
            public Api Api { get; }
            private readonly FrmApis _owner;
            private readonly Label _lblKey;
            private bool _revelado;

            private const int CardWidth  = 320;
            private const int CardHeight = 168;

            public CardApi(Api api, FrmApis owner)
            {
                Api = api;
                _owner = owner;

                Width = CardWidth;
                Height = CardHeight;
                Margin = new Padding(8);
                BackColor = BgCard;
                Cursor = Cursors.Default;

                // Barra acento izquierda
                var accent = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(3, CardHeight),
                    BackColor = Emerald
                };

                // Status dot + nombre
                var dot = new Panel
                {
                    Location = new Point(18, 18),
                    Size = new Size(8, 8),
                    BackColor = Emerald4
                };
                dot.Paint += (_, e) =>
                {
                    using var b = new System.Drawing.SolidBrush(Emerald4);
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(b, 0, 0, 7, 7);
                };

                var lblNombre = new Label
                {
                    Text = api.Nombre ?? "(sin nombre)",
                    Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                    ForeColor = TextMain,
                    Location = new Point(34, 12),
                    Size = new Size(CardWidth - 50, 22),
                    AutoEllipsis = true,
                    BackColor = Color.Transparent
                };

                // Descripción
                var lblDesc = new Label
                {
                    Text = string.IsNullOrWhiteSpace(api.Descripcion) ? "Sin descripción" : api.Descripcion,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = string.IsNullOrWhiteSpace(api.Descripcion) ? Border : TextMuted,
                    Location = new Point(18, 40),
                    Size = new Size(CardWidth - 36, 30),
                    AutoEllipsis = true,
                    BackColor = Color.Transparent
                };

                // Chip "KEY"
                var chip = new Label
                {
                    Text = "KEY",
                    Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                    ForeColor = TextMain,
                    BackColor = Emerald9,
                    Location = new Point(18, 78),
                    Size = new Size(36, 18),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Key enmascarada
                _lblKey = new Label
                {
                    Text = MaskKey(api.key ?? ""),
                    Font = new Font("Consolas", 9f),
                    ForeColor = TextSub,
                    Location = new Point(60, 78),
                    Size = new Size(CardWidth - 130, 18),
                    AutoEllipsis = true,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // 👁 toggle reveal
                var btnReveal = ChipBoton("👁", new Point(CardWidth - 64, 76), TextMuted);
                btnReveal.Click += (_, __) =>
                {
                    _revelado = !_revelado;
                    _lblKey.Text = _revelado ? (api.key ?? "") : MaskKey(api.key ?? "");
                    _lblKey.ForeColor = _revelado ? Emerald4 : TextSub;
                };

                // 📋 copiar
                var btnCopy = ChipBoton("📋", new Point(CardWidth - 36, 76), TextMuted);
                btnCopy.Click += (_, __) =>
                {
                    if (!string.IsNullOrEmpty(api.key))
                    {
                        try { Clipboard.SetText(api.key); _owner.MostrarToast("📋 Key copiada al portapapeles"); }
                        catch { _owner.MostrarToast("⚠ No se pudo copiar", DangerCol); }
                    }
                };

                // Separador
                var sep = new Panel
                {
                    Location = new Point(14, 112),
                    Size = new Size(CardWidth - 28, 1),
                    BackColor = Border
                };

                // Botones acción
                var btnEditar = AccionBoton("✏  Editar", Emerald4, new Point(14, 124));
                btnEditar.Click += (_, __) => _owner.AbrirEditorModificar(api);

                var btnEliminar = AccionBoton("🗑  Eliminar", DangerCol, new Point(CardWidth - 130, 124));
                btnEliminar.Click += (_, __) => _owner.EliminarApi(api);

                Controls.Add(accent);
                Controls.Add(dot);
                Controls.Add(lblNombre);
                Controls.Add(lblDesc);
                Controls.Add(chip);
                Controls.Add(_lblKey);
                Controls.Add(btnReveal);
                Controls.Add(btnCopy);
                Controls.Add(sep);
                Controls.Add(btnEditar);
                Controls.Add(btnEliminar);

                // Hover en la tarjeta — propagamos a children para que no se rompa
                EventHandler enter = (_, __) => BackColor = BgCardHi;
                EventHandler leave = (_, __) =>
                {
                    var p = PointToClient(Cursor.Position);
                    if (!ClientRectangle.Contains(p)) BackColor = BgCard;
                };
                MouseEnter += enter;
                MouseLeave += leave;
                foreach (Control c in Controls)
                {
                    if (c is Panel || c is Label) // botones tienen su propio hover
                    {
                        c.MouseEnter += enter;
                        c.MouseLeave += leave;
                    }
                }
            }

            private static Button ChipBoton(string txt, Point loc, Color color) => new Button
            {
                Text = txt,
                Font = new Font("Segoe UI Emoji", 9f),
                ForeColor = color,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(24, 22),
                Location = loc,
                Cursor = Cursors.Hand,
                TabStop = false,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = BgCardHi }
            };

            private static Button AccionBoton(string txt, Color color, Point loc)
            {
                var b = new Button
                {
                    Text = txt,
                    Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                    ForeColor = color,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(116, 30),
                    Location = loc,
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                b.FlatAppearance.BorderColor = Color.FromArgb(60, color);
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, color);
                return b;
            }

            private static string MaskKey(string k)
            {
                if (string.IsNullOrEmpty(k)) return "(vacía)";
                if (k.Length <= 8) return new string('•', k.Length);
                return new string('•', 12) + "  " + k.Substring(k.Length - 4);
            }
        }
    }
}
