using OPENGIOAI.Modulos.Arnes;
using OPENGIOAI.Themas;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public class FrmArnesHub : Form
    {
        private readonly ArnesServer _server;
        private readonly ArnesRouter _router;
        private readonly ArnesSecurityManager _security;

        private Label lblTitulo = null!;
        private Label lblEstado = null!;
        private Button btnToggleServidor = null!;
        private ListBox listLogs = null!;
        private Label lblPlugs = null!;
        
        // Panel de seguridad
        private DataGridView gridKeys = null!;
        private Button btnNuevaLlave = null!;

        public FrmArnesHub(ArnesServer server, ArnesRouter router, ArnesSecurityManager security)
        {
            _server = server;
            _router = router;
            _security = security;
            
            InitializeComponent();
            ConfigurarEventos();
            ActualizarEstadoUI();
        }

        private void InitializeComponent()
        {
            this.BackColor = EmeraldTheme.BgDeep;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(20);

            lblTitulo = new Label
            {
                Text = "Hub ARNES (Conector de IA Externa)",
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            lblEstado = new Label
            {
                Text = "Estado: Desconocido",
                Font = new Font("Segoe UI", 12f),
                ForeColor = EmeraldTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 60)
            };

            btnToggleServidor = new Button
            {
                Text = "Alternar Servidor",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Size = new Size(160, 40),
                Location = new Point(20, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnToggleServidor.FlatAppearance.BorderSize = 0;
            btnToggleServidor.Click += BtnToggleServidor_Click;

            lblPlugs = new Label
            {
                Text = "Plugs Activos: Antigravity",
                Font = new Font("Segoe UI", 10f),
                ForeColor = EmeraldTheme.TextMuted,
                AutoSize = true,
                Location = new Point(200, 110)
            };

            // ---- SPLIT CONTAINER ----
            var splitContainer = new SplitContainer
            {
                Location = new Point(20, 150),
                Size = new Size(this.Width - 40, this.Height - 170),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                SplitterDistance = (this.Width - 40) / 2, // 50% y 50%
                BackColor = EmeraldTheme.BgDeep
            };

            // ---- PANEL IZQUIERDO (Llaves y Logs) ----
            var lblKeysTitulo = new Label
            {
                Text = "Llaves de Acceso (API Keys):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            gridKeys = new DataGridView
            {
                Location = new Point(0, 30),
                Width = splitContainer.Panel1.Width - 10,
                Height = 150,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = EmeraldTheme.BgCard,
                GridColor = EmeraldTheme.IsDark ? ColorTranslator.FromHtml("#1a3a5c") : ColorTranslator.FromHtml("#C5D8F0"),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                RowTemplate = { Height = 28 },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 32,
                ScrollBars = ScrollBars.Vertical,
                MultiSelect = false,
                Font = new Font("Segoe UI", 8.5f),
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            bool dark = EmeraldTheme.IsDark;
            gridKeys.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = EmeraldTheme.BgCard,
                ForeColor = EmeraldTheme.TextPrimary,
                SelectionBackColor = ColorTranslator.FromHtml(dark ? "#1a3a5c" : "#C5D8F0"),
                SelectionForeColor = EmeraldTheme.TextPrimary,
                Padding = new Padding(4, 0, 4, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            gridKeys.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = EmeraldTheme.BgSurface,
                ForeColor = EmeraldTheme.TextPrimary,
                SelectionBackColor = ColorTranslator.FromHtml(dark ? "#1a3a5c" : "#C5D8F0"),
                SelectionForeColor = EmeraldTheme.TextPrimary,
                Padding = new Padding(4, 0, 4, 0)
            };
            gridKeys.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = EmeraldTheme.BgSurface,
                ForeColor = EmeraldTheme.TextSecondary,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                SelectionBackColor = EmeraldTheme.BgSurface,
                SelectionForeColor = EmeraldTheme.TextSecondary,
                Padding = new Padding(4, 0, 4, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            gridKeys.CellContentClick += GridKeys_CellContentClick;

            btnNuevaLlave = new Button
            {
                Text = "+ Generar Nueva Llave",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(180, 30),
                Location = new Point(0, 190),
                BackColor = EmeraldTheme.Emerald500,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNuevaLlave.FlatAppearance.BorderSize = 0;
            btnNuevaLlave.Click += BtnNuevaLlave_Click;

            var lblLogTitulo = new Label
            {
                Text = "Tráfico en Vivo (Logs):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 240)
            };

            listLogs = new ListBox
            {
                Location = new Point(0, 270),
                Width = splitContainer.Panel1.Width - 10,
                Height = splitContainer.Panel1.Height - 270,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = EmeraldTheme.BgCard,
                ForeColor = EmeraldTheme.TextSecondary,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };

            splitContainer.Panel1.Controls.Add(lblKeysTitulo);
            splitContainer.Panel1.Controls.Add(gridKeys);
            splitContainer.Panel1.Controls.Add(btnNuevaLlave);
            splitContainer.Panel1.Controls.Add(lblLogTitulo);
            splitContainer.Panel1.Controls.Add(listLogs);

            // ---- PANEL DERECHO (Documentación) ----
            var lblDocTitulo = new Label
            {
                Text = "📚 ¿Cómo me conecto? (Guía Rápida)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = EmeraldTheme.Emerald500,
                AutoSize = true,
                Location = new Point(10, 0)
            };

            string docText = @"MÓDULO ARNES - GUÍA RÁPIDA

1. URL DEL ENDPOINT:
   POST http://localhost:5050/arnes/

2. CABECERAS (HEADERS):
   Authorization: Bearer <TU_LLAVE_DE_ACCESO>
   Content-Type: application/json

3. ESTRUCTURA DEL CUERPO (PAYLOAD JSON):
{
  ""source_app"": ""mi_app_externa"",
  ""action"": ""execute_aria"",
  ""parameters"": {
    ""instruction"": ""Escribe aquí tu petición para la IA."",
    ""code"": ""(Opcional) Envía tu código fuente si es necesario analizarlo.""
  }
}

* El parámetro 'source_app' identifica tu aplicación.
* 'action' debe ser 'execute_aria' para llamar al agente de OPENGIOAI.
* Tu código o script NO necesita saber qué modelo usar (Gemini, Claude, etc), el Servidor ARNES leerá la configuración que tengas actualmente seleccionada en la interfaz principal de OPENGIOAI.";

            var txtDoc = new RichTextBox
            {
                Location = new Point(10, 30),
                Width = splitContainer.Panel2.Width - 10,
                Height = splitContainer.Panel2.Height - 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = EmeraldTheme.BgCard,
                ForeColor = EmeraldTheme.TextPrimary,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = docText
            };

            splitContainer.Panel2.Controls.Add(lblDocTitulo);
            splitContainer.Panel2.Controls.Add(txtDoc);

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblEstado);
            this.Controls.Add(btnToggleServidor);
            this.Controls.Add(lblPlugs);
            this.Controls.Add(splitContainer);
            
            CargarGrillaLlaves();
        }

        private void ConfigurarEventos()
        {
            _server.OnLog += LogMensaje;
            _router.OnLog += LogMensaje;
        }

        private void CargarGrillaLlaves()
        {
            gridKeys.Columns.Clear();
            gridKeys.DataSource = null;

            var llaves = _security.ObtenerTodas();
            gridKeys.DataSource = llaves;

            // Ajustar columnas
            gridKeys.Columns["Id"].Visible = false;
            
            gridKeys.Columns["AppName"].HeaderText = "Aplicación";
            gridKeys.Columns["Key"].HeaderText = "Token (Bearer)";
            gridKeys.Columns["CreatedAt"].HeaderText = "Creada";
            gridKeys.Columns["LastUsedAt"].HeaderText = "Último Uso";
            gridKeys.Columns["IsActive"].HeaderText = "Activa";

            var btnRevocar = new DataGridViewButtonColumn
            {
                Name = "Accion",
                HeaderText = "Acción",
                Text = "Revocar/Activar",
                UseColumnTextForButtonValue = true
            };
            gridKeys.Columns.Add(btnRevocar);
            
            var btnCopiar = new DataGridViewButtonColumn
            {
                Name = "Copiar",
                HeaderText = "Copiar",
                Text = "Copiar",
                UseColumnTextForButtonValue = true
            };
            gridKeys.Columns.Add(btnCopiar);
        }

        private void GridKeys_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string id = gridKeys.Rows[e.RowIndex].Cells["Id"].Value.ToString() ?? "";
            
            if (gridKeys.Columns[e.ColumnIndex].Name == "Accion")
            {
                bool isActive = (bool)gridKeys.Rows[e.RowIndex].Cells["IsActive"].Value;
                _security.CambiarEstado(id, !isActive);
                CargarGrillaLlaves();
                LogMensaje($"Estado de la llave {id} cambiado a {!isActive}.");
            }
            else if (gridKeys.Columns[e.ColumnIndex].Name == "Copiar")
            {
                string token = gridKeys.Rows[e.RowIndex].Cells["Key"].Value.ToString() ?? "";
                Clipboard.SetText(token);
                MessageBox.Show("Token copiado al portapapeles.", "ARNES", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnNuevaLlave_Click(object? sender, EventArgs e)
        {
            string appName = Microsoft.VisualBasic.Interaction.InputBox(
                "Ingresa el nombre de la aplicación para la que quieres generar la llave:",
                "Nueva Llave ARNES",
                "Mi App Externa");

            if (!string.IsNullOrWhiteSpace(appName))
            {
                var nueva = _security.GenerarLlave(appName);
                CargarGrillaLlaves();
                LogMensaje($"Nueva llave generada para: {appName}");
                
                MessageBox.Show($"Llave generada exitosamente.\n\nToken: {nueva.Key}\n\nPor favor cópiala ahora.", "Llave Generada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LogMensaje(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => LogMensaje(msg)));
                return;
            }

            string linea = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            listLogs.Items.Add(linea);
            
            // Auto-scroll al final
            listLogs.TopIndex = Math.Max(0, listLogs.Items.Count - 1);
        }

        private void BtnToggleServidor_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_server.IsRunning)
                {
                    _server.Detener();
                    LogMensaje("Servidor detenido manualmente.");
                }
                else
                {
                    _server.Iniciar();
                    LogMensaje("Servidor iniciado manualmente.");
                }
                
                // Pequeña pausa para asegurar que el puerto se libera/asigna
                System.Threading.Thread.Sleep(200);
                ActualizarEstadoUI();
            }
            catch (Exception ex)
            {
                LogMensaje($"Error al cambiar estado: {ex.Message}");
            }
        }

        private void ActualizarEstadoUI()
        {
            if (_server.IsRunning)
            {
                lblEstado.Text = "Estado: 🟢 Activo (Escuchando en localhost:5050)";
                lblEstado.ForeColor = EmeraldTheme.Emerald500;
                btnToggleServidor.Text = "Detener Servidor";
                btnToggleServidor.BackColor = EmeraldTheme.Error;
                btnToggleServidor.ForeColor = Color.White;
            }
            else
            {
                lblEstado.Text = "Estado: 🔴 Detenido";
                lblEstado.ForeColor = EmeraldTheme.Error;
                btnToggleServidor.Text = "Iniciar Servidor";
                btnToggleServidor.BackColor = EmeraldTheme.Emerald500;
                btnToggleServidor.ForeColor = Color.White;
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Desuscribir para evitar memory leaks
            _server.OnLog -= LogMensaje;
            _router.OnLog -= LogMensaje;
            base.OnClosed(e);
        }
    }
}
