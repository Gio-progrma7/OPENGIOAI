using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmModelos : Form
    {
        #region Campos privados

        private List<Modelo> _listaAgentes = new();
        private List<Api> _listaApisDisponibles = new();
        private List<ComboBox> _listaApis = new();
        private List<ComboBox> _listaModels = new();
        private List<CheckBox> _listaEstados = new();
        private List<Servicios> _listaServicios = new();
        private bool _cargandoControles = false;

        // Project ID de GCP detectado desde gcloud (se actualiza al verificar estado)
        private string _antigravityProjectId = "";

        #endregion

        #region Constructor e inicialización

        public FrmModelos()
        {
            InitializeComponent();
            AplicarThema();
        }

        private async void FrmModelos_Load(object sender, EventArgs e)
        {
            await InicializarDatos();
        }

        /// <summary>
        /// Orquesta la carga completa de datos y controles al iniciar el formulario.
        /// Deshabilita los paneles durante la carga para evitar interacciones prematuras.
        /// </summary>
        private async Task InicializarDatos()
        {
            EstadoPanels(false);

            // Ocultar combobox de API para Antigravity — el Project ID viene de gcloud,
            // no de la lista de API keys. Mostrar solo el label explicativo.
            comboBoxApiAntigravity.Visible  = false;
            labelAntigravityApiKey.Text     = "Project ID detectado por gcloud:";
            labelAntigravityApiKey.ForeColor = Color.FromArgb(100, 116, 139);

            await CargarDatos();
            CargarControles();
            await CargarInfoAgentes();

            EstadoPanels(true);

            // Verificar estado de autenticación gcloud en background
            _ = VerificarEstadoGcloudAsync();
        }

        #endregion

        #region Carga de datos y controles

        /// <summary>
        /// Carga los datos iniciales necesarios para el funcionamiento del sistema.
        /// Obtiene la lista de servicios, agentes y APIs desde los archivos de configuración JSON.
        /// Si es la primera ejecución, genera una configuración por defecto.
        /// </summary>
        private async Task CargarDatos()
        {
            _listaServicios = Enum.GetValues<Servicios>().ToList();
            _listaAgentes = JsonManager.Leer<Modelo>(RutasProyecto.ObtenerRutaListModelos());
            _listaApisDisponibles = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());

            if (_listaAgentes.Count != _listaServicios.Count)
                GuardarPrimeraVez();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Inicializa y agrupa los controles de la interfaz (APIs, modelos y estados)
        /// asignando los orígenes de datos correspondientes a cada ComboBox.
        /// </summary>
        private void CargarControles()
        {
            _cargandoControles = true;
            try
            {
                _listaApis = new()
                {
                    comboBoxApiChat,          // [0] ChatGpt    = 1
                    comboBoxApiClau,          // [1] Claude     = 2
                    comboBoxApiGem,           // [2] Gemenni    = 3
                    comboBoxApiOlla,          // [3] Ollama     = 4
                    comboBoxApiDesp,          // [4] Deespeek   = 5
                    ComboxApiOpenroute,       // [5] OpenRouter = 6
                    comboBoxApiAntigravity    // [6] Antigravity= 7
                };

                _listaModels = new()
                {
                    comboBoxMChat,            // [0] ChatGpt
                    comboBoxMClau,            // [1] Claude
                    comboBoxMGem,             // [2] Gemenni
                    comboBoxmOlla,            // [3] Ollama
                    comboBoxMDesp,            // [4] Deespeek
                    ComboxMOpenroute,         // [5] OpenRouter
                    comboBoxMAntigravity      // [6] Antigravity
                };

                _listaEstados = new()
                {
                    checkBoxChat,             // [0] ChatGpt
                    checkBoxClua,             // [1] Claude
                    checkBoxGem,              // [2] Gemenni
                    checkBoxOlla,             // [3] Ollama
                    checkBoxDeesp,            // [4] Deespeek
                    checkBoxOpenroute,        // [5] OpenRouter
                    checkBoxAntigravity       // [6] Antigravity
                };

                for (int i = 0; i < _listaApis.Count; i++)
                {
                    _listaApis[i].DataSource = new List<Api>(_listaApisDisponibles);
                    _listaApis[i].DisplayMember = "Nombre";
                    _listaApis[i].ValueMember = "key";
                    _listaApis[i].SelectedValue = _listaAgentes[i].ApiKey;
                }
            }
            finally
            {
                _cargandoControles = false;
            }
        }

        /// <summary>
        /// Sincroniza la información de cada agente con sus controles visuales correspondientes:
        /// estado activo/inactivo, API seleccionada y modelo configurado.
        /// </summary>
        private async Task CargarInfoAgentes()
        {
            for (int i = 0; i < _listaAgentes.Count; i++)
            {
                _listaEstados[i].Checked = _listaAgentes[i].Estado;

                // Se guarda el modelo antes de que SeleccionarApi lo sobreescriba
                string modeloGuardado = _listaAgentes[i].Modelos;

                await SeleccionarApi(_listaAgentes[i].Agente, _listaAgentes[i].ApiKey);

                // Se restaura el modelo una vez que el DataSource ya está asignado
                _listaModels[i].Text = modeloGuardado;
            }
        }

        #endregion

        #region Lógica de agentes y servicios

        /// <summary>
        /// Obtiene la lista de modelos disponibles para un servicio de IA específico
        /// consultando la API correspondiente con la clave proporcionada.
        /// </summary>
        /// <param name="servicio">Servicio de IA del cual se desean obtener los modelos.</param>
        /// <param name="apiKey">Clave de autenticación requerida para consultar los modelos.</param>
        /// <returns>Lista de <see cref="ModeloAgente"/> con los modelos disponibles.</returns>
        private async Task<List<ModeloAgente>> ObtenerModeloAgente(Servicios servicio, string apiKey)
        {
            List<string> lsModels = servicio switch
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

            return lsModels
                .Select(s => new ModeloAgente { Nombre = s, Estado = true })
                .ToList();
        }

        /// <summary>
        /// Selecciona una API para el servicio indicado, recarga los modelos disponibles
        /// y actualiza el ComboBox correspondiente. Muestra mensajes de estado durante la carga.
        /// </summary>
        /// <param name="servicio">Servicio de IA a actualizar.</param>
        /// <param name="apikey">Clave de API seleccionada.</param>
        private async Task SeleccionarApi(Servicios servicio, string apikey)
        {
            int index = (int)servicio - 1;

            _listaAgentes[index].ApiKey = apikey ?? string.Empty;

            _listaModels[index].DataSource = null;
            _listaModels[index].Items.Clear();
            _listaModels[index].Text = "Cargando modelos...";

            var modelos = await ObtenerModeloAgente(servicio, apikey);

            if (modelos == null || modelos.Count == 0)
            {
                _listaModels[index].Text = "Modelos no disponibles";
                return;
            }

            _listaModels[index].DataSource = modelos;
            _listaModels[index].DisplayMember = "Nombre";
            _listaModels[index].ValueMember = "Estado";
        }

        /// <summary>
        /// Inicializa y guarda la configuración base de los agentes la primera vez
        /// que se ejecuta la aplicación, dejando todos los servicios desactivados.
        /// </summary>
        private void GuardarPrimeraVez()
        {
            _listaAgentes.Clear();

            foreach (var servicio in _listaServicios)
            {
                _listaAgentes.Add(new Modelo
                {
                    Agente = servicio,
                    Estado = false,
                    ApiKey = string.Empty,
                    Modelos = string.Empty
                });
            }

            JsonManager.Guardar(RutasProyecto.ObtenerRutaListModelos(), _listaAgentes);
        }

        /// <summary>
        /// Actualiza en disco la configuración de un agente existente, identificándolo
        /// por su tipo de servicio y reemplazando estado, API y modelo seleccionado.
        /// </summary>
        /// <param name="nuevo">Objeto <see cref="Modelo"/> con la nueva configuración.</param>
        private void ModificarAgente(Modelo nuevo)
        {
            JsonManager.Modificar<Modelo>(
                RutasProyecto.ObtenerRutaListModelos(),
                u => u.Agente == nuevo.Agente,
                u =>
                {
                    u.ApiKey = nuevo.ApiKey;
                    u.Estado = nuevo.Estado;
                    u.Modelos = nuevo.Modelos;
                });
        }

        /// <summary>
        /// Recoge los valores actuales de la interfaz para el servicio indicado,
        /// construye el objeto de configuración y lo persiste en almacenamiento.
        /// </summary>
        /// <param name="servicio">Servicio de IA que se desea guardar.</param>
        private void InicializarAgente(Servicios servicio)
        {
            int index = (int)servicio - 1;

            string apiKey;

            if (servicio == Servicios.Antigravity)
            {
                // Para Antigravity el "ApiKey" NO es una key de la lista —
                // es el GCP Project ID detectado por gcloud.
                apiKey = _antigravityProjectId;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show(
                        "No se detectó un Project ID de GCP activo.\n\n" +
                        "Pasos:\n" +
                        "1. Haz clic en '🔑 Autenticar gcloud'\n" +
                        "2. Completa el login en el navegador\n" +
                        "3. Ejecuta: gcloud config set project TU-PROYECTO\n" +
                        "4. Vuelve a abrir este panel",
                        "Project ID requerido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                Api apiSeleccionada = _listaApis[index].SelectedItem as Api;
                apiKey = apiSeleccionada?.key ?? string.Empty;
            }

            Modelo modeloEditado = new()
            {
                Agente = servicio,
                Estado = _listaEstados[index].Checked,
                ApiKey = apiKey,
                Modelos = _listaModels[index].Text
            };

            ModificarAgente(modeloEditado);

            MessageBox.Show(
                servicio == Servicios.Antigravity
                    ? $"Configuración guardada.\nProject ID: {apiKey}\nModelo: {modeloEditado.Modelos}"
                    : "Configuración guardada correctamente.",
                "Guardar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion

        #region UI — Eventos de botones

        private void btnGuardarChat_Click(object sender, EventArgs e) => InicializarAgente(Servicios.ChatGpt);
        private void btnGuardaGem_Click(object sender, EventArgs e) => InicializarAgente(Servicios.Gemenni);
        private void btnGuardarOlla_Click(object sender, EventArgs e) => InicializarAgente(Servicios.Ollama);
        private void btnGuardarClau_Click(object sender, EventArgs e) => InicializarAgente(Servicios.Claude);
        private void btnGuardarDeesp_Click(object sender, EventArgs e) => InicializarAgente(Servicios.Deespeek);
        private void btnOpenroute_Click(object sender, EventArgs e) => InicializarAgente(Servicios.OpenRouter);
        private void btnGuardarAntigravity_Click(object sender, EventArgs e) => InicializarAgente(Servicios.Antigravity);

        /// <summary>
        /// Abre una ventana CMD con el comando de autenticación gcloud.
        /// El usuario completa el login en el browser; al volver se actualiza el indicador.
        /// </summary>
        private async void btnAutenticarAntigravity_Click(object sender, EventArgs e)
        {
            btnAutenticarAntigravity.Enabled = false;
            lblGcloudStatus.ForeColor = Color.FromArgb(245, 158, 11); // ámbar
            lblGcloudStatus.Text = "⬤  Buscando gcloud...";

            try
            {
                // Buscar ruta real de gcloud (evita el error "no se reconoce como comando")
                string gcloudExe = AIServicios.EncontrarGcloudExe();

                string cmdArgs;
                if (!string.IsNullOrEmpty(gcloudExe))
                {
                    // Ruta encontrada — abrir cmd con la ruta completa
                    cmdArgs = $"/k \"{gcloudExe}\" auth application-default login";
                    lblGcloudStatus.Text = "⬤  Abriendo terminal...";
                }
                else
                {
                    // No encontrado en rutas conocidas — intentar igualmente vía cmd
                    cmdArgs = "/k gcloud auth application-default login";
                    lblGcloudStatus.ForeColor = Color.FromArgb(245, 158, 11);
                    lblGcloudStatus.Text = "⬤  gcloud no encontrado en rutas conocidas";
                    MessageBox.Show(
                        "No se encontró gcloud en las rutas de instalación estándar.\n\n" +
                        "Si tienes Google Cloud SDK instalado, asegúrate de que esté en el PATH.\n" +
                        "Descárgalo desde: https://cloud.google.com/sdk/docs/install",
                        "Google Cloud SDK",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Abrir terminal visible para que el usuario complete el login
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "cmd.exe",
                    Arguments       = cmdArgs,
                    UseShellExecute = true   // true = ventana visible con foco
                });

                // Esperar a que el usuario complete el proceso en el browser
                lblGcloudStatus.Text = "⬤  Esperando autorización en el browser...";
                await Task.Delay(12000);

                // Re-verificar estado
                await VerificarEstadoGcloudAsync();
            }
            catch (Exception ex)
            {
                lblGcloudStatus.ForeColor = Color.FromArgb(239, 68, 68);
                lblGcloudStatus.Text = $"⬤  Error: {ex.Message}";
            }
            finally
            {
                btnAutenticarAntigravity.Enabled = true;
            }
        }

        #endregion

        #region UI — Eventos de ComboBox de API

        private async void comboBoxApiChat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargandoControles || comboBoxApiChat.SelectedItem is not Api api) return;
            await SeleccionarApi(Servicios.ChatGpt, api.key);
        }

        private async void comboBoxApiGem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargandoControles || comboBoxApiGem.SelectedItem is not Api api) return;
            await SeleccionarApi(Servicios.Gemenni, api.key);
        }

        private async void comboBoxApiClau_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargandoControles || comboBoxApiClau.SelectedItem is not Api api) return;
            await SeleccionarApi(Servicios.Claude, api.key);
        }

        private async void ComboxApiOpenroute_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargandoControles || ComboxApiOpenroute.SelectedItem is not Api api) return;
            await SeleccionarApi(Servicios.OpenRouter, api.key);
        }

        private async void comboBoxApiDesp_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargandoControles || comboBoxApiDesp.SelectedItem is not Api api) return;
            await SeleccionarApi(Servicios.Deespeek, api.key);
        }

        private async void comboBoxApiAntigravity_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargandoControles || comboBoxApiAntigravity.SelectedItem is not Api api) return;
            await SeleccionarApi(Servicios.Antigravity, api.key);
        }

        #endregion

        #region Antigravity — Estado gcloud

        /// <summary>
        /// Verifica el estado completo de Antigravity (token + project ID) y actualiza
        /// el indicador visual con mensajes precisos para cada caso de fallo.
        /// </summary>
        private async Task VerificarEstadoGcloudAsync()
        {
            // Mostrar estado intermedio mientras se comprueba
            if (InvokeRequired)
                Invoke(() => { lblGcloudStatus.ForeColor = Color.FromArgb(245, 158, 11); lblGcloudStatus.Text = "⬤  Verificando..."; });
            else
            { lblGcloudStatus.ForeColor = Color.FromArgb(245, 158, 11); lblGcloudStatus.Text = "⬤  Verificando..."; }

            try
            {
                // DiagnosticarAntigravityAsync distingue los tres estados posibles:
                //   a) Sin credenciales ADC
                //   b) Token OK pero sin Project ID
                //   c) Todo OK
                var (tokenOk, projectOk, projectId, mensaje) =
                    await AIServicios.DiagnosticarAntigravityAsync();

                if (InvokeRequired)
                    Invoke(() => AplicarEstadoAntigravity(tokenOk, projectOk, projectId, mensaje));
                else
                    await AplicarEstadoAntigravity(tokenOk, projectOk, projectId, mensaje);
            }
            catch (Exception ex)
            {
                string msg = $"Error al verificar: {ex.Message}";
                if (InvokeRequired)
                    Invoke(() => { lblGcloudStatus.ForeColor = Color.FromArgb(239, 68, 68); lblGcloudStatus.Text = $"⬤  {msg}"; });
                else
                { lblGcloudStatus.ForeColor = Color.FromArgb(239, 68, 68); lblGcloudStatus.Text = $"⬤  {msg}"; }
            }
        }

        /// <summary>
        /// Aplica el resultado del diagnóstico al indicador visual y carga de modelos.
        /// </summary>
        private async Task AplicarEstadoAntigravity(
            bool tokenOk, bool projectOk, string projectId, string mensaje)
        {
            if (!tokenOk)
            {
                // Sin credenciales — rojo + instrucción precisa
                lblGcloudStatus.ForeColor = Color.FromArgb(239, 68, 68);
                lblGcloudStatus.Text = $"⬤  {mensaje}";
                return;
            }

            if (!projectOk)
            {
                // Token OK pero sin proyecto — ámbar + instrucción
                lblGcloudStatus.ForeColor = Color.FromArgb(245, 158, 11);
                lblGcloudStatus.Text = $"⬤  {mensaje}";
                // Cargamos igual con fallback para que el comboBox no quede vacío
                await SeleccionarApi(Servicios.Antigravity, "");
                return;
            }

            // ── Todo OK: token + proyecto detectados ─────────────────────────────
            _antigravityProjectId = projectId;
            lblGcloudStatus.ForeColor = Color.FromArgb(34, 197, 94);
            lblGcloudStatus.Text = $"⬤  {mensaje}";

            // Buscar si ya existe una entrada API con ese project ID
            var apiExistente = _listaApisDisponibles
                .FirstOrDefault(a => a.key == projectId || a.Nombre == projectId);

            if (apiExistente != null)
                comboBoxApiAntigravity.SelectedValue = apiExistente.key;
            else
                await SeleccionarApi(Servicios.Antigravity, projectId);
        }

        #endregion

        #region UI — Visual y tema

        /// <summary>
        /// Habilita o deshabilita todos los paneles de configuración de agentes.
        /// Se usa para bloquear la UI durante operaciones asíncronas de carga.
        /// </summary>
        /// <param name="estado"><c>true</c> para habilitar; <c>false</c> para deshabilitar.</param>
        private void EstadoPanels(bool estado)
        {
            pnllChat.Enabled       = estado;
            pnlGem.Enabled         = estado;
            pnlDeesp.Enabled       = estado;
            pnlClau.Enabled        = estado;
            pnlOllla.Enabled       = estado;
            pnlOpenroute.Enabled   = estado;
            pnlAntigravity.Enabled = estado;
        }

        // ── Paleta Emerald (consistente con FrmPrincipal / FrmApis / FrmAutomatizaciones) ──
        private static readonly Color BgDeep    = ColorTranslator.FromHtml("#050505");
        private static readonly Color BgSurface = ColorTranslator.FromHtml("#0a0a0a");
        private static readonly Color BgCard    = ColorTranslator.FromHtml("#0f1117");
        private static readonly Color BgCardHi  = ColorTranslator.FromHtml("#141826");
        private static readonly Color BgInput   = ColorTranslator.FromHtml("#0b0e16");
        private static readonly Color Emerald   = ColorTranslator.FromHtml("#10b981");
        private static readonly Color Emerald4  = ColorTranslator.FromHtml("#34d399");
        private static readonly Color Emerald9  = ColorTranslator.FromHtml("#064e3b");
        private static readonly Color TextMain  = ColorTranslator.FromHtml("#f0fdf4");
        private static readonly Color TextSub   = ColorTranslator.FromHtml("#a7f3d0");
        private static readonly Color BorderCol = ColorTranslator.FromHtml("#1f2937");

        /// <summary>
        /// Aplica la paleta Emerald al formulario completo: fondo, paneles, botones,
        /// combos, checks y labels. Mantiene los bordes redondeados de los paneles
        /// pero con acento esmeralda en lugar del azul previo.
        /// </summary>
        private void AplicarThema()
        {
            BackColor = BgDeep;
            ForeColor = TextMain;
            Font      = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            var paneles = new[]
            {
                pnlClau, pnlGem, pnlOllla,
                pnlDeesp, pnllChat, pnlOpenroute, pnlAntigravity
            };

            foreach (var panel in paneles)
            {
                panel.BackColor = BgCard;
                panel.RedondearPanel(
                    borderRadius: 18,
                    borderColor:  Emerald,
                    borderSize:   1,
                    agregarSombra: true
                );
            }

            RecolorearArbol(this);
        }

        private void RecolorearArbol(Control raiz)
        {
            foreach (Control c in raiz.Controls)
            {
                switch (c)
                {
                    case Button btn:
                        EstilizarBoton(btn);
                        break;

                    case CheckBox chk:
                        chk.ForeColor = TextSub;
                        chk.BackColor = Color.Transparent;
                        chk.FlatStyle = FlatStyle.Flat;
                        chk.Cursor    = Cursors.Hand;
                        break;

                    case Label lbl:
                        // Respeta colores semánticos ya asignados (status verde/ámbar/rojo)
                        if (EsColorSemantico(lbl.ForeColor)) break;
                        lbl.BackColor = Color.Transparent;
                        lbl.ForeColor = EsTitulo(lbl) ? TextMain : TextSub;
                        break;

                    case TextBox tb:
                        tb.BackColor   = BgInput;
                        tb.ForeColor   = TextMain;
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case ComboBox cmb:
                        cmb.BackColor     = BgInput;
                        cmb.ForeColor     = TextMain;
                        cmb.FlatStyle     = FlatStyle.Flat;
                        break;

                    case Panel pnl when !EsPanelTema(pnl):
                        pnl.BackColor = BgDeep;
                        break;
                }

                if (c.HasChildren) RecolorearArbol(c);
            }
        }

        private void EstilizarBoton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize        = 1;
            btn.FlatAppearance.BorderColor       = Emerald;
            btn.FlatAppearance.MouseOverBackColor = Emerald;
            btn.FlatAppearance.MouseDownBackColor = Emerald4;
            btn.BackColor = Emerald9;
            btn.ForeColor = TextMain;
            btn.Cursor    = Cursors.Hand;
            btn.Font      = new Font("Segoe UI Semibold", 9F);
        }

        private static bool EsTitulo(Label lbl)
        {
            return lbl.Font != null && (lbl.Font.Bold || lbl.Font.Size >= 11F);
        }

        private static bool EsColorSemantico(Color c)
        {
            // Verde 22,197,94 / Ámbar 245,158,11 / Rojo 239,68,68 / Slate 100,116,139
            return (c.R == 34  && c.G == 197 && c.B == 94)
                || (c.R == 245 && c.G == 158 && c.B == 11)
                || (c.R == 239 && c.G == 68  && c.B == 68)
                || (c.R == 100 && c.G == 116 && c.B == 139);
        }

        private bool EsPanelTema(Panel pnl)
        {
            return pnl == pnlClau || pnl == pnlGem || pnl == pnlOllla
                || pnl == pnlDeesp || pnl == pnllChat || pnl == pnlOpenroute
                || pnl == pnlAntigravity;
        }

        #endregion
    }
}