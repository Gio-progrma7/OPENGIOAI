// +--------------------------------------------------------------------------+
// �  FrmMandos.cs � Ada Lovelace IA v3.0                                    �
// �                                                                          �
// �  CAMBIOS APLICADOS (resumen ejecutivo):                                  �
// �                                                                          �
// �  [C1] HILOS / THREAD-SAFETY                                              �
// �    � MostrarMensaje: guard InvokeRequired movido al inicio del m�todo.   �
// �    � EjecutarMotorIAAsync: todos los BeginInvoke centralizados en        �
// �      MostrarMensajeAsync() para evitar race conditions al escribir UI    �
// �      desde tareas paralelas.                                             �
// �    � IniciarSlack / Telegram: lambdas de eventos ahora usan             �
// �      BeginInvoke consistentemente; se eliminaron llamadas a Invoke()     �
// �      (bloqueante) dentro de callbacks async.                             �
// �    � CancellationTokenSource se pasa a EjecutarMotorIAAsync y se        �
// �      respeta con CancellationToken.ThrowIfCancellationRequested()        �
// �      entre las dos pasadas de agentes.                                   �
// �                                                                          �
// �  [C2] POTENCIA DE LLAMADAS / SUB-AGENTES                                �
// �    � EjecutarMotorIAAsync refactorizado en tres m�todos privados:        �
// �        � EjecutarAgente1Async : lanza Agente 1 con timeout configurable. �
// �        � EjecutarAgente2Async : lanza Agente 2 solo si corresponde.     �
// �        � EjecutarDifusi�nAsync: env�a resultado a Telegram + Slack en   �
// �          paralelo con Task.WhenAll para reducir latencia de salida.      �
// �    � Timeout configurable (_timeoutSegundos) por petici�n al modelo;     �
// �      evita bloqueos silenciosos si la API no responde.                   �
// �    � Los dos agentes comparten el mismo CancellationToken; si el usuario �
// �      cancela a mitad, ninguna tarea sigue corriendo en background.       �
// �                                                                          �
// �  [C3] ENCAPSULAMIENTO Y COHESI�N                                         �
// �    � Extracci�n de AgentOrchestrator (inner class / partial) con la     �
// �      l�gica pura del motor desacoplada de la UI.                         �
// �    � CargarListaAgents: movida a DataLoader (static) para testear sin   �
// �      depender del formulario.                                            �
// �    � ConversationWindow ya exist�a; se corrige su integraci�n para que  �
// �      Limpiar() solo se llame UNA vez por cambio de agente/ruta.          �
// �    � _ctsIA: ahora se hace Dispose() del token anterior antes de        �
// �      crear uno nuevo (memory leak corregido).                            �
// �                                                                          �
// �  [C4] BUGS CORREGIDOS                                                    �
// �    � comboBoxAgentes_SelectedIndexChanged: el contador "veces" no se    �
// �      reseteaba � si el usuario cambiaba de agente 3+ veces solo         �
// �      disparaba la primera. Reemplazado por flag _cargaInicialAgente.     �
// �    � IniciarSlack: se protege con null-check en _slack antes de Stop()  �
// �      para evitar NRE en la primera carga.                                �
// �    � EnviarTelegramAsync: par�metro btns movido antes de UsarTelegram   �
// �      para alinear las 5 sobrecargas del sitio de llamada.               �
// �    � ObtenerPromtChat: renombrado a ObtenerContextoChat (typo).         �
// �    � MostrarAdvertencia: no debe lanzar Exception desde un helper        �
// �      llamado en background; ahora devuelve bool + mensaje de error.      �
// +--------------------------------------------------------------------------+

using Newtonsoft.Json.Linq;
using OPENGIOAI.Agentes;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Properties;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.ServiciosSlack;
using OPENGIOAI.ServiciosTTS;
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
        // -- Constantes de comportamiento --------------------------------------
        // [C2] Centralizar aqu� facilita cambiar el timeout sin buscar en el c�digo.
        // NOTA (Fase comandos): TimeoutSegundos pas� de const a field privado para
        // poder modificarse v�a `#timeout <N>` desde Telegram/Slack/UI.
        private int _timeoutSegundos = 120;
        private const int TimeoutMinimo = 10;
        private const int TimeoutMaximo = 1800; // 30 min � techo de seguridad
        private const int MaxTurnosContexto = 6;
        private const int MaxTokensContexto = 3000;

        private int Entradas = 9;

        // -- Controles y utilidades UI -----------------------------------------
        private readonly System.Windows.Forms.ToolTip _toolTipArchivos = new();

        // -- Flags de estado ---------------------------------------------------
        private bool _ajustandoAltura = false;
        private bool _telegramActivo = false;
        private bool _enviarConstructorTelegram = false;
        private bool _enviarConstructorSlack    = false;
        private bool _enviarArchivosTelegram    = false;
        private bool _enviarArchivosSlack       = false;
        private bool _enviarAudio               = false;
        private bool _slackActivo = false;
        private bool _recordarTema = false;
        private bool _soloResptxt = false;
        private bool _soloChat = false;
        private bool _isDragging = false;
        private bool _procesandoSlack = false;

        // [C4] Reemplaza el contador "veces" con flag booleano para mayor claridad.
        private bool _cargaInicialAgente = true;

        // -- Ventana deslizante de contexto conversacional ---------------------
        private readonly ConversationWindow _ventana = new(MaxTurnosContexto, MaxTokensContexto);

        // -- Strings auxiliares ------------------------------------------------
        private string _textoFaltante = "";
        private string _rutasAgregadas = "";

        // -- Misc --------------------------------------------------------------
        private Point _mouseDownLocation;

        // -- Servicios externos (inyectados por DI) ----------------------------
        private readonly SlackChannelService _slackService;
        // [C1] _ctsIA: se hace Dispose del token anterior antes de crear uno nuevo.
        private CancellationTokenSource _ctsIA;
        private readonly TelegramService _telegramService;
        private readonly BroadcastService _broadcast;
        // Nuevo motor de comandos � reemplaza al antiguo CommandRouter.
        private readonly Comandos.CommandRegistry _cmdRegistry = new();
        private Comandos.CommandExecutor _cmdExecutor = null!;

        // -- ARIA: panel de estado de agentes y tracking de fase ---------------
        private PanelAgentes _panelAgentes = null!;
        private BurbujaChat? _burbujaFaseActual;

        // -- Streaming de salida de script en vivo -----------------------------
        private BurbujaChat? _burbujaScriptActual;
        private readonly StringBuilder _bufferScript = new();

        // -- Control de reintentos del Guardi�n --------------------------------
        private NumericUpDown _nudReintentos = null!;

        // -- Throttle de streaming (agrupa updates de burbujas a ~8 fps) -------
        private readonly ChatStreamingThrottleService _streaming;

        // -- Modelos de datos --------------------------------------------------
        private ConfiguracionClient _configuracionClient;
        private readonly AudioTTSService _audioService;
        private Modelo _modeloSeleccionado = new();
        private Archivo _archivoSeleccionado = new();
        private List<Api> _listaApisDisponibles = new();
        private List<Modelo> _listaAgentes = new();
        private List<Archivo> _listaArchivosDisponibles = new();
        private List<ModeloAgente> _modelosAgente = new();

        // =====================================================================
        //  CONSTRUCTOR
        // =====================================================================
        public FrmMandos(
            ConfiguracionClient config,
            TelegramService telegramService,
            SlackChannelService slackService,
            AudioTTSService audioService,
            BroadcastService broadcast)
        {
            InitializeComponent();
            _configuracionClient = config ?? throw new ArgumentNullException(nameof(config));
            _telegramService = telegramService;
            _slackService    = slackService;
            _audioService    = audioService;
            _broadcast       = broadcast;
            _streaming = new ChatStreamingThrottleService(this);
            ConfigurarCommandRouter();
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
            int anchoMax = (pnlChat.ClientSize.Width - 28);
            foreach (Control c in pnlChat.Controls)
            {
                if (c is BurbujaChat burbuja)
                    burbuja.ActualizarAncho(anchoMax);
            }
            AjustarBotonesInput();
        }

        /// <summary>
        /// Alinea btnEnviar y btnCancelar al borde derecho del textbox plano.
        /// Se invoca en ConfigurarUI() y en cada FrmMandos_Resize.
        /// </summary>
        private void AjustarBotonesInput()
        {
            if (textBoxInstrucion == null) return;

            int xBtn = textBoxInstrucion.Right + 6;
            btnEnviar.Left   = xBtn;
            btnCancelar.Left = xBtn;
            // Vertical: enviar arriba, cancelar debajo
            btnEnviar.Top   = textBoxInstrucion.Top;
            btnCancelar.Top = textBoxInstrucion.Top + btnEnviar.Height + 4;
        }

        private void FrmMandos_FormClosing(object sender, FormClosingEventArgs e)
        {
            // [C1] Cancelar cualquier petici�n en vuelo al cerrar el formulario.
            _streaming.Dispose();
            CancelarInstruccion();
            _telegramService.Detener();
            _slackService.Detener();
            _ctsIA?.Dispose();
        }

        private void Form_DragLeave(object sender, EventArgs e) => Cursor = Cursors.Default;

        // =====================================================================
        //  EVENTOS DE CONTROLES
        // =====================================================================

        private void textBoxInstrucion_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            if (e.Data?.GetDataPresent(DataFormats.UnicodeText) == true
                || e.Data?.GetDataPresent(DataFormats.Text) == true)
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            e.Effect = DragDropEffects.None;
        }

        private void textBoxInstrucion_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
            {
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
                return;
            }

            string? texto = e.Data?.GetData(DataFormats.UnicodeText) as string
                         ?? e.Data?.GetData(DataFormats.Text) as string;

            if (string.IsNullOrWhiteSpace(texto)) return;

            // Insertar texto de credencial en el caret (o reemplazar selección)
            int selStart = textBoxInstrucion.SelectionStart;
            int selLen   = textBoxInstrucion.SelectionLength;
            string actual = textBoxInstrucion.Text ?? string.Empty;

            string nuevo = selLen > 0
                ? actual.Remove(selStart, selLen).Insert(selStart, texto)
                : actual.Insert(selStart, texto);

            textBoxInstrucion.Text = nuevo;
            textBoxInstrucion.SelectionStart = selStart + texto.Length;
            textBoxInstrucion.SelectionLength = 0;
            textBoxInstrucion.Focus();
            return;
        }

        private void textBoxInstrucion_TextChanged(object sender, EventArgs e)
        {
            if (_ajustandoAltura) return;
            //ConsultasApis(textBoxInstrucion);
            MostrarPreview(textBoxInstrucion, labelSugerencia);
            //TextBoxRounder.AjustarAlturaConRedondeo(textBoxInstrucion);
        }

        private void textBoxInstrucion_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter ? enviar instrucci�n
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
            // Habilitar / deshabilitar las sub-opciones del Constructor y archivos para Slack
            checkBoxConstructorSlack.Enabled = _slackActivo;
            checkBoxArchivosSlack.Enabled    = _slackActivo;
            if (!_slackActivo)
            {
                checkBoxConstructorSlack.Checked = false;
                checkBoxArchivosSlack.Checked    = false;
            }
            await IniciarSlack();
        }

        private void checkBoxConstructorSlack_CheckedChanged(object sender, EventArgs e)
        {
            _enviarConstructorSlack = checkBoxConstructorSlack.Checked;
        }

        private void checkBoxArchivosSlack_CheckedChanged(object sender, EventArgs e)
        {
            _enviarArchivosSlack = checkBoxArchivosSlack.Checked;
        }

        private void checkBoxAudio_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBoxAudio.Checked) { _enviarAudio = false; return; }

            // Verificar que TTS est� configurado antes de activar
            _audioService.RecargarConfig();
            if (!_audioService.Activo)
            {
                checkBoxAudio.Checked = false;
                _toolTipArchivos.Show(
                    "Configura el proveedor de voz en\nComunicadores ? ?? Audio TTS",
                    checkBoxAudio, 0, -48, 3500);
                return;
            }
            _enviarAudio = true;
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
            // Habilitar / deshabilitar las sub-opciones del Constructor y archivos
            checkBoxConstructorTelegram.Enabled = _telegramActivo;
            checkBoxArchivosTelegram.Enabled    = _telegramActivo;
            if (!_telegramActivo)
            {
                checkBoxConstructorTelegram.Checked = false;
                checkBoxArchivosTelegram.Checked    = false;
            }
            await IniciarConversasionTelegram();
        }

        private void checkBoxConstructorTelegram_CheckedChanged(object sender, EventArgs e)
        {
            _enviarConstructorTelegram = checkBoxConstructorTelegram.Checked;
        }

        private void checkBoxArchivosTelegram_CheckedChanged(object sender, EventArgs e)
        {
            _enviarArchivosTelegram = checkBoxArchivosTelegram.Checked;
        }

        private void checkBoxRapida_CheckedChanged(object sender, EventArgs e)
        {
            _soloResptxt = checkBoxRapida.Checked;
            ChkRes.Visible = _soloResptxt;
            ChkRes.BackColor = Color.DarkOrange;
        }

        /// <summary>
        /// [C4] �dem a comboBoxAgentes: flag booleano en lugar de contador.
        ///      El contador "cargas" nunca se reseteaba, causando que a partir
        ///      del 5.� evento nunca se ejecutara el cambio de modelo.
        /// </summary>
        private void comboBoxModeloIA_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (Entradas > 0) { Entradas--; return; }

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

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            pnlChat.Controls.Clear();
            _ultimaBurbujaComunicador = null;
            _ultimaFechaInsertada     = null;
            MostrarEmptyState();
        }

        /// <summary>
        /// Muestra / oculta las etiquetas de tiempo y consumo de tokens.
        /// Sirve como bot�n ? de estad�sticas en la barra inferior.
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
        //  L�GICA DE AGENTE Y RUTA
        // =====================================================================

        /// <summary>
        /// Selecciona un agente de trabajo y reinicia el contexto conversacional.
        /// Valida la API key antes de cargar la lista de modelos.
        /// </summary>
        private async Task SeleccionarAgente_Trabajo()
        {
            if (comboBoxAgentes.SelectedItem is not Modelo modeloSel) return;
            _modeloSeleccionado = modeloSel;

            await ComprobarApi(_modeloSeleccionado.ApiKey, _modeloSeleccionado.Agente);

            _modelosAgente = await ObtenerModeloAgente(
                _modeloSeleccionado.Agente, _modeloSeleccionado.ApiKey);

            comboBoxModeloIA.DataSource = null;
            comboBoxModeloIA.DataSource = _modelosAgente;
            comboBoxModeloIA.DisplayMember = "Nombre";
            comboBoxModeloIA.ValueMember = "Nombre";
            comboBoxModeloIA.Text = _modeloSeleccionado.Modelos;

            // Reiniciar contexto al cambiar de agente para no mezclar historiales.
            _ventana.Limpiar();
            MostrarMensaje("Contexto de conversaci�n reiniciado.", false);
        }

        /// <summary>
        /// Obtiene din�micamente los modelos disponibles para cada servicio de IA.
        /// [C2] Todas las llamadas son async y respetan un CancellationToken impl�cito
        ///      (el de la petici�n actual); si el usuario cancela no quedan tareas colgadas.
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
        /// Punto de entrada desde la UI. Recopila el texto, lo env�a al motor
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
        /// NOTA: m�todo s�ncrono dise�ado para ejecutarse en Task.Run.
        ///       NO acceder a controles de UI aqu�.
        /// </summary>
        private void CargarListaAgents()
        {
            _listaApisDisponibles     = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());
            _listaAgentes             = JsonManager.Leer<Modelo>(RutasProyecto.ObtenerRutaListModelos());
            _listaArchivosDisponibles = JsonManager.Leer<Archivo>(RutasProyecto.ObtenerRutaListArchivos());
            _audioService.RecargarConfig();
        }

        // ----------------------------------------------------------------------
        //  MOTOR IA � ORQUESTADOR PRINCIPAL
        // ----------------------------------------------------------------------

        /// <summary>
        /// Motor de doble pasada: Agente 1 genera, Agente 2 valida/mejora.
        ///
        /// [C1] Se crea un CancellationTokenSource con timeout global. El token
        ///      se pasa a ambos agentes; si el usuario pulsa Cancelar o vence el
        ///      timeout, ambas tareas se detienen.
        ///
        /// [C1] Se hace Dispose() del token anterior antes de crear uno nuevo
        ///      para evitar el memory leak que exist�a antes.
        ///
        /// [C2] EjecutarDifusionAsync lanza Telegram + Slack en Task.WhenAll,
        ///      reduciendo la latencia de salida cuando ambos est�n activos.
        /// </summary>
        private async Task<string> EjecutarMotorIAAsync(
            string instruccion,
            bool usarTelegram = false,
            bool usarSlack = false)
        {
            // [ARIA] Delegar al nuevo orquestador de 4 agentes.
            return await EjecutarConARIAAsync(instruccion, usarTelegram, usarSlack);
        }

        // ----------------------------------------------------------------------
        //  ORQUESTADOR ARIA � pipeline de 4 agentes con autocorrecci�n
        // ----------------------------------------------------------------------

        /// <summary>
        /// Pipeline ARIA: Analista ? Constructor ? Guardi�n ? Comunicador.
        /// � Cada fase actualiza las p�ldoras de estado en tiempo real.
        /// � El Guardi�n autocorrige errores hasta 3 veces sin molestar al usuario.
        /// � El Comunicador produce respuesta final en streaming con lenguaje amigable.
        /// </summary>
        private async Task<string> EjecutarConARIAAsync(
            string instruccion,
            bool usarTelegram = false,
            bool usarSlack = false)
        {
            _ctsIA?.Cancel();
            _ctsIA?.Dispose();
            _ctsIA = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSegundos));
            var ct = _ctsIA.Token;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            string instruccionOriginal = instruccion;
            instruccion = _ventana.CombinarConInstruccion(instruccion, _recordarTema, _soloChat);

            // -- Reset visual del panel de agentes ----------------------------
            if (InvokeRequired) BeginInvoke(_panelAgentes.Reset);
            else _panelAgentes.Reset();

            // -- Crear orquestador y conectar eventos -------------------------
            var aria = new OrquestadorARIA(
                _modeloSeleccionado.Modelos,
                _archivoSeleccionado.Ruta,
                _modeloSeleccionado.ApiKey,
                Utils.ObtenerNombresApis(_listaApisDisponibles),
                _soloChat,
                _modeloSeleccionado.Agente,
                maxReintentos: (int)_nudReintentos.Value);

            // Flag para indicar si la fase actual debe activar el indicador "escribiendo"
            // Solo se activa en Analista y Comunicador, NO en Constructor ni Guardi�n.
            bool[] typingEnabled = { false };

            aria.OnFaseIniciada += (fase, msg) =>
            {
                // Actualizar flag de typing seg�n la fase
                typingEnabled[0] = fase == FaseAgente.Analista || fase == FaseAgente.Comunicador;

                _panelAgentes.SetEstado(fase, EstadoAgente.Active);
                if (!string.IsNullOrWhiteSpace(msg))
                    MostrarBurbujaFase(fase, msg);

                // Reenviar el plan del Analista a Telegram / Slack en tiempo real.
                // Se filtra el mensaje gen�rico inicial; solo se env�a el plan real.
                if (fase == FaseAgente.Analista
                    && !string.IsNullOrWhiteSpace(msg)
                    && msg != "Analizando tu instrucci�n...")
                {
                    _ = EjecutarDifusionAsync($"🔍 {msg}", usarTelegram, usarSlack);
                }
            };

            aria.OnFaseCompletada += (fase2, _ok) =>
            {
                // Al completar Constructor o Guardi�n, asegurarse de que typing siga desactivado
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
                    _streaming.Finalizar(
                        _bufferScript.Length > 0 ? _bufferScript.ToString().TrimEnd() : null);
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

            // -- Snapshot del directorio de trabajo antes del pipeline --------
            // Se usa para detectar qu� archivos cre� el Constructor.
            var rutaTrabajo = _archivoSeleccionado?.Ruta ?? "";
            var snapshotAntes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if ((_enviarArchivosTelegram || _enviarArchivosSlack) &&
                !string.IsNullOrWhiteSpace(rutaTrabajo) &&
                System.IO.Directory.Exists(rutaTrabajo))
            {
                foreach (var f in System.IO.Directory.GetFiles(rutaTrabajo, "*", System.IO.SearchOption.TopDirectoryOnly))
                    snapshotAntes.Add(f);
            }

            // -- Reenv�o opcional del Constructor a Telegram y/o Slack --------
            aria.OnConstructorCompletado += salidaRaw =>
            {
                if (!string.IsNullOrWhiteSpace(salidaRaw))
                {
                    if (_enviarConstructorTelegram && usarTelegram)
                        _ = _telegramService.EnviarMensajeAsync(FormatearConstructorParaTelegram(salidaRaw));

                    if (_enviarConstructorSlack && usarSlack && _slackService.IsConfigured)
                        _ = _slackService.EnviarCodigoAsync(FormatearConstructorParaSlack(salidaRaw));
                }

                // -- Env�o de archivos nuevos creados por el Constructor ------
                if ((_enviarArchivosTelegram || _enviarArchivosSlack) &&
                    !string.IsNullOrWhiteSpace(rutaTrabajo) &&
                    System.IO.Directory.Exists(rutaTrabajo))
                {
                    var archivosNuevos = System.IO.Directory.GetFiles(
                            rutaTrabajo, "*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(f => !snapshotAntes.Contains(f))
                        .ToList();

                    foreach (var archivo in archivosNuevos)
                    {
                        string nombre = System.IO.Path.GetFileName(archivo);

                        if (_enviarArchivosTelegram && usarTelegram)
                            _ = _telegramService.EnviarArchivoAsync(archivo, $"📎 {nombre}");

                        if (_enviarArchivosSlack && usarSlack && _slackService.IsConfigured)
                            _ = _slackService.EnviarArchivoAsync(archivo, nombre);
                    }
                }
            };

            // -- Indicadores de "escribiendo / pensando" mientras el pipeline corre --

            // Telegram: enviar acci�n "typing" cada 4 s (solo durante Analista y Comunicador)
            using var ctsTyping = new CancellationTokenSource();
            if (usarTelegram && _telegramService.IsConfigured)
            {
                _ = Task.Run(async () =>
                {
                    while (!ctsTyping.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // Solo enviar el indicador si la fase actual lo requiere
                            if (typingEnabled[0])
                                await _telegramService.EnviarTypingAsync();

                            await Task.Delay(4_000, ctsTyping.Token);
                        }
                        catch { break; }
                    }
                }, ctsTyping.Token);
            }

            // Slack: mensaje "pensando..." que se elimina al finalizar
            string? slackTsPensando = null;
            if (usarSlack && _slackService.IsConfigured)
                slackTsPensando = await _slackService.EnviarPensandoAsync();

            // -- Ejecutar pipeline ---------------------------------------------
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
                if (slackTsPensando != null && _slackService.IsConfigured)
                    _ = _slackService.EliminarMensajeAsync(slackTsPensando);
            }

            // Flush final del Comunicador � pasar el buffer acumulado para garantizar
            // que la burbuja muestre el texto aunque el timer a�n no haya disparado.
            // Bug: pasar null dejaba la burbuja vac�a cuando el streaming terminaba
            // antes del primer tick de 120ms.
            string? textoFinalComunicador = _bufferScript.Length > 0
                ? _bufferScript.ToString().TrimEnd()
                : null;
            _streaming.Finalizar(textoFinalComunicador);

            // Mostrar el panel de quick actions debajo de la última respuesta
            MostrarQuickActions();

            // -- Registrar en ventana de contexto y difundir -------------------
            // Fase 2: si HAB_HISTORIAL_COMPRIMIDO est� activa, los turnos que
            // salgan de la ventana se resumen con el proveedor/modelo actual
            // en lugar de perderse. Fail-open: si el resumidor falla, sigue
            // el flujo sin romper nada.
            await _ventana.AgregarAsync(
                instruccionOriginal,
                respuestaFinal,
                (previo, expulsados, ctResumen) => HistorialResumidor.ResumirAsync(
                    previo,
                    expulsados,
                    _modeloSeleccionado.Agente,
                    _modeloSeleccionado.Modelos,
                    _modeloSeleccionado.ApiKey,
                    ctResumen),
                ct);

            if (!string.IsNullOrWhiteSpace(respuestaFinal))
                await EjecutarDifusionAsync(respuestaFinal, usarTelegram, usarSlack);

            return respuestaFinal;
        }

        // ---------------------------------------------------------------------
        //  HELPERS UI � BURBUJAS POR FASE ARIA
        // ---------------------------------------------------------------------

        /// <summary>
        /// Muestra o actualiza la burbuja de un agente espec�fico.
        /// Si ya existe una burbuja activa para esa fase, la actualiza en lugar de crear una nueva.
        /// Thread-safe � puede llamarse desde cualquier hilo.
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

            int anchoMax = Math.Max(80, (pnlChat.ClientSize.Width - 28));
            var burbuja = new BurbujaChat(
                ObtenerPrefijoFase(fase) + mensaje,
                false, anchoMax,
                Properties.Resources.iconos1)
            {
                Tag     = faseTag,
                Margin  = ObtenerMargenFase(fase)
            };

            _burbujaFaseActual = burbuja;
            OcultarEmptyState();
            InsertarSeparadorFechaSiCorresponde();
            pnlChat.Controls.Add(burbuja);
            ScrollSuaveAlFinal();
        }

        /// <summary>
        /// Inicia la burbuja de streaming para la ejecuci�n de script del Constructor o Guardi�n.
        /// Debe llamarse desde el hilo UI (via BeginInvoke).
        /// </summary>
        private void IniciarBurbujaScriptFase(FaseAgente fase)
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (pnlChat.ClientSize.Width - 28));

            string titulo = fase == FaseAgente.Guardian
                ? "🔧 Corrigiendo..."
                : "▶ Ejecutando...";

            _burbujaScriptActual = new BurbujaChat(
                titulo, false, anchoMax, Properties.Resources.iconos1)
            {
                Tag    = fase.ToString(),
                Margin = ObtenerMargenFase(fase)
            };

            pnlChat.Controls.Add(_burbujaScriptActual);
            ScrollSuaveAlFinal();

            _burbujaFaseActual = null; // Pr�ximo OnFaseIniciada de este agente crea nueva burbuja
            _streaming.Apuntar(_burbujaScriptActual);
        }

        /// <summary>
        /// A�ade un token del Comunicador a la burbuja de streaming.
        /// Crea la burbuja si a�n no existe. Thread-safe.
        /// </summary>
        private void AgregarTokenComunicador(string token)
        {
            // Si la burbuja de streaming no pertenece al Comunicador, crear una nueva
            var actual = _streaming.BurbujaActual;
            if (actual == null ||
                actual.IsDisposed ||
                actual.Tag?.ToString() != FaseAgente.Comunicador.ToString())
            {
                if (InvokeRequired)
                {
                    BeginInvoke(() => { IniciarBurbujaComunicador(); AgregarTokenComunicador(token); });
                    return;
                }
                IniciarBurbujaComunicador();
            }

            _bufferScript.Append(token);
            _streaming.AcumularTexto(_bufferScript.ToString());
        }

        /// <summary>
        /// Crea la burbuja de streaming del Comunicador (respuesta final).
        /// Llamar solo desde el hilo UI.
        /// </summary>
        private void IniciarBurbujaComunicador()
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (pnlChat.ClientSize.Width - 28));

            var burbuja = new BurbujaChat(
                "…", false, anchoMax, Properties.Resources.iconos1)
            {
                Tag    = FaseAgente.Comunicador.ToString(),
                Margin = ObtenerMargenFase(FaseAgente.Comunicador)
            };

            OcultarEmptyState();
            InsertarSeparadorFechaSiCorresponde();
            pnlChat.Controls.Add(burbuja);
            ScrollSuaveAlFinal();

            _streaming.Apuntar(burbuja);
            _ultimaBurbujaComunicador = burbuja;
        }

        // -- Utilidades de presentaci�n por fase -------------------------------

        private static string ObtenerPrefijoFase(FaseAgente fase) => fase switch
        {
            FaseAgente.Analista    => "🔍  ",          // U+1F50D  magnifying glass
            FaseAgente.Constructor => "⚙️  ",          // U+2699   gear
            FaseAgente.Guardian    => "🛡️  ",    // U+1F6E1  shield
            FaseAgente.Comunicador => "✨  ",                // U+2728   sparkles
            _ => ""
        };

        private static Padding ObtenerMargenFase(FaseAgente fase) => fase switch
        {
            // Margen unificado por fase. La burbuja se alinea internamente
            // (IA → izquierda, Usuario → derecha) — los márgenes anteriores
            // por fase chocaban con esa alineación interna y causaban que el
            // Comunicador apareciera muy a la derecha respecto del Constructor.
            // Las fases se distinguen por su emoji prefijo (🔍 ⚙️ 🛡️ ✨)
            // y los colores/elementos internos de la burbuja.
            FaseAgente.Analista    => new Padding(0, 4, 0, 2),
            FaseAgente.Constructor => new Padding(0, 2, 0, 2),
            FaseAgente.Guardian    => new Padding(0, 2, 0, 2),
            FaseAgente.Comunicador => new Padding(0, 4, 0, 6),
            _                      => new Padding(0, 3, 0, 3)
        };

        /// <summary>
        /// Ejecuta Agente 1 (generaci�n) con el token de cancelaci�n activo.
        /// [C2] Encapsula la llamada para hacer testeable el paso de generaci�n
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

        // ---------------------------------------------------------------------
        //  STREAMING DE SALIDA DE SCRIPT EN VIVO
        // ---------------------------------------------------------------------

        /// <summary>
        /// Crea la burbuja de "en vivo" al inicio de la ejecuci�n del script.
        /// Debe llamarse en el hilo de UI (ya garantizado v�a BeginInvoke).
        /// </summary>
        private void IniciarBurbujaScriptEnVivo()
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (pnlChat.ClientSize.Width - 28));
            _burbujaScriptActual = new BurbujaChat(
                "▶ Ejecutando script...", false, anchoMax, Properties.Resources.iconos1)
            {
                Tag = "10",
                Margin = new Padding(250, 5, 80, 5)
            };
            pnlChat.Controls.Add(_burbujaScriptActual);
            ScrollSuaveAlFinal();

            _streaming.Apuntar(_burbujaScriptActual);
        }

        /// <summary>
        /// Acumula una l�nea de salida para el siguiente tick del timer de throttle.
        /// Seguro para llamar desde cualquier hilo (incluso thread pool).
        /// </summary>
        private void AgregarLineaScriptEnVivo(string linea)
        {
            if (_burbujaScriptActual == null || _burbujaScriptActual.IsDisposed) return;
            _bufferScript.AppendLine(linea);
            _streaming.AcumularTexto(_bufferScript.ToString().TrimEnd());
        }

        /// <summary>
        /// Agente 2: respuesta de texto directo del LLM con streaming token a token.
        /// No genera ni ejecuta Python � es solo an�lisis y formateo de lenguaje natural.
        /// Esto lo hace ~3x m�s r�pido que la versi�n anterior (sin script execution).
        /// </summary>
        /// <summary>
        /// Agente 2: genera un script Python que corrige errores o informa el resultado.
        ///   - Si Agent 1 fall� ? el script corrige y ejecuta, escribe resultado en respuesta.txt
        ///   - Si Agent 1 tuvo �xito ? el script escribe resumen humano en respuesta.txt
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
            _streaming.Finalizar(_bufferScript.Length > 0
                ? _bufferScript.ToString().TrimEnd()
                : null);

            // El resultado definitivo viene de respuesta.txt (Agent 2 lo escribe ah�)
            return Utils.LimpiarRespuesta(ObtenerContextoChat(_archivoSeleccionado.Ruta));
        }

        /// <summary>
        /// Crea la burbuja de streaming para Agente 2 (verificador/corrector).
        /// Llamar solo desde el hilo UI (v�a BeginInvoke).
        /// </summary>
        private void IniciarBurbujaAgente2()
        {
            _bufferScript.Clear();
            int anchoMax = Math.Max(80, (pnlChat.ClientSize.Width - 28));
            _burbujaScriptActual = new BurbujaChat(
                "🛡 Agente 2 verificando...", false, anchoMax, Properties.Resources.iconos1)
            {
                Tag = "10",
                Margin = new Padding(250, 5, 80, 5)
            };
            pnlChat.Controls.Add(_burbujaScriptActual);
            ScrollSuaveAlFinal();
            _streaming.Apuntar(_burbujaScriptActual);
        }

        /// <summary>
        /// Env�a el resultado a Telegram y Slack en paralelo.
        /// [C2] Task.WhenAll elimina la espera secuencial: si Telegram tarda
        ///      500 ms y Slack 300 ms, el total es ~500 ms en lugar de 800 ms.
        /// </summary>
        private async Task EjecutarDifusionAsync(string mensaje, bool usarTelegram, bool usarSlack)
        {
            // Cuando el audio est� activo, se omite el texto � solo se env�a el audio
            if (!_enviarAudio)
            {
                var tareas = new List<Task>();

                if (usarTelegram)
                    tareas.Add(_telegramService.EnviarRespuestaParticionadaAsync(mensaje));

                if (usarSlack && _slackService.IsConfigured)
                    tareas.Add(_slackService.EnviarMensajeAsync(mensaje));

                if (tareas.Count > 0)
                    await Task.WhenAll(tareas);
            }

            // -- Audio TTS � generar una sola vez y enviar a todos los canales activos --
            if (_enviarAudio)
            {
                string? tmpFile = await _audioService.GenerarArchivoTemporalAsync(mensaje);

                if (tmpFile != null)
                {
                    var tareasAudio = new List<Task>();

                    if (usarTelegram && _telegramService.IsConfigured)
                        tareasAudio.Add(_telegramService.EnviarArchivoAsync(tmpFile, "🎤 Audio"));

                    if (usarSlack && _slackService.IsConfigured)
                        tareasAudio.Add(_slackService.EnviarArchivoAsync(tmpFile, "Audio"));

                    if (tareasAudio.Count > 0)
                        await Task.WhenAll(tareasAudio);

                    try { File.Delete(tmpFile); } catch { }
                }
            }
        }

        /// <summary>
        /// Formatea la salida t�cnica cruda del Constructor para que se vea legible en Telegram.
        /// � Si el contenido es JSON v�lido ? lo indenta y lo envuelve en bloque de c�digo.
        /// � Cualquier otro texto ? bloque &lt;pre&gt; monoespacio.
        /// � Trunca a 3 800 chars para respetar el l�mite de 4 096 de Telegram.
        /// </summary>
        private static string FormatearConstructorParaTelegram(string rawOutput)
        {
            if (string.IsNullOrWhiteSpace(rawOutput))
                return "⚙️ <b>Constructor</b>\n<i>(sin salida)</i>";

            string contenido = rawOutput.Trim();

            // -- Intentar pretty-print si parece JSON -------------------------
            bool esJson = contenido.StartsWith("{") || contenido.StartsWith("[");
            if (esJson)
            {
                try
                {
                    var token = Newtonsoft.Json.Linq.JToken.Parse(contenido);
                    contenido = token.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                catch { /* dejar como est� si no es JSON v�lido */ }
            }

            // -- Escapar caracteres especiales de HTML ------------------------
            contenido = contenido
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            // -- Truncar si supera el l�mite (reservar ~300 chars para cabecera) -
            const int MaxChars = 3_750;
            if (contenido.Length > MaxChars)
                contenido = contenido[..MaxChars] + "\n<i>� (truncado)</i>";

            // -- Construir mensaje con bloque de c�digo monoespacio -----------
            // <pre><code> en Telegram HTML = bloque con fondo gris, fuente mono
            return $"⚙️ <b>Constructor — resultado técnico</b>\n\n<pre><code>{contenido}</code></pre>";
        }

        /// <summary>
        /// Formatea la salida t�cnica del Constructor para Slack.
        /// � JSON v�lido ? pretty-print dentro de bloque de c�digo (triple backtick).
        /// � Truncado a 3 800 chars (l�mite pr�ctico de Slack por mensaje).
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
                contenido = contenido[..MaxChars] + "\n� (truncado)";

            // Slack usa triple backtick para bloques de c�digo monoespaciado
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
        /// Extra�do de EjecutarMotorIAAsync para mantener el m�todo de
        /// orquestaci�n limpio y facilitar pruebas unitarias aisladas.
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

[SALIDA_AGENTE_1 � contenido de respuesta.txt]
{respuestaAgente1}

[CODIGO_EJECUTADO_POR_AGENTE_1]
{codigo}

ERES EL AGENTE VERIFICADOR. Tu �nica tarea es escribir en respuesta.txt el resultado final.

REGLA 1 � SI HAY ERROR o la salida est� vac�a o no cumple la instrucci�n original:
  � Genera un script Python que corrija el problema y lo ejecute.
  � Escribe SOLO el resultado corregido en respuesta.txt.
  � Sin explicaciones, sin comentarios, solo la salida.

REGLA 2 � SI TODO SALI� BIEN:
  � Genera un script Python que escriba en respuesta.txt un resumen claro y humano
    de lo que se hizo y el resultado obtenido.
  � Lenguaje natural, sin tecnicismos, puedes usar emojis con moderaci�n.
  � Sin repetir el c�digo, solo el resultado.

SIEMPRE: tu script debe escribir en respuesta.txt. Nada m�s.
            ";
        }

        /// <summary>
        /// Lee respuesta.txt si el modo SoloChat est� activo y devuelve
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
        /// Valida que haya instrucci�n, modelo y archivo antes de llamar al motor.
        /// [C4] Ahora devuelve (bool ok, string error) en lugar de lanzar
        ///      Exception directamente desde un m�todo auxiliar; quien llama
        ///      decide si interrumpir o solo notificar al usuario.
        /// </summary>
        private (bool ok, string error) ValidarCondiciones(string instruccion)
        {
            if (string.IsNullOrWhiteSpace(instruccion))
                return (false, "La instrucci�n no puede estar vac�a.");
            if (_modeloSeleccionado == null)
                return (false, "No hay modelo seleccionado.");
            if (_archivoSeleccionado == null)
                return (false, "No hay archivo seleccionado.");
            return (true, "");
        }

        // =====================================================================
        //  UI � MENSAJES Y CONTROLES
        // =====================================================================

        /// <summary>
        /// Agrega una burbuja de chat al panel de conversaci�n.
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

            int anchoMax = (pnlChat.ClientSize.Width - 28);

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
            ScrollSuaveAlFinal();
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
        //  API � VALIDACI�N
        // =====================================================================

        /// <summary>
        /// Comprueba que la API key del agente seleccionado sea v�lida.
        /// Notifica al usuario si la validaci�n falla.
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
                // Antigravity: AntigravityEstaActivoAsync ya auto-detecta project si apikey es inv�lido
                Servicios.Antigravity => await AIServicios.AntigravityEstaActivoAsync(apikey),
                _                     => false
            };

            if (!valida)
            {
                string msg = agente == Servicios.Antigravity
                    ? "Antigravity no est� disponible.\n\n" +
                      "Verifica:\n" +
                      "� Ejecuta 'gcloud auth application-default login'\n" +
                      "� Ejecuta 'gcloud config set project TU-PROYECTO'\n" +
                      "� Que la API Vertex AI est� habilitada en el proyecto\n" +
                      "� Guarda la config en Proveedores ? Antigravity"
                    : "Tu Api no es correcta o tiene alg�n error, comprueba por favor.";

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
        /// Inicia o reinicia el listener de Telegram con la configuraci�n actual.
        /// [C1] El listener previo se detiene expl�citamente antes de crear uno nuevo
        ///      para evitar tener dos goroutines escuchando el mismo bot token.
        /// </summary>
        private Task IniciarConversasionTelegram()
        {
            _telegramService.CargarConfiguracion(_archivoSeleccionado?.Ruta ?? "");

            if (!_telegramService.IsConfigured)
            {
                MostrarMensaje(
                    "Telegram a�n no est� configurado.\r\n" +
                    "Proporciona tu API Key y tu ID de usuario y escribe #CONFIGURA TELEGRAM",
                    false);
                return Task.CompletedTask;
            }

            // Resuscribir eventos al reiniciar el listener evita acumulaci�n
            // de handlers si el usuario activa/desactiva Telegram varias veces.
            _telegramService.OnMessageReceived  -= TelegramService_OnMessageReceived;
            _telegramService.OnCallbackReceived -= TelegramService_OnCallbackReceived;
            _telegramService.OnFileReceived     -= TelegramService_OnFileReceived;
            _telegramService.OnError            -= TelegramService_OnError;

            _telegramService.OnMessageReceived  += TelegramService_OnMessageReceived;
            _telegramService.OnCallbackReceived += TelegramService_OnCallbackReceived;
            _telegramService.OnFileReceived     += TelegramService_OnFileReceived;
            _telegramService.OnError            += TelegramService_OnError;

            _telegramService.Iniciar(_archivoSeleccionado?.Ruta ?? "");
            return Task.CompletedTask;
        }

        private void TelegramService_OnMessageReceived(long chatId, string texto)
        {
            if (!IsHandleCreated) return;
            BeginInvoke(async () => await ProcesarMensajeTelegramAsync(chatId, texto));
        }

        private void TelegramService_OnCallbackReceived(long chatId, string data)
        {
            if (!IsHandleCreated) return;
            BeginInvoke(async () =>
            {
                await _telegramService.EnviarMensajeAsync(chatId,
                    "Tu mensaje ha sido recibido y se est� procesando... Por favor espera.",
                    TelegramSender.CancelarConfig());
                await ProcesarMensajeTelegramAsync(chatId, data);
            });
        }

        private void TelegramService_OnFileReceived(long chatId, string fileId, string fileName, long fileSize, string fileType)
        {
            if (!IsHandleCreated) return;
            BeginInvoke(() => MostrarMensaje($"Archivo recibido: {fileName}", false));
        }

        private void TelegramService_OnError(string mensaje)
        {
            if (!IsHandleCreated) { MostrarMensaje(mensaje, false); return; }
            BeginInvoke(() => MostrarMensaje(mensaje, false));
        }

        /// <summary>
        /// Procesa un mensaje entrante de Telegram con exclusi�n mutua (sem�foro)
        /// para evitar procesamiento concurrente de dos mensajes del mismo usuario.
        /// </summary>
        private async Task ProcesarMensajeTelegramAsync(long chatId, string texto)
        {
            await _telegramService.Semaphore.WaitAsync();
            try
            {
                MostrarMensaje(texto, true);

                bool esComando = texto.TrimStart().StartsWith('#');

                if (!esComando)
                {
                    await _telegramService.EnviarMensajeAsync(chatId,
                        "Tu mensaje ha sido recibido y se est� procesando... Por favor espera.",
                        TelegramSender.CancelarConfig());
                }

                // Pasamos el texto ORIGINAL (preserva el case de los args).
                await EjecutarComandoOConsultaAsync(texto, true, false);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error procesando mensaje de Telegram: {ex.Message}", false);
                await _telegramService.EnviarMensajeAsync(chatId,
                    "Ocurri� un error procesando tu mensaje. Intenta de nuevo.");
            }
            finally
            {
                _telegramService.Semaphore.Release();
            }
        }

        // Helpers de teclados para Telegram
        private object CambiarAgente_Telegram() => TelegramSender.CrearKeyboardDesdeListaAgentes(_listaAgentes);
        private object CambiarModelo_Telegram() => TelegramSender.CrearKeyboardDesdeListaModelos(_modelosAgente);
        private object CambiarRuta_Telegram() => TelegramSender.CrearKeyboardDesdeListaRutas(_listaArchivosDisponibles);

        /// <summary>
        /// Enrutador de comandos Telegram / Slack. Usa el nuevo CommandExecutor:
        /// si el texto no es un comando (no empieza con `#`) delega al motor IA
        /// como fallback. Preserva el caso original de los argumentos � cr�tico
        /// para `#ruta`, `#modelo`, voces de TTS, etc.
        /// </summary>
        private async Task EjecutarComandoOConsultaAsync(
            string textoOriginal,
            bool usarTelegram = false, bool usarSlack = false)
        {
            var resultado = await _cmdExecutor.DespacharAsync(
                textoOriginal,
                _telegramService.Chat.ChatId,
                usarTelegram, usarSlack);

            // Si no era un comando `#algo`, cae al motor IA (s�lo Telegram/UI;
            // Slack tiene su propio path en Realizar_Peticiones_Slack).
            if (!resultado.EsComando && !usarSlack && !string.IsNullOrWhiteSpace(textoOriginal))
            {
                textBoxInstrucion.Text = textoOriginal;
                try
                {
                    await EjecutarMotorIAAsync(textoOriginal, usarTelegram);
                }
                finally
                {
                    textBoxInstrucion.Text = "";
                }
            }
        }

        /// <summary>
        /// Registra todos los comandos en el CommandRegistry y crea el executor.
        /// Un comando = una clase (ver Comandos/Handlers/).
        /// </summary>
        private void ConfigurarCommandRouter()
        {
            // Handlers de configuraci�n / estado
            _cmdRegistry.Registrar(new Comandos.Handlers.CancelarCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.EstadoCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.AyudaCommand(_cmdRegistry));

            // Handlers de agente / modelo / ruta
            _cmdRegistry.Registrar(new Comandos.Handlers.AgenteCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.ModeloCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.RutaCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.CambiarAgenteCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.CambiarModeloCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.CambiarRutaCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.ApisCommand());

            // Handlers de configuraci�n
            _cmdRegistry.Registrar(new Comandos.Handlers.ConfiguracionesCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.RecordarCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.SoloChatCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.ReintentosCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.TimeoutCommand());

            // Handlers de integraci�n
            _cmdRegistry.Registrar(new Comandos.Handlers.TelegramCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.ActivaTelegramCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.DesactivaTelegramCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.SlackCommand());
            _cmdRegistry.Registrar(new Comandos.Handlers.AudioCommand());

            // Handlers de habilidades
            _cmdRegistry.Registrar(new Comandos.Handlers.HabilidadCommand());

            // `this` implementa IServiciosComandos (ver FrmMandos.Comandos.cs).
            _cmdExecutor = new Comandos.CommandExecutor(_cmdRegistry, this);
        }

        #endregion

        // =====================================================================
        //  SLACK
        // =====================================================================

        #region Manejo de Slack

        /// <summary>
        /// Procesa una petici�n recibida desde Slack: si es comando la enruta,
        /// si es texto libre la env�a al motor IA.
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

            if (instruccion.TrimStart().StartsWith('#'))
                await EjecutarComandoOConsultaAsync(instruccion, false, true);
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
        /// [C1] El handler usa BeginInvoke en lugar de Invoke para no bloquear
        ///      el hilo de polling de Slack.
        /// </summary>
        private Task IniciarSlack()
        {
            _slackService.CargarConfiguracion(_archivoSeleccionado?.Ruta ?? "");

            if (!_slackService.IsConfigured) return Task.CompletedTask;

            // Resuscribir eventos al reiniciar evita acumulaci�n de handlers.
            _slackService.OnMessageReceived -= SlackService_OnMessageReceived;
            _slackService.OnAviso           -= SlackService_OnAviso;

            _slackService.OnMessageReceived += SlackService_OnMessageReceived;
            _slackService.OnAviso           += SlackService_OnAviso;

            _slackService.Iniciar();
            return Task.CompletedTask;
        }

        private async void SlackService_OnMessageReceived(SlackMessage msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Text)) return;
            if (!string.IsNullOrEmpty(msg.BotId)) return;

            // Refrescar config para tomar usuarios autorizados actualizados.
            _slackService.CargarConfiguracion(_archivoSeleccionado?.Ruta ?? "");

            if (!_slackService.Chat.usuarios.Contains(msg.User))
            {
                await _slackService.EnviarMensajeAsync(
                    "Tu usuario no est� configurado para realizar instrucciones, " +
                    "pide al administrador que te agregue.");
                return;
            }

            if (_procesandoSlack) return;

            _procesandoSlack = true;
            try
            {
                string instruccion = msg.Text;
                if (IsHandleCreated)
                {
                    BeginInvoke(() =>
                    {
                        textBoxInstrucion.Text = instruccion;
                        MostrarMensaje("RECIBIDO: " + instruccion, false);
                    });
                }

                await _slackService.EnviarMensajeAsync("Su instrucci�n se recibi�, espere respuesta...");
                await Realizar_Peticiones_Slack(instruccion);
            }
            catch
            {
                await _slackService.EnviarMensajeAsync("Error procesando la instrucci�n.");
            }
            finally
            {
                _procesandoSlack = false;
            }
        }

        private void SlackService_OnAviso(string mensaje)
        {
            if (!IsHandleCreated) { MostrarMensaje(mensaje, false); return; }
            BeginInvoke(() => MostrarMensaje(mensaje, false));
        }

        #endregion

        // =====================================================================
        //  CONFIGURACI�N, TEMAS Y UTILIDADES
        // =====================================================================

        private void GuardarConfiguracion()
        {
            _configuracionClient.Mimodelo ??= new Modelo();
            _configuracionClient.MiArchivo ??= new Archivo();
            _configuracionClient.Miapi ??= new Api();
            _configuracionClient.Preferencias ??= new PreferenciasMandos();

            _configuracionClient.Mimodelo = comboBoxAgentes.SelectedItem as Modelo ?? new Modelo();
            _configuracionClient.MiArchivo = comboBoxRuta.SelectedItem as Archivo ?? new Archivo();
            _configuracionClient.Miapi.Nombre = comboBoxModeloIA.Text;
            _configuracionClient.Miapi.key = _configuracionClient.Mimodelo.ApiKey;

            _configuracionClient.Preferencias.RecordarTema              = checkBoxRecordar.Checked;
            _configuracionClient.Preferencias.SoloChat                  = checkBoxSoloChat.Checked;
            _configuracionClient.Preferencias.SoloRespuestaRapida       = checkBoxRapida.Checked;
            _configuracionClient.Preferencias.TelegramActivo            = checkBoxTelegram.Checked;
            _configuracionClient.Preferencias.EnviarConstructorTelegram = checkBoxConstructorTelegram.Checked;
            _configuracionClient.Preferencias.EnviarArchivosTelegram    = checkBoxArchivosTelegram.Checked;
            _configuracionClient.Preferencias.SlackActivo               = checkBoxSlack.Checked;
            _configuracionClient.Preferencias.EnviarConstructorSlack    = checkBoxConstructorSlack.Checked;
            _configuracionClient.Preferencias.EnviarArchivosSlack       = checkBoxArchivosSlack.Checked;
            _configuracionClient.Preferencias.EnviarAudio               = checkBoxAudio.Checked;

            Utils.GuardarConfig<ConfiguracionClient>(
                RutasProyecto.ObtenerRutaConfiguracion(), _configuracionClient);

            // Si el usuario cambió la ruta de trabajo desde el comboBox, redirigir
            // todos los archivos del workspace a la nueva carpeta y migrar los
            // que existieran en AppDir (no-destructivo).
            RutasProyecto.EstablecerRutaTrabajo(_configuracionClient.MiArchivo?.Ruta);

            MostrarMensaje("�Configuraci�n guardada!", false);
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
        /// Cancela la petici�n activa al modelo de IA.
        /// [C1] Solo cancela el token; Dispose se hace en la pr�xima llamada
        ///      o en FormClosing para evitar doble-dispose.
        /// </summary>
        private void CancelarInstruccion()
        {
            if (_ctsIA == null || _ctsIA.IsCancellationRequested)
                return;

            _ctsIA.Cancel();

            // Feedback visual inmediato en el bot�n
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
            btnCancelar.Text      = "? Cancelando...";
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

            if(_configuracionClient.MiArchivo != null)
                comboBoxRuta.SelectedValue = _configuracionClient.MiArchivo.Ruta;

            RestaurarPreferencias();
            AplicarTemaEmerald();
            //ConstruirTokenCounter();
            ConstruirControlesZoom();
            ConstruirPanelCredenciales();
            ConstruirToggleScroll();
            MostrarEmptyState();

            // Suscribir cambio de tema
            EmeraldTheme.ThemeChanged += OnTemaChanged;
            Disposed += (_, __) => EmeraldTheme.ThemeChanged -= OnTemaChanged;
        }

        private void OnTemaChanged()
        {
            if (InvokeRequired) { Invoke(OnTemaChanged); return; }
            AplicarTemaEmerald();
            // Actualizar colores del textbox (hardcoded en ConfigurarUI)
            textBoxInstrucion.BackColor = EmeraldTheme.BgDeep;
            textBoxInstrucion.ForeColor = EmeraldTheme.TextPrimary;
            // Forzar repintado de burbujas
            foreach (Control c in pnlChat.Controls)
                c.Invalidate();
        }

        private void RestaurarPreferencias()
        {
            var pref = _configuracionClient.Preferencias;
            if (pref == null) return;

            checkBoxRecordar.Checked            = pref.RecordarTema;
            checkBoxSoloChat.Checked            = pref.SoloChat;
            checkBoxRapida.Checked              = pref.SoloRespuestaRapida;
            checkBoxTelegram.Checked            = pref.TelegramActivo;
            checkBoxConstructorTelegram.Checked = pref.EnviarConstructorTelegram;
            checkBoxArchivosTelegram.Checked    = pref.EnviarArchivosTelegram;
            checkBoxSlack.Checked               = pref.SlackActivo;
            checkBoxConstructorSlack.Checked    = pref.EnviarConstructorSlack;
            checkBoxArchivosSlack.Checked       = pref.EnviarArchivosSlack;
            checkBoxAudio.Checked               = pref.EnviarAudio;
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

                comboBoxAgentes.DataSource = _listaAgentes;
                comboBoxAgentes.DisplayMember = "Agente";
                comboBoxAgentes.ValueMember = "ApiKey";

                textBoxInstrucion.Multiline = true;
                textBoxInstrucion.AllowDrop = true;
                textBoxInstrucion.AcceptsTab = true;

                // -- Barra de estado de agentes ARIA -----------------------------
                // Se inserta entre panelHead (DockStyle.Top) y pnlChat (Fill).
                // SetChildIndex con el �ndice de panelHead empuja panelHead al siguiente
                // z-order, de modo que panelHead se acople primero (arriba del todo)
                // y _panelAgentes se acople debajo sin tocar el Designer.
                _panelAgentes = new PanelAgentes
                {
                    Dock = DockStyle.Top,
                    Height = 30
                };
                Controls.Add(_panelAgentes);
                Controls.SetChildIndex(_panelAgentes, Controls.IndexOf(panelHead));

                // ── Estilo plano y minimalista (sin redondeos, sin wrappers) ──
                //  Eficiencia primero: cero OnPaint custom, cero reparenting.
                //  El textbox vive directamente en pnlContenedorTxt como
                //  Designer lo creó. Aplicamos colores del tema y borde fino.
                textBoxInstrucion.BorderStyle = BorderStyle.FixedSingle;
                textBoxInstrucion.BackColor   = ColorTranslator.FromHtml("#002647"); // BgInput
                textBoxInstrucion.ForeColor   = ColorTranslator.FromHtml("#FFFFFF"); // TextMain

                labelSugerencia.Parent    = textBoxInstrucion;
                labelSugerencia.BackColor = Color.Transparent;
                labelSugerencia.AutoSize  = true;
                labelSugerencia.Enabled   = false;
                labelSugerencia.Font      = textBoxInstrucion.Font;

                // Llevar los botones al frente (z-order) y alinearlos al wrapper
                btnEnviar.BringToFront();
                btnCancelar.BringToFront();

                ConfigurarPanelArchivos();

                _toolTipArchivos.AutoPopDelay = 5000;
                _toolTipArchivos.InitialDelay = 500;
                _toolTipArchivos.ReshowDelay = 200;
                _toolTipArchivos.ShowAlways = true;

                // -- Barra inferior: label "?? Notificar en:" -----------------
                var lblNotif = new Label
                {
                    Text      = "?? Notif.:",
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
                    Location  = new Point(400, 112),
                    Size      = new Size(1, 20),
                    BackColor = Color.FromArgb(50, 65, 90)
                };
                pnlContenedorTxt.Controls.Add(pnlSepNotif);
                pnlSepNotif.BringToFront();

                // Asegurar z-order correcto para toda la barra inferior
                checkBoxTelegram.BringToFront();
                checkBoxConstructorTelegram.BringToFront();
                checkBoxArchivosTelegram.BringToFront();
                checkBoxSlack.BringToFront();
                checkBoxConstructorSlack.BringToFront();
                checkBoxArchivosSlack.BringToFront();
                checkBoxAudio.BringToFront();
                btnLimpiar.BringToFront();
                btnInfo.BringToFront();

                // -- Control de reintentos del Guardi�n (barra inferior) ----
                var lblReintentos = new Label
                {
                    Text      = "Reintentos:",
                    AutoSize  = true,
                    ForeColor = Color.FromArgb(140, 165, 205),
                    BackColor = Color.Transparent,
                    Font      = new Font("Segoe UI", 7.5f),
                    Location  = new Point(412, 115)
                };

                _nudReintentos = new NumericUpDown
                {
                    Minimum            = 0,
                    Maximum            = 5,
                    Value              = 3,
                    Width              = 42,
                    Height             = 22,
                    Location           = new Point(480, 111),
                    BackColor          = Color.FromArgb(30, 41, 59),
                    ForeColor          = Color.White,
                    Font               = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BorderStyle        = BorderStyle.FixedSingle,
                    TextAlign          = HorizontalAlignment.Center,
                    InterceptArrowKeys = true
                };

                _toolTipArchivos.SetToolTip(_nudReintentos,
                    "Correcciones autom�ticas del Guardi�n (0 = sin reintentos)");
                _toolTipArchivos.SetToolTip(lblReintentos,
                    "Correcciones autom�ticas del Guardi�n");

                pnlContenedorTxt.Controls.Add(lblReintentos);
                pnlContenedorTxt.Controls.Add(_nudReintentos);
                lblReintentos.BringToFront();
                _nudReintentos.BringToFront();

                // -- Sub-checkboxes del Constructor: deshabilitados hasta activar el canal --
                checkBoxConstructorTelegram.Enabled = false;
                checkBoxConstructorSlack.Enabled    = false;
                checkBoxArchivosTelegram.Enabled    = false;
                checkBoxArchivosSlack.Enabled       = false;
                // checkBoxAudio siempre habilitado � es independiente

                // -- Labels de estad�sticas ocultas hasta presionar ? ------
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
        /// Carga el modelo guardado en la configuraci�n y actualiza los combos.
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
            comboBoxModeloIA.DisplayMember = "Nombre";
            comboBoxModeloIA.ValueMember = "Nombre";
            comboBoxModeloIA.DataSource = null;
            comboBoxModeloIA.DataSource = modelos;

            _modeloSeleccionado.Agente = _configuracionClient.Mimodelo.Agente;
            _modeloSeleccionado.Modelos = _configuracionClient.Mimodelo.Modelos;
            _modeloSeleccionado.ApiKey = _configuracionClient.Miapi.key;

            comboBoxModeloIA.SelectedValue = _modeloSeleccionado.Modelos;
        }

        // -- Paleta dinámica (sigue a EmeraldTheme.IsDark) --------------------
        private static Color _emBgDeep   => EmeraldTheme.BgDeep;
        private static Color _emBgCard   => EmeraldTheme.BgCard;
        private static Color _emBgInput  => EmeraldTheme.BgDeep;
        private static Color _emEmerald  => EmeraldTheme.Emerald500;
        private static Color _emEmerald4 => EmeraldTheme.Emerald400;
        private static Color _emEmerald9 => EmeraldTheme.Emerald900;
        private static Color _emTextMain => EmeraldTheme.TextPrimary;
        private static Color _emTextSub  => EmeraldTheme.TextSecondary;

        /// <summary>
        /// Aplica la paleta Emerald a todo el �rbol de controles del chat.
        /// No toca el panel de chat (pnlChat) ni las burbujas para no romper el layout
        /// del scroll ni los avatares; solo recolorea fondo, header, controles de
        /// entrada y botones secundarios.
        /// </summary>
        private void AplicarTemaEmerald()
        {
            BackColor = _emBgDeep;
            ForeColor = _emTextMain;

            RecolorearArbolEmerald(this);

            // Acentos espec�ficos en botones de acci�n
            EstilizarBotonEmerald(btnEnviar,   primario: true);
            EstilizarBotonEmerald(btnCancelar, primario: false, danger: true);
        }

        private void RecolorearArbolEmerald(Control raiz)
        {
            foreach (Control c in raiz.Controls)
            {
                // No tocamos las burbujas ni el panel de scroll del chat
                if (c is BurbujaChat) continue;
                if (c == pnlChat) { pnlChat.BackColor = _emBgDeep; continue; }

                switch (c)
                {
                    case Button btn:
                        EstilizarBotonEmerald(btn);
                        break;

                    case CheckBox chk:
                        chk.ForeColor = _emTextSub;
                        chk.BackColor = Color.Transparent;
                        chk.FlatStyle = FlatStyle.Flat;
                        chk.Cursor    = Cursors.Hand;
                        break;

                    case Label lbl:
                        if (EsColorSemanticoEmerald(lbl.ForeColor)) break;
                        lbl.BackColor = Color.Transparent;
                        lbl.ForeColor = (lbl.Font?.Bold ?? false) ? _emTextMain : _emTextSub;
                        break;

                    case TextBox tb:
                        tb.BackColor   = _emBgInput;
                        tb.ForeColor   = _emTextMain;
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case RichTextBox rtb:
                        rtb.BackColor   = _emBgInput;
                        rtb.ForeColor   = _emTextMain;
                        break;

                    case ComboBox cmb:
                        cmb.BackColor = _emBgInput;
                        cmb.ForeColor = _emTextMain;
                        cmb.FlatStyle = FlatStyle.Flat;
                        break;

                    case Panel pnl:
                        // Header / contenedor de input / archivos: card oscura
                        if (pnl == panelHead || pnl == pnlContenedorTxt || pnl == pnlContenedorArchivos)
                            pnl.BackColor = _emBgCard;
                        else
                            pnl.BackColor = _emBgDeep;
                        break;
                }

                if (c.HasChildren) RecolorearArbolEmerald(c);
            }
        }

        private void EstilizarBotonEmerald(Button btn, bool primario = false, bool danger = false)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize  = 1;
            btn.ForeColor = _emTextMain;
            btn.Cursor    = Cursors.Hand;
            btn.Font      = new Font("Segoe UI Semibold", 9F);

            if (danger)
            {
                var rojo  = ColorTranslator.FromHtml("#dc2626");
                var rojo9 = ColorTranslator.FromHtml("#7f1d1d");
                btn.BackColor = rojo9;
                btn.FlatAppearance.BorderColor        = rojo;
                btn.FlatAppearance.MouseOverBackColor = rojo;
                btn.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#991b1b");
            }
            else if (primario)
            {
                btn.BackColor = _emEmerald;
                btn.FlatAppearance.BorderColor        = _emEmerald4;
                btn.FlatAppearance.MouseOverBackColor = _emEmerald4;
                btn.FlatAppearance.MouseDownBackColor = _emEmerald9;
            }
            else
            {
                btn.BackColor = _emEmerald9;
                btn.FlatAppearance.BorderColor        = _emEmerald;
                btn.FlatAppearance.MouseOverBackColor = _emEmerald;
                btn.FlatAppearance.MouseDownBackColor = _emEmerald4;
            }
        }

        private static bool EsColorSemanticoEmerald(Color c)
        {
            return (c.R == 34  && c.G == 197 && c.B == 94)
                || (c.R == 245 && c.G == 158 && c.B == 11)
                || (c.R == 239 && c.G == 68  && c.B == 68)
                || (c.R == 100 && c.G == 116 && c.B == 139);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MEJORAS DE UX: empty state + token counter + quick actions
        // ═══════════════════════════════════════════════════════════════════

        private Panel _emptyStatePanel;
        private Label _lblTokenCount;
        private BurbujaChat _ultimaBurbujaComunicador;

        // ── EMPTY STATE ──────────────────────────────────────────────
        private void MostrarEmptyState()
        {
            if (pnlChat == null) return;
            if (_emptyStatePanel != null && !_emptyStatePanel.IsDisposed)
            {
                _emptyStatePanel.Visible = true;
                if (!pnlChat.Controls.Contains(_emptyStatePanel))
                    pnlChat.Controls.Add(_emptyStatePanel);
                return;
            }

            _emptyStatePanel = new Panel
            {
                BackColor = Color.Transparent,
                Width     = Math.Max(400, pnlChat.ClientSize.Width - 40),
                Height    = 220,
                Margin    = new Padding(0, 60, 0, 0)
            };

            var lblTitulo = new Label
            {
                Text      = "  ¿En qué te ayudo hoy?",
                Font      = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = _emTextMain,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 20)
            };

            var lblSub = new Label
            {
                Text      = "Probá con una de estas o escribí tu propia instrucción ↓",
                Font      = new Font("Segoe UI", 9.5F),
                ForeColor = _emTextSub,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(22, 56)
            };

            _emptyStatePanel.Controls.Add(lblTitulo);
            _emptyStatePanel.Controls.Add(lblSub);

            // Chips de sugerencia
            (string icono, string texto)[] chips =
            {
                ("\U0001F4DD", "Crear un archivo notas.md"),
                ("\U0001F50D", "Buscar archivos en mi carpeta"),
                ("\U0001F4E4", "Enviarme un resumen por Telegram"),
                ("⚡",     "Ejecutar una skill"),
            };

            int x = 22, y = 96;
            int gap = 10, anchoChip = 240, altoChip = 38;
            int colsPorFila = 2;
            int idxChip = 0;

            foreach (var (icono, texto) in chips)
            {
                int col = idxChip % colsPorFila;
                int row = idxChip / colsPorFila;

                var chip = new Label
                {
                    Text       = $"{icono}   {texto}",
                    Font       = new Font("Segoe UI", 9.5F),
                    ForeColor  = _emTextMain,
                    BackColor  = _emBgCard,
                    TextAlign  = ContentAlignment.MiddleLeft,
                    Padding    = new Padding(12, 0, 12, 0),
                    Cursor     = Cursors.Hand,
                    Width      = anchoChip,
                    Height     = altoChip,
                    Location   = new Point(x + col * (anchoChip + gap), y + row * (altoChip + gap)),
                    Tag        = texto
                };
                chip.MouseEnter += (_, __) => chip.BackColor = _emEmerald9;
                chip.MouseLeave += (_, __) => chip.BackColor = _emBgCard;
                chip.Click      += (s, __) =>
                {
                    if (s is Label l && l.Tag is string sugerencia)
                    {
                        textBoxInstrucion.Text = sugerencia;
                        textBoxInstrucion.SelectionStart = sugerencia.Length;
                        textBoxInstrucion.Focus();
                    }
                };

                _emptyStatePanel.Controls.Add(chip);
                idxChip++;
            }

            pnlChat.Controls.Add(_emptyStatePanel);
        }

        private void OcultarEmptyState()
        {
            if (_emptyStatePanel != null && !_emptyStatePanel.IsDisposed)
            {
                _emptyStatePanel.Visible = false;
                pnlChat.Controls.Remove(_emptyStatePanel);
            }
        }

        // ── TOKEN COUNTER ────────────────────────────────────────────
        private void ConstruirTokenCounter()
        {
            if (_lblTokenCount != null) return;
            if (pnlContenedorTxt == null || textBoxInstrucion == null) return;

            _lblTokenCount = new Label
            {
                AutoSize  = true,
                Text      = "0 tokens",
                Font      = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold),
                ForeColor = _emTextSub,
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlContenedorTxt.Controls.Add(_lblTokenCount);

            ReposicionarTokenCounter();
            pnlContenedorTxt.Resize       += (_, __) => ReposicionarTokenCounter();
            pnlContenedorTxt.ClientSizeChanged += (_, __) => ReposicionarTokenCounter();

            textBoxInstrucion.TextChanged += (_, __) =>
            {
                ActualizarTokenCounter();
                ReposicionarTokenCounter();
            };
            ActualizarTokenCounter();
        }

        private void ReposicionarTokenCounter()
        {
            if (_lblTokenCount == null || pnlContenedorTxt == null) return;
            // Zona libre del panel: pegado a btnLimpiar (725, 108) por encima,
            // alineado verticalmente con los checks de notificadores (y ≈ 116).
            // La columna 8..420 está ocupada por checks Telegram/Slack/Audio,
            // y la franja 420..720 contiene 'Reintentos' + nudReintentos.
            // Por eso lo posicionamos a la izquierda de btnLimpiar dentro de
            // esa franja libre (x≈540..720, y≈116) — fuera de cualquier control.
            int yTarget = 116;
            int xTarget = 725 - _lblTokenCount.PreferredWidth - 12;
            // Si el panel es muy angosto, fall-back a la esquina superior derecha
            if (xTarget < 530)
            {
                xTarget = pnlContenedorTxt.ClientSize.Width - _lblTokenCount.PreferredWidth - 14;
                yTarget = 7;
            }
            _lblTokenCount.Location = new Point(Math.Max(0, xTarget), Math.Max(0, yTarget));
            _lblTokenCount.BringToFront();
        }

        private void ActualizarTokenCounter()
        {
            if (_lblTokenCount == null) return;
            string txt = textBoxInstrucion?.Text ?? string.Empty;
            // Aproximación: 1 token ≈ 4 caracteres en español/inglés
            int tokens = (int)Math.Ceiling(txt.Length / 4.0);
            int chars  = txt.Length;
            _lblTokenCount.Text = $"~{tokens} tokens · {chars} chars";

            // Cambia color si es muy largo
            if (tokens > 1500)      _lblTokenCount.ForeColor = ColorTranslator.FromHtml("#f87171");
            else if (tokens > 800)  _lblTokenCount.ForeColor = ColorTranslator.FromHtml("#fbbf24");
            else                    _lblTokenCount.ForeColor = _emTextSub;
        }

        // ── QUICK ACTIONS sobre la última respuesta del Comunicador ──
        public void MostrarQuickActions()
        {
            if (_ultimaBurbujaComunicador == null || _ultimaBurbujaComunicador.IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(MostrarQuickActions); return; }

            // Si ya hay un panel de quick actions debajo, no duplicar
            int idx = pnlChat.Controls.IndexOf(_ultimaBurbujaComunicador);
            if (idx < 0) return;

            // Verificar si el siguiente control ya es un panel de acciones
            if (idx + 1 < pnlChat.Controls.Count
                && pnlChat.Controls[idx + 1] is Panel sig
                && sig.Tag?.ToString() == "QuickActions")
                return;

            var pnlAcciones = new Panel
            {
                Tag       = "QuickActions",
                Width     = 280,
                Height    = 32,
                BackColor = Color.Transparent,
                Margin    = new Padding(250, 0, 80, 8)
            };

            (string icono, string tooltip, Action accion)[] acciones =
            {
                ("\U0001F501", "Regenerar respuesta",  AccionRegenerar),
                ("\U0001F4CB", "Copiar respuesta",     AccionCopiarUltima),
                ("\U0001F44D", "Útil",                 () => Feedback(true)),
                ("\U0001F44E", "No útil",              () => Feedback(false)),
            };

            int xb = 0;
            foreach (var (icono, tip, accion) in acciones)
            {
                var btn = new Label
                {
                    Text       = icono,
                    Font       = new Font("Segoe UI Emoji", 10F),
                    ForeColor  = _emTextSub,
                    BackColor  = _emBgCard,
                    TextAlign  = ContentAlignment.MiddleCenter,
                    Width      = 32,
                    Height     = 28,
                    Location   = new Point(xb, 2),
                    Cursor     = Cursors.Hand
                };
                _toolTipArchivos.SetToolTip(btn, tip);
                btn.MouseEnter += (_, __) => { btn.BackColor = _emEmerald9; btn.ForeColor = _emEmerald4; };
                btn.MouseLeave += (_, __) => { btn.BackColor = _emBgCard;   btn.ForeColor = _emTextSub; };
                btn.Click      += (_, __) => accion();
                pnlAcciones.Controls.Add(btn);
                xb += 36;
            }

            pnlChat.Controls.Add(pnlAcciones);
            pnlChat.Controls.SetChildIndex(pnlAcciones, idx + 1);
            ScrollSuaveAlFinal();
        }

        private void AccionRegenerar()
        {
            // No toca lógica; solo re-dispara btnEnviar si hay texto en el campo
            if (!string.IsNullOrWhiteSpace(textBoxInstrucion.Text))
                btnEnviar_Click(this, EventArgs.Empty);
        }

        private void AccionCopiarUltima()
        {
            try
            {
                if (_ultimaBurbujaComunicador != null && !_ultimaBurbujaComunicador.IsDisposed)
                    Clipboard.SetText(_ultimaBurbujaComunicador.Text ?? string.Empty);
            }
            catch { /* clipboard puede fallar si la ventana no tiene foco */ }
        }

        private void Feedback(bool positivo)
        {
            // Solo feedback visual; sin persistencia
            string msg = positivo ? "👍 Gracias por tu feedback" : "👎 Anotado";
            _toolTipArchivos.Show(msg, this, PointToClient(MousePosition), 1400);
        }

        // ── SEPARADORES CON FECHA ────────────────────────────────────
        private DateTime? _ultimaFechaInsertada;

        /// <summary>
        /// Inserta un separador "Hoy / Ayer / DD MMM" si la fecha actual
        /// no coincide con la última insertada. Llamar antes de cada
        /// pnlChat.Controls.Add(burbuja).
        /// </summary>
        private void InsertarSeparadorFechaSiCorresponde()
        {
            DateTime hoy = DateTime.Now.Date;
            if (_ultimaFechaInsertada == hoy) return;
            _ultimaFechaInsertada = hoy;

            string etiqueta;
            if (hoy == DateTime.Today)                   etiqueta = "Hoy";
            else if (hoy == DateTime.Today.AddDays(-1))  etiqueta = "Ayer";
            else if ((DateTime.Today - hoy).TotalDays < 7)
                etiqueta = hoy.ToString("dddd",
                    System.Globalization.CultureInfo.GetCultureInfo("es-ES"));
            else
                etiqueta = hoy.ToString("dd MMM",
                    System.Globalization.CultureInfo.GetCultureInfo("es-ES"));

            var separador = new Label
            {
                Text       = $"───────  {etiqueta}  ───────",
                AutoSize   = false,
                Width      = pnlChat.ClientSize.Width - 28,
                Height     = 24,
                TextAlign  = ContentAlignment.MiddleCenter,
                Font       = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor  = ColorTranslator.FromHtml("#475569"),
                BackColor  = Color.Transparent,
                Margin     = new Padding(8, 12, 8, 6),
                Tag        = "DateSeparator"
            };
            pnlChat.Controls.Add(separador);
        }

        // ── AUTO-SCROLL ─────────────────────────────────────────────
        // Sistema simple: toggle on/off. Cuando está ON, cada llamada a
        // ScrollSuaveAlFinal salta directo al fondo sin animación ni
        // tracking de eventos de scroll del usuario (eso era la fuente
        // del bug "el scroll sube"). Cuando está OFF el chat no se mueve.
        private bool _autoScrollActivado = true;
        private Label _btnToggleScroll;

        // ── ZOOM +/- (Ctrl++ / Ctrl-- / Ctrl+0) ──────────────────────
        private float _zoomActual = 1.0f;
        private const float ZoomMin = 0.7f, ZoomMax = 1.6f, ZoomStep = 0.1f;
        private Label _lblZoomNivel;

        private void ConstruirControlesZoom()
        {
            if (panelHead == null) return;

            var pnlZoom = new Panel
            {
                Width     = 110,
                Height    = 28,
                BackColor = _emBgCard,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Location  = new Point(panelHead.ClientSize.Width - 220, 11),
                Tag       = "ZoomCtrl"
            };

            var btnMinus = NuevoBotonZoom("−", () => CambiarZoom(-ZoomStep));
            btnMinus.Location = new Point(0, 0);

            _lblZoomNivel = new Label
            {
                Text       = "100%",
                AutoSize   = false,
                Width      = 46,
                Height     = 28,
                TextAlign  = ContentAlignment.MiddleCenter,
                Font       = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor  = _emTextSub,
                BackColor  = _emBgCard,
                Cursor     = Cursors.Hand,
                Location   = new Point(32, 0),
            };
            _lblZoomNivel.Click += (_, __) => ResetZoom();
            _toolTipArchivos.SetToolTip(_lblZoomNivel, "Click: 100% · Ctrl++ / Ctrl-- / Ctrl+0");

            var btnPlus = NuevoBotonZoom("+", () => CambiarZoom(+ZoomStep));
            btnPlus.Location = new Point(80, 0);

            pnlZoom.Controls.Add(btnMinus);
            pnlZoom.Controls.Add(_lblZoomNivel);
            pnlZoom.Controls.Add(btnPlus);
            panelHead.Controls.Add(pnlZoom);

            // Atajos de teclado: form-level
            this.KeyPreview = true;
            this.KeyDown += (_, e) =>
            {
                if (!e.Control) return;
                if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)        { CambiarZoom(+ZoomStep); e.Handled = true; }
                else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract) { CambiarZoom(-ZoomStep); e.Handled = true; }
                else if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)    { ResetZoom();             e.Handled = true; }
            };
        }

        private Label NuevoBotonZoom(string texto, Action accion)
        {
            var btn = new Label
            {
                Text       = texto,
                AutoSize   = false,
                Width      = 32,
                Height     = 28,
                TextAlign  = ContentAlignment.MiddleCenter,
                Font       = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor  = _emEmerald4,
                BackColor  = _emBgCard,
                Cursor     = Cursors.Hand
            };
            btn.MouseEnter += (_, __) => { btn.BackColor = _emEmerald9; btn.ForeColor = _emTextMain; };
            btn.MouseLeave += (_, __) => { btn.BackColor = _emBgCard;   btn.ForeColor = _emEmerald4; };
            btn.Click      += (_, __) => accion();
            return btn;
        }

        private void CambiarZoom(float delta)
        {
            float nuevo = Math.Max(ZoomMin, Math.Min(ZoomMax, _zoomActual + delta));
            if (Math.Abs(nuevo - _zoomActual) < 0.001f) return;
            _zoomActual = nuevo;
            AplicarZoomGlobal();
        }

        private void ResetZoom()
        {
            if (Math.Abs(_zoomActual - 1.0f) < 0.001f) return;
            _zoomActual = 1.0f;
            AplicarZoomGlobal();
        }

        private void AplicarZoomGlobal()
        {
            BurbujaChat.ZoomGlobal = _zoomActual;
            if (_lblZoomNivel != null)
                _lblZoomNivel.Text = $"{(int)Math.Round(_zoomActual * 100)}%";

            // Aplicar a las burbujas existentes
            foreach (Control c in pnlChat.Controls)
                if (c is BurbujaChat b && !b.IsDisposed)
                    b.AplicarZoom(_zoomActual);

            ScrollSuaveAlFinal();
        }

        // ── PANEL DE CREDENCIALES ARRASTRABLES ───────────────────────
        private Panel _pnlCredenciales;
        private Label _btnToggleCredenciales;
        private FlowLayoutPanel _flowCredenciales;

        private void ConstruirPanelCredenciales()
        {
            if (panelHead == null || pnlChat == null) return;

            // ── Botón flotante (FAB) sobre el chat, esquina superior derecha.
            //    Lejos del header → cero colisiones con comboboxes / btnGuardar. ──
            _btnToggleCredenciales = new Label
            {
                Text       = "🔑",
                AutoSize   = false,
                Width      = 36,
                Height     = 36,
                TextAlign  = ContentAlignment.MiddleCenter,
                Font       = new Font("Segoe UI Emoji", 13F, FontStyle.Regular),
                ForeColor  = _emEmerald4,
                BackColor  = _emBgCard,
                Cursor     = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle,
                Tag        = "ToggleCredenciales"
            };
            _btnToggleCredenciales.MouseEnter += (_, __) => { _btnToggleCredenciales.BackColor = _emEmerald9; _btnToggleCredenciales.ForeColor = _emTextMain; };
            _btnToggleCredenciales.MouseLeave += (_, __) => { _btnToggleCredenciales.BackColor = _emBgCard;   _btnToggleCredenciales.ForeColor = _emEmerald4; };
            _btnToggleCredenciales.Click      += (_, __) => ToggleCredenciales();
            _toolTipArchivos.SetToolTip(_btnToggleCredenciales, "Credenciales — abre el panel para arrastrar al chat");

            // Insertar en el mismo parent que pnlChat para flotar encima
            var parentChat = pnlChat.Parent;
            if (parentChat != null)
            {
                parentChat.Controls.Add(_btnToggleCredenciales);
                _btnToggleCredenciales.BringToFront();
            }

            void Reubicar()
            {
                if (pnlChat == null || _btnToggleCredenciales == null) return;
                _btnToggleCredenciales.Location = new Point(
                    pnlChat.Right - _btnToggleCredenciales.Width - 12,
                    pnlChat.Top + 10);
                _btnToggleCredenciales.BringToFront();
            }
            Reubicar();
            pnlChat.Resize        += (_, __) => Reubicar();
            pnlChat.LocationChanged += (_, __) => Reubicar();
            if (parentChat != null) parentChat.Resize += (_, __) => Reubicar();

            // ── Panel flotante: se posiciona MANUALMENTE encima del pnlChat
            //    (no usar Dock para no pelearse con el Dock.Fill del chat) ──
            _pnlCredenciales = new Panel
            {
                Width     = 240,
                BackColor = _emBgCard,
                Padding   = new Padding(10, 14, 10, 12),
                Visible   = false,
                Tag       = "CredencialesPanel",
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitulo = new Label
            {
                Text       = "🔑  Credenciales",
                Font       = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor  = _emTextMain,
                BackColor  = Color.Transparent,
                AutoSize   = true,
                Location   = new Point(10, 6)
            };

            var lblTip = new Label
            {
                Text       = "Arrastra una al chat  ↓",
                Font       = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor  = _emTextSub,
                BackColor  = Color.Transparent,
                AutoSize   = true,
                Location   = new Point(10, 28)
            };

            var btnCerrar = new Label
            {
                Text       = "✕",
                AutoSize   = false,
                Width      = 22, Height = 22,
                TextAlign  = ContentAlignment.MiddleCenter,
                Font       = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor  = _emTextSub,
                BackColor  = Color.Transparent,
                Cursor     = Cursors.Hand,
                Anchor     = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCerrar.MouseEnter += (_, __) => btnCerrar.ForeColor = _emEmerald4;
            btnCerrar.MouseLeave += (_, __) => btnCerrar.ForeColor = _emTextSub;
            btnCerrar.Click      += (_, __) => ToggleCredenciales();

            _flowCredenciales = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = _emBgCard,
                Tag           = "CredencialesFlow"
            };

            _pnlCredenciales.Controls.Add(_flowCredenciales);
            _pnlCredenciales.Controls.Add(btnCerrar);
            _pnlCredenciales.Controls.Add(lblTip);
            _pnlCredenciales.Controls.Add(lblTitulo);

            // ── Insertar al MISMO contenedor que pnlChat, posicionado encima ──
            var parent = pnlChat.Parent;
            if (parent == null) return;
            parent.Controls.Add(_pnlCredenciales);

            // Reposicionar el panel y su contenido manualmente
            void RecolocarPanel()
            {
                if (_pnlCredenciales == null || pnlChat == null) return;
                _pnlCredenciales.Bounds = new Rectangle(
                    pnlChat.Right - _pnlCredenciales.Width - 8,
                    pnlChat.Top + 8,
                    _pnlCredenciales.Width,
                    pnlChat.Height - 16);

                btnCerrar.Location = new Point(_pnlCredenciales.ClientSize.Width - btnCerrar.Width - 6, 6);
                _flowCredenciales.SetBounds(
                    8, 52,
                    _pnlCredenciales.ClientSize.Width - 16,
                    _pnlCredenciales.ClientSize.Height - 60);
            }
            RecolocarPanel();
            pnlChat.Resize += (_, __) => RecolocarPanel();
            parent.Resize  += (_, __) => RecolocarPanel();

            RellenarChipsCredenciales(_flowCredenciales);
            HabilitarDropCredenciales();
        }

        private void RellenarChipsCredenciales(FlowLayoutPanel flow)
        {
            flow.Controls.Clear();
            if (_listaApisDisponibles == null) return;

            foreach (var api in _listaApisDisponibles)
            {
                var chip = new Panel
                {
                    Width     = 196,
                    Height    = 50,
                    Margin    = new Padding(0, 0, 0, 8),
                    BackColor = _emBgInput,
                    Cursor    = Cursors.Hand,
                    Tag       = api
                };

                var lblNombre = new Label
                {
                    Text       = "🔑  " + (api.Nombre ?? "(sin nombre)"),
                    Font       = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    ForeColor  = _emTextMain,
                    BackColor  = Color.Transparent,
                    AutoSize   = false,
                    Width      = 190,
                    Height     = 22,
                    Location   = new Point(6, 4),
                    TextAlign  = ContentAlignment.MiddleLeft
                };

                string previewKey = string.IsNullOrEmpty(api.key)
                    ? "(sin clave)"
                    : (api.key.Length > 8 ? new string('•', 6) + api.key.Substring(api.key.Length - 4) : "•••");

                var lblKey = new Label
                {
                    Text       = previewKey,
                    Font       = new Font("Cascadia Mono", 8F),
                    ForeColor  = _emTextSub,
                    BackColor  = Color.Transparent,
                    AutoSize   = false,
                    Width      = 190,
                    Height     = 18,
                    Location   = new Point(6, 26),
                    TextAlign  = ContentAlignment.MiddleLeft
                };

                chip.Controls.Add(lblNombre);
                chip.Controls.Add(lblKey);

                // Drag handlers — al iniciar arrastre, soltar el nombre como string
                MouseEventHandler iniciarDrag = (_, __) =>
                {
                    DoDragDrop("@" + (api.Nombre ?? ""), DragDropEffects.Copy);
                };
                chip.MouseDown      += iniciarDrag;
                lblNombre.MouseDown += iniciarDrag;
                lblKey.MouseDown    += iniciarDrag;

                // Hover
                EventHandler enter = (_, __) => chip.BackColor = _emEmerald9;
                EventHandler leave = (_, __) => chip.BackColor = _emBgInput;
                chip.MouseEnter      += enter;
                chip.MouseLeave      += leave;
                lblNombre.MouseEnter += enter; lblNombre.MouseLeave += leave;
                lblKey.MouseEnter    += enter; lblKey.MouseLeave    += leave;

                flow.Controls.Add(chip);
            }

            if (_listaApisDisponibles.Count == 0)
            {
                var vacio = new Label
                {
                    Text       = "(sin credenciales — agrégalas en Credenciales)",
                    Font       = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                    ForeColor  = _emTextSub,
                    BackColor  = Color.Transparent,
                    AutoSize   = true,
                    Margin     = new Padding(0, 8, 0, 0)
                };
                flow.Controls.Add(vacio);
            }
        }

        private void HabilitarDropCredenciales()
        {
            if (textBoxInstrucion == null) return;

            // textBoxInstrucion ya tiene AllowDrop=true por configuración previa.
            textBoxInstrucion.DragOver += (_, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.StringFormat) == true)
                {
                    var s = e.Data.GetData(DataFormats.StringFormat) as string;
                    if (s != null && s.StartsWith("@"))
                        e.Effect = DragDropEffects.Copy;
                }
            };
        }

        private void ToggleCredenciales()
        {
            if (_pnlCredenciales == null) return;
            _pnlCredenciales.Visible = !_pnlCredenciales.Visible;
            if (_pnlCredenciales.Visible)
            {
                if (_flowCredenciales != null) RellenarChipsCredenciales(_flowCredenciales);
                _pnlCredenciales.BringToFront();
                _pnlCredenciales.Parent?.PerformLayout();
                _pnlCredenciales.Refresh();
            }
        }

        // ── BOTÓN TOGGLE de auto-scroll ──────────────────────────────
        private void ConstruirToggleScroll()
        {
            if (pnlChat == null) return;
            var parentChat = pnlChat.Parent;
            if (parentChat == null) return;

            _btnToggleScroll = new Label
            {
                Text        = "🔒",
                AutoSize    = false,
                Width       = 36,
                Height      = 36,
                TextAlign   = ContentAlignment.MiddleCenter,
                Font        = new Font("Segoe UI Emoji", 13F, FontStyle.Regular),
                ForeColor   = _emEmerald4,
                BackColor   = _emBgCard,
                Cursor      = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle,
                Tag         = "ToggleScroll"
            };
            _btnToggleScroll.MouseEnter += (_, __) =>
            {
                _btnToggleScroll.BackColor = _emEmerald9;
                _btnToggleScroll.ForeColor = _emTextMain;
            };
            _btnToggleScroll.MouseLeave += (_, __) =>
            {
                _btnToggleScroll.BackColor = _emBgCard;
                _btnToggleScroll.ForeColor = _autoScrollActivado ? _emEmerald4 : _emTextSub;
            };
            _btnToggleScroll.Click += (_, __) => ToggleAutoScroll();
            _toolTipArchivos.SetToolTip(_btnToggleScroll,
                "Auto-scroll al fondo · activado");

            parentChat.Controls.Add(_btnToggleScroll);
            _btnToggleScroll.BringToFront();

            void Reubicar()
            {
                if (_btnToggleScroll == null || pnlChat == null) return;
                // Pegado debajo del botón de credenciales (esquina sup. derecha)
                _btnToggleScroll.Location = new Point(
                    pnlChat.Right - _btnToggleScroll.Width - 12,
                    pnlChat.Top + 10 + 36 + 8);
                _btnToggleScroll.BringToFront();
            }
            Reubicar();
            pnlChat.Resize          += (_, __) => Reubicar();
            pnlChat.LocationChanged += (_, __) => Reubicar();
            parentChat.Resize       += (_, __) => Reubicar();
        }

        private void ToggleAutoScroll()
        {
            _autoScrollActivado = !_autoScrollActivado;
            if (_btnToggleScroll != null)
            {
                _btnToggleScroll.Text      = _autoScrollActivado ? "🔒" : "🔓";
                _btnToggleScroll.ForeColor = _autoScrollActivado ? _emEmerald4 : _emTextSub;
                _toolTipArchivos.SetToolTip(_btnToggleScroll,
                    _autoScrollActivado
                        ? "Auto-scroll al fondo · activado (click para pausar)"
                        : "Auto-scroll pausado (click para reactivar)");
            }
            // Si se reactiva, saltar inmediatamente al fondo
            if (_autoScrollActivado) ScrollSuaveAlFinal();
        }

        /// <summary>
        /// Salta el scroll del chat al fondo si el toggle de auto-scroll
        /// está activado. Sin animación, sin tracking, sin cálculos exotéricos:
        /// usa AutoScrollPosition con un Y enorme y deja que WinForms haga el clamp.
        /// </summary>
        private void ScrollSuaveAlFinal()
        {
            if (pnlChat == null || pnlChat.Controls.Count == 0) return;
            if (!_autoScrollActivado) return;
            if (InvokeRequired) { BeginInvoke(ScrollSuaveAlFinal); return; }

            try
            {
                // Truco WinForms: AutoScrollPosition ignora valores fuera de
                // rango y lo clampa al máximo posible — saltamos al final
                // sin tener que calcular alturas a mano (que era la fuente
                // de los bugs anteriores).
                pnlChat.AutoScrollPosition = new Point(0, int.MaxValue);
            }
            catch { /* tolerar */ }
        }
    }
}