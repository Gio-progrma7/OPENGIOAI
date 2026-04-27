using Microsoft.Extensions.DependencyInjection;
using OPENGIOAI.Data;
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
    public partial class FrmPrincipal : Form
    {
        // ── Paleta Emerald (consistente con el resto de la app) ───────────────
        private static readonly Color BgDeep    = ColorTranslator.FromHtml("#050505");
        private static readonly Color BgSurface = ColorTranslator.FromHtml("#0a0a0a");
        private static readonly Color BgCard    = ColorTranslator.FromHtml("#0f1117");
        private static readonly Color BgHover   = ColorTranslator.FromHtml("#141826");
        private static readonly Color BgActive  = ColorTranslator.FromHtml("#0b2920");
        private static readonly Color Emerald   = ColorTranslator.FromHtml("#10b981");
        private static readonly Color Emerald4  = ColorTranslator.FromHtml("#34d399");
        private static readonly Color Emerald9  = ColorTranslator.FromHtml("#064e3b");
        private static readonly Color TextMain  = ColorTranslator.FromHtml("#f0fdf4");
        private static readonly Color TextSub   = ColorTranslator.FromHtml("#a7f3d0");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#6ee7b7");
        private static readonly Color Border    = ColorTranslator.FromHtml("#1f2937");
        private static readonly Color DangerCol = ColorTranslator.FromHtml("#f87171");

        private const int SidebarExpanded  = 240;
        private const int SidebarCollapsed = 72;

        // ── Estado de la app ──────────────────────────────────────────────────
        private ConfiguracionClient Miconfiguracion = new ConfiguracionClient();
        private readonly AutomatizacionScheduler _scheduler = new();
        private readonly IServiceProvider _services;

        // ── Sidebar ───────────────────────────────────────────────────────────
        private Panel pnlSidebar = null!;
        private Panel pnlLogoArea = null!;
        private FlowLayoutPanel flpItems = null!;
        private Panel pnlSidebarFooter = null!;
        private Label lblLogo = null!;
        private Button btnToggleColapsar = null!;
        private bool _colapsado = false;
        private System.Windows.Forms.Timer _timerColapsar = null!;
        private int _anchoObjetivo;

        // ── Topbar (breadcrumb + back) ────────────────────────────────────────
        private Panel pnlTopbar = null!;
        private Button btnAtras = null!;
        private Button btnCerrarForm = null!;
        private FlowLayoutPanel flpBreadcrumb = null!;

        // ── Container ─────────────────────────────────────────────────────────
        private Panel pnlContenedor = null!;
        private Panel pnlHome = null!;

        // ── Nav stack (LIFO: orden de apertura = orden de cierre) ─────────────
        private sealed class NavEntry
        {
            public Type Tipo = null!;
            public Form Instancia = null!;
            public string Titulo = "";
            public string Icono = "";
        }
        private readonly List<NavEntry> _nav = new();

        // ── Definición declarativa de items ───────────────────────────────────
        private sealed class MenuDef
        {
            public string Icono = "";
            public string Titulo = "";
            public string Grupo = "";
            public Action Accion = () => { };
            public Type? TipoForm; // null = flotante / sin stack
        }
        private readonly List<MenuDef> _items = new();
        private readonly Dictionary<Type, MenuItemBoton> _itemPorTipo = new();
        private readonly List<MenuItemBoton> _todosItems = new();

        // ── Bandeja del sistema ───────────────────────────────────────────────
        private NotifyIcon? _trayIcon;
        private ToolStripMenuItem? _trayItemSegundoPlano;
        private bool _saliendoDeVerdad = false;
        private MenuItemBoton? _itemSegundoPlano;

        public FrmPrincipal(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();
            DoubleBuffered = true;

            CargarConfiguracion();
            DefinirItems();
            ConstruirUI();
            InicializarBandeja();

            FormClosing += FrmPrincipal_FormClosing;
            KeyPreview = true;
            KeyDown += FrmPrincipal_KeyDown;
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONFIGURACIÓN INICIAL
        // ════════════════════════════════════════════════════════════════════

        private void CargarConfiguracion()
        {
            try
            {
                Miconfiguracion = Utils.LeerConfig<ConfiguracionClient>(
                    RutasProyecto.ObtenerRutaConfiguracion()) ?? new ConfiguracionClient();
            }
            catch
            {
                Miconfiguracion = new ConfiguracionClient();
            }
            IniciarScheduler();
        }

        private void IniciarScheduler()
        {
            try
            {
                var apis = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());
                string claves = Utils.ObtenerNombresApis(apis);

                _scheduler.Configurar(
                    Miconfiguracion?.Mimodelo?.Modelos ?? "",
                    Miconfiguracion?.Mimodelo?.ApiKey  ?? "",
                    Miconfiguracion?.MiArchivo?.Ruta   ?? RutasProyecto.ObtenerRutaScripts(),
                    claves,
                    Miconfiguracion?.Mimodelo?.Agente  ?? Servicios.Gemenni);

                _scheduler.Iniciar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Scheduler] init falló: {ex.Message}");
            }
        }

        private void DefinirItems()
        {
            // WORKSPACE — apertura in-panel con stack
            _items.Add(new() { Icono = "💬", Titulo = "Chat",             Grupo = "WORKSPACE",      Accion = AbrirChat,             TipoForm = typeof(FrmMandos) });
            _items.Add(new() { Icono = "⚡", Titulo = "Skills",           Grupo = "WORKSPACE",      Accion = AbrirSkills,           TipoForm = typeof(Skills) });
            _items.Add(new() { Icono = "🤖", Titulo = "Automatizaciones", Grupo = "WORKSPACE",      Accion = AbrirAutomatizaciones, TipoForm = typeof(FrmAutomatizaciones) });

            // CONFIGURACIÓN
            _items.Add(new() { Icono = "👥", Titulo = "Proveedores",      Grupo = "CONFIGURACIÓN",  Accion = AbrirModelos,         TipoForm = typeof(FrmModelos) });
            _items.Add(new() { Icono = "🔑", Titulo = "Credenciales",     Grupo = "CONFIGURACIÓN",  Accion = AbrirApis,            TipoForm = typeof(FrmApis) });
            _items.Add(new() { Icono = "📁", Titulo = "Rutas de trabajo", Grupo = "CONFIGURACIÓN",  Accion = AbrirRutas,           TipoForm = typeof(FrmRutas) });
            _items.Add(new() { Icono = "📡", Titulo = "Comunicadores",    Grupo = "CONFIGURACIÓN",  Accion = AbrirComunicadores,   TipoForm = typeof(FrmComunicadores) });
            _items.Add(new() { Icono = "🧠", Titulo = "Prompts",          Grupo = "CONFIGURACIÓN",  Accion = AbrirPrompts,         TipoForm = typeof(FrmPromts) });

            // INTELIGENCIA
            _items.Add(new() { Icono = "⚙", Titulo = "Habilidades",      Grupo = "INTELIGENCIA",  Accion = AbrirHabilidades,     TipoForm = typeof(FrmHabilidades) });
            _items.Add(new() { Icono = "🧬", Titulo = "Memoria",          Grupo = "INTELIGENCIA",  Accion = AbrirMemoria,         TipoForm = typeof(FrmMemoria) });
            _items.Add(new() { Icono = "🔎", Titulo = "Patrones",         Grupo = "INTELIGENCIA",  Accion = AbrirPatrones,        TipoForm = typeof(FrmPatrones) });
            _items.Add(new() { Icono = "🪡", Titulo = "Embeddings",       Grupo = "INTELIGENCIA",  Accion = AbrirEmbeddings,      TipoForm = typeof(FrmEmbeddings) });

            // OBSERVABILIDAD — flotantes (no entran al stack)
            _items.Add(new() { Icono = "📊", Titulo = "Tokens",           Grupo = "OBSERVABILIDAD", Accion = () => FrmConsumoTokens.MostrarOTraerAlFrente(this) });
            _items.Add(new() { Icono = "🔬", Titulo = "Traces",           Grupo = "OBSERVABILIDAD", Accion = () => FrmTraces.MostrarOTraerAlFrente(this) });
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCCIÓN DE UI
        // ════════════════════════════════════════════════════════════════════

        private void ConstruirUI()
        {
            ConstruirSidebar();
            ConstruirTopbar();
            ConstruirContenedor();
            ConstruirHome();

            Controls.Add(pnlContenedor); // Fill
            Controls.Add(pnlTopbar);     // Top
            Controls.Add(pnlSidebar);    // Left

            ActualizarBreadcrumb();
            ResaltarItemActivo(null);
        }

        private void ConstruirSidebar()
        {
            pnlSidebar = new DoubleBufferedPanel
            {
                Dock = DockStyle.Left,
                Width = SidebarExpanded,
                BackColor = BgSurface
            };

            // Línea derecha sutil
            var hairRight = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Border };
            pnlSidebar.Controls.Add(hairRight);

            // ── Logo + toggle ─────────────────────────────────────────────────
            pnlLogoArea = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = BgSurface
            };
            lblLogo = new Label
            {
                Text = "✦  OPENGIOAI",
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = Emerald4,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(20, 0),
                Size = new Size(SidebarExpanded - 80, 70),
                BackColor = Color.Transparent
            };
            btnToggleColapsar = new Button
            {
                Text = "‹",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(SidebarExpanded - 50, 17),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnToggleColapsar.FlatAppearance.BorderSize = 0;
            btnToggleColapsar.FlatAppearance.MouseOverBackColor = BgHover;
            btnToggleColapsar.Click += (_, __) => ToggleColapsar();

            pnlLogoArea.Controls.Add(lblLogo);
            pnlLogoArea.Controls.Add(btnToggleColapsar);

            var hairLogo = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Border };
            pnlLogoArea.Controls.Add(hairLogo);

            // ── Footer (segundo plano + salir) ────────────────────────────────
            pnlSidebarFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = BgSurface,
                Padding = new Padding(0, 8, 0, 8)
            };
            var hairFoot = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Border };
            pnlSidebarFooter.Controls.Add(hairFoot);

            _itemSegundoPlano = new MenuItemBoton(this, "🌑", "Segundo plano", false)
            {
                Dock = DockStyle.Top
            };
            _itemSegundoPlano.Click += (_, __) => ToggleSegundoPlano();
            ActualizarBotonSegundoPlano();

            var btnSalir = new MenuItemBoton(this, "⏻", "Salir", false, dangerColor: DangerCol)
            {
                Dock = DockStyle.Top
            };
            btnSalir.Click += (_, __) =>
            {
                _saliendoDeVerdad = true;
                Close();
            };

            pnlSidebarFooter.Controls.Add(btnSalir);
            pnlSidebarFooter.Controls.Add(_itemSegundoPlano);

            // ── Lista scrollable de items ─────────────────────────────────────
            flpItems = new FlowLayoutPanelSuave
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = BgSurface,
                Padding = new Padding(0, 10, 0, 10)
            };

            string? ultimoGrupo = null;
            foreach (var def in _items)
            {
                if (def.Grupo != ultimoGrupo)
                {
                    flpItems.Controls.Add(CrearLabelGrupo(def.Grupo));
                    ultimoGrupo = def.Grupo;
                }

                var btn = new MenuItemBoton(this, def.Icono, def.Titulo, true);
                btn.Click += (_, __) => def.Accion();
                _todosItems.Add(btn);
                if (def.TipoForm != null) _itemPorTipo[def.TipoForm] = btn;
                flpItems.Controls.Add(btn);
            }

            pnlSidebar.Controls.Add(flpItems);
            pnlSidebar.Controls.Add(pnlSidebarFooter);
            pnlSidebar.Controls.Add(pnlLogoArea);

            // Timer para animar collapse
            _timerColapsar = new System.Windows.Forms.Timer { Interval = 12 };
            _timerColapsar.Tick += AnimarColapsar;
        }

        private Label CrearLabelGrupo(string txt)
        {
            return new Label
            {
                Text = "  " + txt,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Border,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(SidebarExpanded - 8, 28),
                Margin = new Padding(8, 14, 0, 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = "GRUPO"
            };
        }

        private void ConstruirTopbar()
        {
            pnlTopbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = BgSurface
            };

            btnAtras = new Button
            {
                Text = "←",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 36),
                Location = new Point(12, 8),
                Cursor = Cursors.Hand,
                Visible = false,
                TabStop = false
            };
            btnAtras.FlatAppearance.BorderSize = 0;
            btnAtras.FlatAppearance.MouseOverBackColor = BgHover;
            btnAtras.Click += (_, __) => CerrarTopForm();

            flpBreadcrumb = new FlowLayoutPanel
            {
                Location = new Point(60, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(pnlTopbar.Width - 130, 52),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            btnCerrarForm = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlTopbar.Width - 48, 8),
                Cursor = Cursors.Hand,
                Visible = false,
                TabStop = false
            };
            btnCerrarForm.FlatAppearance.BorderSize = 0;
            btnCerrarForm.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#3b1518");
            btnCerrarForm.Click += (_, __) => CerrarTopForm();

            var hairBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Border };
            pnlTopbar.Controls.Add(hairBottom);
            pnlTopbar.Controls.Add(btnAtras);
            pnlTopbar.Controls.Add(flpBreadcrumb);
            pnlTopbar.Controls.Add(btnCerrarForm);

            pnlTopbar.Resize += (_, __) =>
            {
                flpBreadcrumb.Size = new Size(pnlTopbar.Width - 130, 52);
                btnCerrarForm.Location = new Point(pnlTopbar.Width - 48, 8);
            };
        }

        private void ConstruirContenedor()
        {
            pnlContenedor = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDeep
            };
        }

        private void ConstruirHome()
        {
            pnlHome = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDeep
            };

            var lblHi = new Label
            {
                Text = "✦",
                Font = new Font("Segoe UI Emoji", 64f),
                ForeColor = Emerald9,
                AutoSize = true,
                Location = new Point(60, 60),
                BackColor = Color.Transparent
            };
            var lblTitle = new Label
            {
                Text = "Bienvenido a OPENGIOAI",
                Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize = true,
                Location = new Point(60, 180),
                BackColor = Color.Transparent
            };
            var lblSub = new Label
            {
                Text = "Selecciona una sección del menú lateral para comenzar.\n" +
                       "Atajos:  Esc → cerrar pantalla actual   ·   Ctrl+B → colapsar menú",
                Font = new Font("Segoe UI", 10f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(62, 226),
                BackColor = Color.Transparent
            };
            pnlHome.Controls.Add(lblHi);
            pnlHome.Controls.Add(lblTitle);
            pnlHome.Controls.Add(lblSub);

            pnlContenedor.Controls.Add(pnlHome);
            pnlHome.BringToFront();
        }

        // ════════════════════════════════════════════════════════════════════
        //  COLAPSAR / EXPANDIR SIDEBAR
        // ════════════════════════════════════════════════════════════════════

        private void ToggleColapsar()
        {
            _colapsado = !_colapsado;
            _anchoObjetivo = _colapsado ? SidebarCollapsed : SidebarExpanded;
            btnToggleColapsar.Text = _colapsado ? "›" : "‹";
            _timerColapsar.Start();

            // Modo colapsado: ocultar texto en items y labels de grupo
            foreach (Control c in flpItems.Controls)
            {
                if (c is MenuItemBoton mi) mi.SetCollapsed(_colapsado);
                else if (c is Label l && (l.Tag as string) == "GRUPO") l.Visible = !_colapsado;
            }
            if (_itemSegundoPlano != null) _itemSegundoPlano.SetCollapsed(_colapsado);
            foreach (Control c in pnlSidebarFooter.Controls)
                if (c is MenuItemBoton mi) mi.SetCollapsed(_colapsado);

            lblLogo.Visible = !_colapsado;
            btnToggleColapsar.Location = _colapsado
                ? new Point((SidebarCollapsed - 36) / 2, 17)
                : new Point(SidebarExpanded - 50, 17);
        }

        private void AnimarColapsar(object? sender, EventArgs e)
        {
            int actual = pnlSidebar.Width;
            int delta = _anchoObjetivo - actual;
            if (Math.Abs(delta) <= 4)
            {
                pnlSidebar.Width = _anchoObjetivo;
                _timerColapsar.Stop();
                return;
            }
            // Easing: avanzar 30% del delta cada tick → suave
            pnlSidebar.Width = actual + (int)Math.Round(delta * 0.30);
        }

        // ════════════════════════════════════════════════════════════════════
        //  NAVEGACIÓN (stack LIFO)
        // ════════════════════════════════════════════════════════════════════

        private void AbrirEnPanel(Type tipo, string titulo, string icono, Func<Form> factory)
        {
            // Si ya está abierto → traer al frente (y mover al top de la pila)
            var existente = _nav.FirstOrDefault(n => n.Tipo == tipo);
            if (existente != null)
            {
                _nav.Remove(existente);
                _nav.Add(existente);
                MostrarTop();
                ActualizarBreadcrumb();
                ResaltarItemActivo(tipo);
                return;
            }

            // Nuevo
            var form = factory();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.Opacity = 0;

            var entry = new NavEntry { Tipo = tipo, Instancia = form, Titulo = titulo, Icono = icono };
            _nav.Add(entry);

            pnlContenedor.Controls.Add(form);
            form.BringToFront();
            form.Show();
            FadeIn(form);

            MostrarTop();
            ActualizarBreadcrumb();
            ResaltarItemActivo(tipo);
        }

        private void MostrarTop()
        {
            // Oculta todos los forms y muestra solo el top de la pila.
            foreach (var n in _nav) n.Instancia.Hide();
            if (_nav.Count == 0)
            {
                pnlHome.Visible = true;
                pnlHome.BringToFront();
                return;
            }
            pnlHome.Visible = false;
            var top = _nav[^1];
            top.Instancia.Show();
            top.Instancia.BringToFront();
        }

        private void CerrarTopForm()
        {
            if (_nav.Count == 0) return;
            var top = _nav[^1];
            _nav.RemoveAt(_nav.Count - 1);
            try
            {
                pnlContenedor.Controls.Remove(top.Instancia);
                top.Instancia.Close();
                top.Instancia.Dispose();
            }
            catch { }
            MostrarTop();
            ActualizarBreadcrumb();
            ResaltarItemActivo(_nav.Count > 0 ? _nav[^1].Tipo : null);
        }

        private void FadeIn(Form f)
        {
            var t = new System.Windows.Forms.Timer { Interval = 14 };
            t.Tick += (_, __) =>
            {
                if (f.IsDisposed) { t.Stop(); t.Dispose(); return; }
                if (f.Opacity < 1) f.Opacity = Math.Min(1, f.Opacity + 0.12);
                else { t.Stop(); t.Dispose(); }
            };
            t.Start();
        }

        // ── Breadcrumb ────────────────────────────────────────────────────────
        private void ActualizarBreadcrumb()
        {
            flpBreadcrumb.SuspendLayout();
            flpBreadcrumb.Controls.Clear();

            // Inicio (siempre)
            var lblHome = new Label
            {
                Text = "✦  Inicio",
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = _nav.Count == 0 ? Emerald4 : TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 16, 0, 0),
                Cursor = _nav.Count > 0 ? Cursors.Hand : Cursors.Default,
                BackColor = Color.Transparent
            };
            lblHome.Click += (_, __) => { while (_nav.Count > 0) CerrarTopForm(); };
            flpBreadcrumb.Controls.Add(lblHome);

            for (int i = 0; i < _nav.Count; i++)
            {
                var sep = new Label
                {
                    Text = "  ›  ",
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = Border,
                    AutoSize = true,
                    Margin = new Padding(0, 16, 0, 0),
                    BackColor = Color.Transparent
                };
                flpBreadcrumb.Controls.Add(sep);

                var entry = _nav[i];
                bool esTop = i == _nav.Count - 1;
                int idxLocal = i;
                var lbl = new Label
                {
                    Text = $"{entry.Icono}  {entry.Titulo}",
                    Font = new Font("Segoe UI Semibold", 10f, esTop ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = esTop ? Emerald4 : TextSub,
                    AutoSize = true,
                    Margin = new Padding(0, 16, 0, 0),
                    Cursor = esTop ? Cursors.Default : Cursors.Hand,
                    BackColor = Color.Transparent
                };
                if (!esTop)
                {
                    // Click en breadcrumb anterior → cerrar todos los forms encima
                    lbl.Click += (_, __) =>
                    {
                        while (_nav.Count - 1 > idxLocal) CerrarTopForm();
                    };
                }
                flpBreadcrumb.Controls.Add(lbl);
            }

            flpBreadcrumb.ResumeLayout();

            btnAtras.Visible = _nav.Count > 0;
            btnCerrarForm.Visible = _nav.Count > 0;
        }

        private void ResaltarItemActivo(Type? tipo)
        {
            foreach (var item in _todosItems) item.SetActive(false);
            if (tipo != null && _itemPorTipo.TryGetValue(tipo, out var btn))
                btn.SetActive(true);
        }

        // ════════════════════════════════════════════════════════════════════
        //  HANDLERS DE LOS ITEMS
        // ════════════════════════════════════════════════════════════════════

        private void AbrirChat() =>
            AbrirEnPanel(typeof(FrmMandos), "Chat", "💬",
                () => ActivatorUtilities.CreateInstance<FrmMandos>(_services, Miconfiguracion));

        private void AbrirSkills() =>
            AbrirEnPanel(typeof(Skills), "Skills", "⚡", () => new Skills(Miconfiguracion));

        private void AbrirAutomatizaciones() =>
            AbrirEnPanel(typeof(FrmAutomatizaciones), "Automatizaciones", "🤖",
                () => new FrmAutomatizaciones(Miconfiguracion));

        private void AbrirModelos() =>
            AbrirEnPanel(typeof(FrmModelos), "Proveedores", "👥", () => new FrmModelos());

        private void AbrirApis() =>
            AbrirEnPanel(typeof(FrmApis), "Credenciales", "🔑", () => new FrmApis());

        private void AbrirRutas() =>
            AbrirEnPanel(typeof(FrmRutas), "Rutas de trabajo", "📁", () => new FrmRutas());

        private void AbrirComunicadores() =>
            AbrirEnPanel(typeof(FrmComunicadores), "Comunicadores", "📡",
                () => new FrmComunicadores(Miconfiguracion));

        private void AbrirPrompts() =>
            AbrirEnPanel(typeof(FrmPromts), "Prompts", "🧠", () => new FrmPromts());

        private void AbrirHabilidades() =>
            AbrirEnPanel(typeof(FrmHabilidades), "Habilidades", "⚙", () => new FrmHabilidades());

        private void AbrirMemoria()
        {
            string ruta = Miconfiguracion?.MiArchivo?.Ruta ?? "";
            AbrirEnPanel(typeof(FrmMemoria), "Memoria", "🧬", () => new FrmMemoria(ruta));
        }

        private void AbrirPatrones()
        {
            string ruta = Miconfiguracion?.MiArchivo?.Ruta ?? "";
            AbrirEnPanel(typeof(FrmPatrones), "Patrones", "🔎",
                () => new FrmPatrones(ruta, Miconfiguracion ?? new ConfiguracionClient()));
        }

        private void AbrirEmbeddings()
        {
            string ruta = Miconfiguracion?.MiArchivo?.Ruta ?? "";
            AbrirEnPanel(typeof(FrmEmbeddings), "Embeddings", "🪡", () => new FrmEmbeddings(ruta));
        }

        // ════════════════════════════════════════════════════════════════════
        //  ATAJOS DE TECLADO
        // ════════════════════════════════════════════════════════════════════

        private void FrmPrincipal_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && _nav.Count > 0)
            {
                CerrarTopForm();
                e.Handled = true;
                return;
            }
            if (e.Control && e.KeyCode == Keys.B)
            {
                ToggleColapsar();
                e.Handled = true;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  MODO SEGUNDO PLANO (NotifyIcon)
        // ════════════════════════════════════════════════════════════════════

        private void InicializarBandeja()
        {
            var menu = new ContextMenuStrip();

            var itemMostrar = new ToolStripMenuItem("📂  Mostrar OPENGIOAI");
            itemMostrar.Font = new Font(itemMostrar.Font, FontStyle.Bold);
            itemMostrar.Click += (_, __) => RestaurarDesdeBandeja();

            _trayItemSegundoPlano = new ToolStripMenuItem("🌙  Cerrar a la bandeja")
            {
                Checked = Miconfiguracion?.EjecutarEnSegundoPlano == true
            };
            _trayItemSegundoPlano.Click += (_, __) => ToggleSegundoPlano();

            var itemSalir = new ToolStripMenuItem("✕  Salir");
            itemSalir.Click += (_, __) =>
            {
                _saliendoDeVerdad = true;
                Close();
            };

            menu.Items.Add(itemMostrar);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_trayItemSegundoPlano);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemSalir);

            _trayIcon = new NotifyIcon
            {
                Icon = Icon ?? SystemIcons.Application,
                Text = "OPENGIOAI",
                ContextMenuStrip = menu,
                Visible = false
            };
            _trayIcon.DoubleClick += (_, __) => RestaurarDesdeBandeja();
        }

        private void ToggleSegundoPlano()
        {
            if (Miconfiguracion == null) return;
            Miconfiguracion.EjecutarEnSegundoPlano = !Miconfiguracion.EjecutarEnSegundoPlano;
            Miconfiguracion.PreguntarSegundoPlano = false;
            GuardarConfiguracion();
            ActualizarUiSegundoPlano();
        }

        private void ActualizarUiSegundoPlano()
        {
            if (_trayItemSegundoPlano != null)
                _trayItemSegundoPlano.Checked = Miconfiguracion?.EjecutarEnSegundoPlano == true;
            ActualizarBotonSegundoPlano();
        }

        private void ActualizarBotonSegundoPlano()
        {
            if (_itemSegundoPlano == null) return;
            bool on = Miconfiguracion?.EjecutarEnSegundoPlano == true;
            _itemSegundoPlano.SetIconAndText(on ? "🌙" : "🌑",
                on ? "Segundo plano · ON" : "Segundo plano");
        }

        private void GuardarConfiguracion()
        {
            try
            {
                Utils.GuardarConfig<ConfiguracionClient>(
                    RutasProyecto.ObtenerRutaConfiguracion(), Miconfiguracion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FrmPrincipal] No se pudo guardar config: {ex.Message}");
            }
        }

        private void RestaurarDesdeBandeja()
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
            if (_trayIcon != null) _trayIcon.Visible = false;
        }

        private void EnviarABandeja()
        {
            Hide();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = true;
                _trayIcon.ShowBalloonTip(
                    1500, "OPENGIOAI",
                    "La aplicación sigue ejecutándose en segundo plano. Tus automatizaciones programadas continuarán activas.",
                    ToolTipIcon.Info);
            }
        }

        private void FrmPrincipal_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_saliendoDeVerdad ||
                e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing ||
                e.CloseReason == CloseReason.ApplicationExitCall)
            {
                LiberarRecursosFinales();
                return;
            }

            if (e.CloseReason != CloseReason.UserClosing) return;

            if (Miconfiguracion?.PreguntarSegundoPlano == true)
            {
                var dr = MessageBox.Show(
                    "¿Quieres que OPENGIOAI siga ejecutándose en segundo plano cuando cierres la ventana?\n\n" +
                    "• Sí — la app se minimiza a la bandeja del sistema y las automatizaciones programadas siguen activas.\n" +
                    "• No — la app se cierra completamente.\n\n" +
                    "Puedes cambiarlo desde el menú lateral o el icono de la bandeja.",
                    "Modo segundo plano",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (dr == DialogResult.Cancel) { e.Cancel = true; return; }

                Miconfiguracion!.EjecutarEnSegundoPlano = (dr == DialogResult.Yes);
                Miconfiguracion.PreguntarSegundoPlano = false;
                GuardarConfiguracion();
                ActualizarUiSegundoPlano();
            }

            if (Miconfiguracion?.EjecutarEnSegundoPlano == true)
            {
                e.Cancel = true;
                EnviarABandeja();
            }
            else
            {
                LiberarRecursosFinales();
            }
        }

        private void LiberarRecursosFinales()
        {
            try { _scheduler?.Dispose(); } catch { }
            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONTROLES PERSONALIZADOS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Botón del menú lateral con hover animado, indicador izquierdo y
        /// soporte de modo colapsado (solo icono).
        /// </summary>
        private sealed class MenuItemBoton : Control
        {
            private readonly FrmPrincipal _owner;
            private string _icono;
            private string _titulo;
            private readonly bool _esItem; // true → afecta a _hoverProgress; false → footer
            private readonly Color _danger;
            private bool _hovered;
            private bool _active;
            private bool _collapsed;
            private float _hoverProgress; // 0..1
            private readonly System.Windows.Forms.Timer _timer;

            public MenuItemBoton(FrmPrincipal owner, string icono, string titulo, bool esItem, Color? dangerColor = null)
            {
                _owner = owner;
                _icono = icono;
                _titulo = titulo;
                _esItem = esItem;
                _danger = dangerColor ?? Color.Transparent;

                Width = SidebarExpanded;
                Height = 40;
                Margin = new Padding(0, 1, 0, 1);
                Cursor = Cursors.Hand;
                DoubleBuffered = true;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

                _timer = new System.Windows.Forms.Timer { Interval = 14 };
                _timer.Tick += (_, __) =>
                {
                    float target = _hovered ? 1f : 0f;
                    float delta = target - _hoverProgress;
                    if (Math.Abs(delta) < 0.04f)
                    {
                        _hoverProgress = target;
                        _timer.Stop();
                    }
                    else _hoverProgress += delta * 0.30f;
                    Invalidate();
                };

                MouseEnter += (_, __) => { _hovered = true; _timer.Start(); };
                MouseLeave += (_, __) => { _hovered = false; _timer.Start(); };
            }

            public void SetActive(bool active)
            {
                if (_active == active) return;
                _active = active;
                Invalidate();
            }

            public void SetCollapsed(bool collapsed)
            {
                _collapsed = collapsed;
                Width = collapsed ? SidebarCollapsed : SidebarExpanded;
                Invalidate();
            }

            public void SetIconAndText(string icono, string titulo)
            {
                _icono = icono; _titulo = titulo;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Fondo
                Color bgBase = _active ? BgActive : BgSurface;
                Color bgHover = _danger != Color.Transparent
                    ? Color.FromArgb(40, _danger)
                    : BgHover;
                Color bg = Lerp(bgBase, bgHover, _hoverProgress);
                using (var b = new SolidBrush(bg)) g.FillRectangle(b, 0, 0, Width, Height);

                // Indicador izquierdo (barra emerald) si activo o hovered
                float indicatorAlpha = Math.Max(_active ? 1f : 0f, _hoverProgress * 0.6f);
                if (indicatorAlpha > 0.02f)
                {
                    Color barCol = _danger != Color.Transparent ? _danger : Emerald;
                    using var bar = new SolidBrush(Color.FromArgb((int)(255 * indicatorAlpha), barCol));
                    g.FillRectangle(bar, 0, 8, 3, Height - 16);
                }

                // Icono
                Color fg = _danger != Color.Transparent
                    ? Lerp(_danger, Color.White, _hoverProgress * 0.2f)
                    : (_active ? Emerald4 : Lerp(TextMuted, TextMain, _hoverProgress));

                using var iconFont = new Font("Segoe UI Emoji", 12.5f);
                var iconSize = g.MeasureString(_icono, iconFont);
                float iconX = _collapsed ? (Width - iconSize.Width) / 2f : 18;
                float iconY = (Height - iconSize.Height) / 2f;
                using (var br = new SolidBrush(fg))
                    g.DrawString(_icono, iconFont, br, iconX, iconY);

                // Texto (solo si expandido)
                if (!_collapsed)
                {
                    using var textFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    using var br = new SolidBrush(fg);
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(_titulo, textFont, br,
                        new RectangleF(48, 0, Width - 56, Height), sf);
                }
            }

            private static Color Lerp(Color a, Color b, float t)
            {
                t = Math.Max(0, Math.Min(1, t));
                return Color.FromArgb(
                    (int)(a.R + (b.R - a.R) * t),
                    (int)(a.G + (b.G - a.G) * t),
                    (int)(a.B + (b.B - a.B) * t));
            }
        }

        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);
                DoubleBuffered = true;
                UpdateStyles();
            }
        }
    }
}
