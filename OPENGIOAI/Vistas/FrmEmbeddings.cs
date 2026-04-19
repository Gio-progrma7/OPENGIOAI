// ============================================================
//  FrmEmbeddings.cs  — Fase C (RAG)
//
//  UI de configuración y operación del subsistema de embeddings.
//
//  SECCIONES:
//    · Proveedor (Ollama / OpenAI) + modelo + endpoint + api key
//    · Parámetros de recuperación (TopK, ChunkSize, ChunkOverlap)
//    · Acciones: Probar conexión · Guardar · Re-indexar · Limpiar
//    · Estadísticas: chunks en store, última indexación, manifest
//
//  El form se hospeda en pnlContenedor de FrmPrincipal. Igual que
//  FrmHabilidades: pintamos en Load y re-pintamos en Resize para
//  adaptarnos al tamaño real que nos otorga el contenedor.
// ============================================================

using Newtonsoft.Json;
using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmEmbeddings : Form
    {
        private readonly string _rutaWorkspace;

        // Controles dinámicos que consultamos/actualizamos tras acciones.
        private RadioButton? _rbOllama;
        private RadioButton? _rbOpenAI;
        private TextBox? _txtModelo;
        private TextBox? _txtEndpoint;
        private TextBox? _txtApiKey;
        private NumericUpDown? _nudTopK;
        private NumericUpDown? _nudChunkSize;
        private NumericUpDown? _nudChunkOverlap;
        private Label? _lblStatus;
        private Label? _lblStats;

        public FrmEmbeddings(string rutaWorkspace)
        {
            InitializeComponent();
            _rutaWorkspace = rutaWorkspace ?? "";
            AplicarTema();

            Load += (s, e) => Render();
            pnlContenido.Resize += (s, e) => Render();
        }

        private void AplicarTema()
        {
            BackColor = EmeraldTheme.BgDeep;
            ForeColor = EmeraldTheme.TextPrimary;

            pnlRoot.BackColor       = EmeraldTheme.BgDeep;
            pnlHeader.BackColor     = EmeraldTheme.BgDeep;
            pnlContenido.BackColor  = EmeraldTheme.BgDeep;

            lblTitulo.ForeColor    = EmeraldTheme.Emerald400;
            lblSubtitulo.ForeColor = EmeraldTheme.TextMuted;
        }

        // ══════════════════ Render ══════════════════

        private void Render()
        {
            pnlContenido.SuspendLayout();
            pnlContenido.Controls.Clear();

            var cfg = EmbeddingsService.CargarConfig();

            int anchoUtil = Math.Max(pnlContenido.ClientSize.Width - 40, 820);
            int y = 10;

            // ── Tarjeta 1: Proveedor / modelo / credenciales ──
            var cardProveedor = CrearTarjeta(anchoUtil, 260, "🌐  Proveedor y modelo");
            cardProveedor.Location = new Point(10, y);
            pnlContenido.Controls.Add(cardProveedor);
            ConstruirTarjetaProveedor(cardProveedor, cfg);
            y += 260 + 14;

            // ── Tarjeta 2: Parámetros de RAG ──
            var cardParams = CrearTarjeta(anchoUtil, 180, "🎯  Parámetros de recuperación");
            cardParams.Location = new Point(10, y);
            pnlContenido.Controls.Add(cardParams);
            ConstruirTarjetaParametros(cardParams, cfg);
            y += 180 + 14;

            // ── Tarjeta 3: Acciones + status ──
            var cardAcciones = CrearTarjeta(anchoUtil, 170, "⚡  Acciones");
            cardAcciones.Location = new Point(10, y);
            pnlContenido.Controls.Add(cardAcciones);
            ConstruirTarjetaAcciones(cardAcciones);
            y += 170 + 14;

            // ── Tarjeta 4: Estadísticas ──
            var cardStats = CrearTarjeta(anchoUtil, 130, "📊  Estado del índice");
            cardStats.Location = new Point(10, y);
            pnlContenido.Controls.Add(cardStats);
            ConstruirTarjetaStats(cardStats);

            pnlContenido.ResumeLayout();
        }

        private Panel CrearTarjeta(int ancho, int alto, string titulo)
        {
            var card = new Panel
            {
                Size = new Size(ancho, alto),
                BackColor = EmeraldTheme.BgCard,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(40, 40, 40), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lbl = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = EmeraldTheme.Emerald400,
                AutoSize = true,
                Location = new Point(14, 10),
            };
            card.Controls.Add(lbl);
            return card;
        }

        // ── Tarjeta Proveedor ──

        private void ConstruirTarjetaProveedor(Panel card, EmbeddingConfig cfg)
        {
            _rbOllama = new RadioButton
            {
                Text     = "Ollama (local · gratis)",
                Checked  = cfg.Proveedor == ProveedorEmbedding.Ollama,
                Font     = new Font("Segoe UI", 10F),
                ForeColor= EmeraldTheme.TextPrimary,
                Location = new Point(20, 44),
                AutoSize = true,
            };
            _rbOpenAI = new RadioButton
            {
                Text     = "OpenAI (API · pago por uso)",
                Checked  = cfg.Proveedor == ProveedorEmbedding.OpenAI,
                Font     = new Font("Segoe UI", 10F),
                ForeColor= EmeraldTheme.TextPrimary,
                Location = new Point(220, 44),
                AutoSize = true,
            };
            card.Controls.Add(_rbOllama);
            card.Controls.Add(_rbOpenAI);

            CrearLabel(card, "Modelo:",        14, 84);
            _txtModelo = CrearTextBox(card, cfg.Modelo, 120, 80, 400);

            CrearLabel(card, "Endpoint URL:",  14, 120);
            _txtEndpoint = CrearTextBox(card, cfg.EndpointUrl, 120, 116, 400);

            CrearLabel(card, "API Key:",       14, 156);
            _txtApiKey = CrearTextBox(card, cfg.ApiKey, 120, 152, 400);
            _txtApiKey.UseSystemPasswordChar = true;

            var hint = new Label
            {
                Text = "Ollama: no necesita API Key. OpenAI: requiere clave con permiso de embeddings.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = EmeraldTheme.TextMuted,
                Location = new Point(14, 198),
                AutoSize = true,
            };
            card.Controls.Add(hint);

            // Cambios de proveedor → sugerir modelo por defecto
            _rbOllama.CheckedChanged += (s, e) =>
            {
                if (_rbOllama.Checked && string.IsNullOrWhiteSpace(_txtModelo!.Text))
                    _txtModelo.Text = "nomic-embed-text";
            };
            _rbOpenAI.CheckedChanged += (s, e) =>
            {
                if (_rbOpenAI.Checked && (string.IsNullOrWhiteSpace(_txtModelo!.Text)
                                          || _txtModelo.Text.StartsWith("nomic", StringComparison.OrdinalIgnoreCase)))
                    _txtModelo.Text = "text-embedding-3-small";
            };
        }

        // ── Tarjeta Parámetros ──

        private void ConstruirTarjetaParametros(Panel card, EmbeddingConfig cfg)
        {
            CrearLabel(card, "TopK (chunks por instrucción):", 14, 54);
            _nudTopK = CrearNumeric(card, cfg.TopK, 1, 20, 240, 50);

            CrearLabel(card, "ChunkSize (caracteres):", 14, 90);
            _nudChunkSize = CrearNumeric(card, cfg.ChunkSize, 100, 4000, 240, 86);

            CrearLabel(card, "ChunkOverlap (caracteres):", 14, 126);
            _nudChunkOverlap = CrearNumeric(card, cfg.ChunkOverlap, 0, 1000, 240, 122);

            var hint = new Label
            {
                Text = "Valores recomendados: TopK=5, ChunkSize=500, Overlap=80.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = EmeraldTheme.TextMuted,
                Location = new Point(360, 90),
                AutoSize = true,
            };
            card.Controls.Add(hint);
        }

        // ── Tarjeta Acciones ──

        private void ConstruirTarjetaAcciones(Panel card)
        {
            var btnProbar = CrearBoton(card, "🔌  Probar conexión", 14, 54);
            var btnGuardar = CrearBoton(card, "💾  Guardar",          200, 54);
            var btnReindex = CrearBoton(card, "♻  Re-indexar",        360, 54);
            var btnLimpiar = CrearBoton(card, "🗑  Limpiar índice",   520, 54);

            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F),
                ForeColor = EmeraldTheme.TextSecondary,
                Location = new Point(14, 110),
                AutoSize = true,
                MaximumSize = new Size(card.Width - 28, 50),
            };
            card.Controls.Add(_lblStatus);

            btnProbar.Click  += async (s, e) => await ProbarConexionAsync();
            btnGuardar.Click += (s, e) => GuardarConfig();
            btnReindex.Click += async (s, e) => await ReindexarAsync(forzar: false);
            btnLimpiar.Click += (s, e) => LimpiarIndice();
        }

        // ── Tarjeta Stats ──

        private void ConstruirTarjetaStats(Panel card)
        {
            _lblStats = new Label
            {
                Text = "Calculando...",
                Font = new Font("Consolas", 9.5F),
                ForeColor = EmeraldTheme.TextSecondary,
                Location = new Point(14, 48),
                AutoSize = false,
                Size = new Size(card.Width - 28, 70),
            };
            card.Controls.Add(_lblStats);

            ActualizarStats();
        }

        // ══════════════════ Helpers UI ══════════════════

        private void CrearLabel(Panel card, string texto, int x, int y)
        {
            card.Controls.Add(new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = EmeraldTheme.TextSecondary,
                Location = new Point(x, y),
                AutoSize = true,
            });
        }

        private TextBox CrearTextBox(Panel card, string valor, int x, int y, int ancho)
        {
            var tb = new TextBox
            {
                Text = valor ?? "",
                Location = new Point(x, y),
                Width = ancho,
                BackColor = EmeraldTheme.BgSurface,
                ForeColor = EmeraldTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5F),
            };
            card.Controls.Add(tb);
            return tb;
        }

        private NumericUpDown CrearNumeric(Panel card, int valor, int min, int max, int x, int y)
        {
            var nud = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value   = Math.Max(min, Math.Min(max, valor)),
                Location = new Point(x, y),
                Width = 90,
                BackColor = EmeraldTheme.BgSurface,
                ForeColor = EmeraldTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5F),
            };
            card.Controls.Add(nud);
            return nud;
        }

        private Button CrearBoton(Panel card, string texto, int x, int y)
        {
            var btn = new Button
            {
                Text = texto,
                Size = new Size(150, 38),
                Location = new Point(x, y),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = EmeraldTheme.Emerald900,
                ForeColor = EmeraldTheme.TextPrimary,
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor = EmeraldTheme.Emerald400;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = EmeraldTheme.Emerald600;
            card.Controls.Add(btn);
            return btn;
        }

        private void MostrarStatus(string texto, bool esError = false)
        {
            if (_lblStatus == null) return;
            _lblStatus.Text = texto;
            _lblStatus.ForeColor = esError ? EmeraldTheme.Error : EmeraldTheme.TextSecondary;
        }

        // ══════════════════ Acciones ══════════════════

        private EmbeddingConfig RecolectarConfigDeUI()
        {
            var cfg = EmbeddingsService.CargarConfig().Clone();

            if (_rbOpenAI?.Checked == true) cfg.Proveedor = ProveedorEmbedding.OpenAI;
            else                            cfg.Proveedor = ProveedorEmbedding.Ollama;

            cfg.Modelo       = (_txtModelo?.Text   ?? "").Trim();
            cfg.EndpointUrl  = (_txtEndpoint?.Text ?? "").Trim();
            cfg.ApiKey       = (_txtApiKey?.Text   ?? "").Trim();
            cfg.TopK         = (int)(_nudTopK?.Value         ?? 5);
            cfg.ChunkSize    = (int)(_nudChunkSize?.Value    ?? 500);
            cfg.ChunkOverlap = (int)(_nudChunkOverlap?.Value ?? 80);

            // Dimensión esperada típica por modelo — opcional, el tracker real
            // la valida en ProbarConexion.
            if (cfg.Modelo.Contains("nomic",   StringComparison.OrdinalIgnoreCase)) cfg.DimensionEsperada = 768;
            else if (cfg.Modelo.Contains("small", StringComparison.OrdinalIgnoreCase)) cfg.DimensionEsperada = 1536;
            else if (cfg.Modelo.Contains("large", StringComparison.OrdinalIgnoreCase)) cfg.DimensionEsperada = 3072;

            return cfg;
        }

        private void GuardarConfig()
        {
            try
            {
                var cfg = RecolectarConfigDeUI();
                if (string.IsNullOrWhiteSpace(cfg.Modelo))
                {
                    MostrarStatus("❌ Indica un modelo antes de guardar.", esError: true);
                    return;
                }
                if (cfg.Proveedor == ProveedorEmbedding.OpenAI && string.IsNullOrWhiteSpace(cfg.ApiKey))
                {
                    MostrarStatus("❌ OpenAI requiere API Key.", esError: true);
                    return;
                }
                EmbeddingsService.GuardarConfig(cfg);
                MostrarStatus("✓ Configuración guardada.");
                ActualizarStats();
            }
            catch (Exception ex)
            {
                MostrarStatus($"❌ {ex.Message}", esError: true);
            }
        }

        private async Task ProbarConexionAsync()
        {
            try
            {
                MostrarStatus("⏳ Probando…");
                var cfg = RecolectarConfigDeUI();
                var (ok, mensaje, dim) = await EmbeddingsService.ProbarConexionAsync(cfg, CancellationToken.None);
                if (ok)
                {
                    MostrarStatus($"✓ Conexión OK. Dimensión detectada: {dim}.");
                }
                else
                {
                    MostrarStatus($"❌ {mensaje}", esError: true);
                }
            }
            catch (Exception ex)
            {
                MostrarStatus($"❌ {ex.Message}", esError: true);
            }
        }

        private async Task ReindexarAsync(bool forzar)
        {
            if (string.IsNullOrWhiteSpace(_rutaWorkspace))
            {
                MostrarStatus("❌ No hay ruta de trabajo activa.", esError: true);
                return;
            }

            // Guardar config primero — sería frustrante re-indexar con la anterior.
            try { EmbeddingsService.GuardarConfig(RecolectarConfigDeUI()); } catch { }

            try
            {
                MostrarStatus("⏳ Indexando…");
                var progreso = new Progress<string>(msg => MostrarStatus("⏳ " + msg));
                var r = await MemoriaIndexer.IndexarAsync(
                    _rutaWorkspace,
                    forzarReindexacion: true, // desde UI, siempre forzamos
                    progreso: progreso,
                    ct: CancellationToken.None);
                MostrarStatus(string.IsNullOrWhiteSpace(r.Mensaje)
                    ? $"✓ OK. Chunks indexados: {r.ChunksIndexados}."
                    : "✓ " + r.Mensaje);
                ActualizarStats();
            }
            catch (Exception ex)
            {
                MostrarStatus($"❌ {ex.Message}", esError: true);
            }
        }

        private void LimpiarIndice()
        {
            if (string.IsNullOrWhiteSpace(_rutaWorkspace))
            {
                MostrarStatus("❌ No hay ruta de trabajo activa.", esError: true);
                return;
            }

            var confirm = MessageBox.Show(
                "Esto borrará todos los embeddings indexados de este workspace.\n" +
                "La memoria textual (Hechos.md / Episodios.md) NO se toca.\n\n¿Continuar?",
                "Limpiar índice",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var store = new VectorStore(_rutaWorkspace);
                store.Cargar();
                store.Limpiar();

                // Borrar manifest también — la próxima indexación será completa.
                try
                {
                    string manifestPath = RutasProyecto.ObtenerRutaEmbeddingsManifest(_rutaWorkspace);
                    if (File.Exists(manifestPath)) File.Delete(manifestPath);
                }
                catch { }

                MostrarStatus("✓ Índice limpio.");
                ActualizarStats();
            }
            catch (Exception ex)
            {
                MostrarStatus($"❌ {ex.Message}", esError: true);
            }
        }

        private void ActualizarStats()
        {
            if (_lblStats == null) return;
            try
            {
                if (string.IsNullOrWhiteSpace(_rutaWorkspace))
                {
                    _lblStats.Text = "Sin ruta de trabajo activa.";
                    return;
                }

                var store = new VectorStore(_rutaWorkspace);
                store.Cargar();
                var porFuente = store.ContarPorFuente();

                var manifest = LeerManifest(_rutaWorkspace);
                string ultima = manifest.UltimaIndexacion == default
                    ? "nunca"
                    : manifest.UltimaIndexacion.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

                string lineaFuentes = porFuente.Count == 0
                    ? "(sin chunks)"
                    : string.Join("  ·  ", System.Linq.Enumerable.Select(porFuente, kv => $"{kv.Key}: {kv.Value}"));

                _lblStats.Text =
                    $"Total chunks: {store.Count}   ({lineaFuentes})\n" +
                    $"Proveedor: {manifest.Proveedor ?? "-"}   Modelo: {manifest.Modelo ?? "-"}   Dim: {manifest.Dimension}\n" +
                    $"Última indexación: {ultima}";
            }
            catch (Exception ex)
            {
                _lblStats.Text = $"(error leyendo stats: {ex.Message})";
            }
        }

        private static ManifestEmbeddings LeerManifest(string rutaWorkspace)
        {
            try
            {
                string path = RutasProyecto.ObtenerRutaEmbeddingsManifest(rutaWorkspace);
                if (!File.Exists(path)) return new ManifestEmbeddings();
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ManifestEmbeddings>(json) ?? new ManifestEmbeddings();
            }
            catch
            {
                return new ManifestEmbeddings();
            }
        }
    }
}
