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

        // Nuevos componentes de UI limpia y responsiva
        private Panel pnlHeader = null!;
        private FlowLayoutPanel pnlAcciones = null!;
        private Button btnToggleDoc = null!;
        private Label lblKeysTitulo = null!;
        private Label lblLogTitulo = null!;
        private Label lblDocTitulo = null!;
        private RichTextBox txtDoc = null!;
        private SplitContainer splitContainer = null!;
        private Panel pnlDocCard = null!;
        private Panel pnlLogsCard = null!;

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

            // ---- PANEL DE CABECERA ----
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = EmeraldTheme.BgDeep
            };

            lblTitulo = new Label
            {
                Text = "Hub ARNES (Conector de IA Externa)",
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 5)
            };

            lblEstado = new Label
            {
                Text = "Estado: Desconocido",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = EmeraldTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(2, 42)
            };

            lblPlugs = new Label
            {
                Text = "Plugs Activos: Antigravity",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = EmeraldTheme.TextMuted,
                AutoSize = true,
                Location = new Point(2, 63)
            };

            pnlAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Width = 360,
                Height = 70,
                BackColor = Color.Transparent
            };

            btnToggleServidor = new Button
            {
                Text = "Alternar Servidor",
                Font = new Font("Segoe UI Semibold", 9.5f),
                Size = new Size(150, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 15, 5, 0)
            };
            btnToggleServidor.FlatAppearance.BorderSize = 0;
            btnToggleServidor.Click += BtnToggleServidor_Click;

            btnToggleDoc = new Button
            {
                Text = "📖 Ocultar Ejemplos",
                Font = new Font("Segoe UI Semibold", 9.5f),
                Size = new Size(160, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 15, 5, 0)
            };
            EmeraldTheme.StyleModernButton(btnToggleDoc);
            btnToggleDoc.Click += BtnToggleDoc_Click;

            pnlAcciones.Controls.Add(btnToggleServidor);
            pnlAcciones.Controls.Add(btnToggleDoc);

            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(lblEstado);
            pnlHeader.Controls.Add(lblPlugs);
            pnlHeader.Controls.Add(pnlAcciones);

            // ---- SPLIT CONTAINER PRINCIPAL ----
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 450,
                BackColor = EmeraldTheme.BgDeep
            };

            // ---- PANEL IZQUIERDO (Llaves y Logs) ----
            lblKeysTitulo = new Label
            {
                Text = "Llaves de Acceso (API Keys):",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            btnNuevaLlave = new Button
            {
                Text = "+ Generar Nueva Llave",
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Size = new Size(180, 28),
                Location = new Point(450 - 190, 6),
                BackColor = EmeraldTheme.Emerald500,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNuevaLlave.FlatAppearance.BorderSize = 0;
            btnNuevaLlave.Click += BtnNuevaLlave_Click;

            gridKeys = new DataGridView
            {
                Location = new Point(0, 40),
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

            lblLogTitulo = new Label
            {
                Text = "Tráfico en Vivo (Logs):",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 205),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            pnlLogsCard = new Panel
            {
                Location = new Point(0, 235),
                Width = splitContainer.Panel1.Width - 10,
                Height = splitContainer.Panel1.Height - 245,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = EmeraldTheme.BgCard,
                Padding = new Padding(10)
            };

            listLogs = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = EmeraldTheme.BgCard,
                ForeColor = EmeraldTheme.TextSecondary,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };
            pnlLogsCard.Controls.Add(listLogs);

            splitContainer.Panel1.Controls.Add(lblKeysTitulo);
            splitContainer.Panel1.Controls.Add(btnNuevaLlave);
            splitContainer.Panel1.Controls.Add(gridKeys);
            splitContainer.Panel1.Controls.Add(lblLogTitulo);
            splitContainer.Panel1.Controls.Add(pnlLogsCard);

            // ---- PANEL DERECHO (Documentación y Ejemplos) ----
            lblDocTitulo = new Label
            {
                Text = "📚 ¿Cómo me conecto? (Guía Rápida)",
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                ForeColor = EmeraldTheme.Emerald400,
                AutoSize = true,
                Location = new Point(10, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
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

            pnlDocCard = new Panel
            {
                Location = new Point(10, 40),
                Width = splitContainer.Panel2.Width - 15,
                Height = splitContainer.Panel2.Height - 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = EmeraldTheme.BgCard,
                Padding = new Padding(15)
            };

            txtDoc = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = EmeraldTheme.BgCard,
                ForeColor = EmeraldTheme.TextPrimary,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = docText
            };
            pnlDocCard.Controls.Add(txtDoc);

            splitContainer.Panel2.Controls.Add(lblDocTitulo);
            splitContainer.Panel2.Controls.Add(pnlDocCard);

            this.Controls.Add(splitContainer);
            this.Controls.Add(pnlHeader);
            
            CargarGrillaLlaves();
            ConfigureThemeSupport();
        }

        private void ConfigureThemeSupport()
        {
            EmeraldTheme.ThemeChanged += OnThemeChanged;
            this.Disposed += (s, e) => EmeraldTheme.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            this.BackColor = EmeraldTheme.BgDeep;
            
            if (pnlHeader != null) pnlHeader.BackColor = EmeraldTheme.BgDeep;
            if (lblTitulo != null) lblTitulo.ForeColor = EmeraldTheme.TextPrimary;
            if (lblEstado != null)
            {
                if (_server.IsRunning)
                    lblEstado.ForeColor = EmeraldTheme.Emerald400;
                else
                    lblEstado.ForeColor = EmeraldTheme.Error;
            }
            if (lblPlugs != null) lblPlugs.ForeColor = EmeraldTheme.TextMuted;

            if (splitContainer != null)
            {
                splitContainer.BackColor = EmeraldTheme.BgDeep;
                splitContainer.Panel1.BackColor = EmeraldTheme.BgDeep;
                splitContainer.Panel2.BackColor = EmeraldTheme.BgDeep;
            }

            if (lblKeysTitulo != null) lblKeysTitulo.ForeColor = EmeraldTheme.TextPrimary;
            if (lblLogTitulo != null) lblLogTitulo.ForeColor = EmeraldTheme.TextPrimary;
            if (lblDocTitulo != null) lblDocTitulo.ForeColor = EmeraldTheme.Emerald400;

            if (gridKeys != null)
            {
                gridKeys.BackgroundColor = EmeraldTheme.BgCard;
                gridKeys.GridColor = EmeraldTheme.IsDark ? ColorTranslator.FromHtml("#1a3a5c") : ColorTranslator.FromHtml("#C5D8F0");
                
                bool dark = EmeraldTheme.IsDark;
                gridKeys.DefaultCellStyle.BackColor = EmeraldTheme.BgCard;
                gridKeys.DefaultCellStyle.ForeColor = EmeraldTheme.TextPrimary;
                gridKeys.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml(dark ? "#1a3a5c" : "#C5D8F0");
                gridKeys.DefaultCellStyle.SelectionForeColor = EmeraldTheme.TextPrimary;

                gridKeys.AlternatingRowsDefaultCellStyle.BackColor = EmeraldTheme.BgSurface;
                gridKeys.AlternatingRowsDefaultCellStyle.ForeColor = EmeraldTheme.TextPrimary;
                gridKeys.AlternatingRowsDefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml(dark ? "#1a3a5c" : "#C5D8F0");
                gridKeys.AlternatingRowsDefaultCellStyle.SelectionForeColor = EmeraldTheme.TextPrimary;

                gridKeys.ColumnHeadersDefaultCellStyle.BackColor = EmeraldTheme.BgSurface;
                gridKeys.ColumnHeadersDefaultCellStyle.ForeColor = EmeraldTheme.TextSecondary;
                gridKeys.ColumnHeadersDefaultCellStyle.SelectionBackColor = EmeraldTheme.BgSurface;
                gridKeys.ColumnHeadersDefaultCellStyle.SelectionForeColor = EmeraldTheme.TextSecondary;
            }

            if (listLogs != null)
            {
                listLogs.BackColor = EmeraldTheme.BgCard;
                listLogs.ForeColor = EmeraldTheme.TextSecondary;
            }

            if (txtDoc != null)
            {
                txtDoc.BackColor = EmeraldTheme.BgCard;
                txtDoc.ForeColor = EmeraldTheme.TextPrimary;
            }

            if (btnToggleServidor != null)
            {
                if (_server.IsRunning)
                {
                    btnToggleServidor.BackColor = EmeraldTheme.Error;
                    btnToggleServidor.ForeColor = Color.White;
                }
                else
                {
                    btnToggleServidor.BackColor = EmeraldTheme.Emerald500;
                    btnToggleServidor.ForeColor = Color.White;
                }
            }

            if (btnToggleDoc != null)
            {
                EmeraldTheme.StyleModernButton(btnToggleDoc);
                btnToggleDoc.ForeColor = EmeraldTheme.TextPrimary;
            }

            if (btnNuevaLlave != null)
            {
                btnNuevaLlave.BackColor = EmeraldTheme.Emerald500;
                btnNuevaLlave.ForeColor = Color.White;
            }
        }

        private void BtnToggleDoc_Click(object? sender, EventArgs e)
        {
            splitContainer.Panel2Collapsed = !splitContainer.Panel2Collapsed;
            ActualizarBotonDoc();
        }

        private void ActualizarBotonDoc()
        {
            if (splitContainer.Panel2Collapsed)
            {
                btnToggleDoc.Text = "📖 Mostrar Ejemplos";
            }
            else
            {
                btnToggleDoc.Text = "📖 Ocultar Ejemplos";
            }
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
                lblEstado.ForeColor = EmeraldTheme.Emerald400;
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
