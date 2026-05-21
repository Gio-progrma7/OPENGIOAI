using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmModelos : Form
    {
        #region Campos privados

        private List<Modelo>    _listaAgentes          = new();
        private List<Api>       _listaApisDisponibles  = new();
        private List<ComboBox>  _listaApis             = new();
        private List<ComboBox>  _listaModels           = new();
        private List<CheckBox>  _listaEstados          = new();
        private List<Servicios> _listaServicios        = new();
        private bool            _cargandoControles     = false;

        private string _antigravityProjectId = "";
        private TextBox? txtAntigravityProjectId;
        private Label? lblAntigravityProjectId;

        // CancellationToken para el flujo OAuth (cancelable si el usuario cierra el form)
        private CancellationTokenSource? _oauthCts;

        #endregion

        #region Constructor e inicialización

        public FrmModelos()
        {
            InitializeComponent();
            InicializarControlesProyectoId();
            AplicarThema();

            EmeraldTheme.ThemeChanged += OnTemaChanged;
            Disposed += (_, __) =>
            {
                EmeraldTheme.ThemeChanged -= OnTemaChanged;
                _oauthCts?.Cancel();
                _oauthCts?.Dispose();
            };
        }

        private void InicializarControlesProyectoId()
        {
            lblAntigravityProjectId = new Label
            {
                Text = "PROJECT ID (GCP)",
                Location = new Point(19, 184),
                Size = new Size(120, 15),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Visible = false
            };

            txtAntigravityProjectId = new TextBox
            {
                Location = new Point(19, 201),
                Size = new Size(171, 23),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "my-gcp-project-123",
                Visible = false
            };

            pnlAntigravity.Controls.Add(lblAntigravityProjectId);
            pnlAntigravity.Controls.Add(txtAntigravityProjectId);
        }

        private void OnTemaChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(OnTemaChanged); return; }
            AplicarThema();
            Invalidate(true);
        }

        private async void FrmModelos_Load(object sender, EventArgs e)
        {
            await InicializarDatos();
        }

        private async Task InicializarDatos()
        {
            EstadoPanels(false);

            // El comboBox de API de Antigravity no se usa: el acceso es por OAuth/gcloud
            comboBoxApiAntigravity.Visible = false;
            labelAntigravityApiKey.Visible = false;

            await CargarDatos();
            CargarControles();
            await CargarInfoAgentes();
            CargarConfigOAuth();

            EstadoPanels(true);

            _ = VerificarEstadoAntigravityAsync();
        }

        #endregion

        #region Carga de datos y controles

        private async Task CargarDatos()
        {
            _listaServicios       = Enum.GetValues<Servicios>().ToList();
            _listaAgentes         = JsonManager.Leer<Modelo>(RutasProyecto.ObtenerRutaListModelos());
            _listaApisDisponibles = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());

            if (_listaAgentes.Count != _listaServicios.Count)
                GuardarPrimeraVez();

            await Task.CompletedTask;
        }

        private void CargarControles()
        {
            _cargandoControles = true;
            try
            {
                _listaApis = new()
                {
                    comboBoxApiChat,       // [0] ChatGpt
                    comboBoxApiClau,       // [1] Claude
                    comboBoxApiGem,        // [2] Gemenni
                    comboBoxApiOlla,       // [3] Ollama
                    comboBoxApiDesp,       // [4] Deespeek
                    ComboxApiOpenroute,    // [5] OpenRouter
                    comboBoxApiAntigravity // [6] Antigravity (oculto)
                };

                _listaModels = new()
                {
                    comboBoxMChat,         // [0] ChatGpt
                    comboBoxMClau,         // [1] Claude
                    comboBoxMGem,          // [2] Gemenni
                    comboBoxmOlla,         // [3] Ollama
                    comboBoxMDesp,         // [4] Deespeek
                    ComboxMOpenroute,      // [5] OpenRouter
                    comboBoxMAntigravity   // [6] Antigravity
                };

                _listaEstados = new()
                {
                    checkBoxChat,          // [0] ChatGpt
                    checkBoxClua,          // [1] Claude
                    checkBoxGem,           // [2] Gemenni
                    checkBoxOlla,          // [3] Ollama
                    checkBoxDeesp,         // [4] Deespeek
                    checkBoxOpenroute,     // [5] OpenRouter
                    checkBoxAntigravity    // [6] Antigravity
                };

                for (int i = 0; i < _listaApis.Count; i++)
                {
                    _listaApis[i].DataSource    = new List<Api>(_listaApisDisponibles);
                    _listaApis[i].DisplayMember = "Nombre";
                    _listaApis[i].ValueMember   = "key";
                    _listaApis[i].SelectedValue = _listaAgentes[i].ApiKey;
                }
            }
            finally
            {
                _cargandoControles = false;
            }
        }

        private async Task CargarInfoAgentes()
        {
            for (int i = 0; i < _listaAgentes.Count; i++)
            {
                _listaEstados[i].Checked = _listaAgentes[i].Estado;

                string modeloGuardado = _listaAgentes[i].Modelos;

                await SeleccionarApi(_listaAgentes[i].Agente, _listaAgentes[i].ApiKey);

                _listaModels[i].Text = modeloGuardado;
            }
        }

        /// <summary>
        /// Carga los valores de Client ID / Client Secret / Service Account
        /// desde AntigravityOAuthService y los muestra en los controles.
        /// </summary>
        private void CargarConfigOAuth()
        {
            var cfg = AntigravityOAuthService.Config;

            txtClientId.Text      = cfg.ClientId;
            txtClientSecret.Text  = cfg.ClientSecret;
            txtSvcAccountPath.Text = cfg.ServiceAccountPath;
            if (txtAntigravityProjectId != null)
                txtAntigravityProjectId.Text = cfg.ProjectId;

            // Seleccionar el modo guardado en el combo
            int idx = cfg.Modo switch
            {
                "oauth"           => 0,
                "service_account" => 1,
                _                 => 2   // "gcloud"
            };
            cmbAuthModeAntigravity.SelectedIndex = idx;
            // Mostrar/ocultar campos según modo
            ActualizarVisibilidadCamposOAuth(idx);
        }

        #endregion

        #region Lógica de agentes y servicios

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

            _listaModels[index].DataSource    = modelos;
            _listaModels[index].DisplayMember = "Nombre";
            _listaModels[index].ValueMember   = "Estado";
        }

        private void GuardarPrimeraVez()
        {
            _listaAgentes.Clear();

            foreach (var servicio in _listaServicios)
            {
                _listaAgentes.Add(new Modelo
                {
                    Agente  = servicio,
                    Estado  = false,
                    ApiKey  = string.Empty,
                    Modelos = string.Empty
                });
            }

            JsonManager.Guardar(RutasProyecto.ObtenerRutaListModelos(), _listaAgentes);
        }

        private void ModificarAgente(Modelo nuevo)
        {
            JsonManager.Modificar<Modelo>(
                RutasProyecto.ObtenerRutaListModelos(),
                u => u.Agente == nuevo.Agente,
                u =>
                {
                    u.ApiKey  = nuevo.ApiKey;
                    u.Estado  = nuevo.Estado;
                    u.Modelos = nuevo.Modelos;
                });
        }

        private void InicializarAgente(Servicios servicio)
        {
            int index = (int)servicio - 1;

            string apiKey;

            if (servicio == Servicios.Antigravity)
            {
                apiKey = _antigravityProjectId;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show(
                        "No se detectó un Project ID de GCP activo.\n\n" +
                        "Pasos según el modo seleccionado:\n" +
                        "• OAuth 2.0: introduce Client ID + Secret y haz clic en 'Conectar'\n" +
                        "• Service Account: selecciona el archivo JSON\n" +
                        "• gcloud ADC: ejecuta 'gcloud auth application-default login'\n\n" +
                        "Además configura: gcloud config set project TU-PROYECTO",
                        "Project ID requerido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                Api? apiSeleccionada = _listaApis[index].SelectedItem as Api;
                apiKey = apiSeleccionada?.key ?? string.Empty;
            }

            Modelo modeloEditado = new()
            {
                Agente  = servicio,
                Estado  = _listaEstados[index].Checked,
                ApiKey  = apiKey,
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

        #region UI — Eventos de botones guardar

        private void btnGuardarChat_Click(object sender, EventArgs e)      => InicializarAgente(Servicios.ChatGpt);
        private void btnGuardaGem_Click(object sender, EventArgs e)        => InicializarAgente(Servicios.Gemenni);
        private void btnGuardarOlla_Click(object sender, EventArgs e)      => InicializarAgente(Servicios.Ollama);
        private void btnGuardarClau_Click(object sender, EventArgs e)      => InicializarAgente(Servicios.Claude);
        private void btnGuardarDeesp_Click(object sender, EventArgs e)     => InicializarAgente(Servicios.Deespeek);
        private void btnOpenroute_Click(object sender, EventArgs e)        => InicializarAgente(Servicios.OpenRouter);
        private void btnGuardarAntigravity_Click(object sender, EventArgs e) => InicializarAgente(Servicios.Antigravity);

        #endregion

        #region Antigravity — Autenticación (OAuth / Service Account / gcloud)

        /// <summary>
        /// Botón principal de conexión. Su comportamiento cambia según el modo seleccionado:
        ///   - OAuth 2.0       → abre browser para auth Google
        ///   - Service Account → ya se configuró via "…", solo verifica
        ///   - gcloud ADC      → abre terminal con el comando gcloud
        /// </summary>
        private async void btnAutenticarAntigravity_Click(object sender, EventArgs e)
        {
            btnAutenticarAntigravity.Enabled = false;
            SetStatus(Color.FromArgb(245, 158, 11), "⬤  Conectando...");

            _oauthCts?.Cancel();
            _oauthCts = new CancellationTokenSource();

            try
            {
                int modo = cmbAuthModeAntigravity.SelectedIndex;

                switch (modo)
                {
                    case 0: // OAuth 2.0
                        await AutenticarOAuthAsync(_oauthCts.Token);
                        break;

                    case 1: // Service Account JSON
                        await VerificarServiceAccountAsync();
                        break;

                    default: // gcloud ADC
                        await AutenticarGcloudAsync();
                        break;
                }

                await VerificarEstadoAntigravityAsync();
            }
            catch (OperationCanceledException)
            {
                SetStatus(Color.FromArgb(100, 116, 139), "⬤  Cancelado");
            }
            catch (Exception ex)
            {
                SetStatus(Color.FromArgb(239, 68, 68), $"⬤  Error: {ex.Message}");
            }
            finally
            {
                btnAutenticarAntigravity.Enabled = true;
            }
        }

        private async Task AutenticarOAuthAsync(CancellationToken ct)
        {
            string clientId     = txtClientId.Text.Trim();
            string clientSecret = txtClientSecret.Text.Trim();
            string projectId    = txtAntigravityProjectId?.Text.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(projectId))
            {
                MessageBox.Show(
                    "Introduce el Client ID, el Client Secret y el Project ID de tu aplicación OAuth en Google Cloud Console.\n\n" +
                    "Pasos:\n" +
                    "1. Abre console.cloud.google.com → APIs & Services → Credentials\n" +
                    "2. Crea un OAuth 2.0 Client ID del tipo 'Desktop application'\n" +
                    "3. Copia el Client ID, el Client Secret y el Project ID aquí",
                    "Faltan credenciales OAuth o Project ID",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Persistir Project ID antes del guardado de config
            AntigravityOAuthService.Config.ProjectId = projectId;

            SetStatus(Color.FromArgb(245, 158, 11), "⬤  Abriendo browser...");

            var (ok, msg) = await AntigravityOAuthService.ConectarOAuthAsync(
                clientId, clientSecret, ct);

            SetStatus(
                ok ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68),
                $"⬤  {msg}");
        }

        private async Task VerificarServiceAccountAsync()
        {
            string path = txtSvcAccountPath.Text.Trim();

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                MessageBox.Show(
                    "Selecciona un archivo Service Account JSON válido haciendo clic en '…'.",
                    "Archivo requerido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SetStatus(Color.FromArgb(245, 158, 11), "⬤  Verificando Service Account...");

            var (ok, msg) = await AntigravityOAuthService.ConectarServiceAccountAsync(path);

            SetStatus(
                ok ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68),
                $"⬤  {msg}");
        }

        private async Task AutenticarGcloudAsync()
        {
            string gcloudExe = AIServicios.EncontrarGcloudExe();

            string cmdArgs;
            if (!string.IsNullOrEmpty(gcloudExe))
            {
                cmdArgs = $"/k \"{gcloudExe}\" auth application-default login";
                SetStatus(Color.FromArgb(245, 158, 11), "⬤  Abriendo terminal gcloud...");
            }
            else
            {
                cmdArgs = "/k gcloud auth application-default login";
                SetStatus(Color.FromArgb(245, 158, 11), "⬤  gcloud no encontrado en rutas estándar");
                MessageBox.Show(
                    "No se encontró gcloud en las rutas de instalación estándar.\n\n" +
                    "Descárgalo desde: https://cloud.google.com/sdk/docs/install\n" +
                    "O usa el modo OAuth 2.0 (no requiere gcloud).",
                    "Google Cloud SDK no encontrado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "cmd.exe",
                Arguments       = cmdArgs,
                UseShellExecute = true
            });

            SetStatus(Color.FromArgb(245, 158, 11), "⬤  Esperando autorización en el browser...");
            await Task.Delay(12000);
        }

        private async void btnBrowseSvcAccount_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Selecciona el Service Account JSON",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            txtSvcAccountPath.Text = dlg.FileName;
            SetStatus(Color.FromArgb(245, 158, 11), "⬤  Verificando Service Account...");

            var (ok, msg) = await AntigravityOAuthService.ConectarServiceAccountAsync(dlg.FileName);

            SetStatus(
                ok ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68),
                $"⬤  {msg}");

            if (ok) await VerificarEstadoAntigravityAsync();
        }

        private void cmbAuthModeAntigravity_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbAuthModeAntigravity.SelectedIndex;
            ActualizarVisibilidadCamposOAuth(idx);
            ActualizarTextoBtnAutenticar(idx);
        }

        private void ActualizarVisibilidadCamposOAuth(int modoIdx)
        {
            bool esOAuth = modoIdx == 0;
            bool esSvc   = modoIdx == 1;

            lblClientId.Visible         = esOAuth;
            txtClientId.Visible         = esOAuth;
            lblClientSecret.Visible     = esOAuth;
            txtClientSecret.Visible     = esOAuth;

            if (lblAntigravityProjectId != null) lblAntigravityProjectId.Visible = esOAuth;
            if (txtAntigravityProjectId != null) txtAntigravityProjectId.Visible = esOAuth;

            lblSvcAccountPath.Visible   = esSvc;
            txtSvcAccountPath.Visible   = esSvc;
            btnBrowseSvcAccount.Visible = esSvc;
        }

        private void ActualizarTextoBtnAutenticar(int modoIdx)
        {
            btnAutenticarAntigravity.Text = modoIdx switch
            {
                0 => "🔑  Conectar con Google (OAuth)",
                1 => "✅  Verificar Service Account",
                _ => "🔑  Autenticar gcloud ADC"
            };
        }

        #endregion

        #region Antigravity — Estado / diagnóstico

        private async Task VerificarEstadoAntigravityAsync()
        {
            SetStatus(Color.FromArgb(245, 158, 11), "⬤  Verificando...");

            try
            {
                var (tokenOk, projectOk, projectId, mensaje, modo) =
                    await AntigravityOAuthService.DiagnosticarAsync();

                void Aplicar()
                {
                    if (!tokenOk)
                    {
                        SetStatus(Color.FromArgb(239, 68, 68), $"⬤  {mensaje}");
                        return;
                    }

                    if (!projectOk)
                    {
                        SetStatus(Color.FromArgb(245, 158, 11), $"⬤  {mensaje}");
                        _ = SeleccionarApi(Servicios.Antigravity, "");
                        return;
                    }

                    _antigravityProjectId = projectId;
                    SetStatus(Color.FromArgb(34, 197, 94), $"⬤  {mensaje}");
                    _ = SeleccionarApi(Servicios.Antigravity, projectId);

                    // Sincronizar el combo de modo con el modo detectado
                    int idx = modo switch
                    {
                        "oauth"           => 0,
                        "service_account" => 1,
                        _                 => 2
                    };
                    if (cmbAuthModeAntigravity.SelectedIndex != idx)
                    {
                        cmbAuthModeAntigravity.SelectedIndex = idx;
                        ActualizarVisibilidadCamposOAuth(idx);
                    }
                }

                if (InvokeRequired) Invoke(Aplicar);
                else                Aplicar();
            }
            catch (Exception ex)
            {
                SetStatus(Color.FromArgb(239, 68, 68), $"⬤  Error: {ex.Message}");
            }
        }

        private void SetStatus(Color color, string texto)
        {
            void Aplicar()
            {
                if (IsDisposed) return;
                lblGcloudStatus.ForeColor = color;
                lblGcloudStatus.Text      = texto;
            }
            if (InvokeRequired) Invoke(Aplicar);
            else                Aplicar();
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

        #region UI — Visual y tema

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

        private static Color BgDeep   => EmeraldTheme.BgDeep;
        private static Color BgCard   => EmeraldTheme.BgCard;
        private static Color BgInput  => EmeraldTheme.BgDeep;
        private static Color Emerald  => EmeraldTheme.Emerald500;
        private static Color Emerald4 => EmeraldTheme.Emerald400;
        private static Color Emerald9 => EmeraldTheme.Emerald900;
        private static Color TextMain => EmeraldTheme.TextPrimary;
        private static Color TextSub  => EmeraldTheme.TextSecondary;

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
                        cmb.BackColor = BgInput;
                        cmb.ForeColor = TextMain;
                        cmb.FlatStyle = FlatStyle.Flat;
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
            btn.FlatAppearance.BorderSize         = 1;
            btn.FlatAppearance.BorderColor        = Emerald;
            btn.FlatAppearance.MouseOverBackColor = Emerald;
            btn.FlatAppearance.MouseDownBackColor = Emerald4;
            btn.BackColor = Emerald9;
            btn.ForeColor = TextMain;
            btn.Cursor    = Cursors.Hand;
            btn.Font      = new Font("Segoe UI Semibold", 9F);
        }

        private static bool EsTitulo(Label lbl) =>
            lbl.Font != null && (lbl.Font.Bold || lbl.Font.Size >= 11F);

        private static bool EsColorSemantico(Color c) =>
            (c.R == 34  && c.G == 197 && c.B == 94)
         || (c.R == 245 && c.G == 158 && c.B == 11)
         || (c.R == 239 && c.G == 68  && c.B == 68)
         || (c.R == 100 && c.G == 116 && c.B == 139);

        private bool EsPanelTema(Panel pnl) =>
            pnl == pnlClau || pnl == pnlGem || pnl == pnlOllla
         || pnl == pnlDeesp || pnl == pnllChat || pnl == pnlOpenroute
         || pnl == pnlAntigravity;

        #endregion
    }
}
