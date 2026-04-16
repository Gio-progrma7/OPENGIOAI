using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosTelegram;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmAutomatizaciones : Form
    {
        // ── Colores ───────────────────────────────────────────────────────────
        private static readonly Color BgDeep     = ColorTranslator.FromHtml("#050505");
        private static readonly Color BgSurface  = ColorTranslator.FromHtml("#0a0a0a");
        private static readonly Color BgCard     = ColorTranslator.FromHtml("#0f1117");
        private static readonly Color Emerald    = ColorTranslator.FromHtml("#10b981");
        private static readonly Color Emerald4   = ColorTranslator.FromHtml("#34d399");
        private static readonly Color Emerald9   = ColorTranslator.FromHtml("#064e3b");
        private static readonly Color TextMain   = ColorTranslator.FromHtml("#f0fdf4");
        private static readonly Color TextSub    = ColorTranslator.FromHtml("#a7f3d0");
        private static readonly Color TextMuted  = ColorTranslator.FromHtml("#6ee7b7");
        private static readonly Color ErrorColor = ColorTranslator.FromHtml("#f87171");
        private static readonly Color WarnColor  = ColorTranslator.FromHtml("#fbbf24");

        // ── Credenciales ──────────────────────────────────────────────────────
        private readonly ConfiguracionClient _config;
        private List<Api> _listaApis = new();
        private string Modelo      => _config?.Mimodelo?.Modelos ?? "";
        private string ApiKey      => _config?.Mimodelo?.ApiKey  ?? "";
        private string RutaTrabajo => _config?.MiArchivo?.Ruta   ?? RutasProyecto.ObtenerRutaScripts();
        private Servicios Agente   => _config?.Mimodelo?.Agente  ?? Servicios.Gemenni;
        private string ClavesApis  => Utils.ObtenerNombresApis(_listaApis);

        // ── Estado ────────────────────────────────────────────────────────────
        private List<Automatizacion>     _lista        = new();
        private Automatizacion?          _autoActual   = null;
        private NodoVisualControl?       _nodoEditando = null;
        private CancellationTokenSource? _cts          = null;
        private bool                     _ejecutando   = false;

        // ── Streaming ─────────────────────────────────────────────────────────
        private readonly System.Windows.Forms.Timer _timerStreaming = new() { Interval = 120 };
        private readonly StringBuilder _bufferLog     = new();
        private volatile bool          _streamingPend = false;

        // ── Redimensionar Log ─────────────────────────────────────────────────
        private int _logResizeStartY = -1;
        private int _logResizeStartH = -1;

        // ── Panel credenciales ────────────────────────────────────────────────
        private Panel           pnlCredenciales = null!;
        private FlowLayoutPanel flpChips        = null!;

        // ── Controles ─────────────────────────────────────────────────────────
        private Panel               pnlIzquierdo = null!, pnlDerecho = null!, pnlToolbar = null!;
        private Panel               pnlEditor = null!, pnlLog = null!, pnlBarraIA = null!;
        private Panel               pnlCanvasWrapper = null!;  // wrapper scrollable del canvas
        private FlowLayoutPanel     flpLista     = null!;
        private CanvasAutomatizacion canvas      = null!;
        private Label               lblZoom      = null!;
        private RichTextBox         rtbLog       = null!;
        private TextBox  txtNLInput    = null!;
        private Button   btnCrearIA    = null!, btnNueva = null!, btnGuardar = null!;
        private Button   btnEjecutar   = null!, btnDetener = null!, btnAddNodo = null!, btnToggleLog = null!;
        private Button   btnEliminarAuto = null!;
        private Label    lblTituloAuto = null!, lblInfoConfig = null!, lblEstadoIA = null!;
        // Editor
        private TextBox  edtTitulo = null!, edtDescripcion = null!, edtInstruccion = null!;
        private ComboBox cmbTipoNodo = null!;
        private Button   btnGenScript = null!, btnCerrarEditor = null!;
        private Label    lblScriptStatus = null!, lblResultadoNodo = null!;
        // Conexiones en editor
        private CheckedListBox chkConexiones = null!;
        private Label lblConexiones = null!;
        // Error interactivo
        private Panel    pnlErrorFix = null!;
        private RichTextBox rtbErrorDetalle = null!;
        private TextBox  txtFixInput = null!;
        private Button   btnReintentarFix = null!, btnSaltarError = null!;
        private TaskCompletionSource<string?>? _tcsErrorFix = null;
        private NodoAutomatizacion? _nodoConError = null;
        // Schedule — controles del panel rediseñado
        private Panel          pnlSchedule        = null!;
        private string         _tipoProgActual     = "manual";
        private Button[]       _btnsTipoProg       = Array.Empty<Button>();
        private Button[]       _btnsDiaSemana      = Array.Empty<Button>();
        private Panel          pnlConfigTipo       = null!;   // zona dinámica según tipo
        private DateTimePicker dtpHora             = null!;
        private DateTimePicker dtpHoraInicio       = null!;
        private DateTimePicker dtpHoraFin          = null!;
        private DateTimePicker dtpFechaUnica        = null!;
        private NumericUpDown  nudIntervalo         = null!;
        private Label          lblSchedulePreview   = null!;
        private Label          lblProximaEjecucion  = null!;
        private Label          lblModoEjecucion     = null!;
        private Label          lblScheduleStatus    = null!;
        private Label          lblEstadoEjecucion   = null!;   // indicador de estado en tiempo real
        private Button         btnAplicarSchedule   = null!;
        private Button         btnDesactivarSched   = null!;

        // ══════════════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════════════
        public FrmAutomatizaciones(ConfiguracionClient config)
        {
            InitializeComponent();
            _config = config ?? new ConfiguracionClient
            {
                Mimodelo  = new Modelo(),
                MiArchivo = new Archivo { Ruta = RutasProyecto.ObtenerRutaScripts() }
            };
            _timerStreaming.Tick += (_, _) =>
            {
                if (!_streamingPend) return;
                _streamingPend = false;
                FlushBufferAlLog();
            };
            ConstruirUI();
            CargarLista();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UI
        // ══════════════════════════════════════════════════════════════════════
        private void ConstruirUI()
        {
            Text = "Automatizaciones"; BackColor = BgDeep; ForeColor = TextMain;
            Font = new Font("Segoe UI", 9.5f); MinimumSize = new Size(1000, 620);

            // ── Toolbar ───────────────────────────────────────────────────────
            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BgSurface };
            pnlToolbar.Paint += PintarBordeInf;

            lblTituloAuto = new Label
            {
                Text = "⚡ Automatizaciones", ForeColor = Emerald4,
                Font = new Font("Segoe UI Semibold", 12f),
                AutoSize = false, Width = 390, Height = 24,
                Location = new Point(16, 10), AutoEllipsis = true
            };
            lblInfoConfig = new Label
            {
                Text = ObtenerTextoConfig(),
                ForeColor = Color.FromArgb(90, 110, 200, 140),
                Font = new Font("Segoe UI", 7.5f), AutoSize = true, Location = new Point(18, 35)
            };

            btnNueva       = Btn("+ Nueva",      Emerald,   96);
            btnGuardar     = Btn("💾 Guardar",   Emerald9,  96);
            btnAddNodo     = Btn("＋ Nodo",      BgCard,    90);
            btnEjecutar    = Btn("▶ Ejecutar",   Emerald,   100);
            btnDetener     = Btn("■ Detener",    ErrorColor,96);
            btnEliminarAuto= Btn("🗑 Eliminar",  ErrorColor,100);
            btnToggleLog   = Btn("📋 Log",       BgCard,    76);
            btnDetener.Enabled = false;

            // Controles de zoom (se conectan al canvas después de que este se crea)
            lblZoom = new Label
            {
                Text = "100%", ForeColor = TextMuted, Font = new Font("Segoe UI", 8f),
                AutoSize = false, Width = 42, TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(8, 4, 0, 0)
            };
            var btnZoomOut = new Button
            {
                Text = "−", Size = new Size(26, 26), FlatStyle = FlatStyle.Flat,
                BackColor = BgCard, ForeColor = TextMuted, Font = new Font("Segoe UI", 12f),
                Cursor = Cursors.Hand, Margin = new Padding(2, 4, 0, 0)
            };
            btnZoomOut.FlatAppearance.BorderSize = 0;
            var btnZoomIn = new Button
            {
                Text = "+", Size = new Size(26, 26), FlatStyle = FlatStyle.Flat,
                BackColor = BgCard, ForeColor = TextMuted, Font = new Font("Segoe UI", 12f),
                Cursor = Cursors.Hand, Margin = new Padding(2, 4, 0, 0)
            };
            btnZoomIn.FlatAppearance.BorderSize = 0;
            btnZoomOut.Click += (_, _) => { canvas.ZoomOut(); lblZoom.Text = $"{(int)(canvas.Zoom * 100)}%"; };
            btnZoomIn.Click  += (_, _) => { canvas.ZoomIn();  lblZoom.Text = $"{(int)(canvas.Zoom * 100)}%"; };

            var fp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 12, 4, 0)
            };
            fp.Controls.AddRange(new Control[]
                { btnNueva, btnGuardar, btnAddNodo, btnEjecutar, btnDetener, btnEliminarAuto, btnToggleLog,
                  btnZoomOut, lblZoom, btnZoomIn });

            // Panel izquierdo del toolbar: contiene título e info sin solaparse con los botones
            var pnlTitleBar = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = Color.Transparent };
            lblTituloAuto.Location = new Point(14, 9);
            lblTituloAuto.Width    = 234;
            lblInfoConfig.Location = new Point(16, 34);
            pnlTitleBar.Controls.Add(lblTituloAuto);
            pnlTitleBar.Controls.Add(lblInfoConfig);

            // fp (Fill) debe tener index menor que pnlTitleBar (Left) para que el dock sea correcto
            pnlToolbar.Controls.AddRange(new Control[] { fp, pnlTitleBar });

            // ── Panel izquierdo ───────────────────────────────────────────────
            pnlIzquierdo = new Panel { Dock = DockStyle.Left, Width = 265, BackColor = BgSurface };
            pnlIzquierdo.Paint += PintarBordeDer;
            var lblLT = new Label
            {
                Text = "  ⚡  MIS AUTOMATIZACIONES",
                ForeColor = Color.FromArgb(80, 167, 243, 208),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold), Dock = DockStyle.Top, Height = 36,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0),
                BackColor = ColorTranslator.FromHtml("#060e0a")
            };
            lblLT.Paint += PintarBordeInf;
            flpLista = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true, BackColor = Color.Transparent,
                Padding = new Padding(8, 4, 8, 8)
            };
            pnlIzquierdo.Controls.Add(flpLista);
            pnlIzquierdo.Controls.Add(lblLT);

            // ── Panel derecho ─────────────────────────────────────────────────
            pnlDerecho = new Panel { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#080f1a") };
            // Canvas de tamaño fijo grande — el wrapper provee el scroll
            canvas = new CanvasAutomatizacion { Size = new Size(4500, 2800), Location = new Point(0, 0) };
            canvas.NodoSeleccionado += (_, n) => { if (n == null) pnlEditor.Visible = false; else SeleccionarNodoEditar(n); };
            canvas.ConexionCreada   += (_, args) => OnConexionCreada(args.origen, args.destino);

            // Panel scrollable que contiene el canvas
            pnlCanvasWrapper = new Panel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = ColorTranslator.FromHtml("#080f1a")
            };
            pnlCanvasWrapper.Controls.Add(canvas);

            // Log
            pnlLog = new Panel { Dock = DockStyle.Bottom, Height = 180, BackColor = ColorTranslator.FromHtml("#030810"), Visible = false };
            pnlLog.Paint += PintarBordeSup;
            var lblLogT = new Label
            {
                Text = "  📋  LOG DE EJECUCIÓN",
                ForeColor = Color.FromArgb(90, 167, 243, 208),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0), BackColor = ColorTranslator.FromHtml("#050c18")
            };
            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml("#030810"),
                ForeColor = TextMain, Font = new Font("Consolas", 9f), BorderStyle = BorderStyle.None,
                ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Vertical
            };
            var btnLimpLog = new Button
            {
                Text = "Limpiar", Dock = DockStyle.Right, Width = 70, FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent, ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f), Cursor = Cursors.Hand
            };
            btnLimpLog.FlatAppearance.BorderSize = 0;
            btnLimpLog.Click += (_, _) => { rtbLog.Clear(); _bufferLog.Clear(); };
            lblLogT.Controls.Add(btnLimpLog);
            pnlLog.Controls.Add(rtbLog);
            pnlLog.Controls.Add(lblLogT);

            // Grip redimensionable — arrastra hacia arriba/abajo para cambiar el alto del log
            var logGrip = new Panel
            {
                Dock = DockStyle.Top, Height = 6,
                BackColor = ColorTranslator.FromHtml("#050c18"),
                Cursor = Cursors.HSplit
            };
            logGrip.Paint += (_, e) =>
            {
                int cx = logGrip.Width / 2;
                using var pen = new Pen(Color.FromArgb(90, 52, 211, 153), 1f);
                e.Graphics.DrawLine(pen, cx - 28, 2, cx + 28, 2);
                e.Graphics.DrawLine(pen, cx - 18, 4, cx + 18, 4);
            };
            logGrip.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                _logResizeStartY = Cursor.Position.Y;
                _logResizeStartH = pnlLog.Height;
            };
            logGrip.MouseMove += (_, e) =>
            {
                if (_logResizeStartY < 0) return;
                int delta = _logResizeStartY - Cursor.Position.Y;
                pnlLog.Height = Math.Max(80, Math.Min(700, _logResizeStartH + delta));
            };
            logGrip.MouseUp += (_, _) => _logResizeStartY = -1;
            pnlLog.Controls.Add(logGrip);

            // Barra NL
            pnlBarraIA = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = BgSurface, Padding = new Padding(12, 10, 12, 10) };
            pnlBarraIA.Paint += PintarBordeSup;
            txtNLInput = new TextBox
            {
                PlaceholderText = "✨  Ej: «Captura pantalla y envíalo al correo x@y.com a las 8am todos los días»  (Ctrl+Enter)",
                BackColor = BgCard, ForeColor = TextMain, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f), Dock = DockStyle.Fill
            };
            txtNLInput.KeyDown += (_, e) => { if (e.Control && e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BtnCrearIA_Click(); } };
            txtNLInput.AllowDrop = true;
            txtNLInput.DragEnter += (_, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.Text) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            txtNLInput.DragDrop += (_, e) =>
            {
                string dropped = (e.Data?.GetData(DataFormats.Text) as string) ?? "";
                if (!string.IsNullOrEmpty(dropped)) InsertarEnInput(dropped);
            };
            btnCrearIA = new Button
            {
                Text = "✨ Crear con IA", Size = new Size(130, 38), Dock = DockStyle.Right,
                BackColor = Emerald, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5f), Cursor = Cursors.Hand
            };
            btnCrearIA.FlatAppearance.BorderSize = 0;
            btnCrearIA.Click += (_, _) => BtnCrearIA_Click();
            lblEstadoIA = new Label { Text = "", ForeColor = TextMuted, Font = new Font("Segoe UI", 8f), Dock = DockStyle.Bottom, Height = 0 };
            var pnlWrap = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Padding = new Padding(10, 0, 10, 0) };
            pnlWrap.Controls.Add(txtNLInput);
            pnlBarraIA.Controls.Add(pnlWrap);
            pnlBarraIA.Controls.Add(btnCrearIA);

            // Botón para mostrar/ocultar panel de credenciales
            var btnToggleCreds = new Button
            {
                Text = "🔑", Size = new Size(38, 38), Dock = DockStyle.Left,
                BackColor = Color.Transparent, ForeColor = TextMuted, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 13f), Cursor = Cursors.Hand
            };
            new ToolTip().SetToolTip(btnToggleCreds, "Mostrar / ocultar credenciales disponibles");
            btnToggleCreds.FlatAppearance.BorderSize = 0;
            btnToggleCreds.Click += (_, _) =>
            {
                pnlCredenciales.Visible = !pnlCredenciales.Visible;
                btnToggleCreds.ForeColor = pnlCredenciales.Visible ? Emerald4 : TextMuted;
                if (pnlCredenciales.Visible) RefrescarChipsCredenciales();
            };
            pnlBarraIA.Controls.Add(btnToggleCreds);

            // Panel de credenciales (se muestra encima de la barra de input)
            pnlCredenciales = new Panel
            {
                Dock = DockStyle.Bottom, Height = 52,
                BackColor = ColorTranslator.FromHtml("#06100d"),
                Visible = false, Padding = new Padding(8, 6, 8, 0)
            };
            pnlCredenciales.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(50, 52, 211, 153), 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnlCredenciales.Width, 0);
            };
            var lblCredsHdr = new Label
            {
                Text = "CREDENCIALES  ▸", ForeColor = Color.FromArgb(90, 167, 243, 208),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                AutoSize = false, Width = 108, Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            flpChips = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                AutoScroll = true, AutoScrollMargin = new Size(0, 0)
            };
            pnlCredenciales.Controls.Add(flpChips);
            pnlCredenciales.Controls.Add(lblCredsHdr);

            // Editor nodo
            pnlEditor = new Panel { Dock = DockStyle.Right, Width = 290, BackColor = BgSurface, Visible = false };
            pnlEditor.Paint += PintarBordeIzq;
            ConstruirEditor();

            // Panel schedule (abajo del panel izquierdo)
            ConstruirPanelSchedule();
            pnlIzquierdo.Controls.Add(pnlSchedule); // se agrega al izquierdo

            pnlDerecho.Controls.AddRange(new Control[] { pnlCanvasWrapper, pnlEditor, pnlLog, pnlErrorFix, pnlCredenciales, pnlBarraIA });
            Controls.AddRange(new Control[] { pnlDerecho, pnlIzquierdo, pnlToolbar });

            // Eventos
            btnNueva.Click       += (_, _) => CrearNuevaAuto();
            btnGuardar.Click     += (_, _) => Guardar();
            btnAddNodo.Click     += (_, _) => AgregarNodoManual();
            btnEjecutar.Click    += async (_, _) => await EjecutarAutomatizacionCompleta();
            btnDetener.Click     += (_, _) => { _cts?.Cancel(); EstadoIA("Cancelado.", WarnColor); };
            btnEliminarAuto.Click+= (_, _) => EliminarAutoActual();
            btnToggleLog.Click   += (_, _) => pnlLog.Visible = !pnlLog.Visible;
            // ClientSizeChanged se dispara cuando el scrollbar aparece/desaparece, ajustando anchos exactos
            flpLista.ClientSizeChanged += (_, _) => AjustarAnchosCards();
        }

        private void ConstruirEditor()
        {
            var hdr = new Label
            {
                Text = "EDITAR NODO", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Dock = DockStyle.Top, Height = 38, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0)
            };
            hdr.Paint += PintarBordeInf;
            btnCerrarEditor = new Button
            {
                Text = "✕", Size = new Size(28, 28), Location = new Point(258, 5),
                FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = TextMuted, Cursor = Cursors.Hand
            };
            btnCerrarEditor.FlatAppearance.BorderSize = 0;
            btnCerrarEditor.Click += (_, _) => { pnlEditor.Visible = false; _nodoEditando = null; };
            hdr.Controls.Add(btnCerrarEditor);

            // ── FlowLayoutPanel con scroll real ──────────────────────────────
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Padding = new Padding(10, 6, 10, 6)
            };
            // Forzar ancho de hijos al panel
            flow.Resize += (_, _) =>
            {
                int w = flow.ClientSize.Width - 28;
                foreach (Control c in flow.Controls) c.Width = w;
            };
            int fw = 256; // ancho default

            cmbTipoNodo = new ComboBox
            {
                BackColor = BgCard, ForeColor = TextMain, FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(fw, 28), Font = new Font("Segoe UI", 9.5f)
            };
            cmbTipoNodo.Items.AddRange(new object[] { "Disparador", "Condición", "Acción", "Fin" });
            cmbTipoNodo.SelectedIndex = 2;
            edtTitulo      = new TextBox { BackColor = BgCard, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle, Size = new Size(fw, 26), Font = new Font("Segoe UI", 9.5f) };
            edtDescripcion = new TextBox { BackColor = BgCard, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle, Size = new Size(fw, 26), Font = new Font("Segoe UI", 9.5f) };
            edtInstruccion = new TextBox { BackColor = BgCard, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle, Size = new Size(fw, 70), Multiline = true, Font = new Font("Segoe UI", 9.5f) };

            // Conexiones
            lblConexiones = new Label { Text = "🔗 Conectar a (salidas)", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), Size = new Size(fw, 18) };
            chkConexiones = new CheckedListBox
            {
                BackColor = BgCard, ForeColor = TextMain, Size = new Size(fw, 65),
                Font = new Font("Segoe UI", 8f), BorderStyle = BorderStyle.None, CheckOnClick = true
            };
            chkConexiones.ItemCheck += (_, _) => BeginInvoke(() => AplicarConexionesDesdeEditor());

            btnGenScript = new Button
            {
                Text = "⚡ Generar Script", Size = new Size(fw, 34),
                BackColor = Emerald, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9f), Cursor = Cursors.Hand
            };
            btnGenScript.FlatAppearance.BorderSize = 0;
            btnGenScript.Click += async (_, _) => await GenerarScriptNodo();
            lblScriptStatus = new Label { Text = "", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f), Size = new Size(fw, 16) };
            lblResultadoNodo = new Label { Text = "", ForeColor = Emerald4, Font = new Font("Segoe UI", 7.5f), Size = new Size(fw, 36), AutoEllipsis = true };

            var btnSaveN = new Button
            {
                Text = "💾 Guardar nodo", Size = new Size(fw, 34),
                BackColor = BgCard, ForeColor = Emerald4, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9f), Cursor = Cursors.Hand
            };
            btnSaveN.FlatAppearance.BorderSize = 1; btnSaveN.FlatAppearance.BorderColor = Emerald9;
            btnSaveN.Click += (_, _) => GuardarNodo();

            // ── Botón eliminar nodo ──
            var btnElimNodo = new Button
            {
                Text = "🗑 Eliminar nodo", Size = new Size(fw, 30),
                BackColor = Color.Transparent, ForeColor = ErrorColor, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand
            };
            btnElimNodo.FlatAppearance.BorderSize = 1;
            btnElimNodo.FlatAppearance.BorderColor = Color.FromArgb(60, 248, 113, 113);
            btnElimNodo.Click += (_, _) => EliminarNodoDesdeEditor();

            // Agregar en orden visual (de arriba a abajo)
            Label L(string t) => new Label { Text = t, ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), Size = new Size(fw, 18) };
            flow.Controls.AddRange(new Control[]
            {
                L("Tipo"), cmbTipoNodo,
                L("Título"), edtTitulo,
                L("Descripción"), edtDescripcion,
                L("Instrucción NL"), edtInstruccion,
                lblConexiones, chkConexiones,
                btnGenScript, lblScriptStatus, lblResultadoNodo,
                btnSaveN, btnElimNodo
            });

            pnlEditor.Controls.Add(flow);
            pnlEditor.Controls.Add(hdr);

            // ── Panel de error interactivo ──
            ConstruirPanelErrorFix();
        }

        private void EliminarNodoDesdeEditor()
        {
            if (_nodoEditando == null || _autoActual == null) return;
            var nodo = _nodoEditando.Datos;

            if (MessageBox.Show($"¿Eliminar nodo «{nodo.Titulo}»?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            // Eliminar script del disco
            if (!string.IsNullOrEmpty(nodo.NombreScript) && !string.IsNullOrEmpty(_autoActual.CarpetaScripts))
            {
                string ruta = Path.Combine(_autoActual.CarpetaScripts, nodo.NombreScript);
                if (File.Exists(ruta)) try { File.Delete(ruta); } catch { }
            }

            // Eliminar del modelo y canvas
            _autoActual.Nodos.Remove(nodo);
            canvas.EliminarNodo(_nodoEditando);
            _nodoEditando = null;
            pnlEditor.Visible = false;

            RecalcularOrdenEjecucion();
            GuardarLista();
            canvas.Invalidate();
            EstadoIA("✔ Nodo eliminado", TextMuted);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PANEL DE PROGRAMACIÓN (Schedule) — reloj/calendario
        // ══════════════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════════════
        //  PANEL DE ERROR INTERACTIVO — se muestra cuando un script falla
        //  El usuario puede ver el error, escribir instrucciones, y reintentar
        // ══════════════════════════════════════════════════════════════════════
        private void ConstruirPanelErrorFix()
        {
            pnlErrorFix = new Panel
            {
                Dock = DockStyle.Bottom, Height = 310,
                BackColor = ColorTranslator.FromHtml("#160303"),
                Visible = false, Padding = new Padding(12, 6, 12, 8)
            };

            // Borde animado con degradado rojizo
            pnlErrorFix.Paint += (_, ev) =>
            {
                var g = ev.Graphics;
                // Franja superior de alerta
                using var franBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, pnlErrorFix.Width, 4),
                    Color.FromArgb(220, 248, 113, 113),
                    Color.FromArgb(80, 248, 113, 113),
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                g.FillRectangle(franBrush, 0, 0, pnlErrorFix.Width, 4);
                // Borde exterior
                using var pen = new Pen(Color.FromArgb(160, 248, 113, 113), 1.5f);
                g.DrawRectangle(pen, 0, 0, pnlErrorFix.Width - 1, pnlErrorFix.Height - 1);
            };

            // ── Cabecera ────────────────────────────────────────────────────────
            var pnlHdr = new Panel
            {
                Dock = DockStyle.Top, Height = 36,
                BackColor = ColorTranslator.FromHtml("#2a0808"), Padding = new Padding(10, 0, 10, 0)
            };
            var lblIcono = new Label
            {
                Text = "🔴", Font = new Font("Segoe UI Emoji", 14f),
                ForeColor = ErrorColor, AutoSize = true, Location = new Point(8, 7)
            };
            var lblHdrTxt = new Label
            {
                Text = "SCRIPT FALLIDO — Revisa el error y da instrucciones de corrección",
                ForeColor = Color.FromArgb(255, 200, 200),
                Font = new Font("Segoe UI Semibold", 9.5f),
                AutoSize = false, Height = 36, TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(34, 0), Width = 700
            };
            pnlHdr.Controls.AddRange(new Control[] { lblIcono, lblHdrTxt });

            // ── Info del nodo (nombre + script, en amarillo) ─────────────────
            var pnlNodoInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 26,
                BackColor = Color.Transparent, Padding = new Padding(4, 0, 0, 0)
            };
            var lblNodoInfo = new Label
            {
                Name = "lblNodoInfoDet",
                Text = "",
                ForeColor = Color.FromArgb(252, 211, 77),   // amarillo
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            pnlNodoInfo.Controls.Add(lblNodoInfo);

            // ── Detalle del error (RichTextBox grande con colores) ───────────
            rtbErrorDetalle = new RichTextBox
            {
                Dock = DockStyle.Top, Height = 148, ReadOnly = true,
                BackColor = ColorTranslator.FromHtml("#0d0202"),
                ForeColor = Color.FromArgb(255, 180, 180),
                Font = new Font("Consolas", 8.5f), BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false
            };

            // ── Input del usuario ────────────────────────────────────────────
            var lblInputHint = new Label
            {
                Text = "💡 Instrucción de corrección (opcional — deja vacío para autocorregir con IA):",
                ForeColor = Color.FromArgb(156, 163, 175), Font = new Font("Segoe UI", 7.5f),
                Dock = DockStyle.Top, Height = 18
            };

            txtFixInput = new TextBox
            {
                PlaceholderText = "Ej: 'usa requests en vez de httpx', 'el token va en el header X-API-Key'…",
                BackColor = ColorTranslator.FromHtml("#1f1010"), ForeColor = TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 9f)
            };
            txtFixInput.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _tcsErrorFix?.TrySetResult(txtFixInput.Text.Trim());
                }
            };

            // ── Botones ──────────────────────────────────────────────────────
            var pnlBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0)
            };

            btnReintentarFix = new Button
            {
                Text = "🔄  Corregir y reintentar", Size = new Size(190, 30),
                BackColor = Emerald, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9f), Cursor = Cursors.Hand
            };
            btnReintentarFix.FlatAppearance.BorderSize = 0;
            btnReintentarFix.Click += (_, _) => _tcsErrorFix?.TrySetResult(txtFixInput.Text.Trim());

            btnSaltarError = new Button
            {
                Text = "⏭  Saltar nodo", Size = new Size(120, 30),
                BackColor = ColorTranslator.FromHtml("#1f1010"), ForeColor = TextMuted,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand, Margin = new Padding(8, 0, 0, 0)
            };
            btnSaltarError.FlatAppearance.BorderSize = 1;
            btnSaltarError.FlatAppearance.BorderColor = Color.FromArgb(80, 248, 113, 113);
            btnSaltarError.Click += (_, _) => _tcsErrorFix?.TrySetResult(null);

            var btnAbortarTodo = new Button
            {
                Text = "✖  Abortar automatización", Size = new Size(190, 30),
                BackColor = ColorTranslator.FromHtml("#450a0a"), ForeColor = ErrorColor,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand, Margin = new Padding(8, 0, 0, 0)
            };
            btnAbortarTodo.FlatAppearance.BorderSize = 1;
            btnAbortarTodo.FlatAppearance.BorderColor = ErrorColor;
            btnAbortarTodo.Click += (_, _) =>
            {
                _cts?.Cancel();
                _tcsErrorFix?.TrySetResult(null);
            };

            pnlBtns.Controls.AddRange(new Control[] { btnReintentarFix, btnSaltarError, btnAbortarTodo });

            // ── Ensamblar de abajo hacia arriba (Dock=Top se apila) ──────────
            pnlErrorFix.Controls.Add(pnlBtns);
            pnlErrorFix.Controls.Add(txtFixInput);
            pnlErrorFix.Controls.Add(lblInputHint);
            pnlErrorFix.Controls.Add(rtbErrorDetalle);
            pnlErrorFix.Controls.Add(pnlNodoInfo);
            pnlErrorFix.Controls.Add(pnlHdr);
        }

        /// <summary>
        /// Muestra el panel de error y espera a que el usuario escriba instrucciones o salte.
        /// Devuelve null si el usuario quiere saltar, o el texto de instrucciones.
        /// </summary>
        private async Task<string?> MostrarErrorYEsperarUsuario(
            NodoAutomatizacion nodo, string error, string codigoActual,
            string? hintMensaje = null)
        {
            if (InvokeRequired)
            {
                return await (Task<string?>)Invoke(
                    () => MostrarErrorYEsperarUsuario(nodo, error, codigoActual, hintMensaje));
            }

            _nodoConError = nodo;
            _tcsErrorFix = new TaskCompletionSource<string?>();

            // ── Actualizar label de nodo/script (amarillo) ──────────────────
            var lblInfo = pnlErrorFix.Controls.OfType<Panel>()
                .SelectMany(p => p.Controls.OfType<Label>())
                .FirstOrDefault(l => l.Name == "lblNodoInfoDet");
            if (lblInfo != null)
                lblInfo.Text = $"  📦 Nodo: {nodo.Titulo}   |   📄 Script: {nodo.NombreScript}";

            // ── Actualizar hint dinámico ─────────────────────────────────────
            var lblHintInput = pnlErrorFix.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Text?.StartsWith("💡") == true);
            if (lblHintInput != null && !string.IsNullOrEmpty(hintMensaje))
                lblHintInput.Text = $"💡 {hintMensaje}";

            // ── Colorizar el RichTextBox con secciones ──────────────────────
            rtbErrorDetalle.Clear();
            rtbErrorDetalle.SuspendLayout();

            // Separar el error en líneas y colorear por tipo
            var lineas = error.Replace("\r\n", "\n").Split('\n');
            foreach (var linea in lineas)
            {
                Color colorLinea;
                bool negrita = false;

                if (linea.StartsWith("Traceback", StringComparison.OrdinalIgnoreCase) ||
                    linea.StartsWith("  File ", StringComparison.OrdinalIgnoreCase))
                {
                    colorLinea = Color.FromArgb(253, 186, 116);  // naranja — traza
                }
                else if (linea.Contains("Error:") || linea.Contains("Exception:") ||
                         linea.Contains("error:") || linea.Contains("FAILED") ||
                         (linea.Length > 0 && char.IsUpper(linea[0]) &&
                          (linea.Contains("Error") || linea.Contains("Exception"))))
                {
                    colorLinea = Color.FromArgb(252, 100, 100);  // rojo vivo — tipo de error
                    negrita = true;
                }
                else if (linea.TrimStart().StartsWith("^") || linea.TrimStart().StartsWith("~"))
                {
                    colorLinea = Color.FromArgb(251, 191, 36);   // amarillo — marcador de posición
                }
                else if (linea.TrimStart().StartsWith(">>>") || linea.TrimStart().StartsWith("..."))
                {
                    colorLinea = Color.FromArgb(134, 239, 172);  // verde — prompt interactivo
                }
                else
                {
                    colorLinea = Color.FromArgb(220, 180, 180);  // rosa suave — texto general
                }

                // Aplicar color y negrita al fragmento
                int startIdx = rtbErrorDetalle.TextLength;
                rtbErrorDetalle.AppendText(linea + "\n");
                rtbErrorDetalle.Select(startIdx, linea.Length);
                rtbErrorDetalle.SelectionColor = colorLinea;
                if (negrita) rtbErrorDetalle.SelectionFont =
                    new Font("Consolas", 8.5f, FontStyle.Bold);
                else rtbErrorDetalle.SelectionFont =
                    new Font("Consolas", 8.5f, FontStyle.Regular);
            }

            rtbErrorDetalle.ResumeLayout();
            // Scroll al final para mostrar el mensaje de error más relevante
            rtbErrorDetalle.SelectionStart = rtbErrorDetalle.TextLength;
            rtbErrorDetalle.ScrollToCaret();

            txtFixInput.Clear();
            txtFixInput.Focus();
            pnlErrorFix.Visible = true;
            pnlErrorFix.BringToFront();

            string? resultado = await _tcsErrorFix.Task;

            pnlErrorFix.Visible = false;
            _nodoConError = null;
            return resultado;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CONEXIONES — enlazar/desenlazar desde el editor
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Carga el checkedlistbox de conexiones con todos los nodos excepto el actual.</summary>
        private void CargarConexionesEnEditor(NodoAutomatizacion nodoActual)
        {
            if (_autoActual == null) return;
            chkConexiones.Items.Clear();

            foreach (var n in _autoActual.Nodos)
            {
                if (n.Id == nodoActual.Id) continue;
                string label = $"{n.OrdenEjecucion}: {n.Titulo}";
                bool conectado = nodoActual.ConexionesSalida.Contains(n.Id);
                chkConexiones.Items.Add(new ConexionItem(n.Id, label), conectado);
            }
        }

        /// <summary>Aplica los checks del editor a las conexiones reales del nodo.</summary>
        private void AplicarConexionesDesdeEditor()
        {
            if (_nodoEditando == null || _autoActual == null) return;

            _nodoEditando.Datos.ConexionesSalida.Clear();
            for (int i = 0; i < chkConexiones.Items.Count; i++)
            {
                if (chkConexiones.GetItemChecked(i) && chkConexiones.Items[i] is ConexionItem ci)
                    _nodoEditando.Datos.ConexionesSalida.Add(ci.NodoId);
            }

            RecalcularOrdenEjecucion();
            canvas.Invalidate();
            GuardarLista();
        }

        private class ConexionItem
        {
            public string NodoId { get; }
            private readonly string _label;
            public ConexionItem(string id, string label) { NodoId = id; _label = label; }
            public override string ToString() => _label;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PANEL DE PROGRAMACIÓN — diseño para panel de 265px de ancho
        // ══════════════════════════════════════════════════════════════════════
        private void ConstruirPanelSchedule()
        {
            // Altura total: 28+26+60+100+46+58+36 = 354px
            pnlSchedule = new Panel
            {
                Dock = DockStyle.Bottom, Height = 354, BackColor = BgSurface,
                Visible = false, Padding = new Padding(0)
            };
            pnlSchedule.Paint += PintarBordeSup;

            // ── 1. Cabecera ──────────────────────────────────────────────────
            var pnlHdr = new Panel
            {
                Dock = DockStyle.Top, Height = 30,
                BackColor = ColorTranslator.FromHtml("#08180f")
            };
            pnlHdr.Paint += PintarBordeInf;
            pnlHdr.Controls.Add(new Label
            {
                Text = "  ⏰  PROGRAMACIÓN",
                ForeColor = Color.FromArgb(90, 167, 243, 208),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            });

            // ── 2. Indicador de estado en tiempo real ────────────────────────
            var pnlEstado = new Panel
            {
                Dock = DockStyle.Top, Height = 26,
                BackColor = ColorTranslator.FromHtml("#090909")
            };
            lblEstadoEjecucion = new Label
            {
                Text = "⚪  Sin programación",
                ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            pnlEstado.Controls.Add(lblEstadoEjecucion);

            // ── 3. Tarjetas de tipo — TableLayoutPanel distribuye el ancho ───
            var pnlTipos = new Panel
            {
                Dock = DockStyle.Top, Height = 60,
                BackColor = Color.Transparent, Padding = new Padding(8, 6, 8, 4)
            };

            string[] tiposTexto = { "Manual", "Diaria", "Intervalo", "Única", "Siempre" };
            string[] tiposEmoji = { "🚫", "🕐", "🔁", "1️⃣", "⚡" };
            string[] tiposIds   = { "manual", "diaria", "intervalo", "unica", "siempre" };
            _btnsTipoProg = new Button[5];

            // TableLayoutPanel: 5 celdas iguales, usa el ancho disponible
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1,
                BackColor = Color.Transparent, Margin = new Padding(0), CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int i = 0; i < 5; i++) tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            for (int i = 0; i < 5; i++)
            {
                var btn = new Button
                {
                    Text = $"{tiposEmoji[i]}\n{tiposTexto[i]}",
                    Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7f), Cursor = Cursors.Hand,
                    BackColor = BgCard, ForeColor = TextMuted,
                    Margin = new Padding(0, 0, i < 4 ? 3 : 0, 0), Tag = tiposIds[i]
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(40, 255, 255, 255);
                btn.Click += (_, _) => SeleccionarTipoProg((string)btn.Tag!);
                tlp.Controls.Add(btn, i, 0);
                _btnsTipoProg[i] = btn;
            }
            pnlTipos.Controls.Add(tlp);

            // ── 4. Zona dinámica de configuración (absoluta, 245px útiles) ───
            pnlConfigTipo = new Panel
            {
                Dock = DockStyle.Top, Height = 100,
                BackColor = Color.Transparent
            };

            // Controles para DIARIA / ÚNICA (hora grande)
            dtpHora = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm",
                ShowUpDown = true, Size = new Size(84, 32),
                Font = new Font("Consolas", 14f, FontStyle.Bold),
                Location = new Point(10, 38), Name = "dtpHora"
            };
            var lblEtqHora = new Label
            {
                Text = "Hora:", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f),
                Location = new Point(10, 22), Size = new Size(100, 14), Name = "lblEtqHora"
            };

            // Controles para ÚNICA (fecha)
            dtpFechaUnica = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy",
                Size = new Size(118, 30), Font = new Font("Segoe UI", 9f),
                Location = new Point(10, 38), Name = "dtpFechaUnica"
            };
            var lblEtqFecha = new Label
            {
                Text = "Fecha:", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f),
                Location = new Point(10, 22), Size = new Size(60, 14), Name = "lblEtqFecha"
            };
            var lblEtqHoraU = new Label
            {
                Text = "Hora:", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f),
                Location = new Point(134, 22), Size = new Size(40, 14), Name = "lblEtqHoraU"
            };

            // Controles para INTERVALO
            nudIntervalo = new NumericUpDown
            {
                Minimum = 1, Maximum = 1440, Value = 30, Increment = 5,
                BackColor = BgCard, ForeColor = TextMain,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(46, 22), Size = new Size(62, 28), Name = "nudIntervalo"
            };
            var lblCada = new Label
            {
                Text = "Cada", ForeColor = TextMuted, Font = new Font("Segoe UI", 8f),
                Location = new Point(10, 28), Size = new Size(34, 16), Name = "lblCada"
            };
            var lblMin = new Label
            {
                Text = "minutos", ForeColor = TextMuted, Font = new Font("Segoe UI", 8f),
                Location = new Point(112, 28), Size = new Size(54, 16), Name = "lblMin"
            };
            var lblRangoOpc = new Label
            {
                Text = "Rango (opcional):", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f),
                Location = new Point(10, 58), Size = new Size(120, 14), Name = "lblRangoOpc"
            };
            dtpHoraInicio = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm",
                ShowUpDown = true, Size = new Size(66, 24), Font = new Font("Segoe UI", 8.5f),
                Location = new Point(10, 74), Name = "dtpHoraInicio"
            };
            var lblFlecha = new Label
            {
                Text = "→", ForeColor = TextMuted, Font = new Font("Segoe UI", 9f),
                Location = new Point(80, 76), Size = new Size(14, 16), Name = "lblFlecha"
            };
            dtpHoraFin = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm",
                ShowUpDown = true, Size = new Size(66, 24), Font = new Font("Segoe UI", 8.5f),
                Location = new Point(96, 74), Name = "dtpHoraFin"
            };

            // Label de info para Manual/Siempre
            var lblInfoTipo = new Label
            {
                Text = "", ForeColor = TextMuted, Font = new Font("Segoe UI", 8f),
                Location = new Point(10, 20), Size = new Size(240, 60), Name = "lblInfoTipo"
            };

            pnlConfigTipo.Controls.AddRange(new Control[]
            {
                lblEtqHora, dtpHora,
                lblEtqFecha, dtpFechaUnica, lblEtqHoraU,
                lblCada, nudIntervalo, lblMin, lblRangoOpc,
                dtpHoraInicio, lblFlecha, dtpHoraFin,
                lblInfoTipo
            });

            // ── 5. Días de la semana — FlowLayoutPanel ────────────────────────
            var pnlDias = new Panel
            {
                Dock = DockStyle.Top, Height = 46,
                BackColor = Color.Transparent, Padding = new Padding(8, 4, 8, 0),
                Name = "pnlDias"
            };
            var lblDiasTit = new Label
            {
                Text = "Días activos:", ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f),
                Dock = DockStyle.Top, Height = 15
            };
            var flpDias = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent, WrapContents = false, Padding = new Padding(0)
            };
            string[] nombresDia = { "Do", "Lu", "Ma", "Mi", "Ju", "Vi", "Sá" };
            _btnsDiaSemana = new Button[7];
            for (int d = 0; d < 7; d++)
            {
                var bDia = new Button
                {
                    Text = nombresDia[d], Size = new Size(29, 25), FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5f), Cursor = Cursors.Hand,
                    BackColor = BgCard, ForeColor = TextMuted,
                    Margin = new Padding(0, 0, 2, 0), Tag = false
                };
                bDia.FlatAppearance.BorderSize = 1;
                bDia.FlatAppearance.BorderColor = Color.FromArgb(45, 255, 255, 255);
                bDia.Click += (_, _) =>
                {
                    bool activo = !(bool)bDia.Tag!;
                    bDia.Tag      = activo;
                    bDia.BackColor = activo ? Emerald : BgCard;
                    bDia.ForeColor = activo ? Color.Black : TextMuted;
                    bDia.FlatAppearance.BorderColor = activo ? Emerald : Color.FromArgb(45, 255, 255, 255);
                    ActualizarPreviewSchedule();
                };
                flpDias.Controls.Add(bDia);
                _btnsDiaSemana[d] = bDia;
            }
            pnlDias.Controls.Add(flpDias);
            pnlDias.Controls.Add(lblDiasTit);

            // ── 6. Preview en vivo ────────────────────────────────────────────
            var pnlPreview = new Panel
            {
                Dock = DockStyle.Top, Height = 58,
                BackColor = ColorTranslator.FromHtml("#081510"),
                Padding = new Padding(10, 6, 10, 4)
            };
            lblSchedulePreview = new Label
            {
                Text = "Sin programación activa",
                ForeColor = Color.FromArgb(167, 243, 208), Font = new Font("Segoe UI Semibold", 8f),
                Dock = DockStyle.Top, Height = 18
            };
            lblProximaEjecucion = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(100, 200, 140), Font = new Font("Segoe UI", 7.5f),
                Dock = DockStyle.Top, Height = 16
            };
            lblModoEjecucion = new Label
            {
                Text = "📱 Solo manual",
                ForeColor = Color.FromArgb(70, 167, 243, 208), Font = new Font("Segoe UI", 7f),
                Dock = DockStyle.Top, Height = 15
            };
            pnlPreview.Controls.Add(lblModoEjecucion);
            pnlPreview.Controls.Add(lblProximaEjecucion);
            pnlPreview.Controls.Add(lblSchedulePreview);

            // ── 7. Botones de acción ──────────────────────────────────────────
            var pnlBots = new Panel
            {
                Dock = DockStyle.Top, Height = 36,
                BackColor = Color.Transparent, Padding = new Padding(8, 4, 8, 0)
            };
            btnAplicarSchedule = new Button
            {
                Text = "✔  Activar programación", Location = new Point(8, 4), Size = new Size(172, 28),
                BackColor = ColorTranslator.FromHtml("#064e3b"), ForeColor = Emerald4,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 8.5f), Cursor = Cursors.Hand
            };
            btnAplicarSchedule.FlatAppearance.BorderSize = 1;
            btnAplicarSchedule.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#10b981");
            btnAplicarSchedule.Click += (_, _) => AplicarSchedule();

            btnDesactivarSched = new Button
            {
                Text = "✕  Quitar", Location = new Point(186, 4), Size = new Size(78, 28),
                BackColor = Color.FromArgb(26, 10, 10),
                ForeColor = Color.FromArgb(220, 110, 110),
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f), Cursor = Cursors.Hand
            };
            btnDesactivarSched.FlatAppearance.BorderSize = 1;
            btnDesactivarSched.FlatAppearance.BorderColor = Color.FromArgb(90, 200, 80, 80);
            btnDesactivarSched.Click += (_, _) => DesactivarSchedule();

            lblScheduleStatus = new Label
            {
                Text = "", ForeColor = Emerald4, Font = new Font("Segoe UI", 7f),
                Location = new Point(194, 6), Size = new Size(120, 16)
            };
            pnlBots.Controls.AddRange(new Control[] { btnAplicarSchedule, btnDesactivarSched, lblScheduleStatus });

            // ── Ensamblar (Dock=Top: el primero en Controls.Add queda abajo) ──
            pnlSchedule.Controls.Add(pnlBots);
            pnlSchedule.Controls.Add(pnlPreview);
            pnlSchedule.Controls.Add(pnlDias);
            pnlSchedule.Controls.Add(pnlConfigTipo);
            pnlSchedule.Controls.Add(pnlTipos);
            pnlSchedule.Controls.Add(pnlEstado);
            pnlSchedule.Controls.Add(pnlHdr);

            // ── Eventos de cambio → actualizar preview en vivo ────────────────
            dtpHora.ValueChanged       += (_, _) => ActualizarPreviewSchedule();
            dtpHoraInicio.ValueChanged += (_, _) => ActualizarPreviewSchedule();
            dtpHoraFin.ValueChanged    += (_, _) => ActualizarPreviewSchedule();
            dtpFechaUnica.ValueChanged  += (_, _) => ActualizarPreviewSchedule();
            nudIntervalo.ValueChanged   += (_, _) => ActualizarPreviewSchedule();

            // ── Timer: refresca indicador de estado cada 3 seg ────────────────
            var timerEst = new System.Windows.Forms.Timer { Interval = 3000 };
            timerEst.Tick += (_, _) => RefrescarIndicadorEstado();
            pnlSchedule.VisibleChanged += (_, _) =>
            {
                timerEst.Enabled = pnlSchedule.Visible;
                if (pnlSchedule.Visible) RefrescarIndicadorEstado();
            };

            SeleccionarTipoProg("manual");
        }

        /// <summary>Marca la tarjeta activa y muestra solo los controles del tipo elegido.</summary>
        private void SeleccionarTipoProg(string tipo)
        {
            _tipoProgActual = tipo;
            string[] ids = { "manual", "diaria", "intervalo", "unica", "siempre" };

            // ── Resaltar tarjeta activa ──────────────────────────────────────
            for (int i = 0; i < _btnsTipoProg.Length; i++)
            {
                bool activo = ids[i] == tipo;
                _btnsTipoProg[i].BackColor = activo ? ColorTranslator.FromHtml("#052e16") : BgCard;
                _btnsTipoProg[i].ForeColor = activo ? Emerald4 : TextMuted;
                _btnsTipoProg[i].FlatAppearance.BorderColor =
                    activo ? Emerald : Color.FromArgb(40, 255, 255, 255);
            }

            bool esDiaria    = tipo == "diaria";
            bool esIntervalo = tipo == "intervalo";
            bool esUnica     = tipo == "unica";

            // ── Posicionar y mostrar/ocultar controles por tipo ──────────────
            // Para DIARIA: hora grande centrada
            dtpHora.Location = esUnica ? new Point(134, 38) : new Point(10, 38);
            dtpHora.Size     = new Size(84, 32);

            // Visibilidad de cada control (por Name)
            foreach (Control c in pnlConfigTipo.Controls)
            {
                c.Visible = c.Name switch
                {
                    "dtpHora"       => esDiaria || esUnica,
                    "lblEtqHora"    => esDiaria,
                    "lblEtqHoraU"   => esUnica,
                    "dtpFechaUnica" => esUnica,
                    "lblEtqFecha"   => esUnica,
                    "nudIntervalo"  => esIntervalo,
                    "lblCada"       => esIntervalo,
                    "lblMin"        => esIntervalo,
                    "lblRangoOpc"   => esIntervalo,
                    "dtpHoraInicio" => esIntervalo,
                    "lblFlecha"     => esIntervalo,
                    "dtpHoraFin"    => esIntervalo,
                    "lblInfoTipo"   => tipo is "manual" or "siempre",
                    _               => false
                };
            }

            // Texto del label de info para manual/siempre
            var lblInfo = pnlConfigTipo.Controls["lblInfoTipo"] as Label;
            if (lblInfo != null)
                lblInfo.Text = tipo switch
                {
                    "manual"  => "Ejecución solo manual desde el botón ▶",
                    "siempre" => "Se ejecuta cada ~30 seg mientras la app esté abierta o vía Task Scheduler",
                    _         => ""
                };

            // ── Días de la semana: solo visible si hay horario específico ────
            bool muestraDias = tipo is "diaria" or "intervalo" or "unica";
            var pnlDiasBuscar = pnlSchedule.Controls.OfType<Panel>()
                .FirstOrDefault(p => p.Name == "pnlDias");
            if (pnlDiasBuscar != null) pnlDiasBuscar.Visible = muestraDias;

            // ── Modo de ejecución en el preview ─────────────────────────────
            if (lblModoEjecucion != null)
                lblModoEjecucion.Text = tipo == "manual"
                    ? "📱 Solo manual desde la app"
                    : "🖥  Segundo plano — funciona aunque cierres la app";

            ActualizarPreviewSchedule();
        }

        /// <summary>Refresca el indicador de estado (running / activo / inactivo).</summary>
        private void RefrescarIndicadorEstado()
        {
            if (lblEstadoEjecucion == null || lblEstadoEjecucion.IsDisposed) return;
            if (lblEstadoEjecucion.InvokeRequired)
            { lblEstadoEjecucion.BeginInvoke(RefrescarIndicadorEstado); return; }

            if (_ejecutando)
            {
                lblEstadoEjecucion.Text      = "🟢  Ejecutando ahora...";
                lblEstadoEjecucion.ForeColor = Emerald4;
                lblEstadoEjecucion.Parent!.BackColor = ColorTranslator.FromHtml("#041a0d");
                return;
            }

            if (_autoActual == null)
            {
                lblEstadoEjecucion.Text      = "⚪  Sin selección";
                lblEstadoEjecucion.ForeColor = TextMuted;
                lblEstadoEjecucion.Parent!.BackColor = ColorTranslator.FromHtml("#090909");
                return;
            }

            bool registrado = EstaRegistradoEnTaskScheduler(_autoActual.Id);

            if (registrado)
            {
                string prox = FormatearSchedule(_autoActual);
                lblEstadoEjecucion.Text      = $"🟡  Activa en Windows  ·  {prox}";
                lblEstadoEjecucion.ForeColor = Color.FromArgb(252, 211, 77);
                lblEstadoEjecucion.Parent!.BackColor = ColorTranslator.FromHtml("#1a1200");
            }
            else if (_autoActual.TipoProgramacion != "manual")
            {
                lblEstadoEjecucion.Text      = "⚫  Programada (no registrada en Windows)";
                lblEstadoEjecucion.ForeColor = TextMuted;
                lblEstadoEjecucion.Parent!.BackColor = ColorTranslator.FromHtml("#090909");
            }
            else
            {
                lblEstadoEjecucion.Text      = "⚪  Sin programación automática";
                lblEstadoEjecucion.ForeColor = TextMuted;
                lblEstadoEjecucion.Parent!.BackColor = ColorTranslator.FromHtml("#090909");
            }
        }

        /// <summary>Actualiza las etiquetas de preview y próxima ejecución en tiempo real.</summary>
        private void ActualizarPreviewSchedule()
        {
            if (lblSchedulePreview == null) return;

            string[] diasNombres = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
            var diasSel = _btnsDiaSemana
                .Select((b, i) => ((bool)b.Tag!) ? diasNombres[i] : null)
                .Where(d => d != null).Select(d => d!).ToList();
            string diasStr = diasSel.Count == 0 ? "todos los días"
                           : diasSel.Count == 7 ? "todos los días"
                           : string.Join(", ", diasSel);

            string preview = _tipoProgActual switch
            {
                "manual"    => "📋 Sin programación — ejecutar manualmente",
                "diaria"    => $"📅 Cada día a las {dtpHora.Value:HH:mm} ({diasStr})",
                "intervalo" =>
                    $"🔁 Cada {nudIntervalo.Value} minutos" +
                    (diasSel.Count > 0 && diasSel.Count < 7 ? $" — {diasStr}" : "") +
                    (dtpHoraInicio.Value.Hour != dtpHoraFin.Value.Hour
                        ? $"  [{dtpHoraInicio.Value:HH:mm} → {dtpHoraFin.Value:HH:mm}]" : ""),
                "unica"     => $"1️⃣  Una vez el {dtpFechaUnica.Value:dd/MM/yyyy} a las {dtpHora.Value:HH:mm}",
                "siempre"   => "⚡ Ejecución continua (cada ~30 segundos)",
                _           => ""
            };

            lblSchedulePreview.Text = preview;

            // Calcular próxima ejecución
            string prox = CalcularProximaEjecucion(_tipoProgActual, diasSel);
            lblProximaEjecucion.Text = string.IsNullOrEmpty(prox) ? "" : $"⏭  Próxima: {prox}";
        }

        private string CalcularProximaEjecucion(string tipo, List<string> diasSel)
        {
            var ahora = DateTime.Now;
            string[] diasNombresSem = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

            switch (tipo)
            {
                case "diaria":
                    var horaObjetivo = dtpHora.Value.TimeOfDay;
                    if (diasSel.Count == 0 || diasSel.Count == 7)
                    {
                        var siguiente = ahora.Date.Add(horaObjetivo);
                        if (siguiente <= ahora) siguiente = siguiente.AddDays(1);
                        return FormatearDistancia(siguiente, ahora);
                    }
                    else
                    {
                        // Buscar el próximo día de la semana habilitado
                        for (int d = 0; d <= 7; d++)
                        {
                            var candidato = ahora.Date.AddDays(d).Add(horaObjetivo);
                            string nomDia = diasNombresSem[(int)candidato.DayOfWeek];
                            if (diasSel.Contains(nomDia) && candidato > ahora)
                                return FormatearDistancia(candidato, ahora);
                        }
                        return "";
                    }

                case "intervalo":
                    var prox = ahora.AddMinutes((double)nudIntervalo.Value);
                    return FormatearDistancia(prox, ahora);

                case "unica":
                    var dt = dtpFechaUnica.Value.Date.Add(dtpHora.Value.TimeOfDay);
                    return dt > ahora ? FormatearDistancia(dt, ahora) : "⚠ Fecha pasada";

                case "siempre":
                    return "en ~30 segundos";

                default:
                    return "";
            }
        }

        private static string FormatearDistancia(DateTime objetivo, DateTime ahora)
        {
            var diff = objetivo - ahora;
            if (diff.TotalDays >= 1)
                return $"{objetivo:ddd dd/MM} a las {objetivo:HH:mm} (en {(int)diff.TotalHours}h {diff.Minutes}min)";
            if (diff.TotalHours >= 1)
                return $"hoy a las {objetivo:HH:mm} (en {(int)diff.TotalHours}h {diff.Minutes}min)";
            return $"hoy a las {objetivo:HH:mm} (en {diff.Minutes} min)";
        }

        private void AplicarSchedule()
        {
            if (_autoActual == null) { EstadoIA("Selecciona una automatización.", ErrorColor); return; }

            _autoActual.TipoProgramacion = _tipoProgActual;
            _autoActual.HoraEjecucion    = dtpHora.Value.ToString("HH:mm");
            _autoActual.HoraInicio       = dtpHoraInicio.Value.ToString("HH:mm");
            _autoActual.HoraFin          = dtpHoraFin.Value.ToString("HH:mm");
            _autoActual.IntervaloMinutos = (int)nudIntervalo.Value;
            _autoActual.FechaUnica       = dtpFechaUnica.Value.Date;
            _autoActual.Activa           = _tipoProgActual != "manual";

            // Días activos (índice: 0=Dom…6=Sáb)
            _autoActual.DiasActivos.Clear();
            for (int i = 0; i < _btnsDiaSemana.Length; i++)
                if ((bool)_btnsDiaSemana[i].Tag!) _autoActual.DiasActivos.Add(i);

            GuardarLista();
            RefrescarLista();

            // Registrar en Windows Task Scheduler (ejecución en 2do plano)
            if (_tipoProgActual != "manual")
            {
                string carpeta = AutomatizacionScheduler.ObtenerCarpetaAuto(_autoActual);
                string rutaOrq = GenerarOrquestadorPython(_autoActual, carpeta);
                var (ok, mensaje) = RegistrarEnTaskScheduler(_autoActual, rutaOrq);

                if (ok)
                {
                    lblScheduleStatus.Text      = "✔ Registrado en Windows Task Scheduler";
                    lblScheduleStatus.ForeColor  = Emerald4;
                    EstadoIA($"✔ Programación guardada y registrada: {FormatearSchedule(_autoActual)}", Emerald4);
                }
                else
                {
                    lblScheduleStatus.Text      = "⚠ Error al registrar en Task Scheduler";
                    lblScheduleStatus.ForeColor  = WarnColor;
                    EstadoIA($"⚠ Programación guardada pero Task Scheduler falló: {mensaje}", WarnColor);
                    MessageBox.Show(
                        $"La programación se guardó, pero no se pudo registrar en Windows Task Scheduler.\n\n" +
                        $"Error: {mensaje}\n\n" +
                        $"Posibles soluciones:\n" +
                        $"• Ejecutar la app como Administrador\n" +
                        $"• Verificar que Python está instalado y en el PATH\n" +
                        $"• Verificar que el servicio 'Task Scheduler' de Windows está activo",
                        "Advertencia - Task Scheduler",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                EliminarDeTaskScheduler(_autoActual.Id);
                lblScheduleStatus.Text      = "Desactivado";
                lblScheduleStatus.ForeColor  = TextMuted;
                EstadoIA("Programación desactivada.", TextMuted);
            }

            ActualizarPreviewSchedule();
        }

        private void DesactivarSchedule()
        {
            if (_autoActual == null) return;
            _autoActual.TipoProgramacion = "manual";
            _autoActual.Activa = false;
            EliminarDeTaskScheduler(_autoActual.Id);
            GuardarLista();
            RefrescarLista();
            SeleccionarTipoProg("manual");
            lblScheduleStatus.Text = "Desactivado";
            lblScheduleStatus.ForeColor = TextMuted;
            EstadoIA("Programación desactivada.", WarnColor);
        }

        /// <summary>Carga los valores del schedule de la auto en el panel.</summary>
        private void CargarScheduleEnPanel(Automatizacion auto)
        {
            pnlSchedule.Visible = true;

            string tipo = auto.TipoProgramacion?.ToLowerInvariant() ?? "manual";
            SeleccionarTipoProg(tipo);

            if (TimeSpan.TryParse(auto.HoraEjecucion, out var he))
                dtpHora.Value = DateTime.Today.Add(he);
            if (TimeSpan.TryParse(auto.HoraInicio, out var hi))
                dtpHoraInicio.Value = DateTime.Today.Add(hi);
            if (TimeSpan.TryParse(auto.HoraFin, out var hf))
                dtpHoraFin.Value = DateTime.Today.Add(hf);
            if (auto.FechaUnica != default)
                dtpFechaUnica.Value = auto.FechaUnica;

            nudIntervalo.Value = Math.Max(1, Math.Min(1440, auto.IntervaloMinutos));

            for (int i = 0; i < _btnsDiaSemana.Length; i++)
            {
                bool activo = auto.DiasActivos.Contains(i);
                _btnsDiaSemana[i].Tag = activo;
                _btnsDiaSemana[i].BackColor = activo ? Emerald : BgCard;
                _btnsDiaSemana[i].ForeColor = activo ? Color.Black : TextMuted;
                _btnsDiaSemana[i].FlatAppearance.BorderColor =
                    activo ? Emerald : Color.FromArgb(50, 255, 255, 255);
            }

            // Estado de Task Scheduler
            bool registrado = EstaRegistradoEnTaskScheduler(auto.Id);
            if (registrado)
            {
                lblScheduleStatus.Text      = "✔ Activo en Windows Task Scheduler";
                lblScheduleStatus.ForeColor  = Emerald4;
            }
            else if (auto.TipoProgramacion != "manual" && auto.Activa)
            {
                lblScheduleStatus.Text      = "⚠ No registrado en Task Scheduler";
                lblScheduleStatus.ForeColor  = WarnColor;
            }
            else
            {
                lblScheduleStatus.Text      = "";
            }

            ActualizarPreviewSchedule();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  WINDOWS TASK SCHEDULER — ejecución en segundo plano
        //  Registra las automatizaciones para que corran aunque la app esté cerrada.
        //  Usa schtasks.exe (incluido en Windows, sin instalación extra).
        // ══════════════════════════════════════════════════════════════════════

        // Nombre plano (sin carpeta) para evitar "Acceso denegado" sin permisos de admin
        private static string NombreTarea(string autoId) => $"OPENGIOAI_auto_{autoId[..8]}";

        /// <summary>Genera el script Python orquestador que respeta el grafo de nodos.</summary>
        private static string GenerarOrquestadorPython(Automatizacion auto, string carpeta)
        {
            // Construir mapa de predecesores
            var predMap = auto.Nodos.ToDictionary(n => n.Id, _ => new List<string>());
            foreach (var n in auto.Nodos)
                foreach (var s in n.ConexionesSalida)
                    if (predMap.ContainsKey(s)) predMap[s].Add(n.Id);

            // Serializar nodos en JSON inline para el script Python
            var nodosDef = auto.Nodos.Select(n => new
            {
                id     = n.Id,
                titulo = n.Titulo,
                script = n.NombreScript,
                preds  = predMap[n.Id]
            });
            string nodosJson = System.Text.Json.JsonSerializer.Serialize(nodosDef,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            string carpetaEscapada = carpeta.Replace("\\", "\\\\");

            string codigo = $@"#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Orquestador generado automaticamente para: {auto.Nombre}
# ID: {auto.Id}  |  No editar manualmente.
import subprocess, os, sys
from datetime import datetime

# ── Forzar UTF-8 en stdout/stderr para evitar UnicodeEncodeError en Windows ──
if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
if hasattr(sys.stderr, 'reconfigure'):
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')

CARPETA  = r""{carpeta}""
LOG_PATH = os.path.join(CARPETA, ""_orquestador.log"")

NODOS = {nodosJson}

def log(msg):
    linea = f""{{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}}  {{msg}}""
    # Imprimir con fallback seguro (ASCII si no cabe en la consola)
    try:
        print(linea)
    except Exception:
        print(linea.encode('ascii', errors='replace').decode('ascii'))
    with open(LOG_PATH, 'a', encoding='utf-8') as f:
        f.write(linea + '\n')

def ejecutar_script(ruta_script, contexto_anterior=None):
    env = dict(os.environ)
    env['PYTHONIOENCODING'] = 'utf-8'          # propagar UTF-8 a scripts hijos
    if contexto_anterior:
        env['NODO_ANTERIOR_RESULTADO'] = contexto_anterior

    resp_path = os.path.join(CARPETA, 'respuesta.txt')
    try:
        open(resp_path, 'w', encoding='utf-8').close()
    except Exception:
        pass

    result = subprocess.run(
        [sys.executable, ruta_script],
        env=env,
        capture_output=True,
        text=True,
        encoding='utf-8',
        errors='replace',
        cwd=CARPETA,
        timeout=300
    )

    if result.returncode != 0:
        raise RuntimeError(result.stderr[:800] or result.stdout[:800])

    if os.path.exists(resp_path):
        with open(resp_path, encoding='utf-8', errors='replace') as f:
            contenido = f.read().strip()
        if contenido:
            return contenido

    return result.stdout.strip()

def obtener_contexto(nodo_id, nodos_map, resultados):
    preds = nodos_map[nodo_id]['preds']
    if not preds:
        return ''
    if len(preds) == 1:
        return resultados.get(preds[0], '')
    partes = []
    for pid in preds:
        r = resultados.get(pid, '')
        if r:
            titulo = nodos_map[pid]['titulo']
            partes.append(f'=== Resultado de: {{titulo}} ===\n{{r}}')
    return '\n'.join(partes)

def main():
    log(f'=== Iniciando: {auto.Nombre} ===')

    nodos_map   = {{n['id']: n for n in NODOS}}
    resultados  = {{}}
    completados = set()
    pendientes  = set(nodos_map.keys())
    errores     = 0

    while pendientes:
        listos = [nid for nid in pendientes
                  if all(p in completados for p in nodos_map[nid]['preds'])]

        if not listos:
            log('[ERROR] Ciclo detectado en el grafo -- abortando')
            break

        for nid in listos:
            nodo   = nodos_map[nid]
            script = os.path.join(CARPETA, nodo['script'])

            if not os.path.exists(script):
                log(f""  [AVISO] Script no encontrado: {{nodo['script']}}"")
                resultados[nid] = ''
                completados.add(nid)
                pendientes.discard(nid)
                continue

            ctx = obtener_contexto(nid, nodos_map, resultados)
            try:
                log(f""  >> {{nodo['titulo']}}"")
                resultado = ejecutar_script(script, ctx)
                resultados[nid] = resultado
                log(f""  OK {{nodo['titulo']}}: {{resultado[:100]}}"")
            except Exception as e:
                log(f""  [ERR] {{nodo['titulo']}}: {{e}}"")
                resultados[nid] = ''
                errores += 1

            completados.add(nid)
            pendientes.discard(nid)

    estado = 'Completado OK' if errores == 0 else f'Completado con {{errores}} error(es)'
    log(f'=== {{estado}} ===')

if __name__ == '__main__':
    main()
";
            string rutaOrq = Path.Combine(carpeta, "_orquestador.py");
            File.WriteAllText(rutaOrq, codigo, Encoding.UTF8);
            return rutaOrq;
        }

        /// <summary>Registra la automatización en Windows Task Scheduler vía schtasks.exe.</summary>
        /// <summary>Genera un .bat wrapper y registra la tarea en Windows Task Scheduler.</summary>
        private static (bool ok, string mensaje) RegistrarEnTaskScheduler(Automatizacion auto, string rutaOrquestador)
        {
            string tarea     = NombreTarea(auto.Id);
            string pythonExe = EncontrarPython();
            string carpeta   = Path.GetDirectoryName(rutaOrquestador)!;

            // ── 1) Crear .bat wrapper (Task Scheduler funciona mejor con .bat) ──
            string rutaBat = Path.Combine(carpeta, "_ejecutar.bat");
            string batContenido = $"""
                @echo off
                chcp 65001 >nul 2>&1
                set PYTHONIOENCODING=utf-8
                set PYTHONUTF8=1
                cd /d "{carpeta}"
                "{pythonExe}" "{rutaOrquestador}" >> "_orquestador.log" 2>&1
                exit /b %ERRORLEVEL%
                """;
            // Asegurar que no haya indentación en el .bat
            batContenido = string.Join("\r\n",
                batContenido.Split('\n').Select(l => l.Trim()));
            File.WriteAllText(rutaBat, batContenido, new UTF8Encoding(false));

            // ── 2) Construir días para schtasks (/d MON,TUE,...) ──
            string[] diasSchtasks = { "SUN","MON","TUE","WED","THU","FRI","SAT" };
            string diasArg = "";
            if (auto.DiasActivos.Count > 0 && auto.DiasActivos.Count < 7)
                diasArg = $"/d {string.Join(",", auto.DiasActivos.Select(d => diasSchtasks[d]))} ";

            // ── 3) Eliminar tarea anterior si existe ──
            EjecutarSchtasks($"/delete /tn \"{tarea}\" /f");

            // ── 4) Crear la tarea según el tipo ──
            // Usar ruta del .bat directamente (sin comillas anidadas problemáticas)
            string tr = rutaBat;

            string args = auto.TipoProgramacion switch
            {
                "diaria" when diasArg == "" =>
                    $"/create /tn \"{tarea}\" /tr \"{tr}\" /sc DAILY /st {auto.HoraEjecucion} /f",
                "diaria" =>
                    $"/create /tn \"{tarea}\" /tr \"{tr}\" /sc WEEKLY {diasArg}/st {auto.HoraEjecucion} /f",
                "intervalo" =>
                    $"/create /tn \"{tarea}\" /tr \"{tr}\" /sc MINUTE /mo {auto.IntervaloMinutos} /f",
                "unica" =>
                    $"/create /tn \"{tarea}\" /tr \"{tr}\" /sc ONCE " +
                    $"/sd {auto.FechaUnica:dd/MM/yyyy} /st {auto.HoraEjecucion} /f",
                "siempre" =>
                    $"/create /tn \"{tarea}\" /tr \"{tr}\" /sc MINUTE /mo 1 /f",
                _ => ""
            };

            if (string.IsNullOrEmpty(args))
                return (false, "Tipo de programación no reconocido");

            // Log del comando para diagnóstico
            try
            {
                string logPath = Path.Combine(carpeta, "_schtasks_debug.log");
                File.WriteAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\ncmd /c schtasks {args}\n",
                    Encoding.UTF8);
            }
            catch { }

            var (exitCode, stdout, stderr) = EjecutarSchtasksConResultado(args);

            // Log del resultado
            try
            {
                string logPath = Path.Combine(carpeta, "_schtasks_debug.log");
                File.AppendAllText(logPath,
                    $"ExitCode: {exitCode}\nStdout: {stdout}\nStderr: {stderr}\n",
                    Encoding.UTF8);
            }
            catch { }

            if (exitCode != 0)
            {
                string err = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
                return (false, $"Error schtasks (código {exitCode}): {err.Trim()}");
            }

            // ── 5) Verificar que se registró ──
            bool verificado = EstaRegistradoEnTaskScheduler(auto.Id);
            if (!verificado)
                return (false, "schtasks ejecutó sin error pero la tarea no se encontró al verificar");

            return (true, $"Tarea '{tarea}' registrada correctamente");
        }

        private static void EliminarDeTaskScheduler(string autoId)
            => EjecutarSchtasks($"/delete /tn \"{NombreTarea(autoId)}\" /f");

        private static bool EstaRegistradoEnTaskScheduler(string autoId)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe",
                    $"/c schtasks /query /tn \"{NombreTarea(autoId)}\"")
                { CreateNoWindow = true, UseShellExecute = false,
                  RedirectStandardOutput = true, RedirectStandardError = true };
                using var p = Process.Start(psi);
                p?.WaitForExit(4000);
                return p?.ExitCode == 0;
            }
            catch { return false; }
        }

        /// <summary>Ejecuta schtasks via cmd /c para manejo correcto de comillas con espacios.</summary>
        private static void EjecutarSchtasks(string argumentos)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c schtasks {argumentos}")
                { CreateNoWindow = true, UseShellExecute = false,
                  RedirectStandardOutput = true, RedirectStandardError = true };
                using var p = Process.Start(psi);
                p?.WaitForExit(8000);
            }
            catch { }
        }

        /// <summary>Ejecuta schtasks via cmd /c y devuelve código de salida + stdout + stderr.</summary>
        private static (int exitCode, string stdout, string stderr) EjecutarSchtasksConResultado(string argumentos)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c schtasks {argumentos}")
                {
                    CreateNoWindow = true, UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                if (p == null) return (-1, "", "No se pudo iniciar schtasks");
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(10000);
                return (p.ExitCode, stdout, stderr);
            }
            catch (Exception ex)
            {
                return (-1, "", ex.Message);
            }
        }

        private static string EncontrarPython()
        {
            // 1) Intentar 'where python' para obtener la ruta absoluta real
            try
            {
                var psi = new ProcessStartInfo("where", "python")
                {
                    CreateNoWindow = true, UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                string? salida = p?.StandardOutput.ReadToEnd();
                p?.WaitForExit(5000);
                if (p?.ExitCode == 0 && !string.IsNullOrWhiteSpace(salida))
                {
                    // 'where' puede devolver varias líneas; tomar la primera que exista
                    foreach (string linea in salida.Split('\n', '\r'))
                    {
                        string ruta = linea.Trim();
                        if (ruta.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(ruta))
                            return ruta;
                    }
                }
            }
            catch { /* where no disponible */ }

            // 2) Rutas comunes en disco
            string[] candidatos =
            {
                @"C:\Python312\python.exe", @"C:\Python311\python.exe",
                @"C:\Python310\python.exe", @"C:\Python39\python.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Python\Python312\python.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Python\Python311\python.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Python\Python310\python.exe"),
            };
            foreach (var c in candidatos)
                if (File.Exists(c)) return c;

            // 3) Buscar en py launcher (Windows)
            try
            {
                var psi2 = new ProcessStartInfo("where", "py")
                {
                    CreateNoWindow = true, UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var p2 = Process.Start(psi2);
                string? salida2 = p2?.StandardOutput.ReadToEnd();
                p2?.WaitForExit(5000);
                if (p2?.ExitCode == 0 && !string.IsNullOrWhiteSpace(salida2))
                {
                    string ruta = salida2.Split('\n', '\r')[0].Trim();
                    if (File.Exists(ruta)) return ruta;
                }
            }
            catch { }

            return "python"; // último recurso
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FLUJO PRINCIPAL: instrucción → Agente Planificador → Agente Generador
        //                   → Prueba → Programación → Diagrama visual
        //
        //  Arquitectura limpia de 2 agentes:
        //    Agente 1 (Planificador): LLM directo → JSON con schedule + nodos
        //    Agente 2 (Generador):    LLM directo → código Python por nodo
        //    Sin ejecutar scripts intermedios — todo código puro.
        // ══════════════════════════════════════════════════════════════════════
        private async void BtnCrearIA_Click()
        {
            string texto = txtNLInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto)) return;
            if (!ValidarConfig()) return;

            // ── Detectar si es MODIFICACIÓN de auto existente o CREACIÓN nueva ──
            // Si hay una automatización seleccionada, es modificación
            if (_autoActual != null)
            {
                await ModificarAutomatizacionConNL(texto);
                return;
            }

            btnCrearIA.Enabled = false;
            AbrirLog();
            EstadoIA("🤖 Creando automatización completa…", TextMuted);
            LogW("══════ CREANDO AUTOMATIZACIÓN CON IA ══════\n", Emerald4);
            LogW($"Instrucción: {texto}\n\n", TextSub);

            _cts?.Cancel();
            _cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
            var ct = _cts.Token;

            try
            {
                // ── Construir contexto base (una sola vez, inmutable) ────────
                AgentContext ctxBase = await AgentContext.BuildAsync(
                    RutaTrabajo, Modelo, ApiKey, Agente,
                    soloChat: false, ClavesApis, ct);

                // ═══════════════════════════════════════════════════════════════
                //  PASO 1: AGENTE PLANIFICADOR — descompone la instrucción
                //  LLM directo: no ejecuta scripts, devuelve JSON puro
                // ═══════════════════════════════════════════════════════════════
                LogW("PASO 1: 🧠 Agente Planificador analizando instrucción…\n", WarnColor);
                EstadoIA("🧠 Planificando…", TextMuted);

                AgentContext ctxPlanner = ctxBase.ConPromptPersonalizado(PromptSistemaPlanificador());
                string respPlanner = await AIModelConector.ObtenerRespuestaLLMAsync(
                    PromptUsuarioPlanificador(texto), ctxPlanner, ct);

                LogW($"  Respuesta planificador: {respPlanner.Length} chars\n", TextSub);

                var defAuto = ParsearDefinicionAuto(respPlanner);
                if (defAuto == null) throw new Exception("El planificador no devolvió un JSON válido");

                // ── Crear automatización con schedule ────────────────────────
                var auto = new Automatizacion
                {
                    Nombre            = defAuto.Nombre ?? $"Auto {_lista.Count + 1}",
                    Descripcion       = defAuto.Descripcion ?? texto,
                    TipoProgramacion  = defAuto.TipoProgramacion ?? "manual",
                    HoraEjecucion     = defAuto.HoraEjecucion ?? "",
                    HoraInicio        = defAuto.HoraInicio ?? "",
                    HoraFin           = defAuto.HoraFin ?? "",
                    IntervaloMinutos  = defAuto.IntervaloMinutos > 0 ? defAuto.IntervaloMinutos : 60,
                    Activa            = true
                };

                // Crear carpeta de scripts propia
                string carpeta = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Automatizaciones", auto.Id);
                if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
                auto.CarpetaScripts = carpeta;

                // ── Crear nodos a partir del plan ────────────────────────────
                if (defAuto.Pasos == null || defAuto.Pasos.Count == 0)
                    throw new Exception("El planificador no generó pasos");

                for (int i = 0; i < defAuto.Pasos.Count; i++)
                {
                    var p = defAuto.Pasos[i];
                    TipoNodo tipo = p.Tipo?.ToLowerInvariant() switch
                    {
                        "disparador" => TipoNodo.Disparador,
                        "condicion"  => TipoNodo.Condicion,
                        "fin"        => TipoNodo.Fin,
                        _            => TipoNodo.Accion
                    };
                    auto.Nodos.Add(new NodoAutomatizacion
                    {
                        TipoNodo           = tipo,
                        Titulo             = p.Titulo ?? $"Paso {i + 1}",
                        Descripcion        = p.Descripcion ?? "",
                        InstruccionNatural = p.Instruccion ?? "",
                        OrdenEjecucion     = i,
                        NombreScript       = $"nodo_{i:D2}_{SanitizarNombre(p.Titulo ?? $"paso{i}")}.py",
                        CanvasX            = 70 + i * 245,
                        CanvasY            = 120
                    });
                }

                // Conectar nodos secuencialmente
                for (int i = 0; i < auto.Nodos.Count - 1; i++)
                    auto.Nodos[i].ConexionesSalida.Add(auto.Nodos[i + 1].Id);

                _lista.Add(auto);
                GuardarLista();
                SeleccionarAuto(auto);
                RefrescarLista();

                LogW($"\n✔ Plan creado: {auto.Nombre}\n", Emerald4);
                LogW($"  Programación: {FormatearSchedule(auto)}\n", TextMuted);
                LogW($"  Nodos: {auto.Nodos.Count}\n\n", TextMuted);

                // ═══════════════════════════════════════════════════════════════
                //  PASO 2: AGENTE GENERADOR — crea script Python por cada nodo
                //  LLM directo: devuelve SOLO código Python, se guarda a archivo
                // ═══════════════════════════════════════════════════════════════
                LogW("PASO 2: ⚡ Agente Generador — creando y probando scripts nodo por nodo…\n", WarnColor);
                EstadoIA("⚡ Generando y probando scripts…", TextMuted);

                // Construir resumen de todo el flujo para que cada script sepa su contexto
                string resumenFlujo = ConstruirResumenFlujo(auto);
                string? contextoAnteriorCreacion = null;

                for (int i = 0; i < auto.Nodos.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var nodo = auto.Nodos[i];
                    if (string.IsNullOrWhiteSpace(nodo.InstruccionNatural)) continue;

                    LogW($"\n  [{i + 1}/{auto.Nodos.Count}] {nodo.Titulo}\n", TextSub);

                    // Marcar visual
                    var ctrl = canvas.Nodos.FirstOrDefault(c => c.Datos.Id == nodo.Id);
                    ctrl?.ActualizarEstado(EstadoNodo.Ejecutando);

                    // Llamar al LLM directamente — sin ejecutar nada
                    AgentContext ctxGen = ctxBase.ConPromptPersonalizado(
                        PromptSistemaGenerador(auto, carpeta));
                    string codigoIA = await AIModelConector.ObtenerRespuestaLLMAsync(
                        PromptUsuarioGenerador(nodo, auto, i, resumenFlujo), ctxGen, ct);

                    // Extraer solo el código Python (puede venir envuelto en ```python...```)
                    string codigoLimpio = ExtraerCodigoPython(codigoIA);

                    // Guardar script en archivo propio
                    string rutaScript = Path.Combine(carpeta, nodo.NombreScript);
                    File.WriteAllText(rutaScript, codigoLimpio, Encoding.UTF8);
                    nodo.ScriptGenerado = codigoLimpio;

                    LogW($"  ✔ Script guardado: {nodo.NombreScript} ({codigoLimpio.Length} chars)\n", Emerald4);

                    // ═══════════════════════════════════════════════════════════
                    //  MODO TESTER: probar el script AHORA (hasta 3 intentos)
                    //  Si falla → IA autocorrige → reintenta (x3)
                    //  Si sigue fallando → panel interactivo al usuario
                    //  Si funciona → pasa el resultado al siguiente nodo
                    // ═══════════════════════════════════════════════════════════
                    LogW($"  🧪 Modo Tester — ejecutando nodo [{i + 1}]…\n", WarnColor);
                    bool nodoFuncional = await ProbarNodoConReintentos(
                        nodo, auto, i, carpeta, rutaScript, contextoAnteriorCreacion, ct);
                    if (nodoFuncional)
                        contextoAnteriorCreacion = nodo.UltimaRespuesta;
                }

                GuardarLista();
                LogW("\n✔ Todos los scripts generados y probados nodo por nodo\n\n", Emerald4);

                // ═══════════════════════════════════════════════════════════════
                //  PASO 3: PRUEBA DE EJECUCIÓN — ejecuta cada script real
                // ═══════════════════════════════════════════════════════════════
                LogW("PASO 3: 🧪 Prueba de ejecución…\n", WarnColor);
                EstadoIA("🧪 Ejecutando prueba…", TextMuted);
                await EjecutarPruebaCompleta(auto, ct);

                // ═══════════════════════════════════════════════════════════════
                //  PASO 4: ACTIVAR — la automatización queda programada
                // ═══════════════════════════════════════════════════════════════
                LogW($"\n✔ Automatización activa: {FormatearSchedule(auto)}\n", Emerald4);
                EstadoIA($"✔ Creada, probada y programada: {FormatearSchedule(auto)}", Emerald4);
                txtNLInput.Clear();
            }
            catch (OperationCanceledException)
            {
                EstadoIA("Cancelado.", WarnColor);
                LogW("\nCANCELADO\n", WarnColor);
            }
            catch (Exception ex)
            {
                EstadoIA($"Error: {ex.Message}", ErrorColor);
                LogW($"\n✖ Error: {ex.Message}\n", ErrorColor);
            }
            finally
            {
                btnCrearIA.Enabled = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MODIFICACIÓN CON LENGUAJE NATURAL
        //  Cuando ya hay una automatización seleccionada, la instrucción
        //  modifica el flujo existente: agregar nodos, cambiar, eliminar, etc.
        // ══════════════════════════════════════════════════════════════════════
        private async Task ModificarAutomatizacionConNL(string instruccion)
        {
            if (_autoActual == null) return;

            btnCrearIA.Enabled = false;
            AbrirLog();
            EstadoIA("🧠 Modificando automatización…", TextMuted);
            LogW($"══════ MODIFICANDO: {_autoActual.Nombre} ══════\n", Emerald4);
            LogW($"Instrucción: {instruccion}\n\n", TextSub);

            _cts?.Cancel();
            _cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var ct = _cts.Token;

            try
            {
                AgentContext ctxBase = await AgentContext.BuildAsync(
                    RutaTrabajo, Modelo, ApiKey, Agente,
                    soloChat: false, ClavesApis, ct);

                string resumen = ConstruirResumenFlujo(_autoActual);

                // Enviar al Agente Modificador: tiene contexto TOTAL + instrucción del usuario
                AgentContext ctxMod = ctxBase.ConPromptPersonalizado(PromptSistemaModificador());
                string respMod = await AIModelConector.ObtenerRespuestaLLMAsync(
                    PromptUsuarioModificador(instruccion, _autoActual, resumen), ctxMod, ct);

                LogW($"  Respuesta modificador: {respMod.Length} chars\n", TextSub);

                // Parsear la respuesta de modificación
                var cambios = ParsearModificacion(respMod);
                if (cambios == null) throw new Exception("No se pudo parsear la modificación");

                string carpeta = _autoActual.CarpetaScripts;
                if (string.IsNullOrEmpty(carpeta))
                    carpeta = AutomatizacionScheduler.ObtenerCarpetaAuto(_autoActual);

                // ── Aplicar eliminaciones ────────────────────────────────────
                if (cambios.Eliminar != null)
                {
                    foreach (var idxElim in cambios.Eliminar.OrderByDescending(x => x))
                    {
                        if (idxElim >= 0 && idxElim < _autoActual.Nodos.Count)
                        {
                            var nodo = _autoActual.Nodos[idxElim];
                            string rutaScript = Path.Combine(carpeta, nodo.NombreScript);
                            if (File.Exists(rutaScript)) try { File.Delete(rutaScript); } catch { }
                            LogW($"  🗑 Eliminado: {nodo.Titulo}\n", WarnColor);
                            _autoActual.Nodos.RemoveAt(idxElim);
                        }
                    }
                }

                // ── Aplicar modificaciones a nodos existentes ────────────────
                if (cambios.Modificar != null)
                {
                    foreach (var mod in cambios.Modificar)
                    {
                        if (mod.Paso >= 0 && mod.Paso < _autoActual.Nodos.Count)
                        {
                            var nodo = _autoActual.Nodos[mod.Paso];
                            if (!string.IsNullOrWhiteSpace(mod.Titulo)) nodo.Titulo = mod.Titulo;
                            if (!string.IsNullOrWhiteSpace(mod.Instruccion)) nodo.InstruccionNatural = mod.Instruccion;
                            if (!string.IsNullOrWhiteSpace(mod.Descripcion)) nodo.Descripcion = mod.Descripcion;
                            nodo.ScriptGenerado = ""; // Forzar regeneración
                            string rutaScript = Path.Combine(carpeta, nodo.NombreScript);
                            if (File.Exists(rutaScript)) try { File.Delete(rutaScript); } catch { }
                            LogW($"  ✏ Modificado: {nodo.Titulo}\n", TextSub);
                        }
                    }
                }

                // ── Aplicar nuevos nodos (con inserción en posición exacta) ──
                if (cambios.Agregar != null)
                {
                    foreach (var nuevo in cambios.Agregar)
                    {
                        int i = _autoActual.Nodos.Count;
                        TipoNodo tipo = nuevo.Tipo?.ToLowerInvariant() switch
                        {
                            "disparador" => TipoNodo.Disparador,
                            "condicion"  => TipoNodo.Condicion,
                            "fin"        => TipoNodo.Fin,
                            _            => TipoNodo.Accion
                        };

                        // Determinar nodo de referencia para inserción
                        NodoAutomatizacion? nodoRef = (nuevo.DespuesDePaso >= 0 &&
                            nuevo.DespuesDePaso < _autoActual.Nodos.Count)
                            ? _autoActual.Nodos[nuevo.DespuesDePaso] : null;

                        // Posición canvas: al lado del nodo de referencia o al final
                        int cx = nodoRef != null ? nodoRef.CanvasX + 265 : 70 + (i % 4) * 240;
                        int cy = nodoRef != null ? nodoRef.CanvasY + 20  : 100 + (i / 4) * 160;

                        var nodo = new NodoAutomatizacion
                        {
                            TipoNodo           = tipo,
                            Titulo             = nuevo.Titulo ?? $"Nuevo {i + 1}",
                            Descripcion        = nuevo.Descripcion ?? "",
                            InstruccionNatural = nuevo.Instruccion ?? "",
                            OrdenEjecucion     = i,
                            NombreScript       = $"nodo_{i:D2}_{SanitizarNombre(nuevo.Titulo ?? $"nuevo{i}")}.py",
                            CanvasX            = cx, CanvasY = cy
                        };

                        if (nodoRef != null && !nuevo.EsRama)
                        {
                            // MODO CADENA: nodo ref → nuevo → hijos anteriores del ref
                            var hijosAnteriores = nodoRef.ConexionesSalida.ToList();
                            nodoRef.ConexionesSalida.Clear();
                            nodoRef.ConexionesSalida.Add(nodo.Id);
                            nodo.ConexionesSalida.AddRange(hijosAnteriores);
                            LogW($"  + [{nodo.Titulo}] insertado en cadena después de [{nodoRef.Titulo}]\n", Emerald4);
                        }
                        else if (nodoRef != null && nuevo.EsRama)
                        {
                            // MODO RAMA PARALELA: nodo ref también apunta al nuevo
                            nodoRef.ConexionesSalida.Add(nodo.Id);
                            LogW($"  + [{nodo.Titulo}] rama paralela desde [{nodoRef.Titulo}]\n", Emerald4);
                        }
                        else
                        {
                            // AL FINAL: conectar desde el último nodo existente
                            var ultimo = _autoActual.Nodos.LastOrDefault();
                            if (ultimo != null) ultimo.ConexionesSalida.Add(nodo.Id);
                            LogW($"  + [{nodo.Titulo}] agregado al final\n", Emerald4);
                        }

                        _autoActual.Nodos.Add(nodo);
                    }
                }

                // ── Aplicar cambios de schedule ──────────────────────────────
                if (!string.IsNullOrWhiteSpace(cambios.TipoProgramacion))
                    _autoActual.TipoProgramacion = cambios.TipoProgramacion;
                if (!string.IsNullOrWhiteSpace(cambios.HoraEjecucion))
                    _autoActual.HoraEjecucion = cambios.HoraEjecucion;
                if (cambios.IntervaloMinutos > 0)
                    _autoActual.IntervaloMinutos = cambios.IntervaloMinutos;

                // Recalcular y refrescar
                RecalcularOrdenEjecucion();
                GuardarLista();
                SeleccionarAuto(_autoActual);
                RefrescarLista();

                // ── Generar scripts para nodos que los necesiten ─────────────
                var nodosNuevos = _autoActual.Nodos
                    .Where(n => string.IsNullOrWhiteSpace(n.ScriptGenerado)
                             && !string.IsNullOrWhiteSpace(n.InstruccionNatural))
                    .ToList();

                if (nodosNuevos.Count > 0)
                {
                    LogW($"\n⚡ Generando y probando scripts para {nodosNuevos.Count} nodo(s)…\n", WarnColor);
                    foreach (var nodo in nodosNuevos)
                    {
                        ct.ThrowIfCancellationRequested();
                        int idx = _autoActual.Nodos.IndexOf(nodo);
                        var ctrl = canvas.Nodos.FirstOrDefault(c => c.Datos.Id == nodo.Id);
                        ctrl?.ActualizarEstado(EstadoNodo.Ejecutando);

                        LogW($"  [{idx + 1}] {nodo.Titulo}\n", TextSub);
                        string codigo = await GenerarScriptViaNodo(nodo, _autoActual, idx, carpeta, ct);
                        string ruta = Path.Combine(carpeta, nodo.NombreScript);
                        File.WriteAllText(ruta, codigo, Encoding.UTF8);
                        nodo.ScriptGenerado = codigo;
                        LogW($"  ✔ Script: {nodo.NombreScript}\n", Emerald4);

                        // ── MODO TESTER: probar nodo ahora mismo ──
                        LogW($"  🧪 Modo Tester — ejecutando nodo [{idx + 1}]…\n", WarnColor);
                        await ProbarNodoConReintentos(nodo, _autoActual, idx, carpeta, ruta, null, ct);
                    }
                    GuardarLista();
                }

                EstadoIA($"✔ Automatización modificada ({cambios.Resumen ?? "OK"})", Emerald4);
                txtNLInput.Clear();
            }
            catch (OperationCanceledException)
            {
                EstadoIA("Cancelado.", WarnColor);
            }
            catch (Exception ex)
            {
                EstadoIA($"Error: {ex.Message}", ErrorColor);
                LogW($"\n✖ Error: {ex.Message}\n", ErrorColor);
            }
            finally
            {
                btnCrearIA.Enabled = true;
            }
        }

        // ── Prompt y parseo del Agente Modificador ──────────────────────────

        private static string PromptSistemaModificador() => @"
Eres el AGENTE MODIFICADOR de automatizaciones. Recibes una automatización existente
(con todos sus nodos, scripts, conexiones) y una instrucción del usuario para modificarla.

Devuelve JSON PURO (sin markdown, sin ```json) con esta estructura:
{
  ""resumen"": ""breve descripción del cambio"",
  ""tipoProgramacion"": """",
  ""horaEjecucion"": """",
  ""intervaloMinutos"": 0,
  ""eliminar"": [0, 2],
  ""modificar"": [
    {""paso"": 1, ""titulo"": ""nuevo titulo"", ""instruccion"": ""nueva instrucción"", ""descripcion"": ""nueva desc""}
  ],
  ""agregar"": [
    {""tipo"": ""Accion"", ""titulo"": ""Nuevo paso"", ""descripcion"": ""qué hace"",
     ""instruccion"": ""instrucción concreta"",
     ""despuesDePaso"": 1,
     ""esRama"": false}
  ]
}

REGLAS:
- ""eliminar"": array de ÍNDICES (base 0) de nodos a eliminar. [] si no se elimina nada
- ""modificar"": solo incluir campos que cambian. [] si no se modifica nada
- ""agregar"": campos OBLIGATORIOS tipo/titulo/instruccion; OPCIONALES:
    · ""despuesDePaso"": índice base-0 del nodo tras el cual insertar el nuevo.
      -1 (default) = al FINAL. Úsalo cuando el usuario diga 'después del nodo X' o 'entre X e Y'.
    · ""esRama"": false (default) = INSERTAR EN CADENA (el nuevo toma las conexiones salientes del ref).
                  true = RAMA PARALELA (el nuevo es salida adicional del ref, no toma sus conexiones).
      Úsalo cuando el usuario diga 'agrega una rama', 'en paralelo', 'también envía a', etc.
  Ejemplos:
    Usuario: 'agrega validación después del nodo 0' → despuesDePaso:0, esRama:false
    Usuario: 'agrega notificación Slack en paralelo desde el nodo 2' → despuesDePaso:2, esRama:true
    Usuario: 'agrega un nodo al final' → despuesDePaso:-1 (omitir o -1)
- tipoProgramacion/horaEjecucion/intervaloMinutos: solo si el usuario quiere cambiar el schedule
- Las instrucciones de nodos deben ser CONCRETAS y EJECUTABLES como Python
- SOLO devuelve el JSON";

        private static string PromptUsuarioModificador(
            string instruccion, Automatizacion auto, string resumen) =>
            $@"AUTOMATIZACIÓN ACTUAL:
Nombre: {auto.Nombre}
Schedule: {auto.TipoProgramacion} {auto.HoraEjecucion}

{resumen}

INSTRUCCIÓN DEL USUARIO:
{instruccion}

Devuelve el JSON de modificación.";

        private DefModificacion? ParsearModificacion(string contenido)
        {
            try
            {
                int ini = contenido.IndexOf('{');
                int fin = contenido.LastIndexOf('}');
                if (ini < 0 || fin <= ini) throw new Exception("Sin JSON");
                string json = contenido[ini..(fin + 1)];
                return System.Text.Json.JsonSerializer.Deserialize<DefModificacion>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                LogW($"⚠ Error parseando modificación: {ex.Message}\n", ErrorColor);
                return null;
            }
        }

        private class DefModificacion
        {
            public string? Resumen           { get; set; }
            public string? TipoProgramacion  { get; set; }
            public string? HoraEjecucion     { get; set; }
            public int     IntervaloMinutos  { get; set; }
            public List<int>? Eliminar       { get; set; }
            public List<ModNodo>? Modificar  { get; set; }
            public List<PasoDef>? Agregar    { get; set; }
        }
        private class ModNodo
        {
            public int     Paso        { get; set; }
            public string? Titulo      { get; set; }
            public string? Instruccion { get; set; }
            public string? Descripcion { get; set; }
        }

        /// <summary>
        /// Construye contexto TOTAL del flujo. Cada nodo sabe TODO:
        /// - Todos los nodos, sus scripts, rutas, instrucciones
        /// - Scripts ya generados (código completo si existe)
        /// - Resultados de ejecuciones anteriores
        /// - Conexiones entre nodos
        /// </summary>
        private static string ConstruirResumenFlujo(Automatizacion auto)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("  CONTEXTO COMPLETO DEL FLUJO DE AUTOMATIZACIÓN");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine($"Nombre: {auto.Nombre}");
            sb.AppendLine($"Descripción: {auto.Descripcion}");
            sb.AppendLine($"Carpeta: {auto.CarpetaScripts}");
            sb.AppendLine($"Programación: {auto.TipoProgramacion} {auto.HoraEjecucion}");
            sb.AppendLine($"Total nodos: {auto.Nodos.Count}");
            sb.AppendLine();

            foreach (var n in auto.Nodos.OrderBy(x => x.OrdenEjecucion))
            {
                sb.AppendLine($"  ┌── PASO {n.OrdenEjecucion + 1}: [{n.TipoNodo}] {n.Titulo} ──┐");
                sb.AppendLine($"  │ Script:  {n.NombreScript}");
                sb.AppendLine($"  │ Ruta:    {Path.Combine(auto.CarpetaScripts, n.NombreScript)}");
                sb.AppendLine($"  │ Tarea:   {n.InstruccionNatural}");

                // Conexiones de salida
                if (n.ConexionesSalida.Count > 0)
                {
                    var destinos = n.ConexionesSalida
                        .Select(id => auto.Nodos.FirstOrDefault(x => x.Id == id))
                        .Where(x => x != null)
                        .Select(x => x!.Titulo);
                    sb.AppendLine($"  │ Sale a:  {string.Join(", ", destinos)}");
                }

                // Script ya generado (resumen para contexto)
                if (!string.IsNullOrWhiteSpace(n.ScriptGenerado))
                {
                    string resumenScript = n.ScriptGenerado.Length > 300
                        ? n.ScriptGenerado[..300] + "..."
                        : n.ScriptGenerado;
                    sb.AppendLine($"  │ Código (ya generado):");
                    foreach (var linea in resumenScript.Split('\n').Take(15))
                        sb.AppendLine($"  │   {linea.TrimEnd()}");
                }

                // Resultado de última ejecución
                if (!string.IsNullOrWhiteSpace(n.UltimaRespuesta))
                    sb.AppendLine($"  │ Último resultado: {(n.UltimaRespuesta.Length > 150 ? n.UltimaRespuesta[..150] + "..." : n.UltimaRespuesta)}");

                sb.AppendLine($"  └────────────────────────────────────┘");
                sb.AppendLine();
            }

            sb.AppendLine("EJECUCIÓN: Los pasos corren EN SECUENCIA según OrdenEjecucion.");
            sb.AppendLine("COMUNICACIÓN: Cada script escribe en respuesta.txt (su carpeta).");
            sb.AppendLine("CADENA: El resultado se pasa al siguiente vía env var NODO_ANTERIOR_RESULTADO.");
            sb.AppendLine("ARCHIVOS: Los scripts pueden leer/escribir archivos en la carpeta compartida.");
            return sb.ToString();
        }

        private static string FormatearSchedule(Automatizacion a) => a.TipoProgramacion switch
        {
            "diaria"    => $"Diaria a las {a.HoraEjecucion}",
            "intervalo" => $"Cada {a.IntervaloMinutos} min" +
                           (a.HoraInicio != "" ? $" ({a.HoraInicio}–{a.HoraFin})" : ""),
            "unica"     => $"Una vez a las {a.HoraEjecucion}",
            "siempre"   => "Ejecución continua",
            _           => "Manual"
        };

        // ══════════════════════════════════════════════════════════════════════
        //  EJECUCIÓN (manual desde botón ▶ o desde scheduler)
        // ══════════════════════════════════════════════════════════════════════
        private async Task EjecutarAutomatizacionCompleta()
        {
            if (_autoActual == null || _ejecutando) return;
            if (!ValidarConfig()) return;
            await EjecutarPruebaCompleta(_autoActual, null);
        }

        /// <summary>
        /// Ejecuta la automatización respetando el grafo de dependencias.
        ///
        /// Motor: TaskCompletionSource por nodo.
        ///   • Cada nodo espera a que TODOS sus padres terminen (y sólo a ellos).
        ///   • En cuanto el último padre completa, el nodo hijo arranca sin esperar
        ///     a ningún otro hermano en curso.
        ///   • Fan-out (A → B, C, D): B, C, D arrancan juntos cuando A termina
        ///     y sus sub-flujos corren 100% en paralelo e independientes.
        ///   • Fan-in (B, C → D): D espera a que AMBOS B y C terminen.
        ///   • Errores en paralelo: auto-corrección con pasos visibles;
        ///     panel interactivo aparece después de que termina toda la ola.
        /// </summary>
        private async Task EjecutarPruebaCompleta(Automatizacion auto, CancellationToken? ctExtern)
        {
            if (_ejecutando) return;
            _ejecutando = true;
            btnEjecutar.Enabled = false;
            btnDetener.Enabled  = true;

            if (ctExtern == null)
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            }
            var ct = ctExtern ?? _cts!.Token;

            string carpeta = auto.CarpetaScripts;
            if (string.IsNullOrEmpty(carpeta))
                carpeta = AutomatizacionScheduler.ObtenerCarpetaAuto(auto);

            // ── Grafo de predecesores ───────────────────────────────────────
            var nodosById = auto.Nodos.ToDictionary(n => n.Id);
            var predMap   = auto.Nodos.ToDictionary(n => n.Id, _ => new List<string>());
            foreach (var n in auto.Nodos)
                foreach (var sucId in n.ConexionesSalida)
                    if (predMap.ContainsKey(sucId)) predMap[sucId].Add(n.Id);

            // TCS por nodo: señala que ese nodo terminó (bool = éxito)
            var tcs = auto.Nodos.ToDictionary(
                n => n.Id,
                _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

            // Resultados thread-safe (escritura concurrente de ramas paralelas)
            var resultados = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Errores pendientes de revisión interactiva (nodos con sinInteractivo=true)
            var erroresPendientes =
                new System.Collections.Concurrent.ConcurrentBag<(NodoAutomatizacion nodo, string ruta)>();

            // Contador de nodos ejecutándose ahora mismo (para detectar paralelismo)
            int nodosEnCurso = 0;

            bool todoOk = true;
            foreach (var c in canvas.Nodos) c.ActualizarEstado(EstadoNodo.Pendiente);

            // ── Log de cabecera + grafo visual ──────────────────────────────
            AbrirLog();
            var raices   = predMap.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
            var fanOuts  = nodosById.Values.Where(n => n.ConexionesSalida.Count > 1).ToList();
            var fanIns   = predMap.Where(kv => kv.Value.Count > 1).ToList();

            LogW($"\n╔══════════════════════════════════════════════════════════════════╗\n", Emerald4);
            LogW($"  ⚡ EJECUTANDO: {auto.Nombre}  ({auto.Nodos.Count} nodos)\n", Emerald4);
            if (fanOuts.Any())
                LogW($"  🔀 Fan-out: {string.Join(", ", fanOuts.Select(n => $"{n.Titulo}→[{n.ConexionesSalida.Count}]"))}\n",
                    Color.FromArgb(167, 243, 208));
            if (fanIns.Any())
                LogW($"  🔁 Fan-in : {string.Join(", ", fanIns.Select(kv => nodosById[kv.Key].Titulo))}\n",
                    Color.FromArgb(147, 197, 253));
            LogW($"╚══════════════════════════════════════════════════════════════════╝\n\n", Emerald4);

            _timerStreaming.Start();

            // ── Función que ejecuta un nodo tras esperar a sus padres ───────
            async Task EjecutarNodo(NodoAutomatizacion nodo)
            {
                try
                {
                    // 1. Esperar a que TODOS los predecesores señalicen completado
                    var preds = predMap[nodo.Id];
                    if (preds.Count > 0)
                        await Task.WhenAll(preds.Select(pid => tcs[pid].Task));

                    ct.ThrowIfCancellationRequested();

                    // 2. Detectar si hay otros nodos corriendo en paralelo
                    int enCurso = Interlocked.Increment(ref nodosEnCurso);
                    bool esParalelo = enCurso > 1;

                    // Log: indicar si este nodo arranca en paralelo o solo
                    string prefijo = esParalelo ? $"[{nodo.Titulo}] " : "";
                    if (esParalelo)
                    {
                        LogW($"  ⚡ {prefijo}arranca en paralelo\n",
                            Color.FromArgb(167, 243, 208));
                    }

                    // 3. Obtener contexto combinado de padres
                    var snap = resultados.ToDictionary(kv => kv.Key, kv => kv.Value);
                    string ctx  = ObtenerContextoCombinado(nodo.Id, predMap, snap, nodosById);

                    // 4. Asegurar script
                    int    idxN = auto.Nodos.IndexOf(nodo);
                    string ruta = await AsegurarScript(nodo, auto, idxN, carpeta, ct);

                    // 5. Ejecutar (con corrección automática visible)
                    bool ok = false;
                    if (!string.IsNullOrEmpty(ruta))
                        ok = await ProbarNodoConReintentos(
                            nodo, auto, idxN, carpeta, ruta, ctx, ct,
                            sinInteractivo: esParalelo);   // paralelo → auto-corrige sin bloquear UI

                    resultados[nodo.Id] = nodo.UltimaRespuesta ?? "";

                    // 6. Si falló en modo paralelo → encolar para revisión posterior
                    if (!ok && esParalelo && !string.IsNullOrEmpty(ruta))
                        erroresPendientes.Add((nodo, ruta));

                    if (!ok) Volatile.Write(ref todoOk, false);

                    Interlocked.Decrement(ref nodosEnCurso);
                }
                catch (OperationCanceledException)
                {
                    Volatile.Write(ref todoOk, false);
                }
                catch (Exception ex)
                {
                    LogW($"  ⚠ [{nodo.Titulo}] excepción inesperada: {ex.Message}\n", ErrorColor);
                    resultados[nodo.Id] = "";
                    Volatile.Write(ref todoOk, false);
                }
                finally
                {
                    // Señalizar siempre, aunque falle, para no bloquear nodos dependientes
                    tcs[nodo.Id].TrySetResult(resultados.TryGetValue(nodo.Id, out var r) && !string.IsNullOrEmpty(r));
                }
            }

            try
            {
                // ── Lanzar TODOS los nodos a la vez ────────────────────────
                // Cada uno espera a sus padres vía TCS — puro paralelismo guiado por dependencias
                var todasTareas = auto.Nodos.Select(EjecutarNodo).ToList();
                await Task.WhenAll(todasTareas);

                // ── Revisión interactiva de errores en paralelo (secuencial) ─
                if (!erroresPendientes.IsEmpty)
                {
                    LogW($"\n  ══════════════════════════════════════════════════════\n", WarnColor);
                    LogW($"  🔍 {erroresPendientes.Count} nodo(s) paralelo(s) necesitan revisión\n", WarnColor);
                    LogW($"  ══════════════════════════════════════════════════════\n\n", WarnColor);

                    foreach (var (nodo, ruta) in erroresPendientes)
                    {
                        int    idxN       = auto.Nodos.IndexOf(nodo);
                        string errorAct   = nodo.UltimaRespuesta ?? "Error desconocido";
                        string codigoAct  = File.Exists(ruta) ? File.ReadAllText(ruta, Encoding.UTF8) : "";
                        string ctxRetry   = ObtenerContextoCombinado(
                            nodo.Id, predMap, resultados.ToDictionary(kv => kv.Key, kv => kv.Value), nodosById);

                        // Reutilizar el panel interactivo + corrección con pasos
                        string? instruccion = await MostrarErrorYEsperarUsuario(
                            nodo, errorAct, codigoAct,
                            "Nodo falló durante ejecución paralela. ¿Instrucción para corregir?");

                        if (instruccion == null) continue; // saltar

                        var cOut  = Color.FromArgb(185, 195, 210);
                        var cErr  = Color.FromArgb(255, 110, 110);
                        var cSep  = Color.FromArgb(40, 255, 255, 255);
                        var cPrev = Color.FromArgb(100, 200, 155);

                        string? corregido = await AnalizarYCorregirConPasos(
                            nodo, auto, idxN, carpeta, codigoAct, errorAct,
                            string.IsNullOrWhiteSpace(instruccion) ? null : instruccion,
                            ct, cOut, cErr, cSep, cPrev);

                        if (corregido != null)
                        {
                            File.WriteAllText(ruta, corregido, Encoding.UTF8);
                            nodo.ScriptGenerado = corregido;
                            bool okRetry = await ProbarNodoConReintentos(
                                nodo, auto, idxN, carpeta, ruta, ctxRetry, ct);
                            resultados[nodo.Id] = nodo.UltimaRespuesta ?? "";
                            if (okRetry) Volatile.Write(ref todoOk, true);
                        }
                    }
                }

                auto.UltimoEstado    = todoOk ? "✔ Completado" : "⚠ Parcial";
                auto.UltimaEjecucion = DateTime.Now;
                LogW($"\n══ {auto.UltimoEstado} ══\n", todoOk ? Emerald4 : WarnColor);
                EstadoIA(auto.UltimoEstado, todoOk ? Emerald4 : WarnColor);

                // ── Notificación Telegram/Slack al finalizar ──────────────
                _ = NotificarFinAutomatizacionAsync(auto, todoOk, resultados);
            }
            catch (OperationCanceledException)
            {
                // Señalizar todos los TCS restantes para no dejar tareas huérfanas
                foreach (var t in tcs.Values) t.TrySetResult(false);
                auto.UltimoEstado = "Cancelado";
                LogW("\n── CANCELADO ──\n", WarnColor);
                EstadoIA("Cancelado.", WarnColor);
            }
            finally
            {
                _timerStreaming.Stop();
                _ejecutando = false;
                btnEjecutar.Enabled = true;
                btnDetener.Enabled  = false;
                GuardarLista();
                RefrescarCardActual();
            }
        }

        /// <summary>
        /// Envía una notificación de Telegram (y/o Slack si está disponible) al terminar
        /// una automatización — tanto en ejecución manual como programada.
        /// Lee la config de Telegram del mismo directorio de trabajo que FrmMandos.
        /// No lanza excepciones al caller.
        /// </summary>
        private async Task NotificarFinAutomatizacionAsync(
            Automatizacion auto, bool todoOk,
            System.Collections.Concurrent.ConcurrentDictionary<string, string> resultados)
        {
            try
            {
                // Leer configuración Telegram
                var telegramList = JsonManager.Leer<TelegramChat>(
                    RutasProyecto.ObtenerRutaListTelegram(RutaTrabajo));
                var telegram = telegramList.Count > 0 ? telegramList[0] : null;

                bool tieneTelegram = telegram != null &&
                                     telegram.ChatId != 0 &&
                                     !string.IsNullOrWhiteSpace(telegram.Apikey);

                if (!tieneTelegram) return; // sin config → salir silenciosamente

                // Construir resumen del resultado
                string icono   = todoOk ? "✅" : "⚠️";
                string estado  = todoOk ? "Completada" : "Completada con errores";
                var sb = new StringBuilder();
                sb.AppendLine($"{icono} <b>Automatización {estado}</b>");
                sb.AppendLine($"📋 <b>{HtmlEncode(auto.Nombre)}</b>");
                sb.AppendLine($"🕐 {auto.UltimaEjecucion:HH:mm:ss}  ·  {auto.Nodos.Count} nodo(s)");

                // Resumir resultados de nodos (máx 5 para no saturar)
                var nodosConResultado = auto.Nodos
                    .Where(n => !string.IsNullOrWhiteSpace(n.UltimaRespuesta))
                    .Take(5).ToList();

                if (nodosConResultado.Count > 0)
                {
                    sb.AppendLine();
                    foreach (var n in nodosConResultado)
                    {
                        string respCorta = n.UltimaRespuesta!.Replace("\n", " ").Trim();
                        if (respCorta.Length > 120) respCorta = respCorta[..120] + "…";
                        string nodoIcon = n.UltimaRespuesta!.StartsWith("Error") ? "❌" : "✔";
                        sb.AppendLine($"{nodoIcon} <b>{HtmlEncode(n.Titulo)}</b>: {HtmlEncode(respCorta)}");
                    }
                    if (auto.Nodos.Count > 5)
                        sb.AppendLine($"  … y {auto.Nodos.Count - 5} nodo(s) más");
                }

                await TelegramSender.EnviarMensajeAsync(
                    telegram!.Apikey, telegram.ChatId, sb.ToString());
            }
            catch (Exception ex)
            {
                // Error de red u otro — no interrumpir el flujo principal
                LogW($"  ⚠ Notificación Telegram: {ex.Message}\n",
                    Color.FromArgb(80, 251, 191, 36));
            }
        }

        /// <summary>Escapa los caracteres especiales HTML para mensajes Telegram HTML-mode.</summary>
        private static string HtmlEncode(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>Asegura que el script del nodo existe; si no, lo genera via LLM. Devuelve la ruta o "".</summary>
        private async Task<string> AsegurarScript(NodoAutomatizacion nodo, Automatizacion auto,
            int idx, string carpeta, CancellationToken ct)
        {
            string ruta = Path.Combine(carpeta, nodo.NombreScript);
            if (!File.Exists(ruta) && !string.IsNullOrWhiteSpace(nodo.InstruccionNatural))
            {
                LogW($"  ⚙ Generando script: {nodo.Titulo}\n", WarnColor);
                string codigo = await GenerarScriptViaNodo(nodo, auto, idx, carpeta, ct);
                File.WriteAllText(ruta, codigo, Encoding.UTF8);
                nodo.ScriptGenerado = codigo;
            }
            if (!File.Exists(ruta))
            {
                LogW($"  ⚠ Sin script: {nodo.Titulo} — saltando\n", WarnColor);
                return "";
            }
            return ruta;
        }

        /// <summary>
        /// Combina los resultados de todos los predecesores de un nodo.
        /// — 0 predecesores → "" (primer nodo del flujo)
        /// — 1 predecesor   → su resultado directo
        /// — N predecesores → concatenación etiquetada (fan-in)
        /// </summary>
        private static string ObtenerContextoCombinado(
            string nodoId,
            Dictionary<string, List<string>> predMap,
            Dictionary<string, string> resultados,
            Dictionary<string, NodoAutomatizacion> nodosById)
        {
            var preds = predMap.GetValueOrDefault(nodoId) ?? new List<string>();
            if (preds.Count == 0) return "";
            if (preds.Count == 1) return resultados.GetValueOrDefault(preds[0], "");

            var sb = new StringBuilder();
            foreach (var pid in preds)
            {
                string res = resultados.GetValueOrDefault(pid, "");
                if (string.IsNullOrWhiteSpace(res)) continue;
                string titulo = nodosById.TryGetValue(pid, out var n) ? n.Titulo : pid[..8];
                sb.AppendLine($"=== Resultado de: {titulo} ===");
                sb.AppendLine(res);
            }
            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EJECUCIÓN DIRECTA DE UN SCRIPT .PY (sin pasar por AIModelConector)
        // ══════════════════════════════════════════════════════════════════════
        private static async Task<string> EjecutarScriptDirecto(
            string rutaScript, string carpeta, string? contextoAnterior,
            CancellationToken ct, Action<string>? onLinea = null)
        {
            string respTxt = Path.Combine(carpeta, "respuesta.txt");
            try { File.WriteAllText(respTxt, "", Encoding.UTF8); } catch { }

            var psi = new ProcessStartInfo
            {
                FileName               = "python",
                Arguments              = $"\"{rutaScript}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                WorkingDirectory       = carpeta,
                // ── UTF-8 en stdout/stderr para evitar UnicodeEncodeError en cp1252 ──
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8
            };
            // PYTHONIOENCODING=utf-8 fuerza a Python a usar UTF-8 para print()
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            psi.EnvironmentVariables["PYTHONUTF8"]       = "1";
            if (!string.IsNullOrWhiteSpace(contextoAnterior))
                psi.EnvironmentVariables["NODO_ANTERIOR_RESULTADO"] = contextoAnterior;

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                sbOut.AppendLine(e.Data);
                onLinea?.Invoke(e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                sbErr.AppendLine(e.Data);
                onLinea?.Invoke($"[ERR] {e.Data}");
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync(ct);

            string resultado = "";
            if (File.Exists(respTxt))
                resultado = File.ReadAllText(respTxt, Encoding.UTF8).Trim();
            if (string.IsNullOrWhiteSpace(resultado))
                resultado = sbOut.ToString().Trim();

            if (proc.ExitCode != 0 && sbErr.Length > 0)
                throw new Exception($"Exit {proc.ExitCode}: {Truncar(sbErr.ToString(), 300)}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PROMPTS — AGENTE PLANIFICADOR (system + user, LLM directo)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Prompt de sistema del Agente Planificador. Define su rol y formato de salida.</summary>
        private static string PromptSistemaPlanificador() => @"
Eres el AGENTE PLANIFICADOR de un sistema de automatización.

TU ÚNICO TRABAJO: recibir una instrucción en lenguaje natural y devolver un JSON
que defina la automatización completa: programación horaria + lista de pasos atómicos.

FORMATO DE SALIDA — JSON PURO (sin markdown, sin ```json, sin texto adicional):
{
  ""nombre"": ""nombre corto"",
  ""descripcion"": ""qué hace la automatización"",
  ""tipoProgramacion"": ""diaria"",
  ""horaEjecucion"": ""08:00"",
  ""horaInicio"": """",
  ""horaFin"": """",
  ""intervaloMinutos"": 0,
  ""pasos"": [
    {""tipo"":""Accion"", ""titulo"":""Nombre corto"", ""descripcion"":""Qué hace"", ""instruccion"":""Instrucción CONCRETA para generar un script Python funcional""},
    {""tipo"":""Accion"", ""titulo"":""Nombre corto"", ""descripcion"":""Qué hace"", ""instruccion"":""Instrucción CONCRETA""},
    {""tipo"":""Fin"", ""titulo"":""Fin"", ""descripcion"":""Completado"", ""instruccion"":""""}
  ]
}

REGLAS DE PROGRAMACIÓN:
- tipoProgramacion puede ser: ""manual"", ""diaria"", ""intervalo"", ""unica"", ""siempre""
  · ""a las 8am"" / ""todos los días a las 20:00"" → ""diaria"" + horaEjecucion=""08:00"" / ""20:00""
  · ""cada 30 minutos"" / ""cada hora"" → ""intervalo"" + intervaloMinutos=30 / 60
  · ""de 8 a 9 pm cada 15 min"" → ""intervalo"" + horaInicio=""20:00"" + horaFin=""21:00"" + intervaloMinutos=15
  · ""una vez a las 5pm"" → ""unica"" + horaEjecucion=""17:00""
  · ""siempre"" / ""continuamente"" → ""siempre""
  · Si NO especifica cuándo → ""manual""

REGLAS DE PLANIFICACIÓN:
- Divide en pasos ATÓMICOS (2-6 pasos), cada uno ejecutable como script Python independiente
- Cada instrucción debe ser PRECISA: incluir emails, URLs, servicios, formatos específicos
- Los pasos se ejecutan en SECUENCIA: cada paso puede recibir el resultado del anterior
- El paso final tipo ""Fin"" siempre va al último
- Piensa en dependencias: ¿qué necesita cada paso del anterior?

SOLO devuelve el JSON. Nada más.";

        /// <summary>Prompt de usuario para el planificador — contiene la instrucción NL.</summary>
        private static string PromptUsuarioPlanificador(string instruccion) =>
            $"Planifica esta automatización:\n\n\"{instruccion}\"";

        // ══════════════════════════════════════════════════════════════════════
        //  PROMPTS — AGENTE GENERADOR (system + user, LLM directo)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Prompt de sistema del Agente Generador. Define que debe devolver SOLO código Python.
        /// Incluye la ruta de la carpeta, credenciales completas y contexto del flujo.
        /// </summary>
        private string PromptSistemaGenerador(Automatizacion auto, string carpeta)
        {
            // Construir listado completo de credenciales para que el LLM sepa qué usar
            string credencialesDetalle = ConstruirDetalleCredenciales();

            string rutaApis = RutasProyecto.ObtenerRutaListApis();
            return $@"
Eres el AGENTE GENERADOR DE SCRIPTS de un sistema de automatización.

TU ÚNICO TRABAJO: recibir la descripción de un paso y devolver código Python
COMPLETO, FUNCIONAL y AUTÓNOMO que lo ejecute.

CARPETA DE TRABAJO: {carpeta}
ARCHIVO DE CREDENCIALES: {rutaApis}

═══════════════════════════════════════════════════
  CREDENCIALES DISPONIBLES (ListApis.json)
═══════════════════════════════════════════════════
{credencialesDetalle}

CÓMO USAR CREDENCIALES EN TU SCRIPT:
import json
APIS_PATH = r'{rutaApis}'
with open(APIS_PATH, 'r', encoding='utf-8') as f:
    apis = json.load(f)
# Buscar por nombre (case-insensitive):
api = next((a for a in apis if 'nombre_servicio' in a['Nombre'].lower()), None)
key = api['key'] if api else ''

REGLAS ABSOLUTAS:
1. Devuelve SOLO código Python — sin texto, sin markdown, sin ```python, sin explicaciones
2. Siempre define las rutas así al inicio:
      import os
      SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
      RESP_PATH  = os.path.join(SCRIPT_DIR, 'respuesta.txt')
3. SIEMPRE escribe el resultado final en respuesta.txt Y haz print() del mismo.
   El contenido de respuesta.txt es lo que el siguiente nodo usa como entrada.
   Si la tarea no produce un valor (ej: enviar email), escribe la confirmación:
      with open(RESP_PATH, 'w', encoding='utf-8') as f:
          f.write('OK: email enviado a destino@ejemplo.com')
      print('OK: email enviado a destino@ejemplo.com')
4. Si necesitas el resultado del paso anterior:
      resultado_anterior = os.environ.get('NODO_ANTERIOR_RESULTADO', '')
5. USA las credenciales de ListApis.json — NUNCA inventes claves ni las hardcodees
6. Instala dependencias automáticamente antes de importarlas:
      import subprocess, sys
      subprocess.check_call([sys.executable, '-m', 'pip', 'install', 'paquete'], stdout=subprocess.DEVNULL)
7. NO uses input() ni nada interactivo
8. Maneja errores así — escribe el error en respuesta.txt (NO silencies con pass):
      except Exception as e:
          msg = f'Error: {{e}}'
          with open(RESP_PATH, 'w', encoding='utf-8') as f: f.write(msg)
          print(msg)
          raise
9. El script debe poder ejecutarse MÚLTIPLES VECES sin efectos secundarios (idempotente)
10. Usa encoding UTF-8 en toda lectura/escritura de archivos

IMPORTANTE: respuesta.txt NUNCA puede quedar vacía después de que el script termine.

SOLO código Python. Primera línea: import";
        }

        /// <summary>
        /// Construye un string detallado con todas las credenciales disponibles
        /// (nombre + descripción, SIN exponer las keys al prompt).
        /// El LLM sabe qué servicios hay y los lee del JSON en runtime.
        /// </summary>
        private string ConstruirDetalleCredenciales()
        {
            if (_listaApis == null || _listaApis.Count == 0)
                return "  (Sin credenciales configuradas)";

            var sb = new StringBuilder();
            foreach (var api in _listaApis)
            {
                sb.AppendLine($"  • {api.Nombre} — {api.Descripcion ?? "sin descripción"}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Prompt de usuario para generar un script de un nodo específico.
        /// Incluye contexto completo de nodos anteriores (rutas, scripts, qué producen).
        /// </summary>
        private static string PromptUsuarioGenerador(
            NodoAutomatizacion nodo, Automatizacion auto, int idx, string resumenFlujo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Genera el script Python para este paso:");
            sb.AppendLine();
            sb.AppendLine($"AUTOMATIZACIÓN: {auto.Nombre}");
            sb.AppendLine($"PASO {idx + 1} de {auto.Nodos.Count}: {nodo.Titulo}");
            sb.AppendLine($"TAREA: {nodo.InstruccionNatural}");
            sb.AppendLine($"DESCRIPCIÓN: {nodo.Descripcion}");
            sb.AppendLine($"MI SCRIPT: {nodo.NombreScript}");
            sb.AppendLine($"MI RUTA: {Path.Combine(auto.CarpetaScripts, nodo.NombreScript)}");
            sb.AppendLine();

            // Contexto detallado de nodos anteriores
            if (idx > 0)
            {
                sb.AppendLine("═══ NODOS ANTERIORES (ya ejecutados antes que yo) ═══");
                var anteriores = auto.Nodos
                    .Where(n => n.OrdenEjecucion < idx)
                    .OrderBy(n => n.OrdenEjecucion).ToList();

                foreach (var prev in anteriores)
                {
                    sb.AppendLine($"  PASO {prev.OrdenEjecucion + 1}: {prev.Titulo}");
                    sb.AppendLine($"    Script: {prev.NombreScript}");
                    sb.AppendLine($"    Ruta:   {Path.Combine(auto.CarpetaScripts, prev.NombreScript)}");
                    sb.AppendLine($"    Hace:   {prev.InstruccionNatural}");
                    sb.AppendLine($"    Produce: escribe resultado en respuesta.txt de su carpeta");
                }

                var nodoAnterior = anteriores.Last();
                sb.AppendLine();
                sb.AppendLine($"IMPORTANTE: El paso inmediato anterior ({nodoAnterior.Titulo}) te pasa su resultado");
                sb.AppendLine($"  vía os.environ['NODO_ANTERIOR_RESULTADO']. Úsalo como input de tu tarea.");
                sb.AppendLine($"  El anterior hizo: {nodoAnterior.InstruccionNatural}");
            }
            else
            {
                sb.AppendLine("NOTA: Este es el PRIMER paso. No hay resultado anterior.");
            }

            // Contexto de nodos posteriores (para que sepa qué debe producir)
            var posteriores = auto.Nodos
                .Where(n => n.OrdenEjecucion > idx && n.TipoNodo != TipoNodo.Fin)
                .OrderBy(n => n.OrdenEjecucion).ToList();
            if (posteriores.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("═══ NODOS POSTERIORES (recibirán mi resultado) ═══");
                foreach (var next in posteriores)
                    sb.AppendLine($"  PASO {next.OrdenEjecucion + 1}: {next.Titulo} — {next.InstruccionNatural}");
                sb.AppendLine();
                sb.AppendLine($"Tu resultado en respuesta.txt será el input del paso {posteriores.First().OrdenEjecucion + 1}.");
                sb.AppendLine("Asegúrate de escribir algo útil y estructurado que el siguiente paso pueda usar.");
            }

            sb.AppendLine();
            sb.AppendLine(resumenFlujo);
            sb.AppendLine();
            sb.AppendLine("Genera el script Python completo y funcional.");

            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EXTRACCIÓN DE CÓDIGO PYTHON (limpia la respuesta del LLM)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Extrae código Python limpio de la respuesta del LLM.
        /// Maneja: ```python...```, ```...```, o código directo.
        /// </summary>
        private static string ExtraerCodigoPython(string respuesta)
        {
            if (string.IsNullOrWhiteSpace(respuesta)) return "";

            string r = respuesta.Trim();

            // Caso 1: viene envuelto en ```python ... ```
            int ini = r.IndexOf("```python", StringComparison.OrdinalIgnoreCase);
            if (ini >= 0)
            {
                int start = r.IndexOf('\n', ini);
                if (start < 0) start = ini + 9;
                else start++;
                int end = r.IndexOf("```", start);
                if (end > start) return r[start..end].Trim();
            }

            // Caso 2: viene envuelto en ``` ... ```
            ini = r.IndexOf("```");
            if (ini >= 0)
            {
                int start = r.IndexOf('\n', ini);
                if (start < 0) start = ini + 3;
                else start++;
                int end = r.IndexOf("```", start);
                if (end > start) return r[start..end].Trim();
            }

            // Caso 3: el LLM devolvió código directo (empieza con import, #, from, etc.)
            return r;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PARSEO DE RESPUESTA IA
        // ══════════════════════════════════════════════════════════════════════
        private DefinicionAuto? ParsearDefinicionAuto(string contenido)
        {
            try
            {
                int ini = contenido.IndexOf('{');
                int fin = contenido.LastIndexOf('}');
                if (ini < 0 || fin <= ini) throw new Exception("Sin JSON");
                string json = contenido[ini..(fin + 1)];
                var def = System.Text.Json.JsonSerializer.Deserialize<DefinicionAuto>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return def;
            }
            catch (Exception ex)
            {
                LogW($"⚠ Error parseando: {ex.Message}\n", ErrorColor);
                LogW($"Contenido: {Truncar(contenido, 500)}\n", TextMuted);
                return null;
            }
        }

        private class DefinicionAuto
        {
            public string? Nombre            { get; set; }
            public string? Descripcion       { get; set; }
            public string? TipoProgramacion  { get; set; }
            public string? HoraEjecucion     { get; set; }
            public string? HoraInicio        { get; set; }
            public string? HoraFin           { get; set; }
            public int     IntervaloMinutos  { get; set; }
            public List<PasoDef>? Pasos      { get; set; }
        }
        private class PasoDef
        {
            public string? Tipo          { get; set; }
            public string? Titulo        { get; set; }
            public string? Descripcion   { get; set; }
            public string? Instruccion   { get; set; }
            /// <summary>Índice base-0 del nodo tras el cual insertar. -1 = al final (default).</summary>
            public int     DespuesDePaso { get; set; } = -1;
            /// <summary>true = rama paralela (no toma conexiones del ref). false = insertar en cadena.</summary>
            public bool    EsRama        { get; set; } = false;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EDITOR DE NODO
        // ══════════════════════════════════════════════════════════════════════
        private void SeleccionarNodoEditar(NodoVisualControl ctrl)
        {
            _nodoEditando            = ctrl;
            edtTitulo.Text           = ctrl.Datos.Titulo;
            edtDescripcion.Text      = ctrl.Datos.Descripcion;
            edtInstruccion.Text      = ctrl.Datos.InstruccionNatural;
            cmbTipoNodo.SelectedIndex= Math.Max(0, (int)ctrl.Datos.TipoNodo);
            lblScriptStatus.Text     = !string.IsNullOrWhiteSpace(ctrl.Datos.NombreScript) && File.Exists(
                Path.Combine(_autoActual?.CarpetaScripts ?? "", ctrl.Datos.NombreScript))
                ? $"✔ {ctrl.Datos.NombreScript}" : "Sin script";
            lblScriptStatus.ForeColor = lblScriptStatus.Text.StartsWith("✔") ? Emerald4 : TextMuted;
            lblResultadoNodo.Text    = !string.IsNullOrWhiteSpace(ctrl.Datos.UltimaRespuesta)
                ? "Resultado:\n" + Truncar(ctrl.Datos.UltimaRespuesta, 300) : "";

            // Cargar conexiones (enlazar/desenlazar)
            CargarConexionesEnEditor(ctrl.Datos);

            pnlEditor.Visible = true;
        }

        private void GuardarNodo()
        {
            if (_nodoEditando == null) return;
            _nodoEditando.Datos.Titulo             = edtTitulo.Text.Trim();
            _nodoEditando.Datos.Descripcion        = edtDescripcion.Text.Trim();
            _nodoEditando.Datos.InstruccionNatural = edtInstruccion.Text.Trim();
            _nodoEditando.Datos.TipoNodo           = (TipoNodo)cmbTipoNodo.SelectedIndex;

            // Actualizar nombre del script si cambió el título
            if (_autoActual != null)
            {
                int idx = _autoActual.Nodos.IndexOf(_nodoEditando.Datos);
                if (idx >= 0 && string.IsNullOrWhiteSpace(_nodoEditando.Datos.NombreScript))
                    _nodoEditando.Datos.NombreScript = $"nodo_{idx:D2}_{SanitizarNombre(_nodoEditando.Datos.Titulo)}.py";
            }

            _nodoEditando.Invalidate();
            GuardarLista();
            EstadoIA("✔ Nodo actualizado", Emerald4);
        }

        /// <summary>
        /// Cuando se crea una conexión manual en el canvas, recalcula el orden de ejecución
        /// de los nodos según la topología de conexiones.
        /// </summary>
        private void OnConexionCreada(NodoVisualControl origen, NodoVisualControl destino)
        {
            if (_autoActual == null) return;

            // Recalcular orden de ejecución basado en conexiones
            RecalcularOrdenEjecucion();
            GuardarLista();

            LogW($"  🔗 Conexión: {origen.Datos.Titulo} → {destino.Datos.Titulo}\n", Emerald4);
            EstadoIA($"🔗 {origen.Datos.Titulo} → {destino.Datos.Titulo}", Emerald4);
        }

        /// <summary>
        /// Recalcula OrdenEjecucion de todos los nodos basándose en las conexiones.
        /// Algoritmo: BFS desde nodos sin entradas (raíces).
        /// </summary>
        private void RecalcularOrdenEjecucion()
        {
            if (_autoActual == null) return;
            var nodos = _autoActual.Nodos;
            if (nodos.Count == 0) return;

            // Encontrar todos los IDs que son destino de alguna conexión
            var idsConEntrada = new HashSet<string>(
                nodos.SelectMany(n => n.ConexionesSalida));

            // Raíces = nodos que NO son destino de nadie
            var raices = nodos.Where(n => !idsConEntrada.Contains(n.Id)).ToList();
            if (raices.Count == 0) raices.Add(nodos[0]); // fallback

            // BFS para asignar orden
            var visitados = new HashSet<string>();
            var cola = new Queue<NodoAutomatizacion>(raices);
            int orden = 0;

            while (cola.Count > 0)
            {
                var actual = cola.Dequeue();
                if (!visitados.Add(actual.Id)) continue;
                actual.OrdenEjecucion = orden++;

                foreach (var idSalida in actual.ConexionesSalida)
                {
                    var siguiente = nodos.FirstOrDefault(n => n.Id == idSalida);
                    if (siguiente != null && !visitados.Contains(siguiente.Id))
                        cola.Enqueue(siguiente);
                }
            }

            // Nodos huérfanos (sin conexiones) van al final
            foreach (var n in nodos.Where(n => !visitados.Contains(n.Id)))
                n.OrdenEjecucion = orden++;
        }

        private async Task GenerarScriptNodo()
        {
            if (_nodoEditando == null || _autoActual == null) return;
            if (!ValidarConfig()) return;
            string instr = edtInstruccion.Text.Trim();
            if (string.IsNullOrWhiteSpace(instr)) { EstadoIA("Escribe instrucción.", ErrorColor); return; }

            btnGenScript.Enabled = false;
            lblScriptStatus.Text = "⏳…"; lblScriptStatus.ForeColor = TextMuted;
            AbrirLog();

            _cts?.Cancel();
            _cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var ct = _cts.Token;

            try
            {
                int idx = _autoActual.Nodos.IndexOf(_nodoEditando.Datos);
                string carpeta = AutomatizacionScheduler.ObtenerCarpetaAuto(_autoActual);

                if (string.IsNullOrEmpty(_nodoEditando.Datos.NombreScript))
                    _nodoEditando.Datos.NombreScript = $"nodo_{idx:D2}_{SanitizarNombre(_nodoEditando.Datos.Titulo)}.py";

                // Llamar LLM directo — generar script
                LogW($"\n⚡ Generando script: {_nodoEditando.Datos.Titulo}\n", WarnColor);
                string codigo = await GenerarScriptViaNodo(_nodoEditando.Datos, _autoActual, idx, carpeta, ct);

                string ruta = Path.Combine(carpeta, _nodoEditando.Datos.NombreScript);
                File.WriteAllText(ruta, codigo, Encoding.UTF8);
                _nodoEditando.Datos.ScriptGenerado = codigo;

                lblScriptStatus.Text = $"✔ {_nodoEditando.Datos.NombreScript}";
                lblScriptStatus.ForeColor = Emerald4;

                // Prueba con reintento (hasta 2 correcciones)
                const int MAX_RETRY = 2;
                bool exito = false;

                for (int intento = 0; intento <= MAX_RETRY; intento++)
                {
                    ct.ThrowIfCancellationRequested();
                    LogW($"🧪 Probando script ({intento + 1}/{MAX_RETRY + 1})…\n", WarnColor);

                    try
                    {
                        string resultado = await EjecutarScriptDirecto(ruta, carpeta, null, ct,
                            linea => LogAppend("  " + linea));
                        _nodoEditando.Datos.UltimaRespuesta = resultado;
                        lblResultadoNodo.Text = "Resultado:\n" + Truncar(resultado, 300);
                        _nodoEditando.ActualizarEstado(EstadoNodo.Completado);
                        LogW($"✔ Resultado: {Truncar(resultado, 200)}\n", Emerald4);
                        EstadoIA("✔ Script generado, guardado y probado", Emerald4);
                        exito = true;
                        break;
                    }
                    catch (Exception exRun)
                    {
                        if (intento < MAX_RETRY)
                        {
                            LogW($"⚠ Error: {Truncar(exRun.Message, 200)}\n", WarnColor);
                            LogW($"🔄 Corrigiendo script ({intento + 1}/{MAX_RETRY})…\n", WarnColor);
                            EstadoIA($"🔄 Corrigiendo… ({intento + 1}/{MAX_RETRY})", WarnColor);

                            string codigoActual = File.ReadAllText(ruta, Encoding.UTF8);
                            string corregido = await CorregirScriptConIA(
                                _nodoEditando.Datos, _autoActual, idx, carpeta,
                                codigoActual, exRun.Message, ct);
                            File.WriteAllText(ruta, corregido, Encoding.UTF8);
                            _nodoEditando.Datos.ScriptGenerado = corregido;
                            LogW($"✔ Script corregido, reintentando…\n", Emerald4);
                        }
                        else
                        {
                            LogW($"✖ Error final: {exRun.Message}\n", ErrorColor);
                            _nodoEditando.Datos.UltimaRespuesta = $"Error: {exRun.Message}";
                            _nodoEditando.ActualizarEstado(EstadoNodo.Error);
                            lblScriptStatus.Text = "⚠ Script con error";
                            lblScriptStatus.ForeColor = WarnColor;
                        }
                    }
                }

                GuardarLista();
                if (!exito) EstadoIA("⚠ Script generado pero con errores en prueba", WarnColor);
            }
            catch (OperationCanceledException)
            {
                EstadoIA("Cancelado.", WarnColor);
            }
            catch (Exception ex)
            {
                lblScriptStatus.Text = "✖ Error";
                lblScriptStatus.ForeColor = ErrorColor;
                _nodoEditando.ActualizarEstado(EstadoNodo.Error);
                LogW($"✖ {ex.Message}\n", ErrorColor);
            }
            finally { btnGenScript.Enabled = true; }
        }

        /// <summary>
        /// Genera un script Python para un nodo usando LLM directo (sin ejecutar nada).
        /// Usado tanto por BtnCrearIA_Click como por GenerarScriptNodo y EjecutarPruebaCompleta.
        /// </summary>
        private async Task<string> GenerarScriptViaNodo(
            NodoAutomatizacion nodo, Automatizacion auto, int idx,
            string carpeta, CancellationToken ct)
        {
            AgentContext ctxBase = await AgentContext.BuildAsync(
                RutaTrabajo, Modelo, ApiKey, Agente,
                soloChat: false, ClavesApis, ct);

            AgentContext ctxGen = ctxBase.ConPromptPersonalizado(
                PromptSistemaGenerador(auto, carpeta));

            string resumen = ConstruirResumenFlujo(auto);
            string resp = await AIModelConector.ObtenerRespuestaLLMAsync(
                PromptUsuarioGenerador(nodo, auto, idx, resumen), ctxGen, ct);

            return ExtraerCodigoPython(resp);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CORRECCIÓN CON PASOS VISIBLES — estilo Claude Code
        //  Muestra en el log cada fase: leer código → analizar error →
        //  generar fix → preview de cambios. Colores pasados como parámetro
        //  para usar la misma paleta que el tester que lo invoca.
        // ══════════════════════════════════════════════════════════════════════
        private async Task<string?> AnalizarYCorregirConPasos(
            NodoAutomatizacion nodo, Automatizacion auto, int idx,
            string carpeta, string codigoActual, string errorMsg,
            string? instruccionUsuario,          // null = solo IA sin pista del usuario
            CancellationToken ct,
            Color cOut, Color cErr, Color cSep, Color cPrev)
        {
            var cAnal = Color.FromArgb(147, 197, 253);   // azul — análisis
            var cFix  = Color.FromArgb(252, 211, 77);    // amarillo — corrección
            var cInfo = Color.FromArgb(167, 243, 208);   // verde suave — contexto usuario

            var lineas   = codigoActual.Split('\n');
            int totalLns = lineas.Length;

            // ── PASO 1: Leer el código ───────────────────────────────────────
            LogW($"  📖 Leyendo script  ({totalLns} líneas)…\n", cAnal);
            await Task.Delay(60, ct);

            // Mostrar importaciones y primeras líneas significativas
            var previewLineas = lineas
                .Select((l, i) => (linea: l, num: i + 1))
                .Where(x => x.linea.TrimStart().StartsWith("import ") ||
                            x.linea.TrimStart().StartsWith("from ")   ||
                            x.linea.TrimStart().StartsWith("def ")    ||
                            x.linea.TrimStart().StartsWith("class ")  ||
                            x.num <= 6)
                .Take(10)
                .ToList();
            foreach (var (linea, num) in previewLineas)
                LogW($"  │ {num,3}: {linea}\n", Color.FromArgb(100, 180, 180, 180));
            if (totalLns > previewLineas.Count)
                LogW($"  │  … ({totalLns - previewLineas.Count} líneas más)\n", cSep);
            await Task.Delay(40, ct);

            // ── PASO 2: Analizar el error ────────────────────────────────────
            LogW($"\n  🔍 Analizando el error…\n", cAnal);
            await Task.Delay(80, ct);

            var (tipoError, lineaError, mensajeClave) = ParsearTipoError(errorMsg);

            if (!string.IsNullOrEmpty(tipoError))
            {
                LogW($"  ┄ Tipo    : ", cSep);
                LogW($"{tipoError}\n", Color.FromArgb(252, 100, 100));
            }
            if (lineaError > 0)
            {
                LogW($"  ┄ Línea   : ", cSep);
                LogW($"{lineaError}", Color.FromArgb(253, 186, 116));

                // Mostrar la línea problemática del script
                if (lineaError <= totalLns)
                {
                    string lineaProb = lineas[lineaError - 1].Trim();
                    LogW($"  →  {lineaProb}\n", Color.FromArgb(220, 150, 150));
                }
                else LogW($"\n", cSep);
            }
            if (!string.IsNullOrEmpty(mensajeClave))
            {
                LogW($"  ┄ Mensaje : ", cSep);
                LogW($"{mensajeClave}\n", Color.FromArgb(253, 186, 116));
            }

            // Dar contexto de la instrucción del usuario si existe
            if (!string.IsNullOrWhiteSpace(instruccionUsuario))
            {
                LogW($"\n  💬 Contexto del usuario: ", cInfo);
                LogW($"{instruccionUsuario}\n", cInfo);
            }
            else
            {
                LogW($"  💡 Sin instrucción del usuario — IA analizará causa raíz\n", Color.FromArgb(100, 167, 243, 208));
            }

            await Task.Delay(60, ct);

            // ── PASO 3: Generar corrección ───────────────────────────────────
            LogW($"\n  ✏️  Generando corrección con IA…\n", cFix);

            string corregido;
            var swCorr = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                corregido = string.IsNullOrWhiteSpace(instruccionUsuario)
                    ? await CorregirScriptConIA(nodo, auto, idx, carpeta, codigoActual, errorMsg, ct)
                    : await CorregirScriptConIAConUsuario(nodo, auto, idx, carpeta, codigoActual,
                                                          errorMsg, instruccionUsuario, ct);
            }
            catch (Exception exCorr)
            {
                swCorr.Stop();
                LogW($"  ⚠ No se pudo generar la corrección: {exCorr.Message}\n", ErrorColor);
                return null;
            }
            swCorr.Stop();

            int lineasDespues = corregido.Split('\n').Length;
            int diff          = lineasDespues - totalLns;
            string diffStr    = diff == 0 ? "sin cambio de tamaño"
                              : diff > 0  ? $"+{diff} líneas"
                              : $"{diff} líneas";

            // ── PASO 4: Aplicar y mostrar resultado ──────────────────────────
            LogW($"\n  ✅ Corrección lista  ", Emerald4);
            LogW($"({totalLns} → {lineasDespues} líneas  ·  {diffStr}  ·  {swCorr.Elapsed.TotalSeconds:F1}s)\n", Emerald4);

            // Preview de las primeras líneas del script corregido
            LogW($"  ┄ preview corregido ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n", cSep);
            foreach (var pl in corregido.Split('\n').Take(10))
                LogW($"  │ {pl}\n", cPrev);
            if (lineasDespues > 10) LogW($"  │ … ({lineasDespues - 10} líneas más)\n", cSep);
            LogW($"\n", cSep);

            return corregido;
        }

        /// <summary>
        /// Evalúa semánticamente si la salida de un nodo es válida.
        /// Devuelve null si el resultado es aceptable, o un string con la descripción
        /// del problema si el script corrió pero no cumplió la tarea.
        ///
        /// Detecta tres casos problemáticos:
        ///  1. respuesta.txt vacía o inexistente — el script no escribió nada
        ///  2. Contenido que comienza con "Error:" / "Traceback" — error capturado en el script
        ///  3. Resultado demasiado corto para una tarea con instrucción sustancial
        /// </summary>
        private static string? VerificarSalidaSemantica(string resultado, string instruccion)
        {
            // Caso 1: sin salida
            if (string.IsNullOrWhiteSpace(resultado))
                return "El script se ejecutó pero no escribió ningún resultado en respuesta.txt. " +
                       "Asegúrate de que el script siempre escriba algo útil al final.";

            string trim = resultado.Trim();

            // Caso 2: el script escribió su propio mensaje de error
            if (trim.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ||
                trim.StartsWith("Traceback", StringComparison.OrdinalIgnoreCase) ||
                trim.StartsWith("Exception:", StringComparison.OrdinalIgnoreCase))
                return $"El script escribió un error en respuesta.txt en lugar del resultado esperado: {trim[..Math.Min(200, trim.Length)]}";

            // Caso 3: respuesta simbólica que no dice nada (< 10 chars y tarea no trivial)
            if (trim.Length < 10 && instruccion.Length > 20)
                return $"El script escribió una respuesta demasiado corta ('{trim}') para la tarea: {instruccion[..Math.Min(80, instruccion.Length)]}";

            return null; // OK semántico
        }

        /// <summary>
        /// Extrae tipo de error, número de línea y mensaje clave de un traceback Python.
        /// </summary>
        private static (string tipo, int linea, string mensaje) ParsearTipoError(string error)
        {
            string tipo    = "";
            int    linea   = 0;
            string mensaje = "";

            var lines = error.Replace("\r\n", "\n").Split('\n');

            // Tipo: buscar desde el final la primera línea "XxxError: mensaje"
            foreach (var line in lines.Reverse())
            {
                var trimmed = line.Trim();
                // Patrón: "NombreError: descripción" o "module.NombreError: descripción"
                var mTipo = System.Text.RegularExpressions.Regex.Match(
                    trimmed, @"^(\w+(?:\.\w+)*(?:Error|Exception|Warning))\s*:\s*(.+)$");
                if (mTipo.Success)
                {
                    tipo    = mTipo.Groups[1].Value;
                    mensaje = mTipo.Groups[2].Value.Trim();
                    if (mensaje.Length > 90) mensaje = mensaje[..90] + "…";
                    break;
                }
            }

            // Número de línea: buscar "line N" en el traceback
            foreach (var line in lines)
            {
                var mLinea = System.Text.RegularExpressions.Regex.Match(line, @",\s*line\s+(\d+)");
                if (mLinea.Success)
                {
                    linea = int.Parse(mLinea.Groups[1].Value);
                    // Tomar el último (más cercano al error real)
                }
            }

            return (tipo, linea, mensaje);
        }

        /// <summary>
        /// Agente Corrector: recibe el código que falló + el error, y devuelve
        /// el script corregido. Tiene contexto completo del nodo y del flujo.
        /// </summary>
        private async Task<string> CorregirScriptConIA(
            NodoAutomatizacion nodo, Automatizacion auto, int idx,
            string carpeta, string codigoActual, string errorMsg,
            CancellationToken ct)
        {
            AgentContext ctxBase = await AgentContext.BuildAsync(
                RutaTrabajo, Modelo, ApiKey, Agente,
                soloChat: false, ClavesApis, ct);

            string credenciales = ConstruirDetalleCredenciales();
            string resumen = ConstruirResumenFlujo(auto);

            // Detectar si el fallo es semántico (script corrió pero no produjo resultado)
            // o si es un crash técnico (excepción Python)
            bool falloSemantico = errorMsg.StartsWith("El script se ejecutó") ||
                                  errorMsg.StartsWith("El script escribió un error") ||
                                  errorMsg.StartsWith("El script escribió una respuesta demasiado");

            string tipoFallo = falloSemantico
                ? "FALLO SEMÁNTICO — el script corrió sin excepciones pero no produjo el resultado esperado"
                : "FALLO TÉCNICO — el script lanzó una excepción Python";

            string instruccionCorrecion = falloSemantico
                ? $@"El script corrió sin errores Python pero NO cumplió la tarea.
PROBLEMA: {errorMsg}
TAREA ORIGINAL: {nodo.InstruccionNatural}

Reescribe el script para que:
1. Realice la tarea completa especificada
2. Escriba el resultado real (no vacío, no un placeholder) en respuesta.txt
3. También haga print() del resultado para visibilidad
4. Defina SCRIPT_DIR y RESP_PATH correctamente"
                : $@"El script lanzó una excepción Python.
ERROR: {errorMsg}
TAREA ORIGINAL: {nodo.InstruccionNatural}

Corrige la causa raíz técnica. No ocultes el error con try/except vacío.";

            string promptSistema = $@"
Eres el AGENTE CORRECTOR DE SCRIPTS. Recibes un script Python con un problema
y debes devolver la versión CORREGIDA y COMPLETAMENTE FUNCIONAL.

CARPETA DE TRABAJO: {carpeta}
ARCHIVO DE CREDENCIALES: {RutasProyecto.ObtenerRutaListApis()}
CREDENCIALES DISPONIBLES:
{credenciales}

TIPO DE PROBLEMA: {tipoFallo}

REGLAS:
1. Devuelve SOLO el código Python corregido — sin texto, sin markdown, sin explicaciones
2. Corrige la CAUSA RAÍZ, no pongas un try/except que oculte el problema
3. Si falta un import o dependencia, agrégalo
4. Si la credencial es incorrecta, busca la correcta en ListApis.json
5. El script SIEMPRE debe escribir algo útil en respuesta.txt Y hacer print() del resultado
6. Resultado del paso anterior: os.environ.get('NODO_ANTERIOR_RESULTADO', '')
7. Define siempre:
   SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
   RESP_PATH  = os.path.join(SCRIPT_DIR, 'respuesta.txt')

SOLO código Python corregido.";

            string promptUsuario = $@"Script del paso {idx + 1} ({nodo.Titulo}):

{instruccionCorrecion}

{resumen}

══════ CÓDIGO ACTUAL ══════
{codigoActual}

══════ PROBLEMA ══════
{errorMsg}

Devuelve SOLO el script Python corregido.";

            AgentContext ctxCorr = ctxBase.ConPromptPersonalizado(promptSistema);
            string resp = await AIModelConector.ObtenerRespuestaLLMAsync(promptUsuario, ctxCorr, ct);
            return ExtraerCodigoPython(resp);
        }

        /// <summary>
        /// Agente Corrector con instrucciones del USUARIO.
        /// Recibe el código fallido + error + instrucciones humanas específicas.
        /// </summary>
        private async Task<string> CorregirScriptConIAConUsuario(
            NodoAutomatizacion nodo, Automatizacion auto, int idx,
            string carpeta, string codigoActual, string errorMsg,
            string instruccionUsuario, CancellationToken ct)
        {
            AgentContext ctxBase = await AgentContext.BuildAsync(
                RutaTrabajo, Modelo, ApiKey, Agente,
                soloChat: false, ClavesApis, ct);

            string credenciales = ConstruirDetalleCredenciales();
            string resumen = ConstruirResumenFlujo(auto);

            string promptSistema = $@"
Eres el AGENTE CORRECTOR DE SCRIPTS. El usuario te está ayudando a resolver un error.
Tienes el script que falló, el error, Y las instrucciones del usuario sobre cómo corregirlo.

CARPETA DE TRABAJO: {carpeta}
ARCHIVO DE CREDENCIALES: {RutasProyecto.ObtenerRutaListApis()}
CREDENCIALES DISPONIBLES:
{credenciales}

REGLAS:
1. Devuelve SOLO el código Python corregido — sin texto, sin markdown
2. SIGUE las instrucciones del usuario — él sabe qué está mal
3. Corrige la causa raíz del error
4. Mantén toda la lógica funcional
5. respuesta.txt en misma carpeta, NODO_ANTERIOR_RESULTADO para input

SOLO código Python corregido.";

            string promptUsuario = $@"Script del paso {idx + 1} ({nodo.Titulo}) FALLÓ.

TAREA ORIGINAL: {nodo.InstruccionNatural}

{resumen}

══════ CÓDIGO QUE FALLÓ ══════
{codigoActual}

══════ ERROR ══════
{errorMsg}

══════ INSTRUCCIONES DEL USUARIO PARA CORREGIR ══════
{instruccionUsuario}

Corrige el script siguiendo las instrucciones del usuario. Devuelve SOLO el código Python.";

            AgentContext ctxCorr = ctxBase.ConPromptPersonalizado(promptSistema);
            string resp = await AIModelConector.ObtenerRespuestaLLMAsync(promptUsuario, ctxCorr, ct);
            return ExtraerCodigoPython(resp);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MODO TESTER PODEROSO
        //  Terminal en tiempo real: stdout gris, stderr rojo, errores completos,
        //  preview del script corregido, timing, separadores visuales.
        //  Flujo: ejecuta → si falla → IA corrige (x3 mostrando todo) →
        //         si 3 fallos → panel interactivo → usuario da instrucciones.
        // ══════════════════════════════════════════════════════════════════════
        private async Task<bool> ProbarNodoConReintentos(
            NodoAutomatizacion nodo, Automatizacion auto, int idx,
            string carpeta, string rutaScript, string? contextoAnterior,
            CancellationToken ct,
            bool sinInteractivo = false)   // true en ejecución paralela: no muestra panel de error
        {
            var   ctrl        = canvas.Nodos.FirstOrDefault(c => c.Datos.Id == nodo.Id);
            bool  nodoOk      = false;
            int   intentoAuto = 0;
            int   ejecucionN  = 0;
            const int MAX_AUTO = 3;

            // Paleta de colores del terminal
            var cOut  = Color.FromArgb(185, 195, 210);           // stdout  — gris azulado
            var cErr  = Color.FromArgb(255, 110, 110);           // stderr  — rojo
            var cSep  = Color.FromArgb(40, 255, 255, 255);       // separadores tenues
            var cPrev = Color.FromArgb(100, 200, 155);           // preview código
            var cCmd  = Color.FromArgb(100, 180, 255);           // comando python
            var cBord = Color.FromArgb(60, 16, 185, 129);        // bordes

            // ── Encabezado del nodo ──────────────────────────────────────────
            LogW($"\n", cSep);
            LogW($"╔══════════════════════════════════════════════════════════════╗\n", cBord);
            LogW($"  🧪 TESTER  ·  [{idx + 1}] {nodo.Titulo}\n", WarnColor);
            LogW($"  📄 {nodo.NombreScript}\n", Color.FromArgb(120, 52, 211, 153));
            LogW($"  📂 {carpeta}\n", Color.FromArgb(80, 200, 200, 200));
            LogW($"╚══════════════════════════════════════════════════════════════╝\n", cBord);

            while (!nodoOk)
            {
                ct.ThrowIfCancellationRequested();
                ctrl?.ActualizarEstado(EstadoNodo.Ejecutando);
                ejecucionN++;

                string codigoActual = File.Exists(rutaScript)
                    ? File.ReadAllText(rutaScript, Encoding.UTF8) : "";

                // ── Cabecera de ejecución ────────────────────────────────────
                LogW($"\n", cSep);
                LogW($"  ▶  EJECUCIÓN {ejecucionN}  ──────────────────────────────────────────\n", TextSub);
                LogW($"  $ python {Path.GetFileName(rutaScript)}\n", cCmd);
                LogW($"  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n", cSep);

                var sw = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // Ejecutar con output en tiempo real, coloreado por stream
                    string resultado = await EjecutarScriptDirecto(
                        rutaScript, carpeta, contextoAnterior, ct,
                        linea =>
                        {
                            if (string.IsNullOrEmpty(linea)) return;
                            if (linea.StartsWith("[ERR]"))
                                LogW("  │ " + linea[5..].TrimStart() + "\n", cErr);
                            else
                                LogW("  │ " + linea + "\n", cOut);
                        });

                    sw.Stop();
                    nodo.UltimaRespuesta = resultado;

                    // ── Validación semántica del resultado ───────────────────
                    // Un script puede ejecutarse sin excepciones y aun así
                    // no haber producido ningún resultado útil en respuesta.txt.
                    string? falloSemantico = VerificarSalidaSemantica(resultado, nodo.InstruccionNatural);

                    if (falloSemantico != null)
                    {
                        // Tratar como fallo semántico: el script corrió pero no cumplió la tarea
                        ctrl?.ActualizarEstado(EstadoNodo.Error);
                        string errorSemantico = falloSemantico;
                        nodo.UltimaRespuesta  = errorSemantico;

                        LogW($"  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n", cSep);
                        LogW($"  ⚠  FALLO SEMÁNTICO  ({sw.Elapsed.TotalSeconds:F2}s)\n", WarnColor);
                        LogW($"  ┄ {errorSemantico}\n", WarnColor);
                        LogW($"  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n\n", cSep);

                        // Auto-corrección con contexto semántico (en ambos modos)
                        if (intentoAuto >= MAX_AUTO)
                        {
                            LogW($"  ⏸ [{nodo.Titulo}] agotó {MAX_AUTO} correcciones semánticas\n\n", ErrorColor);
                            break; // sale del while — nodoOk permanece false
                        }
                        intentoAuto++;
                        LogW($"  ━━ CORRECCIÓN SEMÁNTICA {intentoAuto}/{MAX_AUTO} ━━\n", WarnColor);

                        string? fix = await AnalizarYCorregirConPasos(
                            nodo, auto, idx, carpeta, codigoActual, errorSemantico, null, ct,
                            cOut, cErr, cSep, cPrev);
                        if (fix != null)
                        {
                            File.WriteAllText(rutaScript, fix, Encoding.UTF8);
                            nodo.ScriptGenerado = fix;
                            ctrl?.ActualizarEstado(EstadoNodo.Ejecutando);
                        }
                        continue; // reintentar
                    }

                    // ── OK ───────────────────────────────────────────────────
                    LogW($"  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n", cSep);
                    LogW($"  ✔  NODO OK  ({sw.Elapsed.TotalSeconds:F2}s)\n", Emerald4);

                    if (!string.IsNullOrWhiteSpace(resultado))
                    {
                        LogW($"  ↳ respuesta.txt:\n", Color.FromArgb(120, 52, 211, 153));
                        foreach (var rl in resultado.Split('\n').Take(8))
                            LogW($"     {rl}\n", Emerald4);
                        if (resultado.Split('\n').Length > 8)
                            LogW($"     …\n", cSep);
                    }
                    LogW($"\n", cSep);
                    ctrl?.ActualizarEstado(EstadoNodo.Completado);
                    nodoOk = true;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    string errorMsg = ex.Message;
                    ctrl?.ActualizarEstado(EstadoNodo.Error);
                    nodo.UltimaRespuesta = errorMsg;

                    // ── Mostrar error completo en terminal ───────────────────
                    LogW($"  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n", cSep);
                    LogW($"  ✖  FALLÓ  ({sw.Elapsed.TotalSeconds:F2}s)\n", ErrorColor);
                    LogW($"  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n", cSep);
                    foreach (var eL in errorMsg.Split('\n').Take(20))
                        LogW($"  │ {eL}\n", cErr);
                    LogW($"\n", cSep);

                    // ══════════════════════════════════════════════════════════
                    // MODO PARALELO: auto-corrección silenciosa (sin panel)
                    // ══════════════════════════════════════════════════════════
                    if (sinInteractivo)
                    {
                        if (intentoAuto >= MAX_AUTO)
                        {
                            LogW($"  ⏸ [{nodo.Titulo}] agotó {MAX_AUTO} correcciones — pendiente revisión\n\n", WarnColor);
                            return false;
                        }
                        intentoAuto++;
                        LogW($"  ━━ AUTO-CORRECCIÓN {intentoAuto}/{MAX_AUTO} (paralelo) ━━\n", WarnColor);
                        EstadoIA($"🔄 Corrigiendo [{idx + 1}] {nodo.Titulo}  ({intentoAuto}/{MAX_AUTO})", WarnColor);
                        string? autoFix = await AnalizarYCorregirConPasos(
                            nodo, auto, idx, carpeta, codigoActual, errorMsg, null, ct,
                            cOut, cErr, cSep, cPrev);
                        if (autoFix != null)
                        {
                            File.WriteAllText(rutaScript, autoFix, Encoding.UTF8);
                            nodo.ScriptGenerado = autoFix;
                            ctrl?.ActualizarEstado(EstadoNodo.Ejecutando);
                        }
                        continue; // volver al while
                    }

                    // ══════════════════════════════════════════════════════════
                    // MODO INTERACTIVO: panel desde el PRIMER fallo
                    // ══════════════════════════════════════════════════════════
                    if (intentoAuto >= MAX_AUTO)
                    {
                        // Agotados todos los intentos → panel definitivo
                        LogW($"  ══════════════════════════════════════════════════════\n", ErrorColor);
                        LogW($"  ⛔  {MAX_AUTO} CORRECCIONES AGOTADAS — esperando decisión\n", ErrorColor);
                        LogW($"  ══════════════════════════════════════════════════════\n\n", ErrorColor);

                        string? instrFinal = await MostrarErrorYEsperarUsuario(
                            nodo, errorMsg, codigoActual,
                            $"Ya se hicieron {MAX_AUTO} intentos. Si escribes instrucciones se hará un último intento; de lo contrario salta el nodo.");

                        if (instrFinal == null)
                        {
                            LogW($"  ⏭ Nodo saltado por el usuario\n\n", WarnColor);
                            nodo.UltimaRespuesta = $"Saltado: {errorMsg}";
                            return false;
                        }

                        // Usuario dio instrucción → un último intento extra
                        intentoAuto = 0;
                        string? exFix = await AnalizarYCorregirConPasos(
                            nodo, auto, idx, carpeta, codigoActual, errorMsg,
                            string.IsNullOrWhiteSpace(instrFinal) ? null : instrFinal, ct,
                            cOut, cErr, cSep, cPrev);
                        if (exFix != null)
                        {
                            File.WriteAllText(rutaScript, exFix, Encoding.UTF8);
                            nodo.ScriptGenerado = exFix;
                            ctrl?.ActualizarEstado(EstadoNodo.Ejecutando);
                        }
                        continue;
                    }

                    // ── Panel inmediato desde 1er fallo ─────────────────────
                    string hintPanel = intentoAuto == 0
                        ? "¿Sabes qué falló o qué falta? Escríbelo (o deja vacío y la IA analizará sola):"
                        : $"Intento {intentoAuto + 1} también falló. ¿Alguna instrucción adicional para la corrección?";

                    LogW($"  💬 Esperando input del usuario en el panel…\n", TextMuted);

                    string? instruccion = await MostrarErrorYEsperarUsuario(
                        nodo, errorMsg, codigoActual, hintPanel);

                    if (instruccion == null)
                    {
                        LogW($"  ⏭ Nodo saltado por el usuario\n\n", WarnColor);
                        nodo.UltimaRespuesta = $"Saltado: {errorMsg}";
                        return false;
                    }

                    intentoAuto++;
                    string etiqueta = string.IsNullOrWhiteSpace(instruccion)
                        ? $"AUTO-CORRECCIÓN {intentoAuto}/{MAX_AUTO}"
                        : $"CORRECCIÓN CON INSTRUCCIONES {intentoAuto}/{MAX_AUTO}";

                    LogW($"  ━━ {etiqueta} ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n", WarnColor);
                    EstadoIA($"🔄 Corrigiendo [{idx + 1}] {nodo.Titulo}  ({intentoAuto}/{MAX_AUTO})", WarnColor);

                    string? corregido = await AnalizarYCorregirConPasos(
                        nodo, auto, idx, carpeta, codigoActual, errorMsg,
                        string.IsNullOrWhiteSpace(instruccion) ? null : instruccion, ct,
                        cOut, cErr, cSep, cPrev);

                    if (corregido != null)
                    {
                        File.WriteAllText(rutaScript, corregido, Encoding.UTF8);
                        nodo.ScriptGenerado = corregido;
                        ctrl?.ActualizarEstado(EstadoNodo.Ejecutando);
                    }
                }
            }

            return nodoOk;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CRUD AUTOMATIZACIONES
        // ══════════════════════════════════════════════════════════════════════
        private void CrearNuevaAuto()
        {
            var auto = new Automatizacion
            {
                Nombre = $"Automatización {_lista.Count + 1}",
                Descripcion = "Describe qué hace"
            };
            auto.CarpetaScripts = AutomatizacionScheduler.ObtenerCarpetaAuto(auto);
            _lista.Add(auto);
            GuardarLista();
            SeleccionarAuto(auto);
            RefrescarLista();
        }

        private void Guardar()
        {
            if (_autoActual == null) return;
            float zGuardar = canvas.Zoom;
            foreach (var n in canvas.Nodos) { n.Datos.CanvasX = (int)(n.Left / zGuardar); n.Datos.CanvasY = (int)(n.Top / zGuardar); }
            GuardarLista();
            EstadoIA("✔ Guardado", Emerald4);
        }

        private void AgregarNodoManual()
        {
            if (_autoActual == null) { EstadoIA("Crea una automatización primero.", ErrorColor); return; }
            int i = _autoActual.Nodos.Count;
            var nodo = new NodoAutomatizacion
            {
                TipoNodo = TipoNodo.Accion, Titulo = "Nueva acción",
                InstruccionNatural = "", OrdenEjecucion = i,
                NombreScript = $"nodo_{i:D2}_accion.py",
                CanvasX = 70 + (i % 4) * 240, CanvasY = 100 + (i / 4) * 160
            };

            // Conectar automáticamente al último nodo existente
            var ultimoNodo = _autoActual.Nodos
                .OrderByDescending(n => n.OrdenEjecucion).FirstOrDefault();
            if (ultimoNodo != null && !ultimoNodo.ConexionesSalida.Contains(nodo.Id))
                ultimoNodo.ConexionesSalida.Add(nodo.Id);

            _autoActual.Nodos.Add(nodo);
            var ctrl = canvas.AgregarNodo(nodo);
            SeleccionarNodoEditar(ctrl);

            LogW($"  + Nodo agregado: {nodo.Titulo} (conectado al paso {i})\n", Emerald4);
            canvas.Invalidate();
        }

        private void EliminarAutoActual()
        {
            if (_autoActual == null) return;
            if (MessageBox.Show($"¿Eliminar «{_autoActual.Nombre}» y todos sus scripts?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            // Eliminar carpeta de scripts
            if (!string.IsNullOrEmpty(_autoActual.CarpetaScripts) && Directory.Exists(_autoActual.CarpetaScripts))
            {
                try { Directory.Delete(_autoActual.CarpetaScripts, recursive: true); }
                catch (Exception ex) { LogW($"⚠ No se pudo borrar carpeta: {ex.Message}\n", WarnColor); }
            }

            _lista.Remove(_autoActual);
            GuardarLista();
            DeseleccionarAuto();
            RefrescarLista();
            EstadoIA("Automatización eliminada", TextMuted);
        }

        private void SeleccionarAuto(Automatizacion auto)
        {
            _autoActual = auto;
            lblTituloAuto.Text = $"⚡ {auto.Nombre}";
            canvas.CargarNodos(auto.Nodos);
            pnlEditor.Visible = false;
            _nodoEditando = null;
            CargarScheduleEnPanel(auto);
            RefrescarLista();

            // Cambiar texto del botón para indicar modo modificación
            btnCrearIA.Text = "✏ Modificar con IA";
            txtNLInput.PlaceholderText = "✏  Ej: «agrega un paso que notifique a Slack», «cambia el paso 2 para usar Gmail», «elimina el último nodo»";
        }

        private void DeseleccionarAuto()
        {
            _autoActual = null;
            canvas.LimpiarCanvas();
            pnlEditor.Visible = false;
            pnlSchedule.Visible = false;
            _nodoEditando = null;
            btnCrearIA.Text = "✨ Crear con IA";
            txtNLInput.PlaceholderText = "✨  Ej: «Captura pantalla y envíalo al correo x@y.com a las 8am todos los días»  (Ctrl+Enter)";
        }

        // ══════════════════════════════════════════════════════════════════════
        //  LISTA UI
        // ══════════════════════════════════════════════════════════════════════
        private void CargarLista()
        {
            _listaApis = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());
            _lista     = JsonManager.Leer<Automatizacion>(RutasProyecto.ObtenerRutaListAutomatizaciones());
            RefrescarLista();
            lblInfoConfig.Text = ObtenerTextoConfig();
            if (pnlCredenciales.Visible) RefrescarChipsCredenciales();
        }

        private void RefrescarLista()
        {
            flpLista.SuspendLayout();
            flpLista.Controls.Clear();
            foreach (var a in _lista) flpLista.Controls.Add(CrearCard(a));
            flpLista.ResumeLayout(true);
            // Reajusta anchos tras el layout; solo con BeginInvoke si el handle ya existe
            if (IsHandleCreated)
                BeginInvoke(AjustarAnchosCards);
            else
                AjustarAnchosCards();
        }

        private void AjustarAnchosCards()
        {
            int w = flpLista.ClientSize.Width - flpLista.Padding.Horizontal;
            foreach (Control c in flpLista.Controls) c.Width = Math.Max(60, w);
        }

        private void RefrescarCardActual()
        {
            if (_autoActual == null) return;
            if (InvokeRequired) { BeginInvoke(RefrescarCardActual); return; }
            RefrescarLista(); // simple refresh
        }

        private void IniciarRenombrar(Panel card, Label lblN, Automatizacion auto)
        {
            // Evitar abrir dos TextBox si ya hay uno activo
            if (card.Controls.OfType<TextBox>().Any()) return;

            lblN.Visible = false;   // ocultar label mientras se edita

            var txt = new TextBox
            {
                Text     = auto.Nombre,
                Location = new Point(lblN.Left, lblN.Top - 1),
                Size     = new Size(lblN.Width, lblN.Height + 4),
                BackColor      = ColorTranslator.FromHtml("#0d2218"),
                ForeColor      = Emerald4,
                BorderStyle    = BorderStyle.FixedSingle,
                Font           = lblN.Font
            };
            card.Controls.Add(txt);
            txt.BringToFront();
            txt.SelectAll();
            txt.Focus();

            bool committed = false;
            void Commit()
            {
                if (committed) return;
                committed = true;
                string nuevo = txt.Text.Trim();
                if (!string.IsNullOrEmpty(nuevo)) auto.Nombre = nuevo;
                GuardarLista();
                // Restaurar label con el nuevo nombre
                lblN.Text    = auto.Nombre;
                lblN.Visible = true;
                if (card.Controls.Contains(txt)) card.Controls.Remove(txt);
                if (_autoActual?.Id == auto.Id)
                    lblTituloAuto.Text = $"⚡ {auto.Nombre}";
            }
            void Cancel()
            {
                if (committed) return;
                committed = true;
                lblN.Visible = true;
                if (card.Controls.Contains(txt)) card.Controls.Remove(txt);
            }

            txt.KeyDown  += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)  { e.SuppressKeyPress = true; Commit(); }
                if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Cancel(); }
            };
            txt.LostFocus += (_, _) => BeginInvoke((Action)Commit);
        }

        private Panel CrearCard(Automatizacion a)
        {
            bool sel = _autoActual?.Id == a.Id;
            Color cardBgNormal = Color.FromArgb(13, 19, 26);
            Color cardBgSel    = ColorTranslator.FromHtml("#0c1e14");
            Color cardBgHover  = Color.FromArgb(20, 30, 42);

            // Descuenta padding (16) + scrollbar vertical (fijo) para evitar scroll horizontal
            int cardW = flpLista.Width - flpLista.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 2;
            var card = new Panel
            {
                Width = Math.Max(60, cardW), Height = 96,
                BackColor = sel ? cardBgSel : cardBgNormal,
                Cursor = Cursors.Hand, Tag = a, Margin = new Padding(0, 0, 0, 5)
            };
            card.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RR(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 7);
                using var pen  = new Pen(sel ? Emerald : Color.FromArgb(35, 100, 130, 160), sel ? 1.5f : 1f);
                e.Graphics.FillPath(new SolidBrush(card.BackColor), path);
                e.Graphics.DrawPath(pen, path);
                // Franja izquierda en selección
                if (sel) e.Graphics.FillRectangle(new SolidBrush(Emerald), 0, 10, 3, card.Height - 20);
            };

            // ── Fila 1: Nombre ────────────────────────────────────────────────
            var lblN = new Label
            {
                Text = a.Nombre, ForeColor = sel ? Emerald4 : TextMain,
                Font = new Font("Segoe UI Semibold", 9f), Location = new Point(14, 9),
                Width = card.Width - 32, AutoEllipsis = true,
                Cursor = sel ? Cursors.IBeam : Cursors.Hand
            };
            if (sel)
                new ToolTip { InitialDelay = 500 }.SetToolTip(lblN, "✏ Clic para renombrar");

            // ── Fila 2: Tipo de programación ──────────────────────────────────
            string schedTxt = a.TipoProgramacion switch
            {
                "diaria"    => $"🕐 Diaria {a.HoraEjecucion}",
                "intervalo" => $"🔄 Cada {a.IntervaloMinutos}min",
                "unica"     => $"📌 Una vez {a.HoraEjecucion}",
                "siempre"   => "♾ Continua",
                _           => "✋ Manual"
            };
            var lblSched = new Label
            {
                Text = schedTxt, ForeColor = Color.FromArgb(100, 110, 200, 150),
                Font = new Font("Segoe UI", 7.5f), Location = new Point(14, 28),
                Width = card.Width - 28, AutoEllipsis = true
            };

            // ── Fila 3: Estado ────────────────────────────────────────────────
            string st  = a.UltimoEstado ?? "Sin ejecutar";
            Color  stC = st.StartsWith("✔") ? Emerald4
                       : st.StartsWith("✖") ? ErrorColor
                       : Color.FromArgb(130, 150, 160);
            var lblS = new Label
            {
                Text = st, ForeColor = stC,
                Font = new Font("Segoe UI Semibold", 7.5f), Location = new Point(14, 46),
                Width = card.Width - 28, AutoEllipsis = true
            };

            // ── Fila 4: Nodos | Última ejecución ─────────────────────────────
            var lblNodoCnt = new Label
            {
                Text = $"⬡ {a.Nodos.Count} nodo(s)",
                ForeColor = Color.FromArgb(65, 140, 200, 170),
                Font = new Font("Segoe UI", 7f), Location = new Point(14, 67),
                Width = 82, AutoEllipsis = true
            };
            string fechaTxt = a.UltimaEjecucion.HasValue
                ? $"🕐 {a.UltimaEjecucion:dd/MM  HH:mm}"
                : "";
            var lblFecha = new Label
            {
                Text = fechaTxt,
                ForeColor = Color.FromArgb(55, 150, 185, 165),
                Font = new Font("Segoe UI", 7f), Location = new Point(98, 67),
                Width = card.Width - 112, AutoEllipsis = true
            };

            // ── Toggle activo (punto superior derecho) ────────────────────────
            var dot = new Panel
            {
                Size = new Size(9, 9), Location = new Point(card.Width - 20, 13),
                BackColor = a.Activa ? Emerald : Color.FromArgb(55, 65, 80), Cursor = Cursors.Hand
            };
            dot.Paint += (_, e) => e.Graphics.FillEllipse(new SolidBrush(dot.BackColor), 0, 0, 9, 9);
            dot.Click += (_, _) =>
            {
                a.Activa = !a.Activa;
                GuardarLista();
                RefrescarLista();
            };

            // ── Eventos ───────────────────────────────────────────────────────
            EventHandler click = (_, _) => SeleccionarAuto(a);
            card.Click += click; lblSched.Click += click; lblS.Click += click;
            lblFecha.Click += click; lblNodoCnt.Click += click;
            if (sel)
                lblN.Click += (_, _) => IniciarRenombrar(card, lblN, a);
            else
                lblN.Click += click;
            card.MouseEnter += (_, _) => { if (!sel) { card.BackColor = cardBgHover; card.Invalidate(); } };
            card.MouseLeave += (_, _) => { if (!sel) { card.BackColor = cardBgNormal; card.Invalidate(); } };

            card.Controls.AddRange(new Control[] { dot, lblN, lblSched, lblS, lblNodoCnt, lblFecha });
            return card;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  LOG + HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private void AbrirLog() { if (InvokeRequired) BeginInvoke(() => pnlLog.Visible = true); else pnlLog.Visible = true; }

        private void LogW(string txt, Color c)
        {
            if (rtbLog.IsDisposed) return;
            Action act = () =>
            {
                if (rtbLog.IsDisposed) return;
                int s = rtbLog.TextLength;
                rtbLog.AppendText(txt);
                rtbLog.Select(s, txt.Length);
                rtbLog.SelectionColor = c;
                rtbLog.SelectionLength = 0;
                rtbLog.ScrollToCaret();
            };
            if (rtbLog.InvokeRequired) rtbLog.BeginInvoke(act); else act();
        }

        private void LogAppend(string linea)
        {
            _bufferLog.AppendLine(linea);
            _streamingPend = true;
        }

        private void FlushBufferAlLog()
        {
            if (rtbLog.IsDisposed) return;
            string t = _bufferLog.ToString();
            _bufferLog.Clear();
            if (!string.IsNullOrEmpty(t)) LogW(t, TextSub);
        }

        // ── Credenciales ──────────────────────────────────────────────────────
        private void RefrescarChipsCredenciales()
        {
            flpChips.Controls.Clear();
            if (_listaApis.Count == 0)
            {
                flpChips.Controls.Add(new Label
                {
                    Text = "Sin credenciales registradas. Agrégalas en Configuración.",
                    ForeColor = Color.FromArgb(100, 167, 243, 208),
                    Font = new Font("Segoe UI", 8f), AutoSize = true,
                    Margin = new Padding(0, 3, 0, 0)
                });
                return;
            }
            foreach (var api in _listaApis)
                flpChips.Controls.Add(CrearChipApi(api));
        }

        private Control CrearChipApi(Api api)
        {
            string textoIns = $"[credencial: {api.Nombre}]";
            var chipFont    = new Font("Segoe UI Semibold", 8.5f);
            int textW       = TextRenderer.MeasureText(api.Nombre, chipFont).Width;

            var chip = new Panel
            {
                Height = 26, Width = textW + 36,
                BackColor = ColorTranslator.FromHtml("#0d2218"),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 6, 0)
            };
            chip.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RR(new Rectangle(0, 0, chip.Width - 1, chip.Height - 1), 5);
                using var br   = new SolidBrush(chip.BackColor);
                using var pen  = new Pen(ColorTranslator.FromHtml("#1a4030"), 1f);
                e.Graphics.FillPath(br, path);
                e.Graphics.DrawPath(pen, path);
                // Icono 🔑
                TextRenderer.DrawText(e.Graphics, "🔑", new Font("Segoe UI Emoji", 8f),
                    new Rectangle(4, 4, 18, 18), Color.FromArgb(160, 167, 243, 208),
                    TextFormatFlags.VerticalCenter);
                // Nombre
                TextRenderer.DrawText(e.Graphics, api.Nombre, chipFont,
                    new Rectangle(22, 0, textW + 10, chip.Height), Emerald4,
                    TextFormatFlags.VerticalCenter);
            };

            // Tooltip con descripción
            if (!string.IsNullOrWhiteSpace(api.Descripcion))
            {
                var tip = new ToolTip { InitialDelay = 300 };
                tip.SetToolTip(chip, $"{api.Nombre}\n{api.Descripcion}");
            }

            // Hover
            chip.MouseEnter += (_, _) => { chip.BackColor = ColorTranslator.FromHtml("#163324"); chip.Invalidate(); };
            chip.MouseLeave += (_, _) => { chip.BackColor = ColorTranslator.FromHtml("#0d2218"); chip.Invalidate(); };

            // Clic → insertar en el input
            chip.Click += (_, _) => InsertarEnInput(textoIns);

            // Arrastrar al input
            chip.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    chip.DoDragDrop(textoIns, DragDropEffects.Copy);
            };

            return chip;
        }

        private void InsertarEnInput(string texto)
        {
            txtNLInput.Focus();
            int pos    = txtNLInput.SelectionStart;
            string cur = txtNLInput.Text;
            string pre = (pos > 0 && cur[pos - 1] != ' ') ? " " : "";
            string suf = (pos < cur.Length && cur[pos] != ' ') ? " " : "";
            txtNLInput.Text           = cur[..pos] + pre + texto + suf + cur[pos..];
            txtNLInput.SelectionStart = pos + pre.Length + texto.Length + suf.Length;
        }

        private void GuardarLista() =>
            JsonManager.Guardar(RutasProyecto.ObtenerRutaListAutomatizaciones(), _lista);

        private bool ValidarConfig()
        {
            if (!string.IsNullOrWhiteSpace(Modelo)) return true;
            EstadoIA("⚠ Sin modelo. Configura en Modelos primero.", ErrorColor);
            return false;
        }

        private string ObtenerTextoConfig() =>
            string.IsNullOrWhiteSpace(Modelo) ? "⚠ Sin modelo configurado" : $"Modelo: {Modelo} | {Agente}";

        private void EstadoIA(string msg, Color c)
        {
            if (lblEstadoIA == null || lblEstadoIA.IsDisposed) return;
            if (lblEstadoIA.InvokeRequired) { lblEstadoIA.BeginInvoke(() => EstadoIA(msg, c)); return; }
            lblEstadoIA.Text = msg; lblEstadoIA.ForeColor = c;
        }

        private static string SanitizarNombre(string s) =>
            new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray()).ToLowerInvariant();

        private static string Truncar(string s, int max) =>
            s.Length <= max ? s : s[..max] + "…";

        // ── UI Helpers ────────────────────────────────────────────────────────
        private static Button Btn(string text, Color bc, int w)
        {
            bool isPrimary = bc == Emerald;
            bool isSuccess = bc == Emerald9;
            bool isDanger  = bc == ErrorColor;

            Color bg  = isPrimary ? ColorTranslator.FromHtml("#064e3b")
                      : isDanger  ? Color.FromArgb(30, 10, 10)
                      : isSuccess ? Color.FromArgb(8, 28, 18)
                      : Color.FromArgb(18, 24, 32);
            Color fg  = isPrimary ? Emerald4
                      : isDanger  ? Color.FromArgb(252, 129, 129)
                      : isSuccess ? Emerald4
                      : Color.FromArgb(170, 190, 200);
            Color brd = isPrimary ? ColorTranslator.FromHtml("#10b981")
                      : isDanger  ? Color.FromArgb(110, 248, 113, 113)
                      : isSuccess ? Color.FromArgb(70, 16, 185, 129)
                      : Color.FromArgb(45, 120, 150, 180);

            var b = new Button
            {
                Text      = text, Size = new Size(w, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg, ForeColor = fg,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(2, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.BorderColor = brd;
            return b;
        }
        private static Label LblF(string t) => new Label {
            Text = t, ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            Dock = DockStyle.Top, Height = 20, TextAlign = ContentAlignment.BottomLeft
        };
        private static TextBox TxtField(bool ml) => new TextBox {
            BackColor = BgCard, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Top, Height = ml ? 60 : 26, Multiline = ml, Font = new Font("Segoe UI", 9.5f)
        };

        private static void PintarBordeInf(object? s, PaintEventArgs e) { if (s is Control c) using (var p = new Pen(Color.FromArgb(55, 16, 185, 129))) e.Graphics.DrawLine(p, 0, c.Height - 1, c.Width, c.Height - 1); }
        private static void PintarBordeSup(object? s, PaintEventArgs e) { if (s is Control c) using (var p = new Pen(Color.FromArgb(55, 16, 185, 129))) e.Graphics.DrawLine(p, 0, 0, c.Width, 0); }
        private static void PintarBordeDer(object? s, PaintEventArgs e) { if (s is Control c) using (var p = new Pen(Color.FromArgb(55, 16, 185, 129))) e.Graphics.DrawLine(p, c.Width - 1, 0, c.Width - 1, c.Height); }
        private static void PintarBordeIzq(object? s, PaintEventArgs e) { if (s is Control c) using (var p = new Pen(Color.FromArgb(55, 16, 185, 129))) e.Graphics.DrawLine(p, 0, 0, 0, c.Height); }

        private static GraphicsPath RR(Rectangle r, int rad)
        {
            int d = rad * 2; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timerStreaming.Stop(); _timerStreaming.Dispose();
            _cts?.Cancel(); _cts?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
