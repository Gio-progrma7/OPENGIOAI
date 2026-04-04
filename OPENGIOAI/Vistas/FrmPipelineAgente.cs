// ============================================================
//  FrmPipelineAgente.cs  — UI para 1.3 y 1.4
//
//  Permite al usuario ejecutar:
//    • Agente con Planificación (ReAct/CoT)  — 1.3
//    • Pipeline Multi-Agente (4 agentes)     — 1.4
//
//  Construido íntegramente en código (sin Designer)
//  para mantener independencia del generador de UI.
// ============================================================

using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;

namespace OPENGIOAI.Vistas
{
    public class FrmPipelineAgente : Form
    {
        // ── Controles ────────────────────────────────────────────
        private readonly Panel _pnlTop;
        private readonly Label _lblTitulo;
        private readonly ComboBox _cmbModo;
        private readonly Label _lblModo;

        private readonly Panel _pnlInput;
        private readonly Label _lblInstruccion;
        private readonly TextBox _txtInstruccion;
        private readonly Button _btnEjecutar;
        private readonly Button _btnCancelar;

        private readonly Panel _pnlAgentes;         // Indicadores visuales de agentes
        private readonly Label[] _lblAgentes;

        private readonly Panel _pnlLog;
        private readonly RichTextBox _rtbLog;

        // ── Estado ───────────────────────────────────────────────
        private CancellationTokenSource? _cts;
        private ConfiguracionClient _config = new();

        // ── Colores del tema ─────────────────────────────────────
        private static readonly Color ColorFondo = Color.FromArgb(15, 23, 42);
        private static readonly Color ColorPanel = Color.FromArgb(30, 41, 59);
        private static readonly Color ColorTexto = Color.FromArgb(148, 163, 184);
        private static readonly Color ColorAcento = Color.FromArgb(59, 130, 246);
        private static readonly Color ColorVerde = Color.FromArgb(34, 197, 94);
        private static readonly Color ColorAmarillo = Color.FromArgb(234, 179, 8);
        private static readonly Color ColorRojo = Color.Crimson;

        public FrmPipelineAgente()
        {
            _config = Utils.LeerConfig<ConfiguracionClient>(RutasProyecto.ObtenerRutaConfiguracion());

            // ── Form base ────────────────────────────────────────
            Text = "Agentes Avanzados — Planificación y Pipeline";
            BackColor = ColorFondo;
            ForeColor = ColorTexto;
            Size = new Size(900, 700);
            MinimumSize = new Size(700, 500);
            Font = new Font("Segoe UI", 9F);

            // ── Panel superior: título + modo ─────────────────────
            _pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = ColorPanel,
                Padding = new Padding(12, 8, 12, 8)
            };

            _lblTitulo = new Label
            {
                Text = "🤖  Agentes Avanzados",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            _lblModo = new Label
            {
                Text = "Modo:",
                ForeColor = ColorTexto,
                AutoSize = true,
                Location = new Point(12, 42)
            };

            _cmbModo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorFondo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(55, 38),
                Width = 280,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _cmbModo.Items.Add("🧠  1.3 — Agente con Planificación (ReAct/CoT)");
            _cmbModo.Items.Add("🔗  1.4 — Pipeline Multi-Agente (4 agentes)");
            _cmbModo.SelectedIndex = 0;
            _cmbModo.SelectedIndexChanged += (s, e) => ActualizarDescripcion();

            _pnlTop.Controls.AddRange(new Control[] { _lblTitulo, _lblModo, _cmbModo });

            // ── Panel de entrada ──────────────────────────────────
            _pnlInput = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = ColorPanel,
                Padding = new Padding(12, 6, 12, 6)
            };

            _lblInstruccion = new Label
            {
                Text = "Instrucción para el agente:",
                ForeColor = ColorTexto,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            _txtInstruccion = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = ColorFondo,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(12, 28),
                Size = new Size(650, 52),
                PlaceholderText = "Ej: Analiza los archivos CSV en mi carpeta y genera un reporte estadístico..."
            };

            _btnEjecutar = new Button
            {
                Text = "▶  Ejecutar",
                BackColor = ColorAcento,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(670, 28),
                Size = new Size(100, 24),
                Cursor = Cursors.Hand
            };
            _btnEjecutar.FlatAppearance.BorderSize = 0;
            _btnEjecutar.Click += BtnEjecutar_Click;

            _btnCancelar = new Button
            {
                Text = "⏹ Cancelar",
                BackColor = ColorRojo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(670, 56),
                Size = new Size(100, 24),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            _btnCancelar.FlatAppearance.BorderSize = 0;
            _btnCancelar.Click += (s, e) => _cts?.Cancel();

            _pnlInput.Controls.AddRange(new Control[]
            {
                _lblInstruccion, _txtInstruccion, _btnEjecutar, _btnCancelar
            });

            // ── Panel indicadores de agentes (solo para 1.4) ─────
            _pnlAgentes = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = ColorFondo,
                Padding = new Padding(12, 6, 12, 6),
                Visible = false
            };

            string[] nombresAgentes = {
                "1. Planificador", "2. Ejecutor", "3. Verificador", "4. Formateador"
            };
            _lblAgentes = new Label[4];
            for (int i = 0; i < 4; i++)
            {
                _lblAgentes[i] = new Label
                {
                    Text = nombresAgentes[i],
                    ForeColor = ColorTexto,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Size = new Size(185, 34),
                    Location = new Point(12 + i * 195, 8),
                    BackColor = ColorPanel
                };
                _pnlAgentes.Controls.Add(_lblAgentes[i]);
            }

            // ── Panel log ─────────────────────────────────────────
            _pnlLog = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 6, 12, 12)
            };

            _rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 15, 30),
                ForeColor = Color.FromArgb(200, 220, 200),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9.5F),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };

            _pnlLog.Controls.Add(_rtbLog);

            // ── Layout: Top panels stacked, Fill = log ────────────
            Controls.Add(_pnlLog);
            Controls.Add(_pnlAgentes);
            Controls.Add(_pnlInput);
            Controls.Add(_pnlTop);

            ActualizarDescripcion();
            AjustarTamañoInput();
            Resize += (s, e) => AjustarTamañoInput();
        }

        // =====================================================================
        //  LÓGICA DE EJECUCIÓN
        // =====================================================================

        private async void BtnEjecutar_Click(object? sender, EventArgs e)
        {
            string instruccion = _txtInstruccion.Text.Trim();
            if (string.IsNullOrWhiteSpace(instruccion))
            {
                MessageBox.Show("Escribe una instrucción para el agente.", "Campo requerido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cts = new CancellationTokenSource();
            _btnEjecutar.Enabled = false;
            _btnCancelar.Enabled = true;
            _rtbLog.Clear();

            bool esPipeline = _cmbModo.SelectedIndex == 1;
            _pnlAgentes.Visible = esPipeline;

            if (esPipeline) ResetearIndicadoresAgentes();

            try
            {
                var config = _config?.MiArchivo;
                string modelo = _config?.Mimodelo?.Modelos ?? "";
                string apiKey = _config?.Mimodelo?.ApiKey ?? "";
                string ruta = config?.Ruta ?? "";
                var servicio = _config?.Mimodelo?.Agente ?? Servicios.Gemenni;

                if (esPipeline)
                {
                    // ── 1.4 Pipeline Multi-Agente ───────────────────
                    AgregarLog("🔗 Iniciando Pipeline Multi-Agente (4 agentes especializados)...\n", Color.Cyan);

                    int agenteActual = 0;
                    var resultado = await AIModelConector.EjecutarPipelineMultiAgenteAsync(
                        instruccion,
                        modelo, ruta, apiKey, "", false, servicio,
                        onProgreso: msg =>
                        {
                            BeginInvoke(() =>
                            {
                                // Detectar qué agente está activo para el indicador visual
                                if (msg.Contains("AGENTE 1")) { agenteActual = 0; MarcarAgenteActivo(0); }
                                else if (msg.Contains("AGENTE 2")) { agenteActual = 1; MarcarAgenteActivo(1); }
                                else if (msg.Contains("AGENTE 3")) { agenteActual = 2; MarcarAgenteActivo(2); }
                                else if (msg.Contains("AGENTE 4")) { agenteActual = 3; MarcarAgenteActivo(3); }

                                Color color = msg.StartsWith("✓") ? ColorVerde
                                    : msg.StartsWith("❌") ? ColorRojo
                                    : msg.Contains("╔") || msg.Contains("╚") ? ColorAcento
                                    : ColorTexto;
                                AgregarLog(msg + "\n", color);
                            });
                        },
                        ct: _cts.Token);

                    BeginInvoke(() =>
                    {
                        MarcarTodosAgentesCompletados(resultado.Exitoso);
                        AgregarLog("\n═══════════════════════════════════\n", ColorAcento);
                        AgregarLog(resultado.Exitoso
                            ? "✅ Pipeline completado exitosamente.\n"
                            : "⚠️ Pipeline completado con errores.\n",
                            resultado.Exitoso ? ColorVerde : ColorAmarillo);
                    });
                }
                else
                {
                    // ── 1.3 Agente con Planificación ────────────────
                    AgregarLog("🧠 Iniciando Agente con Planificación (ReAct/CoT)...\n", Color.Cyan);

                    string respuesta = await AIModelConector.EjecutarConPlanificacionAsync(
                        instruccion,
                        modelo, ruta, apiKey, "", false, servicio,
                        onProgreso: msg =>
                        {
                            BeginInvoke(() =>
                            {
                                Color color = msg.TrimStart().StartsWith("✓") ? ColorVerde
                                    : msg.TrimStart().StartsWith("✗") ? ColorRojo
                                    : msg.TrimStart().StartsWith("▶") ? ColorAmarillo
                                    : msg.TrimStart().StartsWith("📋") || msg.TrimStart().StartsWith("🧠") ? ColorAcento
                                    : ColorTexto;
                                AgregarLog(msg + "\n", color);
                            });
                        },
                        ct: _cts.Token);

                    BeginInvoke(() =>
                    {
                        AgregarLog("\n═══════════════════════════════════\n", ColorAcento);
                        AgregarLog("RESULTADO FINAL:\n", Color.White);
                        AgregarLog(respuesta + "\n", ColorVerde);
                    });
                }
            }
            catch (OperationCanceledException)
            {
                BeginInvoke(() => AgregarLog("\n⏹ Ejecución cancelada.\n", ColorAmarillo));
            }
            catch (Exception ex)
            {
                BeginInvoke(() => AgregarLog($"\n❌ Error: {ex.Message}\n", ColorRojo));
            }
            finally
            {
                BeginInvoke(() =>
                {
                    _btnEjecutar.Enabled = true;
                    _btnCancelar.Enabled = false;
                    _cts?.Dispose();
                    _cts = null;
                });
            }
        }

        // =====================================================================
        //  INDICADORES VISUALES DE AGENTES (pipeline 1.4)
        // =====================================================================

        private void ResetearIndicadoresAgentes()
        {
            foreach (var lbl in _lblAgentes)
            {
                lbl.BackColor = ColorPanel;
                lbl.ForeColor = ColorTexto;
            }
        }

        private void MarcarAgenteActivo(int indice)
        {
            for (int i = 0; i < _lblAgentes.Length; i++)
            {
                _lblAgentes[i].BackColor = i == indice ? ColorAcento : ColorPanel;
                _lblAgentes[i].ForeColor = i == indice ? Color.White : ColorTexto;
            }
        }

        private void MarcarTodosAgentesCompletados(bool exitoso)
        {
            Color color = exitoso ? ColorVerde : ColorAmarillo;
            foreach (var lbl in _lblAgentes)
            {
                lbl.BackColor = color;
                lbl.ForeColor = Color.Black;
            }
        }

        // =====================================================================
        //  UTILIDADES UI
        // =====================================================================

        private void AgregarLog(string texto, Color color)
        {
            if (InvokeRequired) { BeginInvoke(() => AgregarLog(texto, color)); return; }
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;
            _rtbLog.SelectionColor = color;
            _rtbLog.AppendText(texto);
            _rtbLog.ScrollToCaret();
        }

        private void ActualizarDescripcion()
        {
            bool esPipeline = _cmbModo.SelectedIndex == 1;
            _pnlAgentes.Visible = esPipeline;
            _btnEjecutar.Text = esPipeline ? "▶ Pipeline" : "▶ Planificar";
        }

        private void AjustarTamañoInput()
        {
            int anchoDisponible = ClientSize.Width - 24 - 110;
            _txtInstruccion.Width = Math.Max(200, anchoDisponible);
            _btnEjecutar.Left = _txtInstruccion.Right + 8;
            _btnCancelar.Left = _txtInstruccion.Right + 8;
        }
    }
}
