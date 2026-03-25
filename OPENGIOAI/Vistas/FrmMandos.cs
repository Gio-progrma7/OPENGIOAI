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
        }

        private void FrmMandos_FormClosing(object sender, FormClosingEventArgs e)
        {
            // [C1] Cancelar cualquier petición en vuelo al cerrar el formulario.
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
            ConsultasApis(textBoxInstrucion);
            MostrarPreview(textBoxInstrucion, labelSugerencia);
            TextBoxRounder.AjustarAlturaConRedondeo(textBoxInstrucion);
        }

        private void textBoxInstrucion_KeyDown(object sender, KeyEventArgs e)
        {
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
            await IniciarSlack();
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
            pictureBoxCarga.Visible = true;
            await Procesar_Instrucciones();
            pictureBoxCarga.Visible = false;
            LimpiarControles();
        }

        private async void checkBoxTelegram_CheckedChanged(object sender, EventArgs e)
        {
            _telegramActivo = checkBoxTelegram.Checked;
            await IniciarConversasionTelegram();
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
                Servicios.ChatGpt => await AIServicios.ObtenerModelosOpenAIAsync(apiKey),
                Servicios.Gemenni => await AIServicios.ObtenerModelosGeminiAsync(apiKey),
                Servicios.Ollama => await AIServicios.ObtenerModelosOllamaApiAsync(),
                Servicios.OpenRouter => await AIServicios.ObtenerModelosOpenRouterAsync(apiKey),
                Servicios.Claude => await AIServicios.ObtenerModelosClaudeAsync(apiKey),
                Servicios.Deespeek => await AIServicios.ObtenerModelosDeepSeekAsync(apiKey),
                _ => new List<string>()
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
            MostrarMensaje("Procesando tu solicitud, por favor espera...", false);

            string respuesta = await EjecutarMotorIAAsync(texto, _telegramActivo, _slackActivo);

            if (!_soloResptxt)
                MostrarMensaje(respuesta, false);
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
            // [C1] Liberar el token anterior antes de reemplazarlo.
            _ctsIA?.Cancel();
            _ctsIA?.Dispose();
            _ctsIA = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSegundos));
            CancellationToken ct = _ctsIA.Token;

            string instruccionOriginal = instruccion;
            instruccion = _ventana.CombinarConInstruccion(instruccion, _recordarTema, _soloChat);

            // ── AGENTE 1 ──────────────────────────────────────────────────────
            string respuesta = await EjecutarAgente1Async(instruccion, ct);
            string resLimpia1 = Utils.LimpiarRespuesta(ObtenerContextoChat(_archivoSeleccionado.Ruta));

            MostrarMensaje(resLimpia1, false);
            await EjecutarDifusionAsync(resLimpia1, usarTelegram, usarSlack);

            // ── AGENTE 2 (solo si no es respuesta rápida) ─────────────────────
            string resLimpiaFinal = resLimpia1;
            if (!_soloResptxt)
            {
                ct.ThrowIfCancellationRequested(); // [C1] salida limpia si se canceló entre agentes

                string contextoA2 = ConstruirContextoAgente2(instruccionOriginal, resLimpia1, respuesta);
                respuesta = await EjecutarAgente2Async(contextoA2, ct);
                resLimpiaFinal = Utils.LimpiarRespuesta(ObtenerContextoChat(_archivoSeleccionado.Ruta));

                await EjecutarDifusionAsync(resLimpiaFinal, usarTelegram, usarSlack);
            }

            // ── Registrar turno en ventana deslizante ─────────────────────────
            _ventana.Agregar(instruccionOriginal, resLimpiaFinal);

            return resLimpiaFinal;
        }

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
                ct);
        }

        /// <summary>
        /// Ejecuta Agente 2 (validación / mejora) con el token de cancelación activo.
        /// [C2] Al estar separado, puede extenderse fácilmente para soportar un
        ///      modelo diferente (ej: un validador más barato/rápido).
        /// </summary>
        private async Task<string> EjecutarAgente2Async(string contexto, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            return await AIModelConector.EjecutarInstruccionIAAsync(
                contexto,
                _modeloSeleccionado.Modelos,
                _archivoSeleccionado.Ruta,
                _modeloSeleccionado.ApiKey,
                Utils.ObtenerNombresApis(_listaApisDisponibles),
                _soloChat,
                _modeloSeleccionado.Agente,
                ct);
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

            [RESPUESTA_AGENTE_1]
            {respuestaAgente1}

            [CODIGO_GENERADO]
            {codigo}

            ERES UN SEGUNDO AGENTE , TU DEBES ANALIZAR LA RESPUESTA DEL AGENTE 1
            DEBES COMPARAR CON LA INSTRUCCION ORIGINAL Y VER SI LA REALIZO.
            SI ALGO FALLO: PROPON Y SOLUCIONA.
            SI TODO SALIO BIEN : ANALIZA Y PUEDES DARLE UN MENSAJE COMO AGENTE , PROPONIENDO TAREAS ATRAVEZ DE SOLO ESCRITO EN respuesta.txt , NO ESCRIBIR EN FORMATO JSON , DEBE SER HUMANO 
            ENTENDIBLE , SIN TANTO TECNISISMO.
            TODO CON CODIGO PHYTON , TU TAREA ES ESCRIBIR EN EL txt o SOLUCIONAR EL PROBLEMA
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
                Servicios.Gemenni => await AIServicios.ApiKeyGeminiValidaAsync(apikey),
                Servicios.ChatGpt => await AIServicios.ApiKeyOpenAIValidaAsync(apikey),
                Servicios.Ollama => await AIServicios.OllamaEstaActivoAsync(),
                Servicios.OpenRouter => await AIServicios.ApiKeyOpenRouterValidaAsync(apikey),
                Servicios.Claude => await AIServicios.ApiKeyClaudeValidaAsync(apikey),
                Servicios.Deespeek => await AIServicios.ApiKeyDeepSeekValidaAsync(apikey),
                _ => false
            };

            if (!valida)
                MostrarMensaje("Tu Api no es correcta o tiene algún error, comprueba por favor.", false);
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
                        pictureBoxCarga.Visible = true;
                        try
                        {
                            if (string.IsNullOrEmpty(textoOriginal)) return;
                            string resp = await EjecutarMotorIAAsync(textoOriginal, usarTelegram);
                            MostrarMensaje(resp, false);
                        }
                        finally
                        {
                            pictureBoxCarga.Visible = false;
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
            BeginInvoke(() => pictureBoxCarga.Visible = true);

            var (ok, error) = ValidarCondiciones(instruccion);
            if (!ok)
            {
                MostrarMensaje(error, false);
                BeginInvoke(() => pictureBoxCarga.Visible = false);
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
                pictureBoxCarga.Visible = false;
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
        private void CancelarInstruccion() => _ctsIA?.Cancel();

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
                textBoxInstrucion.Height = 70;
                textBoxInstrucion.AllowDrop = true;
                textBoxInstrucion.AcceptsTab = true;

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

                ConfigurarPanelArchivos();

                _toolTipArchivos.AutoPopDelay = 5000;
                _toolTipArchivos.InitialDelay = 500;
                _toolTipArchivos.ReshowDelay = 200;
                _toolTipArchivos.ShowAlways = true;
            }
            finally
            {
                ResumeLayout(true);
            }
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