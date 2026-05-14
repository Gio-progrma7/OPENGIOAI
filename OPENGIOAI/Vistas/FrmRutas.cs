using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmRutas : Form
    {
        // ── Paleta dinámica ───────────────────────────────────────────────────
        private static Color BgDeep    => EmeraldTheme.BgDeep;
        private static Color BgSurface => EmeraldTheme.BgSurface;
        private static Color BgCard    => EmeraldTheme.BgCard;
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

        // ── Datos ─────────────────────────────────────────────────────────────
        private List<Archivo> _misRutas = new();
        private bool _esNueva = true;
        private string _rutaSeleccionada = "";

        // ── Layout principal ──────────────────────────────────────────────────
        private Panel pnlHeader = null!;
        private Label lblTitulo = null!, lblSubtitulo = null!;
        private TextBox txtFiltro = null!;
        private Button btnNueva = null!;

        private FlowLayoutPanelSuave pnlGrid = null!;
        private Panel pnlEmpty = null!;

        // ── Editor lateral (slide-in) ─────────────────────────────────────────
        private Panel pnlEditor = null!;
        private Label lblModoEdicion = null!;
        private TextBox txtRutaPath = null!, txtDescripcion = null!;
        private Button btnGuardar = null!, btnCancelar = null!, btnCerrarEditor = null!;

        // ── Toast ─────────────────────────────────────────────────────────────
        private Panel pnlToast = null!;
        private Label lblToast = null!;
        private System.Windows.Forms.Timer _timerToast = null!;

        // ── Cache de tarjetas ─────────────────────────────────────────────────
        private readonly Dictionary<string, CardRuta> _cards = new();

        public FrmRutas()
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

        private void FrmRutas_Load(object sender, EventArgs e)
        {
            CargarRutas();
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCCIÓN DE UI
        // ════════════════════════════════════════════════════════════════════

        private void ConstruirUI()
        {
            SuspendLayout();
            try
            {
                BackColor = BgDeep;
                ForeColor = TextMain;
                Controls.Clear();

                // ── Header ──────────────────────────────────────────────────
                pnlHeader = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 88,
                    BackColor = BgSurface,
                    Padding = new Padding(28, 16, 28, 12)
                };

                lblTitulo = new Label
                {
                    Text = "📁  Rutas de Trabajo",
                    Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                    ForeColor = Emerald4,
                    AutoSize = true,
                    Location = new Point(28, 14),
                    BackColor = Color.Transparent
                };

                lblSubtitulo = new Label
                {
                    Text = "Gestiona tus directorios y archivos de referencia rápida",
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
                ConfigurarPlaceholder(txtFiltro, "🔍  Buscar ruta...");
                txtFiltro.TextChanged += (_, __) => {
                    if (txtFiltro.ForeColor != TextMuted) AplicarFiltro(txtFiltro.Text);
                };

                btnNueva = new Button
                {
                    Text = "+  Nueva Ruta",
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
                pnlHeader.Resize += (_, __) => {
                    txtFiltro.Location = new Point(pnlHeader.Width - 470, 28);
                    btnNueva.Location = new Point(pnlHeader.Width - 168, 26);
                };

                var hairline = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Border };
                pnlHeader.Controls.Add(hairline);

                // ── Editor lateral ──
                ConstruirEditor();

                // ── Empty state ──
                ConstruirEmptyState();

                // ── Grid ──
                pnlGrid = new FlowLayoutPanelSuave
                {
                    Dock = DockStyle.Fill,
                    BackColor = BgDeep,
                    AutoScroll = true,
                    Padding = new Padding(20)
                };

                // ── Toast ──
                ConstruirToast();

                Controls.Add(pnlGrid);
                Controls.Add(pnlEditor);
                Controls.Add(pnlHeader);
                Controls.Add(pnlEmpty);
                Controls.Add(pnlToast);

                pnlEmpty.BringToFront();
                pnlToast.BringToFront();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void ConstruirEditor()
        {
            pnlEditor = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                BackColor = BgSurface,
                Padding = new Padding(24),
                Visible = false
            };

            var hairLeft = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Border };
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
            btnCerrarEditor.Click += (_, __) => CerrarEditor();

            var lblTituloEditor = new Label
            {
                Text = "Ruta de Trabajo",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = Emerald4,
                AutoSize = true,
                Location = new Point(24, 22)
            };

            lblModoEdicion = new Label
            {
                Text = "Nueva configuración",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(25, 50)
            };

            int x = 24, y = 90, ancho = pnlEditor.Width - 48;

            pnlEditor.Controls.Add(LabelCampo("Ruta del archivo / directorio", x, y));
            txtRutaPath = TextBoxOscuro(x, y + 22, ancho);
            
            // Botón para examinar
            var btnExaminar = new Button {
                Text = "...",
                Size = new Size(30, 23),
                Location = new Point(x + ancho - 35, y + 23),
                FlatStyle = FlatStyle.Flat,
                BackColor = Emerald9,
                ForeColor = TextMain,
                Cursor = Cursors.Hand
            };
            btnExaminar.FlatAppearance.BorderSize = 0;
            btnExaminar.Click += (_, __) => {
                using (var fbd = new FolderBrowserDialog()) {
                    if (fbd.ShowDialog() == DialogResult.OK) txtRutaPath.Text = fbd.SelectedPath;
                }
            };
            pnlEditor.Controls.Add(btnExaminar);
            pnlEditor.Controls.Add(txtRutaPath);

            y += 76;
            pnlEditor.Controls.Add(LabelCampo("Descripción", x, y));
            txtDescripcion = TextBoxOscuro(x, y + 22, ancho, 90, multiline: true);
            pnlEditor.Controls.Add(txtDescripcion);

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
            btnCancelar.Click += (_, __) => CerrarEditor();

            pnlEditor.Controls.Add(lblTituloEditor);
            pnlEditor.Controls.Add(lblModoEdicion);
            pnlEditor.Controls.Add(btnGuardar);
            pnlEditor.Controls.Add(btnCancelar);
            pnlEditor.Controls.Add(btnCerrarEditor);
        }

        private void ConstruirEmptyState()
        {
            pnlEmpty = new Panel { Size = new Size(420, 200), BackColor = Color.Transparent, Visible = false };
            var lblIcon = new Label { Text = "📁", Font = new Font("Segoe UI Emoji", 36f), ForeColor = Emerald9, AutoSize = true, Location = new Point(180, 0) };
            var lblMsg = new Label { Text = "Sin rutas configuradas", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), ForeColor = TextSub, AutoSize = true, Location = new Point(125, 80) };
            var btnPrimera = new Button { Text = "+  Agregar mi primera ruta", Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold), ForeColor = TextMain, BackColor = Emerald9, FlatStyle = FlatStyle.Flat, Size = new Size(220, 36), Location = new Point(100, 130), Cursor = Cursors.Hand };
            btnPrimera.FlatAppearance.BorderColor = Emerald;
            btnPrimera.Click += (_, __) => AbrirEditorNuevo();
            
            pnlEmpty.Controls.Add(lblIcon);
            pnlEmpty.Controls.Add(lblMsg);
            pnlEmpty.Controls.Add(btnPrimera);
            Resize += (_, __) => CentrarEmpty();
            CentrarEmpty();
        }

        private void CentrarEmpty()
        {
            if (pnlEmpty == null) return;
            int areaW = ClientSize.Width - (pnlEditor?.Visible == true ? pnlEditor.Width : 0);
            int areaH = ClientSize.Height - (pnlHeader?.Height ?? 0);
            pnlEmpty.Location = new Point((areaW - pnlEmpty.Width) / 2, (pnlHeader?.Height ?? 0) + (areaH - pnlEmpty.Height) / 2);
        }

        private void ConstruirToast()
        {
            pnlToast = new Panel { Size = new Size(320, 50), BackColor = Emerald9, Visible = false, Padding = new Padding(16, 8, 16, 8) };
            lblToast = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold), ForeColor = TextMain, TextAlign = ContentAlignment.MiddleLeft };
            pnlToast.Controls.Add(lblToast);
            _timerToast = new System.Windows.Forms.Timer { Interval = 2500 };
            _timerToast.Tick += (_, __) => { _timerToast.Stop(); pnlToast.Visible = false; };
            Resize += (_, __) => UbicarToast();
            UbicarToast();
        }

        private void UbicarToast()
        {
            if (pnlToast == null) return;
            int rightPad = (pnlEditor?.Visible == true ? pnlEditor.Width : 0) + 24;
            pnlToast.Location = new Point(ClientSize.Width - pnlToast.Width - rightPad, ClientSize.Height - pnlToast.Height - 24);
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
        //  HELPERS UI
        // ════════════════════════════════════════════════════════════════════
        private static Label LabelCampo(string txt, int x, int y) => new Label { Text = txt, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = TextMuted, AutoSize = true, Location = new Point(x, y), BackColor = Color.Transparent };
        private TextBox TextBoxOscuro(int x, int y, int w, int h = 30, bool multiline = false) => new TextBox { Font = new Font("Segoe UI", 10f), BackColor = BgInput, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle, Location = new Point(x, y), Size = new Size(w, h), Multiline = multiline };
        private void ConfigurarPlaceholder(TextBox tb, string placeholder) {
            tb.Text = placeholder; tb.ForeColor = TextMuted;
            tb.Enter += (_, __) => { if (tb.Text == placeholder) { tb.Text = ""; tb.ForeColor = TextMain; } };
            tb.Leave += (_, __) => { if (string.IsNullOrEmpty(tb.Text)) { tb.Text = placeholder; tb.ForeColor = TextMuted; } };
        }

        // ════════════════════════════════════════════════════════════════════
        //  LÓGICA DE DATOS
        // ════════════════════════════════════════════════════════════════════
        private void CargarRutas()
        {
            _misRutas = JsonManager.Leer<Archivo>(RutasProyecto.ObtenerRutaListArchivos()) ?? new List<Archivo>();
            RenderizarTarjetas();
        }

        private void RenderizarTarjetas()
        {
            pnlGrid.SuspendLayout();
            pnlGrid.Controls.Clear();
            _cards.Clear();

            foreach (var ruta in _misRutas)
            {
                var card = new CardRuta(ruta, this);
                _cards[ruta.Ruta ?? Guid.NewGuid().ToString()] = card;
                pnlGrid.Controls.Add(card);
            }

            pnlGrid.ResumeLayout(true);
            ActualizarEmpty();
        }

        private void ActualizarEmpty()
        {
            bool vacio = _misRutas.Count == 0;
            bool sinResultados = _misRutas.Count > 0 && _cards.Values.All(c => !c.Visible);
            pnlEmpty.Visible = vacio || sinResultados;
            CentrarEmpty();
        }

        private void AplicarFiltro(string filtro)
        {
            string f = (filtro ?? "").Trim();
            foreach (var card in _cards.Values)
            {
                bool match = string.IsNullOrEmpty(f) ||
                             (card.Ruta.Ruta?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (card.Ruta.Descripcion?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false);
                card.Visible = match;
            }
            ActualizarEmpty();
        }

        private void AbrirEditorNuevo()
        {
            _esNueva = true; _rutaSeleccionada = "";
            lblModoEdicion.Text = "Nueva ruta de trabajo";
            txtRutaPath.Text = ""; txtDescripcion.Text = "";
            txtRutaPath.Enabled = true;
            MostrarEditor();
        }

        public void AbrirEditorModificar(Archivo archivo)
        {
            _esNueva = false; _rutaSeleccionada = archivo.Ruta ?? "";
            lblModoEdicion.Text = "Editando ruta";
            txtRutaPath.Text = archivo.Ruta ?? "";
            txtDescripcion.Text = archivo.Descripcion ?? "";
            txtRutaPath.Enabled = false;
            MostrarEditor();
        }

        private void MostrarEditor() { pnlEditor.Visible = true; CentrarEmpty(); UbicarToast(); }
        private void CerrarEditor() { pnlEditor.Visible = false; CentrarEmpty(); UbicarToast(); }

        private void GuardarOModificar()
        {
            string path = txtRutaPath.Text.Trim();
            string desc = txtDescripcion.Text.Trim();

            if (string.IsNullOrEmpty(path)) { MostrarToast("⚠ La ruta es obligatoria", DangerCol); return; }

            if (_esNueva) {
                JsonManager.Agregar(RutasProyecto.ObtenerRutaListArchivos(), new Archivo { Ruta = path, Descripcion = desc });
                MostrarToast("✔ Ruta agregada correctamente");
            } else {
                JsonManager.Modificar<Archivo>(RutasProyecto.ObtenerRutaListArchivos(), a => a.Ruta == _rutaSeleccionada, a => a.Descripcion = desc);
                MostrarToast("✔ Ruta actualizada");
            }

            CargarRutas();
            CerrarEditor();
        }

        public void EliminarRuta(Archivo archivo)
        {
            if (MessageBox.Show($"¿Eliminar la ruta \"{Path.GetFileName(archivo.Ruta)}\"?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                JsonManager.Eliminar<Archivo>(RutasProyecto.ObtenerRutaListArchivos(), a => a.Ruta == archivo.Ruta);
                CargarRutas();
                MostrarToast("🗑 Ruta eliminada");
            }
        }

        public void AbrirExplorador(string ruta)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ruta) { UseShellExecute = true }); }
            catch { MostrarToast("⚠ No se pudo abrir la ruta", DangerCol); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  CARD PERSONALIZADA
        // ════════════════════════════════════════════════════════════════════
        private sealed class CardRuta : Panel
        {
            public Archivo Ruta { get; }
            private readonly FrmRutas _owner;

            public CardRuta(Archivo archivo, FrmRutas owner)
            {
                Ruta = archivo; _owner = owner;
                Width = 320; Height = 140; Margin = new Padding(8); BackColor = BgCard;
                
                string ext = Path.GetExtension(archivo.Ruta ?? "").ToLower();
                (string icono, Color accent) = ext switch {
                    ".cs" or ".py" or ".js" => ("💻", Color.FromArgb(16, 185, 129)),
                    ".pdf" or ".doc" or ".docx" => ("📄", Color.FromArgb(239, 68, 68)),
                    ".xlsx" or ".csv" => ("📊", Color.FromArgb(34, 197, 94)),
                    _ => (Directory.Exists(archivo.Ruta ?? "") ? "📁" : "📄", Emerald)
                };

                // Barra acento
                var pnlAcc = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent };
                Controls.Add(pnlAcc);

                var lblIcon = new Label { Text = icono, Font = new Font("Segoe UI Emoji", 20f), Location = new Point(15, 15), AutoSize = true };
                var lblName = new Label { Text = Path.GetFileName(archivo.Ruta) ?? "Sin nombre", Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), ForeColor = TextMain, Location = new Point(55, 12), Width = 250, AutoEllipsis = true };
                var lblPath = new Label { Text = archivo.Ruta, Font = new Font("Consolas", 7.5f), ForeColor = TextMuted, Location = new Point(57, 36), Width = 245, AutoEllipsis = true };
                var lblDesc = new Label { Text = archivo.Descripcion, Font = new Font("Segoe UI", 8.5f), ForeColor = TextSub, Location = new Point(18, 65), Width = 280, Height = 32, AutoEllipsis = true };

                // Botones acción
                var btnEdit = CrearBtnAccion("✏", 230, 105);
                btnEdit.Click += (_, __) => _owner.AbrirEditorModificar(archivo);
                
                var btnOpen = CrearBtnAccion("📂", 260, 105);
                btnOpen.Click += (_, __) => _owner.AbrirExplorador(archivo.Ruta);
                
                var btnDel = CrearBtnAccion("🗑", 290, 105, DangerCol);
                btnDel.Click += (_, __) => _owner.EliminarRuta(archivo);

                Controls.Add(lblIcon); Controls.Add(lblName); Controls.Add(lblPath); Controls.Add(lblDesc);
                Controls.Add(btnEdit); Controls.Add(btnOpen); Controls.Add(btnDel);

                this.RedondearPanel(borderRadius: 12);
                this.MouseEnter += (_, __) => BackColor = EmeraldTheme.IsDark ? Color.FromArgb(35, 35, 37) : Color.FromArgb(245, 245, 247);
                this.MouseLeave += (_, __) => BackColor = BgCard;
            }

            private Button CrearBtnAccion(string icon, int x, int y, Color? color = null) => new Button {
                Text = icon, Size = new Size(26, 26), Location = new Point(x, y), FlatStyle = FlatStyle.Flat,
                ForeColor = color ?? TextMuted, Cursor = Cursors.Hand, Font = new Font("Segoe UI Emoji", 9f)
            };
        }
    }
}
