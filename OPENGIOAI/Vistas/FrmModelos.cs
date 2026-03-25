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

            await CargarDatos();
            CargarControles();
            await CargarInfoAgentes();

            EstadoPanels(true);
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
                    comboBoxApiChat,
                    comboBoxApiClau,
                    comboBoxApiGem,
                    comboBoxApiOlla,
                    comboBoxApiDesp,
                    ComboxApiOpenroute
                };

                _listaModels = new()
                {
                    comboBoxMChat,
                    comboBoxMClau,
                    comboBoxMGem,
                    comboBoxmOlla,
                    comboBoxMDesp,
                    ComboxMOpenroute
                };

                _listaEstados = new()
                {
                    checkBoxChat,
                    checkBoxClua,
                    checkBoxGem,
                    checkBoxOlla,
                    checkBoxDeesp,
                    checkBoxOpenroute
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
                Servicios.ChatGpt => await AIServicios.ObtenerModelosOpenAIAsync(apiKey),
                Servicios.Gemenni => await AIServicios.ObtenerModelosGeminiAsync(apiKey),
                Servicios.Ollama => await AIServicios.ObtenerModelosOllamaApiAsync(),
                Servicios.OpenRouter => await AIServicios.ObtenerModelosOpenRouterAsync(apiKey),
                Servicios.Claude => await AIServicios.ObtenerModelosClaudeAsync(apiKey),
                Servicios.Deespeek => await AIServicios.ObtenerModelosDeepSeekAsync(apiKey),
                _ => new List<string>()
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

            Api apiSeleccionada = _listaApis[index].SelectedItem as Api;

            Modelo modeloEditado = new()
            {
                Agente = servicio,
                Estado = _listaEstados[index].Checked,
                ApiKey = apiSeleccionada?.key ?? string.Empty,
                Modelos = _listaModels[index].Text
            };

            ModificarAgente(modeloEditado);

            MessageBox.Show(
                "Configuración guardada correctamente.",
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

        #endregion

        #region UI — Visual y tema

        /// <summary>
        /// Habilita o deshabilita todos los paneles de configuración de agentes.
        /// Se usa para bloquear la UI durante operaciones asíncronas de carga.
        /// </summary>
        /// <param name="estado"><c>true</c> para habilitar; <c>false</c> para deshabilitar.</param>
        private void EstadoPanels(bool estado)
        {
            pnllChat.Enabled = estado;
            pnlGem.Enabled = estado;
            pnlDeesp.Enabled = estado;
            pnlClau.Enabled = estado;
            pnlOllla.Enabled = estado;
            pnlOpenroute.Enabled = estado; // BUG FIX: faltaba este panel
        }

        /// <summary>
        /// Aplica el tema visual unificado a todos los paneles del formulario:
        /// bordes redondeados, color de acento y sombra exterior.
        /// </summary>
        private void AplicarThema()
        {
            var paneles = new[]
            {
                pnlClau, pnlGem, pnlOllla,
                pnlDeesp, pnllChat, pnlOpenroute
            };

            foreach (var panel in paneles)
            {
                panel.RedondearPanel(
                    borderRadius: 20,
                    borderColor: Color.FromArgb(64, 158, 255),
                    borderSize: 2,
                    agregarSombra: true
                );
            }
        }

        #endregion
    }
}