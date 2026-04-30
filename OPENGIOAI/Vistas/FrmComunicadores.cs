using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosTTS;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmComunicadores : Form
    {
        // ── Ruta de trabajo (igual que FrmMandos usa _archivoSeleccionado.Ruta) ─
        private readonly string _rutaBase;

        // ── Estado de existencia de archivos ─────────────────────────────────────
        private bool _slackExiste    = false;
        private bool _telegramExiste = false;

        // ── Configuración TTS ────────────────────────────────────────────────────
        private ConfiguracionTTS _configTTS = new();

        // ── Estado edición Slack ─────────────────────────────────────────────────
        private List<SlackChat> _listaSlack = new();
        private bool   _esNuevaSlack        = true;
        private string _tokenOriginalSlack  = "";
        private string _canalOriginalSlack  = "";

        // ── Estado edición Telegram ──────────────────────────────────────────────
        private List<TelegramChat> _listaTelegram  = new();
        private bool _esNuevaTelegram              = true;
        private long _chatIdOriginalTelegram        = 0;

        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { WriteIndented = true };

        // ── Templates de estructura vacía ────────────────────────────────────────
        private static readonly string _templateSlack =
            JsonSerializer.Serialize(
                new List<SlackChat>
                {
                    new SlackChat { Tokem = "", IDcanal = "", usuarios = new List<string>() }
                }, _jsonOpts);

        private static readonly string _templateTelegram =
            JsonSerializer.Serialize(
                new List<TelegramChat>
                {
                    new TelegramChat { ChatId = 0, Apikey = "" }
                }, _jsonOpts);

        // ─────────────────────────────────────────────────────────────────────────
        public FrmComunicadores(ConfiguracionClient config)
        {
            InitializeComponent();

            // Misma lógica de ruta que FrmMandos
            _rutaBase = config?.MiArchivo?.Ruta ?? "";
            if (string.IsNullOrWhiteSpace(_rutaBase))
                _rutaBase = RutasProyecto.ObtenerRutaScripts();

            lblSubtitulo.Text = $"Ruta de trabajo:  {_rutaBase}";

            pnlListaSlack.FlowDirection    = FlowDirection.LeftToRight;
            pnlListaSlack.WrapContents     = true;
            pnlListaSlack.AutoScroll       = true;
            pnlListaSlack.Padding          = new Padding(8);

            pnlListaTelegram.FlowDirection = FlowDirection.LeftToRight;
            pnlListaTelegram.WrapContents  = true;
            pnlListaTelegram.AutoScroll    = true;
            pnlListaTelegram.Padding       = new Padding(8);

            tabControl.SelectedIndexChanged += (s, e) => RefrescarTabActual();
            this.Resize                     += (s, e) => RefrescarTabActual();

            AplicarTema();

            EmeraldTheme.ThemeChanged += OnTemaChanged;
            Disposed += (_, __) => EmeraldTheme.ThemeChanged -= OnTemaChanged;
        }

        private void OnTemaChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(OnTemaChanged); return; }
            AplicarTema();
            Invalidate(true);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void FrmComunicadores_Load(object sender, EventArgs e)
        {
            string rutaSlack    = RutasProyecto.ObtenerRutaSlack(_rutaBase);
            string rutaTelegram = RutasProyecto.ObtenerRutaListTelegram(_rutaBase);

            _slackExiste    = File.Exists(rutaSlack);
            _telegramExiste = File.Exists(rutaTelegram);

            // Solo leer si el archivo ya existe — no lo creamos aquí
            _listaSlack    = _slackExiste    ? JsonManager.Leer<SlackChat>(rutaSlack)       : new();
            _listaTelegram = _telegramExiste ? JsonManager.Leer<TelegramChat>(rutaTelegram) : new();

            // TTS: cargar configuración guardada
            _configTTS = Utils.LeerConfig<ConfiguracionTTS>(RutasProyecto.ObtenerRutaConfiguracionTTS());
            CargarFormularioTTS();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RenderizarSlack();
            RenderizarTelegram();
            ActualizarConteos();
        }

        private void RefrescarTabActual()
        {
            if (tabControl.SelectedTab == tabSlack)
                RenderizarSlack();
            else if (tabControl.SelectedTab == tabTelegram)
                RenderizarTelegram();
            // tabAudio no necesita re-render — su formulario siempre está visible
        }

        private void ActualizarConteos()
        {
            lblConteoSlack.Text = _slackExiste
                ? $"Slack  ·  {_listaSlack.Count} configuración(es)"
                : "Slack  ·  archivo no creado";

            lblConteoTelegram.Text = _telegramExiste
                ? $"Telegram  ·  {_listaTelegram.Count} configuración(es)"
                : "Telegram  ·  archivo no creado";
        }

        // ══════════════════════════════════════════════════════════════════════════
        // TOGGLE FORMULARIOS
        // ══════════════════════════════════════════════════════════════════════════

        private void ToggleFormSlack(bool? forzar = null)
        {
            bool mostrar = forzar ?? !pnlFormSlack.Visible;
            pnlFormSlack.Visible = mostrar;

            if (!_slackExiste)
            {
                // Mientras el archivo no exista, el botón siempre dice "Crear archivo"
                btnToggleSlack.Text = "💾  Crear archivo";
                btnToggleSlack.FlatAppearance.BorderColor = Color.FromArgb(74, 181, 130);
                btnToggleSlack.ForeColor                  = Color.FromArgb(74, 181, 130);
            }
            else
            {
                btnToggleSlack.Text = mostrar ? "✖  Cerrar" : "➕  Nueva";
                btnToggleSlack.FlatAppearance.BorderColor =
                    mostrar ? Color.DarkRed : Color.FromArgb(74, 181, 130);
                btnToggleSlack.ForeColor =
                    mostrar ? Color.DarkRed : Color.FromArgb(74, 181, 130);
            }

            if (!mostrar) LimpiarFormSlack();
        }

        private void ToggleFormTelegram(bool? forzar = null)
        {
            bool mostrar = forzar ?? !pnlFormTelegram.Visible;
            pnlFormTelegram.Visible = mostrar;

            if (!_telegramExiste)
            {
                btnToggleTelegram.Text = "💾  Crear archivo";
                btnToggleTelegram.FlatAppearance.BorderColor = Color.FromArgb(41, 182, 246);
                btnToggleTelegram.ForeColor                   = Color.FromArgb(41, 182, 246);
            }
            else
            {
                btnToggleTelegram.Text = mostrar ? "✖  Cerrar" : "➕  Nueva";
                btnToggleTelegram.FlatAppearance.BorderColor =
                    mostrar ? Color.DarkRed : Color.FromArgb(41, 182, 246);
                btnToggleTelegram.ForeColor =
                    mostrar ? Color.DarkRed : Color.FromArgb(41, 182, 246);
            }

            if (!mostrar) LimpiarFormTelegram();
        }

        // ══════════════════════════════════════════════════════════════════════════
        // SLACK — lógica
        // ══════════════════════════════════════════════════════════════════════════

        private void CargarSlack()
        {
            string ruta  = RutasProyecto.ObtenerRutaSlack(_rutaBase);
            _slackExiste = File.Exists(ruta);
            _listaSlack  = _slackExiste ? JsonManager.Leer<SlackChat>(ruta) : new();
            RenderizarSlack();
            ActualizarConteos();
        }

        private void RenderizarSlack()
        {
            int ancho = Math.Max(300, pnlListaSlack.ClientSize.Width);

            pnlListaSlack.SuspendLayout();
            pnlListaSlack.Controls.Clear();

            if (!_slackExiste)
            {
                // Mostrar estructura vacía + abrir formulario automáticamente
                pnlListaSlack.Controls.Add(CrearPanelEstructura(
                    "ListSlack.json",
                    _templateSlack,
                    "Token de acceso (xoxb-...),  ID del canal (C0AJ...),  IDs de usuarios separados por coma.",
                    Color.FromArgb(74, 181, 130)));

                ToggleFormSlack(true);  // abrir form si no estaba abierto
                btnGuardarSlack.Text = "💾 Crear archivo";
            }
            else
            {
                // Actualizar texto del botón guardar al modo normal
                if (_esNuevaSlack) btnGuardarSlack.Text = "Agregar";

                for (int i = 0; i < _listaSlack.Count; i++)
                    pnlListaSlack.Controls.Add(CrearTarjetaSlack(_listaSlack[i], i, ancho));

                if (_listaSlack.Count == 0)
                    pnlListaSlack.Controls.Add(CrearPanelVacio(
                        "El archivo existe pero está vacío.\nUsa ➕ Nueva para agregar una configuración."));
            }

            pnlListaSlack.ResumeLayout(true);
            // Pasar el estado actual para solo actualizar apariencia del botón sin cambiar visibilidad
            ToggleFormSlack(pnlFormSlack.Visible);
        }

        private void GuardarSlack()
        {
            string token = txtSlackToken.Text.Trim();
            string canal = txtSlackCanal.Text.Trim();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(canal))
            {
                MessageBox.Show("El Token y el ID de Canal son obligatorios.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usuarios = txtSlackUsuarios.Text
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim()).Where(u => u.Length > 0).ToList();

            string ruta = RutasProyecto.ObtenerRutaSlack(_rutaBase);

            if (_esNuevaSlack)
            {
                // Agregar crea el archivo si no existe
                JsonManager.Agregar(ruta, new SlackChat
                    { Tokem = token, IDcanal = canal, usuarios = usuarios });
            }
            else
            {
                JsonManager.Modificar<SlackChat>(ruta,
                    s => s.Tokem == _tokenOriginalSlack && s.IDcanal == _canalOriginalSlack,
                    s => { s.Tokem = token; s.IDcanal = canal; s.usuarios = usuarios; });
            }

            LimpiarFormSlack();
            ToggleFormSlack(false);
            CargarSlack();  // recarga y detecta que el archivo ya existe
        }

        private void ActivarSlack(SlackChat item)
        {
            int idx = _listaSlack.FindIndex(s => s.Tokem == item.Tokem && s.IDcanal == item.IDcanal);
            if (idx <= 0) return;
            _listaSlack.RemoveAt(idx);
            _listaSlack.Insert(0, item);
            JsonManager.Guardar(RutasProyecto.ObtenerRutaSlack(_rutaBase), _listaSlack);
            RenderizarSlack();
        }

        private void EliminarSlack(SlackChat item)
        {
            if (MessageBox.Show($"¿Eliminar la conexión Slack · Canal {item.IDcanal}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            JsonManager.Eliminar<SlackChat>(RutasProyecto.ObtenerRutaSlack(_rutaBase),
                s => s.Tokem == item.Tokem && s.IDcanal == item.IDcanal);
            CargarSlack();
        }

        private void EditarSlack(SlackChat item)
        {
            _tokenOriginalSlack   = item.Tokem;
            _canalOriginalSlack   = item.IDcanal;
            txtSlackToken.Text    = item.Tokem;
            txtSlackCanal.Text    = item.IDcanal;
            txtSlackUsuarios.Text = string.Join(", ", item.usuarios ?? new List<string>());
            _esNuevaSlack         = false;
            btnGuardarSlack.Text  = "Modificar";
            ToggleFormSlack(true);
        }

        private void LimpiarFormSlack()
        {
            txtSlackToken.Text    = "";
            txtSlackCanal.Text    = "";
            txtSlackUsuarios.Text = "";
            _esNuevaSlack         = true;
            _tokenOriginalSlack   = "";
            _canalOriginalSlack   = "";
            btnGuardarSlack.Text  = _slackExiste ? "Agregar" : "💾 Crear archivo";
        }

        // ── Tarjeta Slack ─────────────────────────────────────────────────────────
        private Panel CrearTarjetaSlack(SlackChat item, int indice, int anchoContenedor)
        {
            bool esActiva  = indice == 0;
            int  anchoCard = Math.Max(280, (anchoContenedor / 2) - 20);
            string jsonTxt = JsonSerializer.Serialize(item, _jsonOpts);

            PanelSuave card = new();
            card.Width     = anchoCard;
            card.Height    = 295;
            card.Margin    = new Padding(6);
            card.BackColor = esActiva ? Color.FromArgb(18, 38, 28) : Color.FromArgb(30, 41, 59);

            PanelSuave barra = new();
            barra.Size      = new Size(4, card.Height);
            barra.Location  = new Point(0, 0);
            barra.BackColor = esActiva ? Color.FromArgb(52, 211, 153) : Color.FromArgb(74, 181, 130);

            if (esActiva)
                card.Controls.Add(BuildBadge("⭐ ACTIVA", new Point(anchoCard - 82, 8),
                    Color.FromArgb(52, 211, 153), Color.FromArgb(0, 60, 35)));

            card.Controls.Add(BuildLabel("💬", new Point(12, 10), new Font("Segoe UI Emoji", 14)));
            card.Controls.Add(BuildChip("TOKEN", new Point(58, 10), Color.FromArgb(74, 181, 130), 50));

            var lblTok = BuildLabel(item.Tokem, new Point(114, 12), new Font("Consolas", 7.5f));
            lblTok.ForeColor = Color.FromArgb(80, 120, 100);
            lblTok.Width     = anchoCard - 126;
            lblTok.AutoEllipsis = true;
            card.Controls.Add(lblTok);

            var lblCanal = BuildLabel($"Canal:  {item.IDcanal}", new Point(58, 34),
                new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold));
            lblCanal.ForeColor = Color.White;
            lblCanal.Width     = anchoCard - 70;
            lblCanal.AutoEllipsis = true;
            card.Controls.Add(lblCanal);

            int cnt = item.usuarios?.Count ?? 0;
            string preview = cnt > 0
                ? string.Join(", ", item.usuarios!.Take(4)) + (cnt > 4 ? $"  +{cnt - 4}" : "")
                : "—";
            var lblUs = BuildLabel($"Usuarios ({cnt}):  {preview}", new Point(58, 56),
                new Font("Segoe UI", 8.5f));
            lblUs.ForeColor = Color.FromArgb(110, 130, 120);
            lblUs.Width     = anchoCard - 70;
            lblUs.Height    = 30;
            lblUs.AutoEllipsis = true;
            card.Controls.Add(lblUs);

            card.Controls.Add(BuildSep(new Point(10, 92), anchoCard));
            card.Controls.Add(BuildLabel("JSON", new Point(12, 97),
                new Font("Segoe UI", 7, FontStyle.Bold), Color.FromArgb(100, 116, 139)));
            card.Controls.Add(BuildJsonBox(jsonTxt, new Point(10, 113), new Size(anchoCard - 20, 100)));
            card.Controls.Add(BuildSep(new Point(10, 220), anchoCard));

            var btnEdit = BuildBoton("✏ Editar", new Point(12, 232), Color.DarkGreen);
            btnEdit.Click += (s, e) => EditarSlack(item);
            card.Controls.Add(btnEdit);

            var btnDel = BuildBoton("🗑 Borrar", new Point(101, 232), Color.DarkRed);
            btnDel.Click += (s, e) => EliminarSlack(item);
            card.Controls.Add(btnDel);

            if (!esActiva)
            {
                var btnAct = BuildBoton("⭐ Activar", new Point(anchoCard - 103, 232),
                    Color.FromArgb(50, 180, 100));
                btnAct.Click += (s, e) => ActivarSlack(item);
                card.Controls.Add(btnAct);
            }
            else
            {
                var lblAct = BuildLabel("✅ En uso", new Point(anchoCard - 100, 238),
                    new Font("Segoe UI", 8.5f, FontStyle.Bold), Color.FromArgb(52, 211, 153));
                card.Controls.Add(lblAct);
            }

            card.Controls.Add(barra);
            card.RedondearPanel(12, esActiva ? Color.FromArgb(52, 211, 100) : Color.FromArgb(200, 215, 240));
            card.MouseEnter += (s, e) => ((PanelSuave)s).BackColor =
                esActiva ? Color.FromArgb(18, 45, 32) : Color.FromArgb(30, 48, 60);
            card.MouseLeave += (s, e) => ((PanelSuave)s).BackColor =
                esActiva ? Color.FromArgb(18, 38, 28) : Color.FromArgb(30, 41, 59);

            return card;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // TELEGRAM — lógica
        // ══════════════════════════════════════════════════════════════════════════

        private void CargarTelegram()
        {
            string ruta     = RutasProyecto.ObtenerRutaListTelegram(_rutaBase);
            _telegramExiste = File.Exists(ruta);
            _listaTelegram  = _telegramExiste ? JsonManager.Leer<TelegramChat>(ruta) : new();
            RenderizarTelegram();
            ActualizarConteos();
        }

        private void RenderizarTelegram()
        {
            int ancho = Math.Max(300, pnlListaTelegram.ClientSize.Width);

            pnlListaTelegram.SuspendLayout();
            pnlListaTelegram.Controls.Clear();

            if (!_telegramExiste)
            {
                pnlListaTelegram.Controls.Add(CrearPanelEstructura(
                    "ListTelegram.json",
                    _templateTelegram,
                    "Chat ID (número entero)  y  API Key del bot de Telegram.",
                    Color.FromArgb(41, 182, 246)));

                ToggleFormTelegram(true);
                btnGuardarTelegram.Text = "💾 Crear archivo";
            }
            else
            {
                if (_esNuevaTelegram) btnGuardarTelegram.Text = "Agregar";

                for (int i = 0; i < _listaTelegram.Count; i++)
                    pnlListaTelegram.Controls.Add(CrearTarjetaTelegram(_listaTelegram[i], i, ancho));

                if (_listaTelegram.Count == 0)
                    pnlListaTelegram.Controls.Add(CrearPanelVacio(
                        "El archivo existe pero está vacío.\nUsa ➕ Nueva para agregar una configuración."));
            }

            pnlListaTelegram.ResumeLayout(true);
            // Pasar el estado actual para solo actualizar apariencia del botón sin cambiar visibilidad
            ToggleFormTelegram(pnlFormTelegram.Visible);
        }

        private void GuardarTelegram()
        {
            string chatIdStr = txtTelegramChatId.Text.Trim();
            string apikey    = txtTelegramApikey.Text.Trim();

            if (string.IsNullOrWhiteSpace(chatIdStr) || string.IsNullOrWhiteSpace(apikey))
            {
                MessageBox.Show("El Chat ID y la API Key son obligatorios.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!long.TryParse(chatIdStr, out long chatId))
            {
                MessageBox.Show("El Chat ID debe ser un número entero.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ruta = RutasProyecto.ObtenerRutaListTelegram(_rutaBase);

            if (_esNuevaTelegram)
                JsonManager.Agregar(ruta, new TelegramChat { ChatId = chatId, Apikey = apikey });
            else
                JsonManager.Modificar<TelegramChat>(ruta,
                    t => t.ChatId == _chatIdOriginalTelegram,
                    t => { t.ChatId = chatId; t.Apikey = apikey; });

            LimpiarFormTelegram();
            ToggleFormTelegram(false);
            CargarTelegram();
        }

        private void ActivarTelegram(TelegramChat item)
        {
            int idx = _listaTelegram.FindIndex(t => t.ChatId == item.ChatId && t.Apikey == item.Apikey);
            if (idx <= 0) return;
            _listaTelegram.RemoveAt(idx);
            _listaTelegram.Insert(0, item);
            JsonManager.Guardar(RutasProyecto.ObtenerRutaListTelegram(_rutaBase), _listaTelegram);
            RenderizarTelegram();
        }

        private void EliminarTelegram(TelegramChat item)
        {
            if (MessageBox.Show($"¿Eliminar Telegram · Chat ID {item.ChatId}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            JsonManager.Eliminar<TelegramChat>(RutasProyecto.ObtenerRutaListTelegram(_rutaBase),
                t => t.ChatId == item.ChatId && t.Apikey == item.Apikey);
            CargarTelegram();
        }

        private void EditarTelegram(TelegramChat item)
        {
            _chatIdOriginalTelegram = item.ChatId;
            txtTelegramChatId.Text  = item.ChatId.ToString();
            txtTelegramApikey.Text  = item.Apikey;
            _esNuevaTelegram        = false;
            btnGuardarTelegram.Text = "Modificar";
            ToggleFormTelegram(true);
        }

        private void LimpiarFormTelegram()
        {
            txtTelegramChatId.Text  = "";
            txtTelegramApikey.Text  = "";
            _esNuevaTelegram        = true;
            _chatIdOriginalTelegram = 0;
            btnGuardarTelegram.Text = _telegramExiste ? "Agregar" : "💾 Crear archivo";
        }

        // ── Tarjeta Telegram ──────────────────────────────────────────────────────
        private Panel CrearTarjetaTelegram(TelegramChat item, int indice, int anchoContenedor)
        {
            bool esActiva  = indice == 0;
            int  anchoCard = Math.Max(280, (anchoContenedor / 2) - 20);
            string jsonTxt = JsonSerializer.Serialize(item, _jsonOpts);

            PanelSuave card = new();
            card.Width     = anchoCard;
            card.Height    = 250;
            card.Margin    = new Padding(6);
            card.BackColor = esActiva ? Color.FromArgb(12, 28, 48) : Color.FromArgb(30, 41, 59);

            PanelSuave barra = new();
            barra.Size      = new Size(4, card.Height);
            barra.Location  = new Point(0, 0);
            barra.BackColor = esActiva ? Color.FromArgb(56, 210, 255) : Color.FromArgb(41, 182, 246);

            if (esActiva)
                card.Controls.Add(BuildBadge("⭐ ACTIVA", new Point(anchoCard - 82, 8),
                    Color.FromArgb(56, 210, 255), Color.FromArgb(0, 30, 60)));

            card.Controls.Add(BuildLabel("✈", new Point(12, 10), new Font("Segoe UI Emoji", 14)));
            card.Controls.Add(BuildChip("CHAT ID", new Point(58, 10), Color.FromArgb(41, 182, 246), 58));

            var lblChatId = BuildLabel(item.ChatId.ToString(), new Point(58, 32),
                new Font("Consolas", 12, FontStyle.Bold));
            lblChatId.ForeColor = Color.White;
            lblChatId.Width     = anchoCard - 70;
            lblChatId.AutoEllipsis = true;
            card.Controls.Add(lblChatId);

            card.Controls.Add(BuildChip("API KEY", new Point(58, 60), Color.FromArgb(100, 116, 139), 56));

            var lblApi = BuildLabel(item.Apikey, new Point(120, 62), new Font("Consolas", 7.5f));
            lblApi.ForeColor = Color.FromArgb(80, 100, 140);
            lblApi.Width     = anchoCard - 132;
            lblApi.AutoEllipsis = true;
            card.Controls.Add(lblApi);

            card.Controls.Add(BuildSep(new Point(10, 88), anchoCard));
            card.Controls.Add(BuildLabel("JSON", new Point(12, 93),
                new Font("Segoe UI", 7, FontStyle.Bold), Color.FromArgb(100, 116, 139)));
            card.Controls.Add(BuildJsonBox(jsonTxt, new Point(10, 109), new Size(anchoCard - 20, 72)));
            card.Controls.Add(BuildSep(new Point(10, 188), anchoCard));

            var btnEdit = BuildBoton("✏ Editar", new Point(12, 200), Color.DarkGreen);
            btnEdit.Click += (s, e) => EditarTelegram(item);
            card.Controls.Add(btnEdit);

            var btnDel = BuildBoton("🗑 Borrar", new Point(101, 200), Color.DarkRed);
            btnDel.Click += (s, e) => EliminarTelegram(item);
            card.Controls.Add(btnDel);

            if (!esActiva)
            {
                var btnAct = BuildBoton("⭐ Activar", new Point(anchoCard - 103, 200),
                    Color.FromArgb(30, 170, 220));
                btnAct.Click += (s, e) => ActivarTelegram(item);
                card.Controls.Add(btnAct);
            }
            else
            {
                var lblAct = BuildLabel("✅ En uso", new Point(anchoCard - 100, 207),
                    new Font("Segoe UI", 8.5f, FontStyle.Bold), Color.FromArgb(56, 210, 255));
                card.Controls.Add(lblAct);
            }

            card.Controls.Add(barra);
            card.RedondearPanel(12, esActiva ? Color.FromArgb(56, 210, 200) : Color.FromArgb(200, 215, 240));
            card.MouseEnter += (s, e) => ((PanelSuave)s).BackColor =
                esActiva ? Color.FromArgb(12, 35, 58) : Color.FromArgb(30, 48, 60);
            card.MouseLeave += (s, e) => ((PanelSuave)s).BackColor =
                esActiva ? Color.FromArgb(12, 28, 48) : Color.FromArgb(30, 41, 59);

            return card;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // HELPERS DE UI
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Panel que se muestra cuando el archivo JSON no existe todavía.
        /// Muestra la estructura vacía como referencia para que el usuario
        /// sepa qué campos debe llenar.
        /// </summary>
        private static Panel CrearPanelEstructura(
            string nombreArchivo, string jsonTemplate, string descripcionCampos, Color colorAccent)
        {
            PanelSuave p = new();
            p.Width     = 520;
            p.Height    = 230;
            p.Margin    = new Padding(8);
            p.BackColor = Color.FromArgb(22, 30, 42);
            p.Padding   = new Padding(14);

            // Encabezado
            Label lblTitulo = new()
            {
                Text      = $"⚠  {nombreArchivo} no encontrado",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 191, 36),   // amarillo advertencia
                Location  = new Point(14, 12),
                AutoSize  = true
            };

            Label lblDesc = new()
            {
                Text      = $"Completa el formulario de arriba y pulsa 💾 Crear archivo.\nCampos:  {descripcionCampos}",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location  = new Point(14, 36),
                Size      = new Size(488, 36),
                AutoSize  = false
            };

            // Etiqueta "Estructura del archivo"
            Label lblEstr = new()
            {
                Text      = "Estructura esperada del archivo:",
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = colorAccent,
                Location  = new Point(14, 78),
                AutoSize  = true
            };

            // Vista del JSON template
            TextBox txtTemplate = new()
            {
                Text        = jsonTemplate,
                ReadOnly    = true,
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical,
                Font        = new Font("Consolas", 8.5f),
                BackColor   = Color.FromArgb(8, 14, 22),
                ForeColor   = Color.FromArgb(80, 200, 120),
                BorderStyle = BorderStyle.None,
                Location    = new Point(14, 97),
                Size        = new Size(488, 118),
                TabStop     = false
            };

            p.Controls.Add(lblTitulo);
            p.Controls.Add(lblDesc);
            p.Controls.Add(lblEstr);
            p.Controls.Add(txtTemplate);
            p.RedondearPanel(10, Color.FromArgb(251, 191, 36));
            return p;
        }

        private static Panel CrearPanelVacio(string mensaje)
        {
            var p = new Panel { Width = 420, Height = 90, Margin = new Padding(20) };
            p.Controls.Add(new Label
            {
                Text      = mensaje,
                Font      = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize  = false,
                Size      = new Size(420, 90),
                TextAlign = ContentAlignment.MiddleCenter
            });
            return p;
        }

        private static Label BuildLabel(string text, Point loc, Font font,
            Color? color = null)
        {
            var lbl = new Label
            {
                Text     = text,
                Font     = font,
                ForeColor = color ?? Color.White,
                Location = loc,
                AutoSize = true
            };
            return lbl;
        }

        private static Label BuildBadge(string text, Point loc, Color fore, Color back)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = fore,
                BackColor = back,
                AutoSize  = false,
                Size      = new Size(74, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = loc
            };
        }

        private static Label BuildChip(string text, Point loc, Color back, int width)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = back,
                AutoSize  = false,
                Size      = new Size(width, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = loc
            };
        }

        private static TextBox BuildJsonBox(string text, Point loc, Size size)
        {
            return new TextBox
            {
                Text        = text,
                ReadOnly    = true,
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical,
                Font        = new Font("Consolas", 8),
                BackColor   = Color.FromArgb(8, 14, 22),
                ForeColor   = Color.FromArgb(80, 200, 120),
                BorderStyle = BorderStyle.None,
                Location    = loc,
                Size        = size,
                TabStop     = false
            };
        }

        private static Panel BuildSep(Point loc, int anchoCard)
        {
            return new Panel
            {
                BackColor = Color.FromArgb(55, 65, 85),
                Location  = loc,
                Size      = new Size(anchoCard - 20, 1)
            };
        }

        private static Button BuildBoton(string text, Point loc, Color colorBorde)
        {
            var btn = new Button
            {
                Text     = text,
                Size     = new Size(83, 28),
                Location = loc,
                Font     = new Font("Segoe UI", 8.5f)
            };
            btn.AplicarEstiloOutline(colorBorde, 8);
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        // ── Tema ─────────────────────────────────────────────────────────────────
        private void AplicarTema()
        {
            BackColor = EmeraldTheme.BgDeep;
            ForeColor = EmeraldTheme.TextPrimary;

            Color borde = EmeraldTheme.IsDark
                ? ColorTranslator.FromHtml("#1a3a5c")
                : ColorTranslator.FromHtml("#C5D8F0");

            // Fondos base
            if (pnlListaSlack != null) pnlListaSlack.BackColor = EmeraldTheme.BgDeep;
            if (pnlListaTelegram != null) pnlListaTelegram.BackColor = EmeraldTheme.BgDeep;
            if (tabControl != null) { tabControl.BackColor = EmeraldTheme.BgDeep; tabControl.ForeColor = EmeraldTheme.TextPrimary; }
            if (tabSlack != null) { tabSlack.BackColor = EmeraldTheme.BgDeep; tabSlack.ForeColor = EmeraldTheme.TextPrimary; }
            if (tabTelegram != null) { tabTelegram.BackColor = EmeraldTheme.BgDeep; tabTelegram.ForeColor = EmeraldTheme.TextPrimary; }
            if (tabAudio != null) { tabAudio.BackColor = EmeraldTheme.BgDeep; tabAudio.ForeColor = EmeraldTheme.TextPrimary; }

            txtSlackToken.RedondearTextBox(10, borde);
            txtSlackCanal.RedondearTextBox(10, borde);
            txtSlackUsuarios.RedondearTextBox(10, borde);
            btnGuardarSlack.AplicarEstiloOutline(EmeraldTheme.Emerald500, 9);
            btnCancelarSlack.AplicarEstiloOutline(Color.DarkRed, 9);
            pnlFormSlack.RedondearPanel(10, borde);

            txtTelegramChatId.RedondearTextBox(10, borde);
            txtTelegramApikey.RedondearTextBox(10, borde);
            btnGuardarTelegram.AplicarEstiloOutline(EmeraldTheme.Emerald500, 9);
            btnCancelarTelegram.AplicarEstiloOutline(Color.DarkRed, 9);
            pnlFormTelegram.RedondearPanel(10, borde);

            // TTS
            txtApiKeyTTS.RedondearTextBox(10, borde);
            txtIdiomaTTS.RedondearTextBox(10, borde);
            pnlInfoTTS.RedondearPanel(10, Color.FromArgb(60, 80, 130));
        }

        // ── Event Handlers ────────────────────────────────────────────────────────
        private void btnToggleSlack_Click(object sender, EventArgs e)
        {
            if (!_slackExiste)
                ToggleFormSlack(true);   // si no existe, siempre mostrar el form
            else
                ToggleFormSlack();
        }

        private void btnToggleTelegram_Click(object sender, EventArgs e)
        {
            if (!_telegramExiste)
                ToggleFormTelegram(true);
            else
                ToggleFormTelegram();
        }

        private void btnGuardarSlack_Click(object sender, EventArgs e)     => GuardarSlack();
        private void btnCancelarSlack_Click(object sender, EventArgs e)    => ToggleFormSlack(false);
        private void btnGuardarTelegram_Click(object sender, EventArgs e)  => GuardarTelegram();
        private void btnCancelarTelegram_Click(object sender, EventArgs e) => ToggleFormTelegram(false);

        // ══════════════════════════════════════════════════════════════════════════
        // AUDIO TTS — lógica
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>Vuelca la configuración TTS cargada al formulario.</summary>
        private void CargarFormularioTTS()
        {
            // Activar
            chkActivarTTS.Checked = _configTTS.Activo;

            // Proveedor
            rbSystemSpeech.Checked = _configTTS.Proveedor == ProveedorTTS.SystemSpeech;
            rbOpenAI.Checked       = _configTTS.Proveedor == ProveedorTTS.OpenAI;
            rbGoogle.Checked       = _configTTS.Proveedor == ProveedorTTS.Google;

            txtApiKeyTTS.Text  = _configTTS.ApiKey  ?? "";
            txtIdiomaTTS.Text  = string.IsNullOrWhiteSpace(_configTTS.Idioma)
                                 ? "es-ES" : _configTTS.Idioma;

            ActualizarUI_PorProveedor();

            // Seleccionar voz en el combo (si existe en la lista)
            if (!string.IsNullOrWhiteSpace(_configTTS.Voz) &&
                cmbVozTTS.Items.Contains(_configTTS.Voz))
                cmbVozTTS.SelectedItem = _configTTS.Voz;
            else if (cmbVozTTS.Items.Count > 0)
                cmbVozTTS.SelectedIndex = 0;

            ActualizarLblConteoAudio();
        }

        /// <summary>
        /// Muestra u oculta campos según el proveedor seleccionado y
        /// puebla el combo de voces.
        /// </summary>
        private void ActualizarUI_PorProveedor()
        {
            bool esWindows = rbSystemSpeech.Checked;
            bool esGoogle  = rbGoogle.Checked;

            pnlApiKeyTTS.Visible   = !esWindows;
            lblIdiomasTTS.Visible  = esGoogle;
            txtIdiomaTTS.Visible   = esGoogle;

            // Poblar combo de voces
            cmbVozTTS.Items.Clear();

            if (esWindows)
            {
                var voces = ServicioTTS.ObtenerVocesInstaladas();
                if (voces.Count == 0)
                    voces.Add("(no hay voces SAPI instaladas)");
                cmbVozTTS.Items.AddRange(voces.Cast<object>().ToArray());
            }
            else if (rbOpenAI.Checked)
            {
                cmbVozTTS.Items.AddRange(ServicioTTS.VocesOpenAI.Cast<object>().ToArray());
            }
            else // Google
            {
                cmbVozTTS.Items.AddRange(ServicioTTS.VocesGoogle.Cast<object>().ToArray());
            }

            if (cmbVozTTS.Items.Count > 0) cmbVozTTS.SelectedIndex = 0;
        }

        private void ActualizarLblConteoAudio()
        {
            if (!_configTTS.Activo)
            {
                lblConteoAudio.Text = "Audio TTS  ·  sin configurar";
                return;
            }

            string proveedor = _configTTS.Proveedor switch
            {
                ProveedorTTS.OpenAI => "OpenAI TTS",
                ProveedorTTS.Google => "Google Cloud TTS",
                _                  => "Sistema Windows (SAPI)"
            };
            string voz = string.IsNullOrWhiteSpace(_configTTS.Voz) ? "voz predeterminada" : _configTTS.Voz;
            lblConteoAudio.Text = $"Audio TTS  ·  ✅ Activo  ·  {proveedor}  ·  {voz}";
        }

        private void GuardarTTS()
        {
            var proveedor = rbOpenAI.Checked ? ProveedorTTS.OpenAI
                          : rbGoogle.Checked ? ProveedorTTS.Google
                          : ProveedorTTS.SystemSpeech;

            // Validar API key cuando se requiere
            if (proveedor != ProveedorTTS.SystemSpeech &&
                string.IsNullOrWhiteSpace(txtApiKeyTTS.Text))
            {
                MessageBox.Show(
                    "Ingresa la API Key para poder guardar la configuración.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string voz = cmbVozTTS.SelectedItem?.ToString() ?? "";

            _configTTS = new ConfiguracionTTS
            {
                Proveedor = proveedor,
                ApiKey    = txtApiKeyTTS.Text.Trim(),
                Voz       = voz,
                Idioma    = txtIdiomaTTS.Text.Trim(),
                Activo    = chkActivarTTS.Checked
            };

            Utils.GuardarConfig(RutasProyecto.ObtenerRutaConfiguracionTTS(), _configTTS);
            ActualizarLblConteoAudio();

            lblEstadoTTS.ForeColor = Color.FromArgb(74, 181, 130);
            lblEstadoTTS.Text = "✅ Configuración guardada";
        }

        // ── Event Handlers TTS ─────────────────────────────────────────────────

        private void rbProveedorTTS_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
                ActualizarUI_PorProveedor();
        }

        private void btnGuardarTTS_Click(object sender, EventArgs e) => GuardarTTS();

        private async void btnTestTTS_Click(object sender, EventArgs e)
        {
            lblEstadoTTS.ForeColor = Color.FromArgb(100, 116, 139);
            lblEstadoTTS.Text      = "Generando audio de prueba...";
            btnTestTTS.Enabled     = false;

            // Construir config temporal desde el formulario (sin necesidad de guardar)
            var configTest = new ConfiguracionTTS
            {
                Proveedor = rbOpenAI.Checked ? ProveedorTTS.OpenAI
                          : rbGoogle.Checked ? ProveedorTTS.Google
                          : ProveedorTTS.SystemSpeech,
                ApiKey    = txtApiKeyTTS.Text.Trim(),
                Voz       = cmbVozTTS.SelectedItem?.ToString() ?? "",
                Idioma    = txtIdiomaTTS.Text.Trim(),
                Activo    = true
            };

            try
            {
                var (bytes, ext) = await ServicioTTS.GenerarAudioAsync(
                    "Hola, este es un audio de prueba del asistente ARIA.", configTest);

                if (bytes.Length == 0)
                {
                    lblEstadoTTS.ForeColor = Color.IndianRed;
                    lblEstadoTTS.Text      = "Error: no se pudo generar el audio. Revisa la API Key.";
                    return;
                }

                string tmp = Path.Combine(Path.GetTempPath(), $"opengioai_tts_test.{ext}");
                await File.WriteAllBytesAsync(tmp, bytes);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = tmp,
                    UseShellExecute = true
                });

                lblEstadoTTS.ForeColor = Color.FromArgb(74, 181, 130);
                lblEstadoTTS.Text      = "▶ Reproduciendo con el reproductor del sistema...";
            }
            catch (Exception ex)
            {
                lblEstadoTTS.ForeColor = Color.IndianRed;
                lblEstadoTTS.Text      = $"Error: {ex.Message}";
            }
            finally
            {
                btnTestTTS.Enabled = true;
            }
        }
    }
}
