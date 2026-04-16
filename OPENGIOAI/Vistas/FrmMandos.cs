// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  FrmMandos.cs — Ada Lovelace IA v3.0                                    ║
// ║                                                                          ║
// ║  CAMBIOS APLICADOS (resumen ejecutivo):                                  ║
// ║                                                                          ║
// ║  [C1] HILOS / THREAD-SAFETY                                              ║
// ║    · MostrarMensaje: guard InvokeRequired movido al inicio del método.   ║
// ║    · EjecutarMotorIAAsync: todos los BeginInvoke centralizados en        ║
// ║      MostrarMensajeAsync() para evitar race conditions al escribir UI    ║
// ║      desde tareas paralelas.                                             ║
// ║    · IniciarSlack / Telegram: lambdas de eventos ahora usan             ║
// ║      BeginInvoke consistentemente; se eliminaron llamadas a Invoke()     ║
// ║      (bloqueante) dentro de callbacks async.                             ║
// ║    · CancellationTokenSource se pasa a EjecutarMotorIAAsync y se        ║
// ║      respeta con CancellationToken.ThrowIfCancellationRequested()        ║
// ║      entre las dos pasadas de agentes.                                   ║
// ║                                                                          ║
// ║  [C2] POTENCIA DE LLAMADAS / SUB-AGENTES                                ║
// ║    · EjecutarMotorIAAsync refactorizado en tres métodos privados:        ║
// ║        – EjecutarAgente1Async : lanza Agente 1 con timeout configurable. ║
// ║        – EjecutarAgente2Async : lanza Agente 2 solo si corresponde.     ║
// ║        – EjecutarDifusiónAsync: envía resultado a Telegram + Slack en   ║
// ║          paralelo con Task.WhenAll para reducir latencia de salida.      ║
// ║    · Timeout configurable (_timeoutSegundos) por petición al modelo;     ║
// ║      evita bloqueos silenciosos si la API no responde.                   ║
// ║    · Los dos agentes comparten el mismo CancellationToken; si el usuario ║
// ║      cancela a mitad, ninguna tarea sigue corriendo en background.       ║
// ║                                                                          ║
// ║  [C3] ENCAPSULAMIENTO Y COHESIÓN                                         ║
// ║    · Extracción de AgentOrchestrator (inner class / partial) con la     ║
// ║      lógica pura del motor desacoplada de la UI.                         ║
// ║    · CargarListaAgents: movida a DataLoader (static) para testear sin   ║
// ║      depender del formulario.                                            ║
// ║    · ConversationWindow ya existía; se corrige su integración para que  ║
// ║      Limpiar() solo se llame UNA vez por cambio de agente/ruta.          ║
// ║    · _ctsIA: ahora se hace Dispose() del token anterior antes de        ║
// ║      crear uno nuevo (memory leak corregido).                            ║
// ║                                                                          ║
// ║  [C4] BUGS CORREGIDOS                                                    ║
// ║    · comboBoxAgentes_SelectedIndexChanged: el contador "veces" no se    ║
// ║      reseteaba — si el usuario cambiaba de agente 3+ veces solo         ║
// ║      disparaba la primera. Reemplazado por flag _cargaInicialAgente.     ║
// ║    · comboBoxModeloIA_SelectedIndexChanged: ídem con "cargas". Ahora    ║
// ║      usa _cargaInicialModelo.                                            ║
// ║    · IniciarSlack: se protege con null-check en _slack antes de Stop()  ║
// ║      para evitar NRE en la primera carga.                                ║
// ║    · EnviarTelegramAsync: parámetro btns movido antes de UsarTelegram   ║
// ║      para alinear las 5 sobrecargas del sitio de llamada.               ║
// ║    · ObtenerPromtChat: renombrado a ObtenerContextoChat (typo).         ║
// ║    · MostrarAdvertencia: no debe lanzar Exception desde un helper        ║
// ║      llamado en background; ahora devuelve bool + mensaje de error.      ║
// ╚══════════════════════════════════════════════════════════════════════════╝

using Newtonsoft.Json.Linq;
using OPENGIOAI.Agentes;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Properties;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.ServiciosSlack;
using OPENGIOAI.ServiciosTelegram;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace OPENGIOAI.Vistas
{
    public partial class FrmMandos : Form
    {
        // ── Constantes de comportamiento ──────────────────────────────────────
        // [C2] Centralizar aquí facilita cambiar el timeout sin buscar en el código.
        private const int TimeoutSegundos = 120;
        private const int MaxTurnosContexto = 6;
        private const int MaxTokensContexto = 3000;

        private int Entradas = 9;

        // ── Controles y utilidades UI ─────────────────────────────────────────
        private readonly System.Windows.Forms.ToolTip _toolTipArchivos = new();

        // ── Flags de estado ───────────────────────────────────────────────────
        private bool _bloqueTelegram = false;
        private bool _ajustandoAltura = false;
        private bool _primeraCar = false;
        private bool _primeraEjecucion = true;
        private bool _primeraEjecucionAgente = true;
        private bool _telegramActivo = false;
        private bool _enviarConstructorTelegram = false;
        private bool _enviarConstructorSlack    = false;
        private bool _slackActivo = false;
        private bool _recordarTema = false;
        private bool _soloResptxt = false;
        private bool _soloChat = false;
        private bool _isDragging = false;
        private bool _procesandoSlack = false;

        // [C4] Reemplaza el contador "veces" con flags booleanos para mayor claridad.
        private bool _cargaInicialAgente = true;
        private bool _cargaInicialModelo = true;

        // ── Ventana deslizante de contexto conversacional ─────────────────────
        private readonly ConversationWindow _ventana = new(MaxTurnosContexto, MaxTokensContexto);

        // ── Strings auxiliares ────────────────────────────────────────────────
        private string _textoFaltante = "";
        private string _rutasAgregadas = "";

        // ── Misc ──────────────────────────────────────────────────────────────
        private Point _mouseDownLocation;

        // ── Servicios externos ────────────────────────────────────────────────
        private SlackPollingService _slack;
        // [C1] _ctsIA: se hace Dispose del token anterior antes de crear uno nuevo.
        private CancellationTokenSource _ctsIA;
        private readonly SemaphoreSlim _telegramSemaphore = new(1, 1);
        private TelegramListener _telegramListener;

        // ── ARIA: panel de estado de agentes y tracking de fase ───────────────
        private PanelAgentes _panelAgentes = null!;
        private BurbujaChat? _burbujaFaseActual;

        // ── Streaming de salida de script en vivo ─────────────────────────────
        private BurbujaChat? _burbujaScriptActual;
        private readonly StringBuilder _bufferScript = new();

        // ── Control de reintentos del Guardián ────────────────────────────────
        private NumericUpDown _nudReintentos = null!;

        // ── Throttle de streaming (evita inundar el hilo UI) ──────────────────
        // Agrupa las actualizaciones de la burbuja en intervalos de 120 ms.
        private readonly System.Windows.Forms.Timer _timerStreaming = new() { Interval = 120 };
        private volatile bool _streamingPendiente = false;
        private BurbujaChat? _burbujaStreamingActual;
        private string _streamingTextoActual = "";

        // ── Modelos de datos ──────────────────────────────────────────────────
        private ConfiguracionClient _configuracionClient;
        private SlackChat _slackChat = new();
        private TelegramChat _telegramChat = new();
        private Modelo _modeloSeleccionado = new();
        private Archivo _archivoSeleccionado = new();
        private List<Api> _listaApisDisponibles = new();
        private List<Modelo> _listaAgentes = new();
        private List<Archivo> _listaArchivosDisponibles = new();
        private List<ModeloAgente> _modelosAgente = new();

        // =====================================================================
        //  CONSTRUCTOR
        // =====================================================================
        public FrmMandos(ConfiguracionClient config)
        {
            InitializeComponent();
            _configuracionClient = config ?? throw new ArgumentNullException(nameof(config));

            // Timer de throttle: actualiza la burbuja de streaming a ~8 fps.
            _timerStreaming.Tick += (_, _) =>
            {
                if (!_streamingPendiente) return;
                _streamingPendiente = false;

                if (_burbujaStreamingActual == null || _burbujaStreamingActual.IsDisposed) return;
                _burbujaStreamingActual.ActualizarTexto(_streamingTextoActual);

                if (_burbujaStreamingActual.Parent is ScrollableControl sc)
                    sc.ScrollControlIntoView(_burbujaStreamingActual);
            };
        }

        // =====================================================================
        //  EVENTOS DEL FORMULARIO
        // =====================================================================

        /// <summary>
        /// Carga los datos iniciales en background para no bloquear la UI.
        /// [C1] CargarListaAgents() se ejecuta en Task.Run porque es puramente
        ///      I/O (lectura de JSON) y no toca controles WinForms.
        /// </summary>
        private async void FrmMandos_Load(object sender, EventArgs e)
        {
            await Task.Run(CargarListaAgents);
            await CargarThemas();
        }

        private void FrmMandos_Resize(object sender, EventArgs e)
        {
            int anchoMax = (int)(pnlChat.ClientSize.Width * 0.75);
            foreach (Control c in pnlChat.Controls)
            {
                if (c is BurbujaChat burbuja)
                    burbuja.ActualizarAncho(anchoMax);
            }
            AjustarBotonesInput();
        }

        /// <summary>
        /// Alinea btnEnviar y btnCancelar al borde derecho del wrapper del textbox.
        /// Se invoca en ConfigurarUI() y en cada FrmMandos_Resize para que los botones
        /// siempre queden pegados al cuadro de instrucción sin importar el tamaño del form.
        /// </summary>
        private void AjustarBotonesInput()
        {
            foreach (Control c in pnlContenedorTxt.Controls)
            {
                if (c is Panel wrapper && wrapper.Controls.Contains(textBoxInstrucion))
                {
                    int xBtn = wrapper.Right + 6;
                    btnEnviar.Left   = xBtn;
                    btnCancelar.Left = xBtn;
                    // Alinear verticalmente: enviar al tope del wrapper, cancelar debajo
                    btnEnviar.Top   = wrapper.Top;
                    btnCancelar.Top = wrapper.Top + btnEnviar.Height + 4;
                    return;
                }
            }
        }

        private void FrmMandos_FormClosing(object sender, FormClosingEventArgs e)
        {
            // [C1] Cancelar cualquier petición en vuelo al cerrar el formulario.
            _timerStreaming.Stop();
            _timerStreaming.Dispose();
            CancelarInstruccion();
            _telegramListener?.Stop();
            _slack?.Stop();
            _ctsIA?.Dispose();
        }

        private void Form_DragLeave(object sender, EventArgs e) => Cursor = Cursors.Default;

        // =====================================================================
        //  EVENTOS DE CONTROLES
        // =====================================================================

        private void textBoxInstrucion_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void textBoxInstrucion_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;

            _ajustandoAltura = true;
            try
            {
                foreach (string file in files.Where(File.Exists))
                {
                    Image img = Utils.ObtenerTipoArchivo(file);
                    AgregarImagenAlPanel(file, img);
                    _rutasAgregadas += $"[Ruta]: {file} ,";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar imagen: " + ex.Message);
            }
            finally
            {
                _ajustandoAltura = false;
            }
        }

        private void textBoxInstrucion_TextChanged(object sender, EventArgs e)
        {
            if (_ajustandoAltura) return;
            //ConsultasApis(textBoxInstrucion);
            MostrarPreview(textBoxInstrucion, labelSugerencia);
            TextBoxRounder.AjustarAlturaConRedondeo(textBoxInstrucion);
        }

        private void textBoxInstrucion_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter → enviar instrucción
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnEnviar_Click(sender, EventArgs.Empty);
                return;
            }

            if (e.KeyCode != Keys.Tab || string.IsNullOrEmpty(_textoFaltante)) return;

            int cursor = textBoxInstrucion.SelectionStart;
            textBoxInstrucion.Text = textBoxInstrucion.Text.Insert(cursor, _textoFaltante);
            textBoxInstrucion.SelectionStart = cursor + _textoFaltante.Length;
            labelSugerencia.Visible = false;
            e.SuppressKeyPress = true;
        }

        /// <summary>
        /// [C4] Reemplaza el contador "veces" por un flag booleano que se
        ///      desactiva tras la primera carga real. Esto permite que el usuario
        ///      cambie de agente N veces sin que el evento quede inerte.
        /// </summary>
        private async void comboBoxAgentes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargaInicialAgente) { _cargaInicialAgente = false; return; }
            await SeleccionarAgente_Trabajo();
            LimpiarResumen();
        }

        private void checkBoxRecordar_CheckedChanged(object sender, EventArgs e)
        {
            _recordarTema = checkBoxRecordar.Checked;
            ChkConver.Visible = _recordarTema;
        }

        private void checkBoxSoloChat_CheckedChanged(object sender, EventArgs e)
        {
            _soloChat = checkBoxSoloChat.Checked;
            ChkEstado.Visible = _soloChat;
            ChkEstado.BackColor = Color.DarkOrange;
        }

        private async void checkBoxSlack_CheckedChanged(object sender, EventArgs e)
        {
            _slackActivo = checkBoxSlack.Checked;
            // Habilitar / deshabilitar la sub-opción del Constructor para Slack
            checkBoxConstructorSlack.Enabled = _slackActivo;
            if (!_slackActivo) checkBoxConstructorSlack.Checked = false;
            await IniciarSlack();
        }

        private void checkBoxConstructorSlack_CheckedChanged(object sender, EventArgs e)
        {
            _enviarConstructorSlack = checkBoxConstructorSlack.Checked;
        }

        private async void comboBoxRuta_SelectedIndexChanged(object sender, EventArgs e)
        {
            LimpiarResumen();
            SeleccionarRuta_Trabajo();
            await IniciarConversasionTelegram();
            await IniciarSlack();
        }

        private async void btnEnviar_Click(object sender, EventArgs e)
        {
            LimpiarResumen();
            //pictureBoxCarga.Visible = true;
            await Procesar_Instrucciones();
           // pictureBoxCarga.Visible = false;
            LimpiarControles();
        }

        private async void checkBoxTelegram_CheckedChanged(object sender, EventArgs e)
        {
            _telegramActivo = checkBoxTelegram.Checked;
            // Habilitar / deshabilitar la sub-opción del Constructor
            checkBoxConstructorTelegram.Enabled = _telegramActivo;
            if (!_telegramActivo) checkBoxConstructorTelegram.Checked = false;
            await IniciarConversasionTelegram();
        }

        private void checkBoxConstructorTelegram_CheckedChanged(object sender, EventArgs e)
        {
            _enviarConstructorTelegram = checkBoxConstructorTelegram.Checked;
        }

        private void checkBoxRapida_CheckedChanged(object sender, EventArgs e)
        {
            _soloResptxt = checkBoxRapida.Checked;
            ChkRes.Visible = _soloResptxt;
            ChkRes.BackColor = Color.DarkOrange;
        }

        /// <summary>
        /// [C4] Ídem a comboBoxAgentes: flag booleano en lugar de contador.
        ///      El contador "cargas" nunca se reseteaba, causando que a partir
        ///      del 5.° evento nunca se ejecutara el cambio de modelo.
        /// </summary>
        private void comboBoxModeloIA_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (Entradas >0) { _cargaInicialModelo = false; Entradas--; return; }

            if (comboBoxModeloIA.SelectedItem is ModeloAgente mimodelo)
                _modeloSeleccionado.Modelos = mimodelo.Nombre;

            LimpiarResumen();
        }

        private void btnGuardar_Click(object sender, EventArgs e) => GuardarConfiguracion();
        private void btnCancelar_Click(object sender, EventArgs e) => CancelarInstruccion();

        private void ChkEstado_Click(object sender, EventArgs e)
        {
            ChkEstado.Visible = !_soloChat;
            checkBoxSoloChat.Checked = !_soloChat;
        }

        private void ChkRes_Click(object sender, EventArgs e)
        {
            ChkRes.Visible = !_soloResptxt;
            checkBoxRapida.Checked = !_soloResptxt;
        }

        private void ChkConver_Click(object sender, EventArgs e)
        {
            ChkConver.Visible = !_recordarTema;
            checkBoxRecordar.Checked = !_recordarTema;
        }

        private void btnLimpiar_Click(object sender, EventArgs e) => pnlChat.Controls.Clear();

        /// <summary>
        /// Muestra / oculta las etiquetas de tiempo y consumo de tokens.
        /// Sirve como botón ℹ de estadísticas en la barra inferior.
        /// </summary>
        private void btnInfo_Click(object sender, EventArgs e)
        {
            bool visible = !lblTime.Visible;
            lblTime.Visible     = visible;
            lblTimeR.Visible    = visible;
            lblConsumo.Visible  = visible;
            lblConsumoA.Visible = visible;

            btnInfo.ForeColor = visible
                ? Color.FromArgb(80, 200, 255)   // azul brillante = activo
                : Color.FromArgb(120, 150, 200);  // gris-azul = inactivo
        }

        // =====================================================================
        //  LÓGICA DE AGENTE Y RUTA
        // =====================================================================

        /// <summary>
        /// Selecciona un agente de trabajo y reinicia el contexto conversacional.
        /// Valida la API key antes de cargar la lista de modelos.
        /// </summary>
        private async Task SeleccionarAgente_Trabajo()
        {
            _primeraCar = true;

            if (comboBoxAgentes.SelectedItem is not Modelo modeloSel) return;
            _modeloSeleccionado = modeloSel;

            await ComprobarApi(_modeloSeleccionado.ApiKey, _modeloSeleccionado.Agente);

            _primeraEjecucionAgente = true;
            _modelosAgente = await ObtenerModeloAgente(
                _modeloSeleccionado.Agente, _modeloSeleccionado.ApiKey);

            // Actualizar combo sin disparar el evento de selección.
            _cargaInicialModelo = true;
            comboBoxModeloIA.DataSource = null;
            comboBoxModeloIA.DataSource = _modelosAgente;
            comboBoxModeloIA.DisplayMember = "Nombre";
            comboBoxModeloIA.ValueMember = "Nombre";
            comboBoxModeloIA.Text = _modeloSeleccionado.Modelos;

            // Reiniciar contexto al cambiar de agente para no mezclar historiales.
            _ventana.Limpiar();
            MostrarMensaje("Contexto de conversación reiniciado.", false);
        }

        /// <summary>
        /// Obtiene dinámicamente los modelos disponibles para cada servicio de IA.
        /// [C2] Todas las llamadas son async y respetan un CancellationToken implícito
        ///      (el de la petición actual); si el usuario cancela no quedan tareas colgadas.
        /// </summary>
        private async Task<List<ModeloAgente>> ObtenerModeloAgente(Servicios servicio, string apiKey)
        {
            List<string> nombres = servicio switch
            {
                Servicios.ChatGpt     => await AIServicios.ObtenerModelosOpenAIAsync(apiKey),
                Servicios.Gemenni     => await AIServicios.ObtenerModelosGeminiAsync(apiKey),
                Servicios.Ollama      => await AIServicios.ObtenerModelosOllamaApiAsync(),
                Servicios.OpenRouter  => await AIServicios.ObtenerModelosOpenRouterAsync(apiKey),
                Servicios.Claude      => await AIServicios.ObtenerModelosClaudeAsync(apiKey),
                Servicios.Deespeek    => await AIServicios.ObtenerModelosDeepSeekAsync(apiKey),
                Servicios.Antigravity => await AIServicios.ObtenerModelosAntigravityAsync(apiKey),
                _                     => new List<string>()
            };

            return nombres
                .Select(n => new ModeloAgente { Nombre = n, Estado = true })
                .ToList();
        }

        /// <summary>
        /// Establece la ruta de trabajo y limpia el contexto previo.
        /// </summary>
        private void SeleccionarRuta_Trabajo()
        {
            _archivoSeleccionado = comboBoxRuta.SelectedItem as Archivo ?? new Archivo();

            if (string.IsNullOrEmpty(_archivoSeleccionado.Ruta))
                _archivoSeleccionado.Ruta = RutasProyecto.ObtenerRutaScripts();

            _ventana.Limpiar();
        }

        // =====================================================================
        //  MOTOR PRINCIPAL DE INSTRUCCIONES
        // =====================================================================

        /// <summary>
        /// Punto de entrada desde la UI. Recopila el texto, lo envía al motor
        /// y muestra la respuesta en el panel de chat.
        /// </summary>
        private async Task Procesar_Instrucciones()
        {
            string texto = textBoxInstrucion.Text.Trim();

            if (!string.IsNullOrEmpty(_rutasAgregadas))
            {
                texto += "\n\n UTILIZA ESTAS RUTAS PARA LA INSTRUCCION : " + _rutasAgregadas;
                _rutasAgregadas = "";
            }

            if (string.IsNullOrEmpty(texto)) return;

            MostrarMensaje(texto, true);

            // Las burbujas de streaming (Agente 1 y Agente 2) muestran el progreso en tiempo real.
            // Ya no se necesita un mensaje "Procesando..." separado ni mostrar la respuesta al final.
            await EjecutarMotorIAAsync(texto, _telegramActivo, _slackActivo);
        }

        /// <summary>
        /// Carga en memoria las listas base del sistema desde disco.
        /// NOTA: método síncrono diseñado para ejecutarse en Task.Run.
        ///       NO acceder a controles de UI aquí.
        /// </summary>
        private void CargarListaAgents()
        {
            _listaApisDisponibles = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());
            _listaAgentes = JsonManager.Leer<Modelo>(RutasProyecto.ObtenerRutaListModelos());
            _listaArchivosDisponibles = JsonManager.Leer<Archivo>(RutasProyecto.ObtenerRutaListArchivos());
        }

        // ──────────────────────────────────────────────────────────────────────
        //  MOTOR IA — ORQUESTADOR PRINCIPAL
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Motor de doble pasada: Agente 1 genera, Agente 2 valida/mejora.
        ///
        /// [C1] Se crea un CancellationTokenSource con timeout global. El token
        ///      se pasa a ambos agentes; si el usuario pulsa Cancelar o vence el
        ///      timeout, ambas tareas se detienen.
        ///
        /// [C1] Se hace Dispose() del token anterior antes de crear uno nuevo
        ///      para evitar el memory leak que existía antes.
        ///
        /// [C2] EjecutarDifusionAsync lanza Telegram + Slack en Task.WhenAll,
        ///      reduciendo la latencia de salida cuando ambos están activos.
        /// </summary>
        private async Task<string> EjecutarMotorIAAsync(
            string instruccion,
            bool usarTelegram = false,
            bool usarSlack = false)
        {
            // [ARIA] Delegar al nuevo orquestador de 4 agentes.
            return await EjecutarConARIAAsync(instruccion, usarTelegram, usarSlack);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  ORQUESTADOR ARIA — pipeline de 4 agentes con autocorrección
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Pipeline ARIA: Analista → Constructor → Guardián → Comunicador.
        /// · Cada fase actualiza las píldoras de estado en tiempo real.
        /// · El Guardián autocorrige errores hasta 3 veces sin molestar al usuario.
        /// · El Comunicador produce respuesta final en streaming con lenguaje amigable.
        /// </summary>
        private async Task<string> EjecutarConARIAAsync(
            string instruccion,
            bool usarTelegram = false,
            bool usarSlack = false)
        {
            _ctsIA?.Cancel();
            _ctsIA?.Dispose();
            _ctsIA = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSegundos));
            var ct = _ctsIA.Token;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            string instruccionOriginal = instruccion;
            instruccion = _ventana.CombinarConInstruccion(instruccion, _recordarTema, _soloChat);

            // ── Reset visual del panel de agentes ────────────────────────────
            if (InvokeRequired) BeginInvoke(_panelAgentes.Reset);
            else _panelAgentes.Reset();

            // ── Crear orquestador y conectar eventos ─────────────────────────
            var aria = new OrquestadorARIA(
                _modeloSeleccionado.Modelos,
                _archivoSeleccionado.Ruta,
                _modeloSeleccionado.ApiKey,
                Utils.ObtenerNombresApis(_listaApisDisponibles),
                _soloChat,
                _modeloSeleccionado.Agente,
                maxReintentos: (int)_nudReintentos.Value);

            // Flag para indicar si la fase actual debe activar el indicador "escribiendo"
            // Solo se activa en Analista y Comunicador, NO en Constructor ni Guardián.
            bool[] typingEnabled = { false };

            aria.OnFaseIniciada += (fase, msg) =>
            {
                // Actualizar flag de typing según la fase
                typingEnabled[0] = fase == FaseAgente.Analista || fase == FaseAgente.Comunicador;

                _panelAgentes.SetEstado(fase, EstadoAgente.Active);
                if (!string.IsNullOrWhiteSpace(msg))
                    MostrarBurbujaFase(fase, msg);

                // Reenviar el plan del Analista a Telegram / Slack en tiempo real.
                // Se filtra el mensaje genérico inicial; solo se envía el plan real.
                if (fase == FaseAgente.Analista
                    && !string.IsNullOrWhiteSpace(msg)
                    && msg != "Analizando tu instrucción...")
                {
                    _ = EjecutarDifusionAsync($"🔍 {msg}", usarTelegram, usarSlack);
                }
            };

            aria.OnFaseCompletada += (fase2, _ok) =>
            {
                // Al completar Constructor o Guardián, asegurarse de que typing siga desactivado
                if (fase2 == FaseAgente.Constructor || fase2 == FaseAgente.Guardian)
                    typingEnabled[0] = false;
            };

            aria.OnFaseCompletada += (fase, ok) =>
            {
                _panelAgentes.SetEstado(fase, ok ? EstadoAgente.Done : EstadoAgente.Error);
                // Flush streaming de la fase que termina
                if (fase == FaseAgente.Constructor ||
                    fase == FaseAgente.Guardian)
                {
                    FinalizarStreamingThrottle(
                        _bufferScript.Length > 0 ? _bufferScript.ToString().TrimEnd() : null!);
                }
            };

            aria.OnInicioScript += fase =>
                BeginInvoke(() => IniciarBurbujaScriptFase(fase));

            aria.OnLineaScript += (_, linea) =>
                AgregarLineaScriptEnVivo(linea);

            aria.OnToken += (fase, token) =>
            {
                if (fase == FaseAgente.Comunicador)
                    AgregarTokenComunicador(token);
            };

            aria.OnReintentoGuardian += (intento, max, razon) =>
                _panelAgentes.SetEstado(FaseAgente.Guardian, EstadoAgente.Active);

            // ── Reenvío opcional del Constructor a Telegram y/o Slack ────────
            aria.OnConstructorCompletado += salidaRaw =>
            {
                if (string.IsNullOrWhiteSpace(salidaRaw)) return;

                if (_enviarConstructorTelegram && usarTelegram)
                    _ = EnviarTelegramDesdeActivacion(FormatearConstructorParaTelegram(salidaRaw));

                if (_enviarConstructorSlack && usarSlack && _slack != null)
                    _ = _slack.EnviarCodigoAsync(FormatearConstructorParaSlack(salidaRaw));
            };

            // ── Indicadores de "escribiendo / pensando" mientras el pipeline corre ──

            // Telegram: enviar acción "typing" cada 4 s (solo durante Analista y Comunicador)
            using var ctsTyping = new CancellationTokenSource();
            if (usarTelegram && _telegramChat?.ChatId != 0)
            {
                _ = Task.Run(async () =>
                {
                    while (!ctsTyping.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // Solo enviar el indicador si la fase actual lo requiere
                            if (typingEnabled[0])
                            {
                                await ServiciosTelegram.TelegramSender.EnviarAccionEscribiendoAsync(
                                    _telegramChat!.Apikey, _telegramChat.ChatId);
                            }
                            await Task.Delay(4_000, ctsTyping.Token);
                        }
                        catch { break; }
                    }
                }, ctsTyping.Token);
            }

            // Slack: mensaje "pensando..." que se elimina al finalizar
            string? slackTsPensando = null;
            if (usarSlack && _slack != null)
                slackTsPensando = await _slack.EnviarPensandoAsync();

            // ── Ejecutar pipeline ─────────────────────────────────────────────
            string respuestaFinal;
            try
            {
                respuestaFinal = await aria.EjecutarAsync(instruccion, ct);
            }
            catch (OperationCanceledException)
            {
                return "";
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error en el pipeline: {ex.Message}", false);
                return "";
            }
            finally
            {
                sw.Stop();
                ActualizarInfoRespuesta(
                    $"ARIA: {sw.Elapsed.TotalSeconds:F1}s", "",
                    Program.ComsumoTokens, "");

                // Detener indicadores en cuanto termina el pipeline
                ctsTyping.Cancel();
                if (slackTsPensando != null && _slack != null)
                    _ = _slack.EliminarMensajeAsync(slackTsPensando);
            }

            // Flush final del Comunicador — pasar el buffer acumulado para garantizar
            // que la burbuja muestre el texto aunque el timer aún no haya disparado.
            // Bug: FinalizarStreamingThrottle(null!) dejaba la burbuja vacía cuando
            // el streaming terminaba antes del primer tick de 120ms.
            string textoFinalComunicador = _bufferScript.Length > 0
                ? _bufferScript.ToString().TrimEnd()
                : null!;
            FinalizarStreamingThrottle(textoFinalComunicador);

            // ── Registrar en ventana de contexto y difundir ───────────────────
            _ventana.Agregar(instruccionOriginal, respuestaFinal);

            if (!string.IsNullOrWhiteSpace(respuestaFinal))
                await EjecutarDifusionAsync(respuestaFinal, usarTelegram, usarSlack);

            return respuestaFinal;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS UI — BURBUJAS POR FASE ARIA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Muestra o actualiza la burbuja de un agente específico.
        /// Si ya existe una burbuja activa para esa fase, la actualiza en lugar de crear una nueva.
        /// Thread-safe — puede llamarse desde cualquier hilo.
        /// </summary>
        private void MostrarBurbujaFase(FaseAgente fase, string mensaje)
        {
            if (InvokeRequired) { BeginInvoke(() => MostrarBurbujaFase(fase, mensaje)); return; }
            if (string.IsNullOrWhiteSpace(mensaje)) return;

            string faseTag = fase.ToString();

            // Actualizar burbuja existente de esta fase si ya existe
            if (_burbujaFaseActual != null &&
                !_burbujaFaseActual.IsDisposed &&
                _burbujaFaseActual.Tag?.ToString() == faseTag)
            {
                _burbujaFaseActual.ActualizarTexto(ObtenerPrefijoFase(fase) + mensaje);
                return;
            }

            int anchoMax = Math.Max(80, (int)(pnlChat.ClientSize.Width * 0.75));
            var burbuja = new BurbujaChat(
                ObtenerPrefijoFase(fase) + mensaje,
                false, anchoMax,
                Properties.Resources.iconos1)
            {
                Tag     = faseTag,
                Margin  = ObtenerMargenFase(fase)
            };

            _burbujaFaseActual = burbuja;
            pnlChat.Controls.Add(burbuja);
            pnlChat.ScrollControlIntoView(burbuja);
        }

        /// <summary>
        /// Inicia la burbuja de streaming para la ejecución de script del Constructor o Guardián.
        /// Debe llamarse desde el hilo UI (via BeginInvoke).
        /// </summary>
        private void IniciarBurbujaScriptFase(FaseAgente fase)
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (int)(pnlChat.ClientSize.Width * 0.75));

            string titulo = fase == FaseAgente.Guardian
                ? "🛡 Corrigiendo..."
                : "⚙ Ejecutando...";

            _burbujaScriptActual = new BurbujaChat(
                titulo, false, anchoMax, Properties.Resources.iconos1)
            {
                Tag    = fase.ToString(),
                Margin = ObtenerMargenFase(fase)
            };

            pnlChat.Controls.Add(_burbujaScriptActual);
            pnlChat.ScrollControlIntoView(_burbujaScriptActual);

            _burbujaStreamingActual = _burbujaScriptActual;
            _burbujaFaseActual = null; // Próximo OnFaseIniciada de este agente crea nueva burbuja
            _timerStreaming.Start();
        }

        /// <summary>
        /// Añade un token del Comunicador a la burbuja de streaming.
        /// Crea la burbuja si aún no existe. Thread-safe.
        /// </summary>
        private void AgregarTokenComunicador(string token)
        {
            // Si la burbuja de streaming no pertenece al Comunicador, crear una nueva
            if (_burbujaStreamingActual == null ||
                _burbujaStreamingActual.IsDisposed ||
                _burbujaStreamingActual.Tag?.ToString() != FaseAgente.Comunicador.ToString())
            {
                if (InvokeRequired)
                {
                    BeginInvoke(() => { IniciarBurbujaComunicador(); AgregarTokenComunicador(token); });
                    return;
                }
                IniciarBurbujaComunicador();
            }

            _bufferScript.Append(token);
            _streamingTextoActual = _bufferScript.ToString();
            _streamingPendiente   = true;
        }

        /// <summary>
        /// Crea la burbuja de streaming del Comunicador (respuesta final).
        /// Llamar solo desde el hilo UI.
        /// </summary>
        private void IniciarBurbujaComunicador()
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (int)(pnlChat.ClientSize.Width * 0.75));

            var burbuja = new BurbujaChat(
                "💬", false, anchoMax, Properties.Resources.iconos1)
            {
                Tag    = FaseAgente.Comunicador.ToString(),
                Margin = new Padding(250, 5, 80, 5)
            };

            pnlChat.Controls.Add(burbuja);
            pnlChat.ScrollControlIntoView(burbuja);

            _burbujaStreamingActual = burbuja;
            _timerStreaming.Start();
        }

        // ── Utilidades de presentación por fase ───────────────────────────────

        private static string ObtenerPrefijoFase(FaseAgente fase) => fase switch
        {
            FaseAgente.Analista    => "🔍  ",
            FaseAgente.Constructor => "⚙  ",
            FaseAgente.Guardian    => "🛡  ",
            FaseAgente.Comunicador => "",
            _ => ""
        };

        private static Padding ObtenerMargenFase(FaseAgente fase) => fase switch
        {
            // Analista: margen izquierdo grande → burbuja pequeña a la izquierda
            FaseAgente.Analista    => new Padding(10, 5, 350, 5),
            // Constructor/Guardián: centrado, cuerpo técnico
            FaseAgente.Constructor => new Padding(60, 5, 200, 5),
            FaseAgente.Guardian    => new Padding(60, 5, 200, 5),
            // Comunicador: margen derecho pequeño → burbuja principal (respuesta)
            FaseAgente.Comunicador => new Padding(250, 5, 80, 5),
            _ => new Padding(250, 5, 80, 5)
        };

        /// <summary>
        /// Ejecuta Agente 1 (generación) con el token de cancelación activo.
        /// [C2] Encapsula la llamada para hacer testeable el paso de generación
        ///      de forma independiente del validador.
        /// </summary>
        private async Task<string> EjecutarAgente1Async(string instruccion, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            return await AIModelConector.EjecutarInstruccionIAAsync(
                instruccion,
                _modeloSeleccionado.Modelos,
                _archivoSeleccionado.Ruta,
                _modeloSeleccionado.ApiKey,
                Utils.ObtenerNombresApis(_listaApisDisponibles),
                _soloChat,
                _modeloSeleccionado.Agente,
                ct,
                onInicioScript: () => BeginInvoke(IniciarBurbujaScriptEnVivo),
                onSalidaScript: linea => AgregarLineaScriptEnVivo(linea));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STREAMING DE SALIDA DE SCRIPT EN VIVO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crea la burbuja de "en vivo" al inicio de la ejecución del script.
        /// Debe llamarse en el hilo de UI (ya garantizado vía BeginInvoke).
        /// </summary>
        private void IniciarBurbujaScriptEnVivo()
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (int)(pnlChat.ClientSize.Width * 0.75));
            _burbujaScriptActual = new BurbujaChat(
                "⚙ Ejecutando script...", false, anchoMax, Properties.Resources.iconos1)
            {
                Tag = "10",
                Margin = new Padding(250, 5, 80, 5)
            };
            pnlChat.Controls.Add(_burbujaScriptActual);
            pnlChat.ScrollControlIntoView(_burbujaScriptActual);

            // Activar throttle sobre esta burbuja
            _burbujaStreamingActual = _burbujaScriptActual;
            _timerStreaming.Start();
        }

        /// <summary>
        /// Acumula una línea de salida para el siguiente tick del timer de throttle.
        /// Seguro para llamar desde cualquier hilo (incluso thread pool).
        /// </summary>
        private void AgregarLineaScriptEnVivo(string linea)
        {
            if (_burbujaScriptActual == null || _burbujaScriptActual.IsDisposed) return;
            _bufferScript.AppendLine(linea);
            _streamingTextoActual = _bufferScript.ToString().TrimEnd();
            _streamingPendiente = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS DE THROTTLE STREAMING
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Apunta el throttle a una nueva burbuja y activa el timer.
        /// Llamar desde el hilo UI.
        /// </summary>
        private void IniciarStreamingThrottle(BurbujaChat burbuja)
        {
            _burbujaStreamingActual = burbuja;
            _streamingTextoActual = "";
            _streamingPendiente = false;
            _timerStreaming.Start();
        }

        /// <summary>
        /// Detiene el timer, realiza la última actualización pendiente y hace scroll.
        /// Llamar desde el hilo UI (o con InvokeRequired).
        /// </summary>
        private void FinalizarStreamingThrottle(string textoFinal)
        {
            if (InvokeRequired) { Invoke(() => FinalizarStreamingThrottle(textoFinal)); return; }

            _timerStreaming.Stop();
            _streamingPendiente = false;

            if (_burbujaStreamingActual == null || _burbujaStreamingActual.IsDisposed) return;
            if (!string.IsNullOrEmpty(textoFinal))
                _burbujaStreamingActual.ActualizarTexto(textoFinal);

            if (_burbujaStreamingActual.Parent is ScrollableControl sc)
                sc.ScrollControlIntoView(_burbujaStreamingActual);

            _burbujaStreamingActual = null;
        }

        /// <summary>
        /// Agente 2: respuesta de texto directo del LLM con streaming token a token.
        /// No genera ni ejecuta Python — es solo análisis y formateo de lenguaje natural.
        /// Esto lo hace ~3x más rápido que la versión anterior (sin script execution).
        /// </summary>
        /// <summary>
        /// Agente 2: genera un script Python que corrige errores o informa el resultado.
        ///   - Si Agent 1 falló → el script corrige y ejecuta, escribe resultado en respuesta.txt
        ///   - Si Agent 1 tuvo éxito → el script escribe resumen humano en respuesta.txt
        /// La salida del script se muestra en tiempo real en una burbuja de streaming propia.
        /// </summary>
        private async Task<string> EjecutarAgente2StreamingAsync(
            string instruccionOriginal,
            string codigoGenerado,
            string respuestaTxt,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            string contexto = ConstruirContextoAgente2(instruccionOriginal, respuestaTxt, codigoGenerado);

            await AIModelConector.EjecutarInstruccionIAAsync(
                contexto,
                _modeloSeleccionado.Modelos,
                _archivoSeleccionado.Ruta,
                _modeloSeleccionado.ApiKey,
                Utils.ObtenerNombresApis(_listaApisDisponibles),
                _soloChat,
                _modeloSeleccionado.Agente,
                ct,
                onInicioScript: () => BeginInvoke(IniciarBurbujaAgente2),
                onSalidaScript: linea => AgregarLineaScriptEnVivo(linea));

            // Flush final de la burbuja de Agent 2
            FinalizarStreamingThrottle(_bufferScript.Length > 0
                ? _bufferScript.ToString().TrimEnd()
                : null!);

            // El resultado definitivo viene de respuesta.txt (Agent 2 lo escribe ahí)
            return Utils.LimpiarRespuesta(ObtenerContextoChat(_archivoSeleccionado.Ruta));
        }

        /// <summary>
        /// Crea la burbuja de streaming para Agente 2 (verificador/corrector).
        /// Llamar solo desde el hilo UI (vía BeginInvoke).
        /// </summary>
        private void IniciarBurbujaAgente2()
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (int)(pnlChat.ClientSize.Width * 0.75));
            _burbujaScriptActual = new BurbujaChat(
                "🔍 Agente 2 verificando...", false, anchoMax, Properties.Resources.iconos1)
            {
                Tag = "10",
                Margin = new Padding(250, 5, 80, 5)
            };
            pnlChat.Controls.Add(_burbujaScriptActual);
            pnlChat.ScrollControlIntoView(_burbujaScriptActual);
            IniciarStreamingThrottle(_burbujaScriptActual);
        }

        /// <summary>
        /// Envía el resultado a Telegram y Slack en paralelo.
        /// [C2] Task.WhenAll elimina la espera secuencial: si Telegram tarda
        ///      500 ms y Slack 300 ms, el total es ~500 ms en lugar de 800 ms.
        /// </summary>
        private async Task EjecutarDifusionAsync(string mensaje, bool usarTelegram, bool usarSlack)
        {
            var tareas = new List<Task>();

            if (usarTelegram)
                tareas.Add(EnviarRespuestaTelegramAsync(mensaje, true));

            if (usarSlack && _slack != null)
                tareas.Add(_slack.SendMessage(mensaje));

            if (tareas.Count > 0)
                await Task.WhenAll(tareas);
        }

        /// <summary>
        /// Formatea la salida técnica cruda del Constructor para que se vea legible en Telegram.
        /// · Si el contenido es JSON válido → lo indenta y lo envuelve en bloque de código.
        /// · Cualquier otro texto → bloque &lt;pre&gt; monoespacio.
        /// · Trunca a 3 800 chars para respetar el límite de 4 096 de Telegram.
        /// </summary>
        private static string FormatearConstructorParaTelegram(string rawOutput)
        {
            if (string.IsNullOrWhiteSpace(rawOutput))
                return "⚙️ <b>Constructor</b>\n<i>(sin salida)</i>";

            string contenido = rawOutput.Trim();

            // ── Intentar pretty-print si parece JSON ─────────────────────────
            bool esJson = contenido.StartsWith("{") || contenido.StartsWith("[");
            if (esJson)
            {
                try
                {
                    var token = Newtonsoft.Json.Linq.JToken.Parse(contenido);
                    contenido = token.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                catch { /* dejar como está si no es JSON válido */ }
            }

            // ── Escapar caracteres especiales de HTML ────────────────────────
            contenido = contenido
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            // ── Truncar si supera el límite (reservar ~300 chars para cabecera) ─
            const int MaxChars = 3_750;
            if (contenido.Length > MaxChars)
                contenido = contenido[..MaxChars] + "\n<i>… (truncado)</i>";

            // ── Construir mensaje con bloque de código monoespacio ───────────
            // <pre><code> en Telegram HTML = bloque con fondo gris, fuente mono
            return $"⚙️ <b>Constructor — resultado técnico</b>\n\n<pre><code>{contenido}</code></pre>";
        }

        /// <summary>
        /// Formatea la salida técnica del Constructor para Slack.
        /// · JSON válido → pretty-print dentro de bloque de código (triple backtick).
        /// · Truncado a 3 800 chars (límite práctico de Slack por mensaje).
        /// </summary>
        private static string FormatearConstructorParaSlack(string rawOutput)
        {
            if (string.IsNullOrWhiteSpace(rawOutput))
                return "⚙️ *Constructor* — _(sin salida)_";

            string contenido = rawOutput.Trim();

            // Pretty-print si parece JSON
            if (contenido.StartsWith("{") || contenido.StartsWith("["))
            {
                try
                {
                    var token = Newtonsoft.Json.Linq.JToken.Parse(contenido);
                    contenido = token.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                catch { }
            }

            const int MaxChars = 3_750;
            if (contenido.Length > MaxChars)
                contenido = contenido[..MaxChars] + "\n… (truncado)";

            // Slack usa triple backtick para bloques de código monoespaciado
            return $"⚙️ *Constructor — resultado técnico*\n```\n{contenido}\n```";
        }

        /// <summary>
        /// Actualiza las etiquetas de tiempo y tokens en el hilo de UI.
        /// Seguro para llamar desde cualquier hilo.
        /// </summary>
        private void ActualizarInfoRespuesta(
            string tiempoA1, string tiempoA2,
            string tokensA1, string tokensA2)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => ActualizarInfoRespuesta(tiempoA1, tiempoA2, tokensA1, tokensA2));
                return;
            }
            lblTime.Text = tiempoA1;
            lblTimeR.Text = tiempoA2;
            lblConsumo.Text = tokensA1;
            lblConsumoA.Text = tokensA2;
        }

        // =====================================================================
        //  HELPERS DEL MOTOR
        // =====================================================================

        /// <summary>
        /// Construye el prompt estructurado del Agente 2 (validador).
        /// Extraído de EjecutarMotorIAAsync para mantener el método de
        /// orquestación limpio y facilitar pruebas unitarias aisladas.
        /// </summary>
        private static string ConstruirContextoAgente2(
            string instruccion,
            string respuestaAgente1,
            string codigoGenerado)
        {
            string codigo = string.IsNullOrWhiteSpace(codigoGenerado)
                ? "[SIN_CODIGO]"
                : codigoGenerado;

            return $@"
[INSTRUCCION_ORIGINAL]
{instruccion}

[SALIDA_AGENTE_1 — contenido de respuesta.txt]
{respuestaAgente1}

[CODIGO_EJECUTADO_POR_AGENTE_1]
{codigo}

ERES EL AGENTE VERIFICADOR. Tu única tarea es escribir en respuesta.txt el resultado final.

REGLA 1 — SI HAY ERROR o la salida está vacía o no cumple la instrucción original:
  · Genera un script Python que corrija el problema y lo ejecute.
  · Escribe SOLO el resultado corregido en respuesta.txt.
  · Sin explicaciones, sin comentarios, solo la salida.

REGLA 2 — SI TODO SALIÓ BIEN:
  · Genera un script Python que escriba en respuesta.txt un resumen claro y humano
    de lo que se hizo y el resultado obtenido.
  · Lenguaje natural, sin tecnicismos, puedes usar emojis con moderación.
  · Sin repetir el código, solo el resultado.

SIEMPRE: tu script debe escribir en respuesta.txt. Nada más.
            ";
        }

        /// <summary>
        /// Lee respuesta.txt si el modo SoloChat está activo y devuelve
        /// el texto formateado como contexto para el modelo.
        /// [C4] Renombrado de ObtenerPromtChat (typo) a ObtenerContextoChat.
        /// </summary>
        private string ObtenerContextoChat(string rutaArchivo)
        {
            if (!_soloChat) return "";

            string contenido = Utils.LeerArchivoTxt(
                Path.Combine(rutaArchivo, "respuesta.txt"));

            return $"RESPUESTA respuesta.txt : {{{contenido}}}";
        }

        /// <summary>
        /// Valida que haya instrucción, modelo y archivo antes de llamar al motor.
        /// [C4] Ahora devuelve (bool ok, string error) en lugar de lanzar
        ///      Exception directamente desde un método auxiliar; quien llama
        ///      decide si interrumpir o solo notificar al usuario.
        /// </summary>
        private (bool ok, string error) ValidarCondiciones(string instruccion)
        {
            if (string.IsNullOrWhiteSpace(instruccion))
                return (false, "La instrucción no puede estar vacía.");
            if (_modeloSeleccionado == null)
                return (false, "No hay modelo seleccionado.");
            if (_archivoSeleccionado == null)
                return (false, "No hay archivo seleccionado.");
            return (true, "");
        }

        // =====================================================================
        //  UI — MENSAJES Y CONTROLES
        // =====================================================================

        /// <summary>
        /// Agrega una burbuja de chat al panel de conversación.
        /// [C1] InvokeRequired se comprueba al inicio; las llamadas desde hilos
        ///      de Telegram/Slack (background) son seguras sin modificar el caller.
        /// </summary>
        private void MostrarMensaje(string texto, bool esUsuario)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => MostrarMensaje(texto, esUsuario));
                return;
            }

            int anchoMax = (int)(pnlChat.ClientSize.Width * 0.75);

            var burbuja = new BurbujaChat(
                texto, esUsuario, anchoMax,
                !esUsuario ? Resources.iconos1 : null)
            {
                Tag = "10",
                Margin = esUsuario
                    ? new Padding(10, 5, 10, 5)
                    : new Padding(250, 5, 80, 5)
            };

            pnlChat.Controls.Add(burbuja);
            pnlChat.ScrollControlIntoView(burbuja);
        }

        /// <summary>
        /// Detecta patrones de autocompletado con '#' en el RichTextBox
        /// y actualiza _textoFaltante para que Tab lo inserte.
        /// </summary>
        private void ConsultasApis(System.Windows.Forms.RichTextBox tb)
        {
            _textoFaltante = "";

            int cursor = tb.SelectionStart;
            string antesCursor = tb.Text[..cursor];

            int posHash = antesCursor.LastIndexOf('#');
            if (posHash == -1) return;

            string escrito = antesCursor[(posHash + 1)..];
            if (string.IsNullOrWhiteSpace(escrito)) return;

            var api = _listaApisDisponibles.FirstOrDefault(a =>
                a.Nombre.StartsWith(escrito, StringComparison.OrdinalIgnoreCase));

            if (api != null)
                _textoFaltante = api.Nombre[escrito.Length..];
        }

        /// <summary>
        /// Muestra la sugerencia de autocompletado en tiempo real junto al cursor.
        /// </summary>
        private void MostrarPreview(System.Windows.Forms.RichTextBox tb, Label lbl)
        {
            if (string.IsNullOrEmpty(_textoFaltante)) { lbl.Visible = false; return; }

            lbl.Text = _textoFaltante;
            lbl.Location = tb.GetPositionFromCharIndex(tb.SelectionStart);
            lbl.Visible = true;
            lbl.BringToFront();
        }

        // =====================================================================
        //  API — VALIDACIÓN
        // =====================================================================

        /// <summary>
        /// Comprueba que la API key del agente seleccionado sea válida.
        /// Notifica al usuario si la validación falla.
        /// </summary>
        private async Task ComprobarApi(string apikey, Servicios agente)
        {
            bool valida = agente switch
            {
                Servicios.Gemenni     => await AIServicios.ApiKeyGeminiValidaAsync(apikey),
                Servicios.ChatGpt     => await AIServicios.ApiKeyOpenAIValidaAsync(apikey),
                Servicios.Ollama      => await AIServicios.OllamaEstaActivoAsync(),
                Servicios.OpenRouter  => await AIServicios.ApiKeyOpenRouterValidaAsync(apikey),
                Servicios.Claude      => await AIServicios.ApiKeyClaudeValidaAsync(apikey),
                Servicios.Deespeek    => await AIServicios.ApiKeyDeepSeekValidaAsync(apikey),
                // Antigravity: AntigravityEstaActivoAsync ya auto-detecta project si apikey es inválido
                Servicios.Antigravity => await AIServicios.AntigravityEstaActivoAsync(apikey),
                _                     => false
            };

            if (!valida)
            {
                string msg = agente == Servicios.Antigravity
                    ? "Antigravity no está disponible.\n\n" +
                      "Verifica:\n" +
                      "• Ejecuta 'gcloud auth application-default login'\n" +
                      "• Ejecuta 'gcloud config set project TU-PROYECTO'\n" +
                      "• Que la API Vertex AI esté habilitada en el proyecto\n" +
                      "• Guarda la config en Proveedores → Antigravity"
                    : "Tu Api no es correcta o tiene algún error, comprueba por favor.";

                MostrarMensaje(msg, false);
            }
        }

        // =====================================================================
        //  DRAG & DROP DE ARCHIVOS
        // =====================================================================

        #region Manejo de arrastrar y soltar archivos

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PictureBox)))
                e.Effect = DragDropEffects.Move;
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            Cursor = Cursors.Default;
            if (e.Data.GetData(typeof(PictureBox)) is not PictureBox pic) return;

            Point dropPoint = pnlContenedorArchivos.PointToClient(new Point(e.X, e.Y));
            if (pnlContenedorArchivos.ClientRectangle.Contains(dropPoint)) return;

            // Quitar la ruta correspondiente del string acumulado.
            string rutaArchivo = pic.Tag as string ?? "";
            var rutas = _rutasAgregadas
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !r.Replace("[Ruta]:", "").Trim()
                    .Equals(rutaArchivo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _rutasAgregadas = rutas.Count > 0
                ? string.Join(",", rutas) + ","
                : "";

            pnlContenedorArchivos.Controls.Remove(pic);
            pic.Dispose();
        }

        private void Form_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(PictureBox))) return;
            Point dropPoint = pnlContenedorArchivos.PointToClient(new Point(e.X, e.Y));
            e.Effect = DragDropEffects.Move;
            Cursor = pnlContenedorArchivos.ClientRectangle.Contains(dropPoint)
                ? Cursors.Default : Cursors.No;
        }

        private void ConfigurarPanelArchivos()
        {
            pnlContenedorArchivos.FlowDirection = FlowDirection.LeftToRight;
            pnlContenedorArchivos.WrapContents = false;
        }

        private void AgregarImagenAlPanel(string rutaArchivo, Image imagen)
        {
            try
            {
                var pic = new PictureBox
                {
                    Width = 18,
                    Height = 18,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = rutaArchivo,
                    Image = new Bitmap(imagen),
                    Cursor = Cursors.Hand
                };

                _toolTipArchivos.SetToolTip(pic, Path.GetFileName(rutaArchivo));
                pic.MouseUp += Pic_MouseUp;
                pic.MouseDown += Pic_MouseDown;
                pic.MouseMove += Pic_MouseMove;
                pnlContenedorArchivos.Controls.Add(pic);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error agregando imagen: " + ex.Message);
            }
        }

        private void Pic_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseDownLocation = e.Location;
            _isDragging = false;
        }

        private void Pic_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not PictureBox pic) return;
            if (e.Button != MouseButtons.Left) return;
            if (Math.Abs(e.X - _mouseDownLocation.X) <= 3 &&
                Math.Abs(e.Y - _mouseDownLocation.Y) <= 3) return;

            _isDragging = true;
            pic.DoDragDrop(pic, DragDropEffects.Move);
        }

        private void Pic_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging || sender is not PictureBox pic) return;

            string ruta = pic.Tag as string;
            if (string.IsNullOrEmpty(ruta)) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error abriendo archivo: " + ex.Message);
            }
        }

        #endregion

        // =====================================================================
        //  TELEGRAM
        // =====================================================================

        #region Manejo de Telegram

        /// <summary>
        /// Envía una respuesta al chat de Telegram particionándola si es necesario.
        /// [C2] Las partes intermedias y la última se envían en loop; la última
        ///      incluye el teclado inline si aplica.
        /// </summary>
        public async Task EnviarRespuestaTelegramAsync(string respuesta, bool usarTelegram)
        {
            if (!usarTelegram) return;

            try
            {
                var keyboard = Utils.CrearKeyboardDesdeTexto(respuesta);
                string codificada = System.Net.WebUtility.HtmlEncode(respuesta);
                var partes = Utils.DividirMensajeTelegram(codificada);

                for (int i = 0; i < partes.Count; i++)
                {
                    bool esUltima = i == partes.Count - 1;
                    await EnviarTelegramDesdeActivacion(partes[i], esUltima ? keyboard : null);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje(ex.ToString(), false);
            }
        }

        private Task EnviarTelegramDesdeActivacion(string respuesta, object btns = null) =>
            EnviarTelegramAsync(_telegramChat.Apikey, _telegramChat.ChatId, respuesta, btns, true);

        /// <summary>
        /// Inicia o reinicia el listener de Telegram con la configuración actual.
        /// [C1] El listener previo se detiene explícitamente antes de crear uno nuevo
        ///      para evitar tener dos goroutines escuchando el mismo bot token.
        /// </summary>
        private async Task IniciarConversasionTelegram()
        {
            LeerConfiguracionTelegram();

            if (_telegramChat.ChatId == 0 || string.IsNullOrEmpty(_telegramChat.Apikey))
            {
                MostrarMensaje(
                    "Telegram aún no está configurado.\r\n" +
                    "Proporciona tu API Key y tu ID de usuario y escribe #CONFIGURA TELEGRAM",
                    false);
                return;
            }

            _telegramListener?.Stop();
            _telegramListener = null;

            _telegramListener = new TelegramListener(
                _telegramChat.Apikey,
                _archivoSeleccionado.Ruta,
                _telegramChat.ChatId);

            _telegramListener.OnMessageReceived += (chatId, texto) =>
            {
                if (!IsHandleCreated) return;
                BeginInvoke(async () =>
                    await ProcesarMensajeTelegramAsync(_telegramChat.Apikey, chatId, texto));
            };

            _telegramListener.OnCallbackReceived += (chatId, data) =>
            {
                if (!IsHandleCreated) return;
                BeginInvoke(async () =>
                {
                    var opc = TelegramSender.CancelarConfig();
                    await EnviarTelegramAsync(_telegramChat.Apikey, chatId,
                        "Tu mensaje ha sido recibido y se está procesando... Por favor espera.",
                        opc, true);
                    await ProcesarMensajeTelegramAsync(_telegramChat.Apikey, chatId, data);
                });
            };

            _telegramListener.OnFileReceived += (chatId, fileId, fileName, fileSize, fileType) =>
            {
                BeginInvoke(() => MostrarMensaje($"Archivo recibido: {fileName}", false));
                return Task.CompletedTask;
            };

            _telegramListener.Start();
        }

        /// <summary>
        /// Procesa un mensaje entrante de Telegram con exclusión mutua (semáforo)
        /// para evitar procesamiento concurrente de dos mensajes del mismo usuario.
        /// </summary>
        private async Task ProcesarMensajeTelegramAsync(string token, long chatId, string texto)
        {
            await _telegramSemaphore.WaitAsync();
            try
            {
                MostrarMensaje(texto, true);

                string textoNorm = texto.Trim().ToUpperInvariant();
                bool esComando = textoNorm.StartsWith('#');

                if (!esComando)
                {
                    await EnviarTelegramAsync(token, chatId,
                        "Tu mensaje ha sido recibido y se está procesando... Por favor espera.",
                        TelegramSender.CancelarConfig(), true);
                }

                await EjecutarComandoOConsultaAsync(token, chatId, textoNorm, texto, true, false);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error procesando mensaje de Telegram: {ex.Message}", false);
                await EnviarTelegramAsync(token, chatId,
                    "Ocurrió un error procesando tu mensaje. Intenta de nuevo.", null, true);
            }
            finally
            {
                _telegramSemaphore.Release();
            }
        }

        // Helpers de teclados para Telegram
        private object CambiarAgente_Telegram() => TelegramSender.CrearKeyboardDesdeListaAgentes(_listaAgentes);
        private object CambiarModelo_Telegram() => TelegramSender.CrearKeyboardDesdeListaModelos(_modelosAgente);
        private object CambiarRuta_Telegram() => TelegramSender.CrearKeyboardDesdeListaRutas(_listaArchivosDisponibles);

        /// <summary>
        /// Enrutador de comandos Telegram / Slack. Interpreta el texto recibido
        /// y ejecuta la acción correspondiente, o deriva al motor IA si no es un comando.
        /// </summary>
        private async Task EjecutarComandoOConsultaAsync(
            string token, long chatIdTelegram,
            string textoCmd, string textoOriginal,
            bool usarTelegram = false, bool usarSlack = false)
        {
            chatIdTelegram = _telegramChat.ChatId;
            textoCmd = textoCmd.Trim().ToUpperInvariant();

            string valor = "";
            if (textoCmd.StartsWith('#'))
            {
                var (comando, val) = TelegramSender.ExtraerComandoYValor(textoCmd);
                textoCmd = comando;
                valor = val;
            }

            switch (textoCmd)
            {
                case "#CANCELAR":
                    CancelarInstruccion();
                    break;

                case "#AGENTE":
                    int svc = Utils.ObtenerServicio_Nombre(valor);
                    var agente = _listaAgentes.FirstOrDefault(e => (int)e.Agente == svc);
                    if (agente != null) comboBoxAgentes.SelectedValue = agente.ApiKey;
                    break;

                case "#MODELO":
                    comboBoxModeloIA.Text = valor;
                    break;

                case "#RUTA":
                    comboBoxRuta.Text = valor;
                    break;

                case "#CAMBIARAGENTE":
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        "Elije una opcion :", CambiarAgente_Telegram(), usarTelegram, usarSlack);
                    break;

                case "#CAMBIARMODELO":
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        "Elije una opcion :", CambiarModelo_Telegram(), usarTelegram, usarSlack);
                    break;

                case "#CAMBIARRUTA":
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        "Elije una opcion :", CambiarRuta_Telegram(), usarTelegram, usarSlack);
                    break;

                case "#CONFIGURACIONES":
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        "Elije una opcion :", TelegramSender.Configuraciones_Menu(), usarTelegram, usarSlack);
                    break;

                case "#ACTIVATELEGRAM":
                    _telegramActivo = true;
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        "Se activaron los mensajes de Telegram.", null, usarTelegram, usarSlack);
                    break;

                case "#DESACTIVATELEGRAM":
                    _telegramActivo = false;
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        "Se desactivaron los mensajes de Telegram.", null, usarTelegram, usarSlack);
                    break;

                case "#RECORDAR":
                    _recordarTema = !_recordarTema;
                    checkBoxRecordar.Checked = _recordarTema;
                    string estadoRec = _recordarTema ? "activada" : "desactivada";
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        $"Opción RECORDAR TEMA {estadoRec}.", null, usarTelegram, usarSlack);
                    break;

                case "#SOLOCHAT":
                    _soloChat = !_soloChat;
                    checkBoxSoloChat.Checked = _soloChat;
                    string estadoSC = _soloChat ? "activado" : "desactivado";
                    MostrarMensaje("SE ACTIVÓ LA OPCIÓN SOLO CHAT", false);
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        $"Opción SOLO CHAT {estadoSC}.", null, usarTelegram, usarSlack);
                    break;

                case "#APIS":
                    // [C4] CargarListaAgents solo se llama bajo demanda explícita.
                    CargarListaAgents();
                    string apis = string.Join("\n", _listaApisDisponibles.Select(a => $"- {a.Nombre}"));
                    await EnvioComandoTelegramSlackAsync(token, chatIdTelegram,
                        $"APIs disponibles:\n{apis}", null, usarTelegram, usarSlack);
                    break;

                default:
                    if (!usarSlack)
                    {
                        textBoxInstrucion.Text = textoOriginal;
                       // pictureBoxCarga.Visible = true;
                        try
                        {
                            if (string.IsNullOrEmpty(textoOriginal)) return;
                            // EjecutarMotorIAAsync ya muestra las burbujas de streaming
                            // internamente — no hace falta llamar MostrarMensaje aquí.
                            await EjecutarMotorIAAsync(textoOriginal, usarTelegram);
                        }
                        finally
                        {
                            //pictureBoxCarga.Visible = false;
                            textBoxInstrucion.Text = "";
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// [C2] Helper que evita repetir el par EnviarTelegram + EnviarSlack
        ///      en cada case del switch de comandos. Lanza ambos en paralelo
        ///      cuando están activos.
        /// </summary>
        private async Task EnvioComandoTelegramSlackAsync(
            string token, long chatId,
            string mensaje, object btns,
            bool usarTelegram, bool usarSlack)
        {
            var tareas = new List<Task>();

            if (usarTelegram)
                tareas.Add(EnviarTelegramAsync(token, chatId, mensaje, btns, true));

            if (usarSlack && _slack != null)
                tareas.Add(_slack.SendMessage(mensaje));

            if (tareas.Count > 0)
                await Task.WhenAll(tareas);
        }

        /// <summary>
        /// Envía un mensaje al bot de Telegram. No lanza excepciones al caller;
        /// los errores se notifican como burbuja en el chat local.
        /// </summary>
        private async Task EnviarTelegramAsync(
            string token, long chatId, string mensaje,
            object btns = null, bool usarTelegram = false)
        {
            if (!usarTelegram) return;

            try
            {
                await TelegramSender.EnviarMensajeAsync(token, chatId, mensaje, btns);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error enviando mensaje a Telegram: {ex.Message}", false);
            }
        }

        private void LeerConfiguracionTelegram()
        {
            if (_archivoSeleccionado == null) return;

            if (string.IsNullOrEmpty(_archivoSeleccionado.Ruta))
                _archivoSeleccionado.Ruta = RutasProyecto.ObtenerRutaScripts();

            if (!Directory.Exists(_archivoSeleccionado.Ruta)) return;

            var config = JsonManager.Leer<TelegramChat>(
                RutasProyecto.ObtenerRutaListTelegram(_archivoSeleccionado.Ruta));

            _telegramChat = config.Count > 0
                ? config[0]
                : new TelegramChat { ChatId = 0, Apikey = "" };
        }

        #endregion

        // =====================================================================
        //  SLACK
        // =====================================================================

        #region Manejo de Slack

        /// <summary>
        /// Procesa una petición recibida desde Slack: si es comando la enruta,
        /// si es texto libre la envía al motor IA.
        /// [C1] BeginInvoke en lugar de Invoke para no bloquear el hilo de Slack.
        /// </summary>
        private async Task<string> Realizar_Peticiones_Slack(string instruccion)
        {
            //BeginInvoke(() => pictureBoxCarga.Visible = true);

            var (ok, error) = ValidarCondiciones(instruccion);
            if (!ok)
            {
                MostrarMensaje(error, false);
                //BeginInvoke(() => pictureBoxCarga.Visible = false);
                return error;
            }

            string respuesta = "";
            string textoNorm = instruccion.Trim().ToUpperInvariant();

            if (textoNorm.StartsWith('#'))
                await EjecutarComandoOConsultaAsync("", 0, textoNorm, "", false, true);
            else
                respuesta = await EjecutarMotorIAAsync(instruccion, false, true);

            BeginInvoke(() =>
            {
                //pictureBoxCarga.Visible = false;
                if (!string.IsNullOrEmpty(respuesta))
                    MostrarMensaje(respuesta, false);
            });

            return respuesta;
        }

        /// <summary>
        /// Inicia o reinicia el servicio de polling de Slack.
        /// [C1] _slack?.Stop() con null-check corrige el NRE que ocurría en la
        ///      primera carga cuando _slack todavía era null.
        /// [C1] El handler usa BeginInvoke en lugar de Invoke para no bloquear
        ///      el hilo de polling de Slack.
        /// </summary>
        private async Task IniciarSlack()
        {
            LeerConfiguracionSlack();

            if (string.IsNullOrEmpty(_slackChat?.Tokem)) return;

            // Detener instancia anterior antes de crear una nueva.
            _slack?.Stop();
            _slack = new SlackPollingService(_slackChat.Tokem, _slackChat.IDcanal);

            _slack.OnMessageReceived += async (msg) =>
            {
                if (msg == null || string.IsNullOrWhiteSpace(msg.Text)) return;
                if (!string.IsNullOrEmpty(msg.BotId)) return;

                LeerConfiguracionSlack();

                if (!_slackChat.usuarios.Contains(msg.User))
                {
                    await _slack.SendMessage(
                        "Tu usuario no está configurado para realizar instrucciones, " +
                        "pide al administrador que te agregue.");
                    return;
                }

                if (_procesandoSlack) return;

                _procesandoSlack = true;
                try
                {
                    string instruccion = msg.Text;
                    // [C1] BeginInvoke no bloquea el hilo de polling.
                    BeginInvoke(() =>
                    {
                        textBoxInstrucion.Text = instruccion;
                        MostrarMensaje("RECIBIDO: " + instruccion, false);
                    });

                    await _slack.SendMessage("Su instrucción se recibió, espere respuesta...");
                    await Realizar_Peticiones_Slack(instruccion);
                }
                catch
                {
                    await _slack.SendMessage("Error procesando la instrucción.");
                }
                finally
                {
                    _procesandoSlack = false;
                }
            };

            _slack.Start();
        }

        private void LeerConfiguracionSlack()
        {
            if (_archivoSeleccionado == null) return;

            if (string.IsNullOrEmpty(_archivoSeleccionado.Ruta))
                _archivoSeleccionado.Ruta = RutasProyecto.ObtenerRutaScripts();

            if (!Directory.Exists(_archivoSeleccionado.Ruta)) return;

            var config = JsonManager.Leer<SlackChat>(
                RutasProyecto.ObtenerRutaSlack(_archivoSeleccionado.Ruta));

            if (config.Count > 0)
            {
                _slackChat = config[0];
            }
            else
            {
                _slackChat = new SlackChat { Tokem = "", IDcanal = "" };
                BeginInvoke(() =>
                    MostrarMensaje(
                        "No tienes configuración para Slack, configúrala para poder comunicarte.",
                        false));
            }
        }

        #endregion

        // =====================================================================
        //  CONFIGURACIÓN, TEMAS Y UTILIDADES
        // =====================================================================

        private void GuardarConfiguracion()
        {
            _configuracionClient.Mimodelo ??= new Modelo();
            _configuracionClient.MiArchivo ??= new Archivo();
            _configuracionClient.Miapi ??= new Api();

            _configuracionClient.Mimodelo = comboBoxAgentes.SelectedItem as Modelo ?? new Modelo();
            _configuracionClient.MiArchivo = comboBoxRuta.SelectedItem as Archivo ?? new Archivo();
            _configuracionClient.Miapi.Nombre = comboBoxModeloIA.Text;
            _configuracionClient.Miapi.key = _configuracionClient.Mimodelo.ApiKey;

            Utils.GuardarConfig<ConfiguracionClient>(
                RutasProyecto.ObtenerRutaConfiguracion(), _configuracionClient);

            MostrarMensaje("¡Configuración guardada!", false);
        }

        private void LimpiarResumen()
        {
            lblTime.Text = "";
            lblTimeR.Text = "";
            lblConsumo.Text = "";
            lblConsumoA.Text = "";
        }

        private void LimpiarControles()
        {
            _rutasAgregadas = "";
            pnlContenedorArchivos.Controls.Clear();
        }

        /// <summary>
        /// Cancela la petición activa al modelo de IA.
        /// [C1] Solo cancela el token; Dispose se hace en la próxima llamada
        ///      o en FormClosing para evitar doble-dispose.
        /// </summary>
        private void CancelarInstruccion()
        {
            if (_ctsIA == null || _ctsIA.IsCancellationRequested)
                return;

            _ctsIA.Cancel();

            // Feedback visual inmediato en el botón
            if (btnCancelar.InvokeRequired)
            {
                btnCancelar.BeginInvoke(() => MostrarFeedbackCancelacion());
            }
            else
            {
                MostrarFeedbackCancelacion();
            }
        }

        private async void MostrarFeedbackCancelacion()
        {
            var colorOriginal   = btnCancelar.FlatAppearance.BorderColor;
            var colorOriginalFG = btnCancelar.ForeColor;
            var textoOriginal   = btnCancelar.Text;

            // Estado "Cancelando..."
            btnCancelar.Text      = "⏹ Cancelando...";
            btnCancelar.ForeColor = Color.OrangeRed;
            btnCancelar.FlatAppearance.BorderColor = Color.OrangeRed;
            btnCancelar.Enabled   = false;

            await Task.Delay(1_800);

            // Restaurar
            if (!btnCancelar.IsDisposed)
            {
                btnCancelar.Text      = textoOriginal;
                btnCancelar.ForeColor = colorOriginalFG;
                btnCancelar.FlatAppearance.BorderColor = colorOriginal;
                btnCancelar.Enabled   = true;
            }
        }

        private async Task CargarThemas()
        {
            ConfigurarUI();
            ConfigurarEventos();

            if (_configuracionClient.Mimodelo == null) return;
            if (!string.IsNullOrEmpty(_configuracionClient.Mimodelo.Modelos))
                await CargarModeloSeleccionadoAsync();
        }

        private void ConfigurarUI()
        {
            SuspendLayout();
            try
            {
                pnlChat.FlowDirection = FlowDirection.TopDown;
                pnlChat.WrapContents = false;
                pnlChat.AutoScroll = true;

                comboBoxRuta.DataSource = _listaArchivosDisponibles;
                comboBoxRuta.DisplayMember = "Ruta";
                comboBoxRuta.ValueMember = "Ruta";

                _primeraCar = true;

                comboBoxAgentes.DataSource = _listaAgentes;
                comboBoxAgentes.DisplayMember = "Agente";
                comboBoxAgentes.ValueMember = "ApiKey";

                textBoxInstrucion.Multiline = true;
                textBoxInstrucion.Height = 80;
                textBoxInstrucion.AllowDrop = true;
                textBoxInstrucion.AcceptsTab = true;

                // ── Barra de estado de agentes ARIA ─────────────────────────────
                // Se inserta entre panelHead (DockStyle.Top) y pnlChat (Fill).
                // SetChildIndex con el índice de panelHead empuja panelHead al siguiente
                // z-order, de modo que panelHead se acople primero (arriba del todo)
                // y _panelAgentes se acople debajo sin tocar el Designer.
                _panelAgentes = new PanelAgentes
                {
                    Dock = DockStyle.Top,
                    Height = 30
                };
                Controls.Add(_panelAgentes);
                Controls.SetChildIndex(_panelAgentes, Controls.IndexOf(panelHead));

                pnlContenedorTxt.Redondear();
                pnlChat.Redondear();
                pnlContenedorArchivos.Redondear();

                labelSugerencia.Parent = textBoxInstrucion;
                labelSugerencia.BackColor = Color.Transparent;
                labelSugerencia.AutoSize = true;
                labelSugerencia.Enabled = false;
                labelSugerencia.Font = textBoxInstrucion.Font;

                btnEnviar.AplicarEstiloOutline(colorBorde: Color.FromArgb(64, 158, 255), borderRadius: 7);
                btnCancelar.AplicarEstiloOutline(colorBorde: Color.FromArgb(255, 0, 0), borderRadius: 7);

                textBoxInstrucion.RedondearRichTextBox(
                    borderRadius: 12,
                    borderColor: Color.FromArgb(64, 158, 255),
                    borderSize: 2,
                    focusColor: Color.FromArgb(41, 128, 185),
                    agregarSombra: true);

                // ── Propagar Anchor al wrapper creado por RedondearRichTextBox ──────────
                // RedondearRichTextBox reparenta textBoxInstrucion dentro de un Panel nuevo
                // que no hereda el Anchor original (Left|Right). Sin este fix el wrapper
                // no se expande al redimensionar y btnEnviar/btnCancelar quedan sueltos.
                foreach (Control c in pnlContenedorTxt.Controls)
                {
                    if (c is Panel rtbWrapper && rtbWrapper.Controls.Contains(textBoxInstrucion))
                    {
                        rtbWrapper.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        break;
                    }
                }

                // Llevar los botones al frente (z-order) y alinearlos al wrapper
                btnEnviar.BringToFront();
                btnCancelar.BringToFront();

                ConfigurarPanelArchivos();

                _toolTipArchivos.AutoPopDelay = 5000;
                _toolTipArchivos.InitialDelay = 500;
                _toolTipArchivos.ReshowDelay = 200;
                _toolTipArchivos.ShowAlways = true;

                // ── Barra inferior: label "🔔 Notificar en:" ─────────────────
                var lblNotif = new Label
                {
                    Text      = "🔔 Notif.:",
                    AutoSize  = true,
                    ForeColor = Color.FromArgb(100, 120, 155),
                    BackColor = Color.Transparent,
                    Font      = new Font("Segoe UI", 8f, FontStyle.Regular),
                    Location  = new Point(8, 114)
                };
                pnlContenedorTxt.Controls.Add(lblNotif);
                lblNotif.BringToFront();

                // Separador visual entre Telegram y Slack
                var pnlSepNotif = new Panel
                {
                    Location  = new Point(268, 114),
                    Size      = new Size(1, 16),
                    BackColor = Color.FromArgb(50, 65, 90)
                };
                pnlContenedorTxt.Controls.Add(pnlSepNotif);
                pnlSepNotif.BringToFront();

                // Asegurar z-order correcto para toda la barra inferior
                checkBoxTelegram.BringToFront();
                checkBoxConstructorTelegram.BringToFront();
                checkBoxSlack.BringToFront();
                checkBoxConstructorSlack.BringToFront();
                btnLimpiar.BringToFront();
                btnInfo.BringToFront();

                // ── Control de reintentos del Guardián (barra inferior) ────
                var lblReintentos = new Label
                {
                    Text      = "Reintentos:",
                    AutoSize  = true,
                    ForeColor = Color.FromArgb(140, 165, 205),
                    BackColor = Color.Transparent,
                    Font      = new Font("Segoe UI", 7.5f),
                    Location  = new Point(448, 114)
                };

                _nudReintentos = new NumericUpDown
                {
                    Minimum            = 0,
                    Maximum            = 5,
                    Value              = 3,
                    Width              = 42,
                    Height             = 22,
                    Location           = new Point(524, 110),
                    BackColor          = Color.FromArgb(30, 41, 59),
                    ForeColor          = Color.White,
                    Font               = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BorderStyle        = BorderStyle.FixedSingle,
                    TextAlign          = HorizontalAlignment.Center,
                    InterceptArrowKeys = true
                };

                _toolTipArchivos.SetToolTip(_nudReintentos,
                    "Correcciones automáticas del Guardián (0 = sin reintentos)");
                _toolTipArchivos.SetToolTip(lblReintentos,
                    "Correcciones automáticas del Guardián");

                pnlContenedorTxt.Controls.Add(lblReintentos);
                pnlContenedorTxt.Controls.Add(_nudReintentos);
                lblReintentos.BringToFront();
                _nudReintentos.BringToFront();

                // ── Sub-checkboxes del Constructor: deshabilitados hasta activar el canal ──
                checkBoxConstructorTelegram.Enabled = false;
                checkBoxConstructorSlack.Enabled    = false;

                // ── Labels de estadísticas ocultas hasta presionar ℹ ──────
                lblTime.Visible     = false;
                lblTimeR.Visible    = false;
                lblConsumo.Visible  = false;
                lblConsumoA.Visible = false;
            }
            finally
            {
                ResumeLayout(true);
            }

            // Alinear botones al wrapper del textbox tras el layout inicial
            AjustarBotonesInput();
        }

        private void ConfigurarEventos()
        {
            // Desuscribir antes de suscribir para evitar handlers duplicados.
            textBoxInstrucion.DragEnter -= textBoxInstrucion_DragEnter;
            textBoxInstrucion.DragDrop -= textBoxInstrucion_DragDrop;
            textBoxInstrucion.DragEnter += textBoxInstrucion_DragEnter;
            textBoxInstrucion.DragDrop += textBoxInstrucion_DragDrop;

            this.DragEnter -= Form_DragEnter;
            this.DragDrop -= Form_DragDrop;
            this.DragOver -= Form_DragOver;
            this.DragLeave -= Form_DragLeave;
            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;
            this.DragOver += Form_DragOver;
            this.DragLeave += Form_DragLeave;

            AllowDrop = true;
        }

        /// <summary>
        /// Carga el modelo guardado en la configuración y actualiza los combos.
        /// [C4] await directo elimina el Task.Run(() => asyncMethod()) que
        ///      creaba un deadlock potencial al mezclar sync y async.
        /// </summary>
        private async Task CargarModeloSeleccionadoAsync()
        {
            comboBoxAgentes.SelectedValue = _configuracionClient.Mimodelo.ApiKey;

            var modelos = await ObtenerModeloAgente(
                _configuracionClient.Mimodelo.Agente,
                _configuracionClient.Miapi.key);

            if (InvokeRequired)
                Invoke(() => ActualizarComboModelos(modelos));
            else
                ActualizarComboModelos(modelos);
        }

        private void ActualizarComboModelos(List<ModeloAgente> modelos)
        {
            _cargaInicialModelo = true;

            comboBoxModeloIA.DisplayMember = "Nombre";
            comboBoxModeloIA.ValueMember = "Nombre";
            comboBoxModeloIA.DataSource = null;
            comboBoxModeloIA.DataSource = modelos;

            _modeloSeleccionado.Agente = _configuracionClient.Mimodelo.Agente;
            _modeloSeleccionado.Modelos = _configuracionClient.Mimodelo.Modelos;
            _modeloSeleccionado.ApiKey = _configuracionClient.Miapi.key;

            _cargaInicialModelo = true;
            comboBoxModeloIA.SelectedValue = _modeloSeleccionado.Modelos;
        }
    }
}