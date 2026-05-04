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
    public partial class FrmDashboard : Form
    {
        // ── Paleta dinámica ───────────────────────────────────────────────────
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

        // ── Colores de barras por proveedor ───────────────────────────────────
        private static readonly Color[] PaletaProveedores =
        {
            ColorTranslator.FromHtml("#94E6EC"),
            ColorTranslator.FromHtml("#4158D0"),
            ColorTranslator.FromHtml("#f87171"),
            ColorTranslator.FromHtml("#34d399"),
            ColorTranslator.FromHtml("#a78bfa"),
            ColorTranslator.FromHtml("#fbbf24"),
        };

        // ── Periodo activo ────────────────────────────────────────────────────
        private enum Periodo { Sesion, Hoy, Semana }
        private Periodo _periodo = Periodo.Sesion;

        // ── KPI labels ────────────────────────────────────────────────────────
        private KpiCard _kpiTokens     = null!;
        private KpiCard _kpiUsd        = null!;
        private KpiCard _kpiEjecuciones = null!;
        private KpiCard _kpiCache      = null!;

        // ── Gráficas ──────────────────────────────────────────────────────────
        private GraficaBarras _grafTokens     = null!;
        private GraficaBarras _grafProveedores = null!;

        // ── Tabla ─────────────────────────────────────────────────────────────
        private DataGridView _grid = null!;

        // ── Botones de periodo ────────────────────────────────────────────────
        private Button _btnSesion  = null!;
        private Button _btnHoy     = null!;
        private Button _btnSemana  = null!;
        private Button _btnRefresh = null!;

        // ── Paneles principales ───────────────────────────────────────────────
        private Panel _pnlHeader  = null!;
        private Panel _pnlKpis    = null!;
        private Panel _pnlCharts  = null!;
        private Panel _pnlGrid    = null!;

        // ═════════════════════════════════════════════════════════════════════
        //  Constructor
        // ═════════════════════════════════════════════════════════════════════

        public FrmDashboard()
        {
            InitializeComponent();
            DoubleBuffered = true;
            ConstruirUI();

            ConsumoTokensTracker.Instancia.OnConsumoRegistrado   += HandleConsumoReg;
            ConsumoTokensTracker.Instancia.OnEjecucionFinalizada += HandleEjecFinalizada;
            EmeraldTheme.ThemeChanged += RefrescarTema;

            FormClosed += (_, __) =>
            {
                ConsumoTokensTracker.Instancia.OnConsumoRegistrado   -= HandleConsumoReg;
                ConsumoTokensTracker.Instancia.OnEjecucionFinalizada -= HandleEjecFinalizada;
                EmeraldTheme.ThemeChanged -= RefrescarTema;
            };

            Cargar();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  UI
        // ═════════════════════════════════════════════════════════════════════

        private void ConstruirUI()
        {
            BackColor = BgDeep;
            Padding   = new Padding(0);

            ConstruirHeader();
            ConstruirKpis();
            ConstruirCharts();
            ConstruirGrid();

            Controls.Add(_pnlGrid);
            Controls.Add(_pnlCharts);
            Controls.Add(_pnlKpis);
            Controls.Add(_pnlHeader);
        }

        // ── Header ────────────────────────────────────────────────────────────

        private void ConstruirHeader()
        {
            _pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = BgSurface,
                Padding   = new Padding(20, 0, 20, 0)
            };

            var lblTitulo = new Label
            {
                Text      = "📈  Dashboard Telemetría",
                Font      = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize  = true,
                Location  = new Point(20, 14),
                BackColor = Color.Transparent
            };

            var flpPeriodo = new FlowLayoutPanel
            {
                Anchor        = AnchorStyles.Top | AnchorStyles.Right,
                Size          = new Size(310, 36),
                Location      = new Point(_pnlHeader.Width - 340, 8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };

            _btnSesion  = PeriodBtn("Sesión");
            _btnHoy     = PeriodBtn("Hoy");
            _btnSemana  = PeriodBtn("7 días");
            _btnRefresh = new Button
            {
                Text      = "↺",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(36, 30),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(6, 0, 0, 0)
            };
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.Click += (_, __) => Cargar();

            _btnSesion.Click += (_, __) => CambiarPeriodo(Periodo.Sesion);
            _btnHoy.Click    += (_, __) => CambiarPeriodo(Periodo.Hoy);
            _btnSemana.Click += (_, __) => CambiarPeriodo(Periodo.Semana);

            flpPeriodo.Controls.Add(_btnSesion);
            flpPeriodo.Controls.Add(_btnHoy);
            flpPeriodo.Controls.Add(_btnSemana);
            flpPeriodo.Controls.Add(_btnRefresh);

            _pnlHeader.Resize += (_, __) =>
                flpPeriodo.Location = new Point(_pnlHeader.Width - 340, 8);

            var hairBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Border };

            _pnlHeader.Controls.Add(lblTitulo);
            _pnlHeader.Controls.Add(flpPeriodo);
            _pnlHeader.Controls.Add(hairBottom);

            ActualizarBotonesPeriodo();
        }

        private static Button PeriodBtn(string text) => new Button
        {
            Text      = text,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(70, 30),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(2, 0, 2, 0),
            FlatAppearance = { BorderColor = Border, BorderSize = 1 }
        };

        // ── KPIs ─────────────────────────────────────────────────────────────

        private void ConstruirKpis()
        {
            _pnlKpis = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 108,
                BackColor = BgDeep,
                Padding   = new Padding(16, 10, 16, 6)
            };

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 1,
                BackColor   = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _kpiTokens      = new KpiCard("🔢", "Total Tokens",   "0",     Accent);
            _kpiUsd         = new KpiCard("💰", "Costo USD",      "$0.00",  ColorTranslator.FromHtml("#34d399"));
            _kpiEjecuciones = new KpiCard("✅", "Ejecuciones",    "0",     ColorTranslator.FromHtml("#a78bfa"));
            _kpiCache       = new KpiCard("⚡", "Cache Hit",      "0.0%",  ColorTranslator.FromHtml("#fbbf24"));

            tbl.Controls.Add(_kpiTokens,      0, 0);
            tbl.Controls.Add(_kpiUsd,         1, 0);
            tbl.Controls.Add(_kpiEjecuciones, 2, 0);
            tbl.Controls.Add(_kpiCache,       3, 0);

            _pnlKpis.Controls.Add(tbl);
        }

        // ── Charts ────────────────────────────────────────────────────────────

        private void ConstruirCharts()
        {
            _pnlCharts = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 230,
                BackColor = BgDeep,
                Padding   = new Padding(16, 4, 16, 8)
            };

            _grafTokens = new GraficaBarras
            {
                Titulo       = "Tokens por ejecución (últimas 15)",
                FormatoValor = "{0:N0}",
                ColorDefault = Accent,
                Dock         = DockStyle.None,
                BackColor    = BgCard
            };

            _grafProveedores = new GraficaBarras
            {
                Titulo       = "Tokens por proveedor / modelo",
                FormatoValor = "{0:N0}",
                ColorDefault = ColorTranslator.FromHtml("#4158D0"),
                Dock         = DockStyle.None,
                BackColor    = BgCard
            };

            _pnlCharts.Controls.Add(_grafTokens);
            _pnlCharts.Controls.Add(_grafProveedores);

            _pnlCharts.Resize += LayoutCharts;
            LayoutCharts(null, EventArgs.Empty);
        }

        private void LayoutCharts(object? sender, EventArgs e)
        {
            if (_grafTokens == null || _grafProveedores == null) return;
            int gap   = 10;
            int h     = _pnlCharts.Height - _pnlCharts.Padding.Vertical;
            int half  = (_pnlCharts.Width - _pnlCharts.Padding.Horizontal - gap) / 2;
            if (half < 80) return;

            _grafTokens.SetBounds(
                _pnlCharts.Padding.Left, _pnlCharts.Padding.Top, half, h);
            _grafProveedores.SetBounds(
                _pnlCharts.Padding.Left + half + gap, _pnlCharts.Padding.Top, half, h);
        }

        // ── Grid ─────────────────────────────────────────────────────────────

        private void ConstruirGrid()
        {
            _pnlGrid = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgDeep,
                Padding   = new Padding(16, 4, 16, 16)
            };

            var lblGridTitle = new Label
            {
                Text      = "Ejecuciones recientes",
                Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize  = true,
                Location  = new Point(16, 6),
                BackColor = Color.Transparent
            };

            _grid = new DataGridView
            {
                Dock                    = DockStyle.Fill,
                BackgroundColor         = BgCard,
                GridColor               = Border,
                BorderStyle             = BorderStyle.None,
                RowHeadersVisible       = false,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                AllowUserToResizeRows   = false,
                ReadOnly                = true,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode        = DataGridViewAutoSizeRowsMode.None,
                RowTemplate             = { Height = 28 },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight     = 32,
                ScrollBars              = ScrollBars.Vertical,
                MultiSelect             = false,
                Font                    = new Font("Segoe UI", 8.5f)
            };

            EstilosGrid();
            ColumnsGrid();

            var pnlInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 28, 0, 0) };
            pnlInner.Controls.Add(_grid);

            _pnlGrid.Controls.Add(pnlInner);
            _pnlGrid.Controls.Add(lblGridTitle);
        }

        private void EstilosGrid()
        {
            bool dark = EmeraldTheme.IsDark;

            _grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor         = BgCard,
                ForeColor         = TextMain,
                SelectionBackColor = ColorTranslator.FromHtml(dark ? "#1a3a5c" : "#C5D8F0"),
                SelectionForeColor = TextMain,
                Padding           = new Padding(4, 0, 4, 0),
                Alignment         = DataGridViewContentAlignment.MiddleLeft
            };
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor         = BgSurface,
                ForeColor         = TextMain,
                SelectionBackColor = ColorTranslator.FromHtml(dark ? "#1a3a5c" : "#C5D8F0"),
                SelectionForeColor = TextMain,
                Padding           = new Padding(4, 0, 4, 0)
            };
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor   = BgSurface,
                ForeColor   = TextSub,
                Font        = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                SelectionBackColor = BgSurface,
                SelectionForeColor = TextSub,
                Padding     = new Padding(4, 0, 4, 0),
                Alignment   = DataGridViewContentAlignment.MiddleLeft
            };
        }

        private void ColumnsGrid()
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Instruccion", HeaderText = "Instrucción", Width = 220, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Modelo",     HeaderText = "Modelo / Proveedor", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Tokens",     HeaderText = "Tokens",   Width = 80,  DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Costo",      HeaderText = "Costo $",  Width = 80,  DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Duracion",   HeaderText = "Duración", Width = 80,  DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Cache",      HeaderText = "Cache %",  Width = 70,  DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Estado",     HeaderText = "Estado",   Width = 80 });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Carga de datos
        // ═════════════════════════════════════════════════════════════════════

        private void Cargar()
        {
            if (IsDisposed) return;
            if (_periodo == Periodo.Sesion) CargarSesion();
            else CargarHistorico(_periodo == Periodo.Hoy ? 1 : 7);
        }

        // ── Modo sesión ───────────────────────────────────────────────────────

        private void CargarSesion()
        {
            var (tokens, usd)     = ConsumoTokensTracker.Instancia.TotalSesion();
            var (cacheRead, _)    = ConsumoTokensTracker.Instancia.CacheSesion();
            var historial         = ConsumoTokensTracker.Instancia.Historial();
            int ejecuciones       = historial.Count;
            int totalPrompt       = historial.Sum(e => e.TotalPromptTokens);
            double cacheHit       = totalPrompt > 0 ? (double)cacheRead / totalPrompt * 100.0 : 0;

            _kpiTokens.SetValor(tokens.ToString("N0"));
            _kpiUsd.SetValor("$" + usd.ToString("F4"));
            _kpiEjecuciones.SetValor(ejecuciones.ToString());
            _kpiCache.SetValor(cacheHit.ToString("F1") + "%");

            // ── Gráfica tokens por ejecución ──
            var datosTokens = historial
                .Take(15)
                .Reverse()
                .Select(e => new DatoGrafica
                {
                    Etiqueta = Acortar(e.Instruccion, 10),
                    Valor    = e.TotalTokens
                })
                .ToList();
            _grafTokens.SetDatos(datosTokens);

            // ── Gráfica por proveedor ──
            var proveedor = historial
                .SelectMany(e => e.Llamadas)
                .GroupBy(c => $"{c.Proveedor ?? "?"}/{ModeloCorto(c.Modelo)}")
                .Select((g, idx) => new DatoGrafica
                {
                    Etiqueta      = g.Key,
                    Valor         = g.Sum(c => c.TotalTokens),
                    ColorOverride = PaletaProveedores[idx % PaletaProveedores.Length]
                })
                .OrderByDescending(d => d.Valor)
                .Take(8)
                .ToList();
            _grafProveedores.SetDatos(proveedor);

            // ── Grid ──
            _grid.SuspendLayout();
            _grid.Rows.Clear();
            foreach (var e in historial)
            {
                string modelo   = PrimerModelo(e);
                double durSec   = e.Fin.HasValue ? (e.Fin.Value - e.Inicio).TotalSeconds : 0;
                int    prompt   = e.TotalPromptTokens;
                double chHit    = prompt > 0 ? (double)e.TotalCacheReadTokens / prompt * 100.0 : 0;

                _grid.Rows.Add(
                    Acortar(e.Instruccion, 60),
                    modelo,
                    e.TotalTokens.ToString("N0"),
                    "$" + e.CostoEstimadoUsd.ToString("F5"),
                    durSec > 0 ? durSec.ToString("F1") + "s" : "—",
                    chHit.ToString("F1") + "%",
                    e.Fin.HasValue ? "✅ OK" : "🔄 activa"
                );
            }
            _grid.ResumeLayout();
        }

        // ── Modo histórico (Hoy / 7 días) ────────────────────────────────────

        private void CargarHistorico(int dias)
        {
            var items = new List<TraceIndiceItem>();
            var fechas = TraceStorage.FechasDisponibles();
            DateTime limite = DateTime.Today.AddDays(-(dias - 1));

            foreach (var fecha in fechas.Where(f => f >= limite))
            {
                try { items.AddRange(TraceStorage.LeerIndice(fecha)); }
                catch { }
            }

            long totalTokens   = items.Sum(i => (long)i.TotalTokens);
            decimal totalCosto = items.Sum(i => i.TotalCostoUsd);
            int ejecuciones    = items.Count;

            _kpiTokens.SetValor(totalTokens.ToString("N0"));
            _kpiUsd.SetValor("$" + totalCosto.ToString("F4"));
            _kpiEjecuciones.SetValor(ejecuciones.ToString());
            _kpiCache.SetValor("—");

            // ── Gráfica tokens por ejecución ──
            var datosTokens = items
                .OrderBy(i => i.Inicio)
                .TakeLast(15)
                .Select(i => new DatoGrafica
                {
                    Etiqueta = Acortar(i.Instruccion, 10),
                    Valor    = i.TotalTokens
                })
                .ToList();
            _grafTokens.SetDatos(datosTokens);

            // ── Gráfica por modelo ──
            var porModelo = items
                .GroupBy(i => $"{i.Servicio}/{ModeloCorto(i.Modelo)}")
                .Select((g, idx) => new DatoGrafica
                {
                    Etiqueta      = g.Key,
                    Valor         = g.Sum(i => i.TotalTokens),
                    ColorOverride = PaletaProveedores[idx % PaletaProveedores.Length]
                })
                .OrderByDescending(d => d.Valor)
                .Take(8)
                .ToList();
            _grafProveedores.SetDatos(porModelo);

            // ── Grid ──
            _grid.SuspendLayout();
            _grid.Rows.Clear();
            foreach (var i in items.OrderByDescending(x => x.Inicio))
            {
                string dur = i.DuracionMs > 0 ? (i.DuracionMs / 1000.0).ToString("F1") + "s" : "—";
                _grid.Rows.Add(
                    Acortar(i.Instruccion, 60),
                    $"{i.Servicio}/{ModeloCorto(i.Modelo)}",
                    i.TotalTokens.ToString("N0"),
                    "$" + i.TotalCostoUsd.ToString("F5"),
                    dur,
                    "—",
                    EstadoStr(i.Estado)
                );
            }
            _grid.ResumeLayout();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Periodo
        // ═════════════════════════════════════════════════════════════════════

        private void CambiarPeriodo(Periodo p)
        {
            _periodo = p;
            ActualizarBotonesPeriodo();
            Cargar();
        }

        private void ActualizarBotonesPeriodo()
        {
            if (_btnSesion == null) return;

            ActualizarEstadoBtn(_btnSesion, _periodo == Periodo.Sesion);
            ActualizarEstadoBtn(_btnHoy,    _periodo == Periodo.Hoy);
            ActualizarEstadoBtn(_btnSemana, _periodo == Periodo.Semana);
        }

        private static void ActualizarEstadoBtn(Button btn, bool activo)
        {
            btn.ForeColor = activo ? EmeraldTheme.Emerald400 : EmeraldTheme.TextMuted;
            btn.FlatAppearance.BorderColor = activo
                ? EmeraldTheme.Emerald400
                : (EmeraldTheme.IsDark
                    ? ColorTranslator.FromHtml("#1a3a5c")
                    : ColorTranslator.FromHtml("#C5D8F0"));
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Eventos en tiempo real
        // ═════════════════════════════════════════════════════════════════════

        private void HandleConsumoReg(ConsumoTokens c)
        {
            if (_periodo != Periodo.Sesion) return;
            if (IsDisposed) return;
            if (InvokeRequired) { try { Invoke(() => HandleConsumoReg(c)); } catch { } return; }
            Cargar();
        }

        private void HandleEjecFinalizada(EjecucionConsumo e)
        {
            if (_periodo != Periodo.Sesion) return;
            if (IsDisposed) return;
            if (InvokeRequired) { try { Invoke(() => HandleEjecFinalizada(e)); } catch { } return; }
            Cargar();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Tema
        // ═════════════════════════════════════════════════════════════════════

        private void RefrescarTema()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { Invoke(RefrescarTema); return; }

            BackColor = BgDeep;
            if (_pnlHeader  != null) _pnlHeader.BackColor  = BgSurface;
            if (_pnlKpis    != null) _pnlKpis.BackColor    = BgDeep;
            if (_pnlCharts  != null) _pnlCharts.BackColor  = BgDeep;
            if (_pnlGrid    != null) _pnlGrid.BackColor    = BgDeep;
            if (_grafTokens     != null) { _grafTokens.BackColor     = BgCard; _grafTokens.Invalidate(); }
            if (_grafProveedores != null) { _grafProveedores.BackColor = BgCard; _grafProveedores.Invalidate(); }
            if (_kpiTokens      != null) _kpiTokens.RefrescarTema();
            if (_kpiUsd         != null) _kpiUsd.RefrescarTema();
            if (_kpiEjecuciones != null) _kpiEjecuciones.RefrescarTema();
            if (_kpiCache       != null) _kpiCache.RefrescarTema();

            EstilosGrid();
            ActualizarBotonesPeriodo();
            Invalidate(true);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Helpers
        // ═════════════════════════════════════════════════════════════════════

        private static string Acortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s[..max] + "…";
        }

        private static string ModeloCorto(string? modelo)
        {
            if (string.IsNullOrEmpty(modelo)) return "?";
            // Acortar nombres largos como "claude-3-5-sonnet-20241022" → "sonnet"
            if (modelo.Contains("sonnet"))    return "sonnet";
            if (modelo.Contains("haiku"))     return "haiku";
            if (modelo.Contains("opus"))      return "opus";
            if (modelo.Contains("gemini"))    return "gemini";
            if (modelo.Contains("gpt"))       return "gpt";
            if (modelo.Contains("mistral"))   return "mistral";
            return modelo.Length > 12 ? modelo[..12] : modelo;
        }

        private static string PrimerModelo(EjecucionConsumo e)
        {
            if (e.Llamadas.Count == 0) return "—";
            var c = e.Llamadas[0];
            return $"{c.Proveedor}/{ModeloCorto(c.Modelo)}";
        }

        private static string EstadoStr(SpanEstado estado) => estado switch
        {
            SpanEstado.Ok        => "✅ OK",
            SpanEstado.Error     => "❌ Error",
            SpanEstado.EnCurso   => "🔄 activa",
            SpanEstado.Cancelado => "⛔ Cancelado",
            _                    => estado.ToString()
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  KpiCard — tarjeta de indicador KPI
    // ═════════════════════════════════════════════════════════════════════════

    internal sealed class KpiCard : Panel
    {
        private readonly Label _lblIcono;
        private readonly Label _lblNombre;
        private readonly Label _lblValor;
        private readonly Color _accentColor;

        public KpiCard(string icono, string nombre, string valor, Color accentColor)
        {
            _accentColor = accentColor;
            DoubleBuffered = true;
            Margin  = new Padding(4);
            Cursor  = Cursors.Default;
            Paint  += OnPaintCard;

            _lblIcono = new Label
            {
                Text      = icono,
                Font      = new Font("Segoe UI Emoji", 18f),
                ForeColor = accentColor,
                AutoSize  = true,
                Location  = new Point(14, 12),
                BackColor = Color.Transparent
            };
            _lblNombre = new Label
            {
                Text      = nombre,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = EmeraldTheme.TextMuted,
                AutoSize  = true,
                Location  = new Point(52, 12),
                BackColor = Color.Transparent
            };
            _lblValor = new Label
            {
                Text      = valor,
                Font      = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(52, 32),
                BackColor = Color.Transparent
            };

            Controls.Add(_lblIcono);
            Controls.Add(_lblNombre);
            Controls.Add(_lblValor);
        }

        public void SetValor(string v)
        {
            _lblValor.Text = v;
        }

        public void RefrescarTema()
        {
            BackColor       = EmeraldTheme.BgCard;
            _lblNombre.ForeColor = EmeraldTheme.TextMuted;
            _lblValor.ForeColor  = EmeraldTheme.TextPrimary;
            Invalidate();
        }

        private void OnPaintCard(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = ClientRectangle;

            // Fondo con radio
            using var path = RoundedRect(rc, 8);
            using var bg   = new SolidBrush(EmeraldTheme.BgCard);
            g.FillPath(bg, path);

            // Borde con acento sutil
            using var pen = new Pen(Color.FromArgb(60, _accentColor), 1f);
            g.DrawPath(pen, path);

            // Línea lateral de acento
            using var penLeft = new Pen(_accentColor, 3f);
            g.DrawLine(penLeft, 0, 8, 0, rc.Height - 8);
        }

        private static GraphicsPath RoundedRect(Rectangle r, float radius)
        {
            float d    = radius * 2f;
            var   path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
