namespace OPENGIOAI.Vistas
{
    partial class FrmComunicadores
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmComunicadores));
            panelPrincipal = new Panel();
            tabControl = new TabControl();
            tabSlack = new TabPage();
            pnlListaSlack = new FlowLayoutPanel();
            pnlFormSlack = new Panel();
            btnCancelarSlack = new Button();
            btnGuardarSlack = new Button();
            txtSlackUsuarios = new TextBox();
            lblSlackUsuarios = new Label();
            txtSlackCanal = new TextBox();
            lblSlackCanal = new Label();
            txtSlackToken = new TextBox();
            lblSlackToken = new Label();
            pnlCabeceraSlack = new Panel();
            lblConteoSlack = new Label();
            btnToggleSlack = new Button();
            tabTelegram = new TabPage();
            pnlListaTelegram = new FlowLayoutPanel();
            pnlFormTelegram = new Panel();
            btnCancelarTelegram = new Button();
            btnGuardarTelegram = new Button();
            txtTelegramApikey = new TextBox();
            lblTelegramApikey = new Label();
            txtTelegramChatId = new TextBox();
            lblTelegramChatId = new Label();
            pnlCabeceraTelegram = new Panel();
            lblConteoTelegram = new Label();
            btnToggleTelegram = new Button();
            tabAudio = new TabPage();
            pnlBodyAudio = new Panel();
            pnlInfoTTS = new Panel();
            lblInfoTTS = new Label();
            lblEstadoTTS = new Label();
            btnGuardarTTS = new Button();
            btnTestTTS = new Button();
            txtIdiomaTTS = new TextBox();
            lblIdiomasTTS = new Label();
            cmbVozTTS = new ComboBox();
            lblVozTTS = new Label();
            pnlApiKeyTTS = new Panel();
            lblApiKeyTTS = new Label();
            txtApiKeyTTS = new TextBox();
            rbGoogle = new RadioButton();
            rbOpenAI = new RadioButton();
            rbSystemSpeech = new RadioButton();
            lblProveedorTTS = new Label();
            chkActivarTTS = new CheckBox();
            pnlCabeceraAudio = new Panel();
            lblConteoAudio = new Label();
            lblSubtitulo = new Label();
            lblTitulo = new Label();
            panelPrincipal.SuspendLayout();
            tabControl.SuspendLayout();
            tabSlack.SuspendLayout();
            pnlFormSlack.SuspendLayout();
            pnlCabeceraSlack.SuspendLayout();
            tabTelegram.SuspendLayout();
            pnlFormTelegram.SuspendLayout();
            pnlCabeceraTelegram.SuspendLayout();
            tabAudio.SuspendLayout();
            pnlBodyAudio.SuspendLayout();
            pnlInfoTTS.SuspendLayout();
            pnlApiKeyTTS.SuspendLayout();
            pnlCabeceraAudio.SuspendLayout();
            SuspendLayout();
            // 
            // panelPrincipal
            // 
            panelPrincipal.BackColor = Color.Black;
            panelPrincipal.Controls.Add(tabControl);
            panelPrincipal.Controls.Add(lblSubtitulo);
            panelPrincipal.Controls.Add(lblTitulo);
            panelPrincipal.Dock = DockStyle.Fill;
            panelPrincipal.Location = new Point(0, 0);
            panelPrincipal.Name = "panelPrincipal";
            panelPrincipal.Size = new Size(1008, 797);
            panelPrincipal.TabIndex = 0;
            // 
            // tabControl
            // 
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Controls.Add(tabSlack);
            tabControl.Controls.Add(tabTelegram);
            tabControl.Controls.Add(tabAudio);
            tabControl.Location = new Point(20, 78);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1776, 1397);
            tabControl.TabIndex = 2;
            // 
            // tabSlack
            // 
            tabSlack.BackColor = Color.FromArgb(15, 23, 42);
            tabSlack.Controls.Add(pnlListaSlack);
            tabSlack.Controls.Add(pnlFormSlack);
            tabSlack.Controls.Add(pnlCabeceraSlack);
            tabSlack.Location = new Point(4, 24);
            tabSlack.Name = "tabSlack";
            tabSlack.Padding = new Padding(4);
            tabSlack.Size = new Size(1768, 1369);
            tabSlack.TabIndex = 0;
            tabSlack.Text = "  💬  Slack  ";
            // 
            // pnlListaSlack
            // 
            pnlListaSlack.BackColor = Color.Black;
            pnlListaSlack.Dock = DockStyle.Fill;
            pnlListaSlack.Location = new Point(4, 267);
            pnlListaSlack.Name = "pnlListaSlack";
            pnlListaSlack.Size = new Size(1760, 1098);
            pnlListaSlack.TabIndex = 0;
            // 
            // pnlFormSlack
            // 
            pnlFormSlack.BackColor = Color.Black;
            pnlFormSlack.Controls.Add(btnCancelarSlack);
            pnlFormSlack.Controls.Add(btnGuardarSlack);
            pnlFormSlack.Controls.Add(txtSlackUsuarios);
            pnlFormSlack.Controls.Add(lblSlackUsuarios);
            pnlFormSlack.Controls.Add(txtSlackCanal);
            pnlFormSlack.Controls.Add(lblSlackCanal);
            pnlFormSlack.Controls.Add(txtSlackToken);
            pnlFormSlack.Controls.Add(lblSlackToken);
            pnlFormSlack.Dock = DockStyle.Top;
            pnlFormSlack.Location = new Point(4, 52);
            pnlFormSlack.Name = "pnlFormSlack";
            pnlFormSlack.Padding = new Padding(12, 10, 12, 10);
            pnlFormSlack.Size = new Size(1760, 215);
            pnlFormSlack.TabIndex = 1;
            pnlFormSlack.Visible = false;
            // 
            // btnCancelarSlack
            // 
            btnCancelarSlack.FlatAppearance.BorderColor = Color.DarkRed;
            btnCancelarSlack.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 0, 0);
            btnCancelarSlack.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 0, 0);
            btnCancelarSlack.FlatStyle = FlatStyle.Flat;
            btnCancelarSlack.ForeColor = Color.White;
            btnCancelarSlack.Location = new Point(180, 168);
            btnCancelarSlack.Name = "btnCancelarSlack";
            btnCancelarSlack.Size = new Size(150, 28);
            btnCancelarSlack.TabIndex = 7;
            btnCancelarSlack.Text = "Cancelar";
            btnCancelarSlack.UseVisualStyleBackColor = true;
            btnCancelarSlack.Click += btnCancelarSlack_Click;
            // 
            // btnGuardarSlack
            // 
            btnGuardarSlack.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnGuardarSlack.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 128, 255);
            btnGuardarSlack.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuardarSlack.FlatStyle = FlatStyle.Flat;
            btnGuardarSlack.ForeColor = Color.White;
            btnGuardarSlack.Location = new Point(17, 168);
            btnGuardarSlack.Name = "btnGuardarSlack";
            btnGuardarSlack.Size = new Size(150, 28);
            btnGuardarSlack.TabIndex = 6;
            btnGuardarSlack.Text = "Agregar";
            btnGuardarSlack.UseVisualStyleBackColor = true;
            btnGuardarSlack.Click += btnGuardarSlack_Click;
            // 
            // txtSlackUsuarios
            // 
            txtSlackUsuarios.BackColor = Color.FromArgb(15, 23, 42);
            txtSlackUsuarios.ForeColor = Color.White;
            txtSlackUsuarios.Location = new Point(17, 88);
            txtSlackUsuarios.Multiline = true;
            txtSlackUsuarios.Name = "txtSlackUsuarios";
            txtSlackUsuarios.Size = new Size(718, 65);
            txtSlackUsuarios.TabIndex = 5;
            // 
            // lblSlackUsuarios
            // 
            lblSlackUsuarios.AutoSize = true;
            lblSlackUsuarios.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSlackUsuarios.ForeColor = Color.FromArgb(148, 163, 184);
            lblSlackUsuarios.Location = new Point(17, 70);
            lblSlackUsuarios.Name = "lblSlackUsuarios";
            lblSlackUsuarios.Size = new Size(204, 15);
            lblSlackUsuarios.TabIndex = 4;
            lblSlackUsuarios.Text = "IDs de usuarios (separados por coma)";
            // 
            // txtSlackCanal
            // 
            txtSlackCanal.BackColor = Color.FromArgb(15, 23, 42);
            txtSlackCanal.ForeColor = Color.White;
            txtSlackCanal.Location = new Point(485, 34);
            txtSlackCanal.Name = "txtSlackCanal";
            txtSlackCanal.Size = new Size(250, 23);
            txtSlackCanal.TabIndex = 3;
            // 
            // lblSlackCanal
            // 
            lblSlackCanal.AutoSize = true;
            lblSlackCanal.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSlackCanal.ForeColor = Color.FromArgb(148, 163, 184);
            lblSlackCanal.Location = new Point(485, 16);
            lblSlackCanal.Name = "lblSlackCanal";
            lblSlackCanal.Size = new Size(51, 15);
            lblSlackCanal.TabIndex = 2;
            lblSlackCanal.Text = "ID Canal";
            // 
            // txtSlackToken
            // 
            txtSlackToken.BackColor = Color.FromArgb(15, 23, 42);
            txtSlackToken.ForeColor = Color.White;
            txtSlackToken.Location = new Point(17, 34);
            txtSlackToken.Name = "txtSlackToken";
            txtSlackToken.Size = new Size(450, 23);
            txtSlackToken.TabIndex = 1;
            // 
            // lblSlackToken
            // 
            lblSlackToken.AutoSize = true;
            lblSlackToken.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSlackToken.ForeColor = Color.FromArgb(148, 163, 184);
            lblSlackToken.Location = new Point(17, 16);
            lblSlackToken.Name = "lblSlackToken";
            lblSlackToken.Size = new Size(93, 15);
            lblSlackToken.TabIndex = 0;
            lblSlackToken.Text = "Token de acceso";
            // 
            // pnlCabeceraSlack
            // 
            pnlCabeceraSlack.BackColor = Color.FromArgb(22, 33, 50);
            pnlCabeceraSlack.Controls.Add(lblConteoSlack);
            pnlCabeceraSlack.Controls.Add(btnToggleSlack);
            pnlCabeceraSlack.Dock = DockStyle.Top;
            pnlCabeceraSlack.Location = new Point(4, 4);
            pnlCabeceraSlack.Name = "pnlCabeceraSlack";
            pnlCabeceraSlack.Padding = new Padding(12, 0, 12, 0);
            pnlCabeceraSlack.Size = new Size(1760, 48);
            pnlCabeceraSlack.TabIndex = 2;
            // 
            // lblConteoSlack
            // 
            lblConteoSlack.BackColor = Color.Black;
            lblConteoSlack.Dock = DockStyle.Fill;
            lblConteoSlack.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConteoSlack.ForeColor = Color.FromArgb(226, 232, 240);
            lblConteoSlack.Location = new Point(12, 0);
            lblConteoSlack.Name = "lblConteoSlack";
            lblConteoSlack.Size = new Size(1591, 48);
            lblConteoSlack.TabIndex = 0;
            lblConteoSlack.Text = "Slack  (cargando...)";
            lblConteoSlack.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnToggleSlack
            // 
            btnToggleSlack.Dock = DockStyle.Right;
            btnToggleSlack.FlatAppearance.BorderColor = Color.FromArgb(74, 181, 130);
            btnToggleSlack.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 40, 25);
            btnToggleSlack.FlatStyle = FlatStyle.Flat;
            btnToggleSlack.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnToggleSlack.ForeColor = Color.FromArgb(74, 181, 130);
            btnToggleSlack.Location = new Point(1603, 0);
            btnToggleSlack.Name = "btnToggleSlack";
            btnToggleSlack.Size = new Size(145, 48);
            btnToggleSlack.TabIndex = 1;
            btnToggleSlack.Text = "➕  Nueva";
            btnToggleSlack.UseVisualStyleBackColor = false;
            btnToggleSlack.Click += btnToggleSlack_Click;
            // 
            // tabTelegram
            // 
            tabTelegram.BackColor = Color.FromArgb(15, 23, 42);
            tabTelegram.Controls.Add(pnlListaTelegram);
            tabTelegram.Controls.Add(pnlFormTelegram);
            tabTelegram.Controls.Add(pnlCabeceraTelegram);
            tabTelegram.Location = new Point(4, 24);
            tabTelegram.Name = "tabTelegram";
            tabTelegram.Padding = new Padding(4);
            tabTelegram.Size = new Size(1768, 1369);
            tabTelegram.TabIndex = 1;
            tabTelegram.Text = "  ✈  Telegram  ";
            // 
            // pnlListaTelegram
            // 
            pnlListaTelegram.BackColor = Color.Black;
            pnlListaTelegram.Dock = DockStyle.Fill;
            pnlListaTelegram.Location = new Point(4, 200);
            pnlListaTelegram.Name = "pnlListaTelegram";
            pnlListaTelegram.Size = new Size(1760, 1165);
            pnlListaTelegram.TabIndex = 0;
            // 
            // pnlFormTelegram
            // 
            pnlFormTelegram.BackColor = Color.Black;
            pnlFormTelegram.Controls.Add(btnCancelarTelegram);
            pnlFormTelegram.Controls.Add(btnGuardarTelegram);
            pnlFormTelegram.Controls.Add(txtTelegramApikey);
            pnlFormTelegram.Controls.Add(lblTelegramApikey);
            pnlFormTelegram.Controls.Add(txtTelegramChatId);
            pnlFormTelegram.Controls.Add(lblTelegramChatId);
            pnlFormTelegram.Dock = DockStyle.Top;
            pnlFormTelegram.Location = new Point(4, 52);
            pnlFormTelegram.Name = "pnlFormTelegram";
            pnlFormTelegram.Padding = new Padding(12, 10, 12, 10);
            pnlFormTelegram.Size = new Size(1760, 148);
            pnlFormTelegram.TabIndex = 1;
            pnlFormTelegram.Visible = false;
            // 
            // btnCancelarTelegram
            // 
            btnCancelarTelegram.FlatAppearance.BorderColor = Color.DarkRed;
            btnCancelarTelegram.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 0, 0);
            btnCancelarTelegram.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 0, 0);
            btnCancelarTelegram.FlatStyle = FlatStyle.Flat;
            btnCancelarTelegram.ForeColor = Color.White;
            btnCancelarTelegram.Location = new Point(180, 100);
            btnCancelarTelegram.Name = "btnCancelarTelegram";
            btnCancelarTelegram.Size = new Size(150, 28);
            btnCancelarTelegram.TabIndex = 5;
            btnCancelarTelegram.Text = "Cancelar";
            btnCancelarTelegram.UseVisualStyleBackColor = true;
            btnCancelarTelegram.Click += btnCancelarTelegram_Click;
            // 
            // btnGuardarTelegram
            // 
            btnGuardarTelegram.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnGuardarTelegram.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 128, 255);
            btnGuardarTelegram.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuardarTelegram.FlatStyle = FlatStyle.Flat;
            btnGuardarTelegram.ForeColor = Color.White;
            btnGuardarTelegram.Location = new Point(17, 100);
            btnGuardarTelegram.Name = "btnGuardarTelegram";
            btnGuardarTelegram.Size = new Size(150, 28);
            btnGuardarTelegram.TabIndex = 4;
            btnGuardarTelegram.Text = "Agregar";
            btnGuardarTelegram.UseVisualStyleBackColor = true;
            btnGuardarTelegram.Click += btnGuardarTelegram_Click;
            // 
            // txtTelegramApikey
            // 
            txtTelegramApikey.BackColor = Color.FromArgb(15, 23, 42);
            txtTelegramApikey.ForeColor = Color.White;
            txtTelegramApikey.Location = new Point(265, 34);
            txtTelegramApikey.Name = "txtTelegramApikey";
            txtTelegramApikey.Size = new Size(480, 23);
            txtTelegramApikey.TabIndex = 3;
            // 
            // lblTelegramApikey
            // 
            lblTelegramApikey.AutoSize = true;
            lblTelegramApikey.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTelegramApikey.ForeColor = Color.FromArgb(148, 163, 184);
            lblTelegramApikey.Location = new Point(265, 16);
            lblTelegramApikey.Name = "lblTelegramApikey";
            lblTelegramApikey.Size = new Size(47, 15);
            lblTelegramApikey.TabIndex = 2;
            lblTelegramApikey.Text = "API Key";
            // 
            // txtTelegramChatId
            // 
            txtTelegramChatId.BackColor = Color.FromArgb(15, 23, 42);
            txtTelegramChatId.ForeColor = Color.White;
            txtTelegramChatId.Location = new Point(17, 34);
            txtTelegramChatId.Name = "txtTelegramChatId";
            txtTelegramChatId.Size = new Size(230, 23);
            txtTelegramChatId.TabIndex = 1;
            // 
            // lblTelegramChatId
            // 
            lblTelegramChatId.AutoSize = true;
            lblTelegramChatId.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTelegramChatId.ForeColor = Color.FromArgb(148, 163, 184);
            lblTelegramChatId.Location = new Point(17, 16);
            lblTelegramChatId.Name = "lblTelegramChatId";
            lblTelegramChatId.Size = new Size(46, 15);
            lblTelegramChatId.TabIndex = 0;
            lblTelegramChatId.Text = "Chat ID";
            // 
            // pnlCabeceraTelegram
            // 
            pnlCabeceraTelegram.BackColor = Color.FromArgb(22, 33, 50);
            pnlCabeceraTelegram.Controls.Add(lblConteoTelegram);
            pnlCabeceraTelegram.Controls.Add(btnToggleTelegram);
            pnlCabeceraTelegram.Dock = DockStyle.Top;
            pnlCabeceraTelegram.Location = new Point(4, 4);
            pnlCabeceraTelegram.Name = "pnlCabeceraTelegram";
            pnlCabeceraTelegram.Padding = new Padding(12, 0, 12, 0);
            pnlCabeceraTelegram.Size = new Size(1760, 48);
            pnlCabeceraTelegram.TabIndex = 2;
            // 
            // lblConteoTelegram
            // 
            lblConteoTelegram.BackColor = Color.Black;
            lblConteoTelegram.Dock = DockStyle.Fill;
            lblConteoTelegram.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConteoTelegram.ForeColor = Color.FromArgb(226, 232, 240);
            lblConteoTelegram.Location = new Point(12, 0);
            lblConteoTelegram.Name = "lblConteoTelegram";
            lblConteoTelegram.Size = new Size(1591, 48);
            lblConteoTelegram.TabIndex = 0;
            lblConteoTelegram.Text = "Telegram  (cargando...)";
            lblConteoTelegram.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnToggleTelegram
            // 
            btnToggleTelegram.Dock = DockStyle.Right;
            btnToggleTelegram.FlatAppearance.BorderColor = Color.FromArgb(41, 182, 246);
            btnToggleTelegram.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 20, 50);
            btnToggleTelegram.FlatStyle = FlatStyle.Flat;
            btnToggleTelegram.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnToggleTelegram.ForeColor = Color.FromArgb(41, 182, 246);
            btnToggleTelegram.Location = new Point(1603, 0);
            btnToggleTelegram.Name = "btnToggleTelegram";
            btnToggleTelegram.Size = new Size(145, 48);
            btnToggleTelegram.TabIndex = 1;
            btnToggleTelegram.Text = "➕  Nueva";
            btnToggleTelegram.UseVisualStyleBackColor = false;
            btnToggleTelegram.Click += btnToggleTelegram_Click;
            // 
            // tabAudio
            // 
            tabAudio.BackColor = Color.FromArgb(15, 23, 42);
            tabAudio.Controls.Add(pnlBodyAudio);
            tabAudio.Controls.Add(pnlCabeceraAudio);
            tabAudio.Location = new Point(4, 24);
            tabAudio.Name = "tabAudio";
            tabAudio.Padding = new Padding(4);
            tabAudio.Size = new Size(1768, 1369);
            tabAudio.TabIndex = 2;
            tabAudio.Text = "  🔊  Audio TTS  ";
            // 
            // pnlBodyAudio
            // 
            pnlBodyAudio.AutoScroll = true;
            pnlBodyAudio.BackColor = Color.Black;
            pnlBodyAudio.Controls.Add(pnlInfoTTS);
            pnlBodyAudio.Controls.Add(lblEstadoTTS);
            pnlBodyAudio.Controls.Add(btnGuardarTTS);
            pnlBodyAudio.Controls.Add(btnTestTTS);
            pnlBodyAudio.Controls.Add(txtIdiomaTTS);
            pnlBodyAudio.Controls.Add(lblIdiomasTTS);
            pnlBodyAudio.Controls.Add(cmbVozTTS);
            pnlBodyAudio.Controls.Add(lblVozTTS);
            pnlBodyAudio.Controls.Add(pnlApiKeyTTS);
            pnlBodyAudio.Controls.Add(rbGoogle);
            pnlBodyAudio.Controls.Add(rbOpenAI);
            pnlBodyAudio.Controls.Add(rbSystemSpeech);
            pnlBodyAudio.Controls.Add(lblProveedorTTS);
            pnlBodyAudio.Controls.Add(chkActivarTTS);
            pnlBodyAudio.Dock = DockStyle.Fill;
            pnlBodyAudio.Location = new Point(4, 52);
            pnlBodyAudio.Name = "pnlBodyAudio";
            pnlBodyAudio.Padding = new Padding(24, 20, 24, 20);
            pnlBodyAudio.Size = new Size(1760, 1313);
            pnlBodyAudio.TabIndex = 0;
            // 
            // pnlInfoTTS
            // 
            pnlInfoTTS.BackColor = Color.FromArgb(22, 30, 48);
            pnlInfoTTS.Controls.Add(lblInfoTTS);
            pnlInfoTTS.Location = new Point(24, 320);
            pnlInfoTTS.Name = "pnlInfoTTS";
            pnlInfoTTS.Padding = new Padding(14, 10, 14, 10);
            pnlInfoTTS.Size = new Size(680, 120);
            pnlInfoTTS.TabIndex = 7;
            // 
            // lblInfoTTS
            // 
            lblInfoTTS.Dock = DockStyle.Fill;
            lblInfoTTS.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblInfoTTS.ForeColor = Color.FromArgb(100, 130, 180);
            lblInfoTTS.Location = new Point(14, 10);
            lblInfoTTS.Name = "lblInfoTTS";
            lblInfoTTS.Size = new Size(652, 100);
            lblInfoTTS.TabIndex = 0;
            lblInfoTTS.Text = resources.GetString("lblInfoTTS.Text");
            // 
            // lblEstadoTTS
            // 
            lblEstadoTTS.AutoSize = true;
            lblEstadoTTS.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEstadoTTS.ForeColor = Color.FromArgb(100, 116, 139);
            lblEstadoTTS.Location = new Point(24, 292);
            lblEstadoTTS.Name = "lblEstadoTTS";
            lblEstadoTTS.Size = new Size(0, 15);
            lblEstadoTTS.TabIndex = 8;
            // 
            // btnGuardarTTS
            // 
            btnGuardarTTS.FlatAppearance.BorderColor = Color.FromArgb(74, 222, 128);
            btnGuardarTTS.FlatAppearance.CheckedBackColor = Color.FromArgb(20, 80, 40);
            btnGuardarTTS.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 50, 25);
            btnGuardarTTS.FlatStyle = FlatStyle.Flat;
            btnGuardarTTS.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnGuardarTTS.ForeColor = Color.FromArgb(74, 222, 128);
            btnGuardarTTS.Location = new Point(200, 250);
            btnGuardarTTS.Name = "btnGuardarTTS";
            btnGuardarTTS.Size = new Size(160, 32);
            btnGuardarTTS.TabIndex = 7;
            btnGuardarTTS.Text = "💾  Guardar";
            btnGuardarTTS.UseVisualStyleBackColor = false;
            btnGuardarTTS.Click += btnGuardarTTS_Click;
            // 
            // btnTestTTS
            // 
            btnTestTTS.FlatAppearance.BorderColor = Color.FromArgb(139, 92, 246);
            btnTestTTS.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 10, 60);
            btnTestTTS.FlatStyle = FlatStyle.Flat;
            btnTestTTS.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnTestTTS.ForeColor = Color.FromArgb(196, 181, 253);
            btnTestTTS.Location = new Point(24, 250);
            btnTestTTS.Name = "btnTestTTS";
            btnTestTTS.Size = new Size(160, 32);
            btnTestTTS.TabIndex = 6;
            btnTestTTS.Text = "🔊  Probar audio";
            btnTestTTS.UseVisualStyleBackColor = false;
            btnTestTTS.Click += btnTestTTS_Click;
            // 
            // txtIdiomaTTS
            // 
            txtIdiomaTTS.BackColor = Color.FromArgb(15, 23, 42);
            txtIdiomaTTS.ForeColor = Color.White;
            txtIdiomaTTS.Location = new Point(370, 208);
            txtIdiomaTTS.Name = "txtIdiomaTTS";
            txtIdiomaTTS.Size = new Size(160, 23);
            txtIdiomaTTS.TabIndex = 5;
            txtIdiomaTTS.Text = "es-ES";
            txtIdiomaTTS.Visible = false;
            // 
            // lblIdiomasTTS
            // 
            lblIdiomasTTS.AutoSize = true;
            lblIdiomasTTS.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblIdiomasTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblIdiomasTTS.Location = new Point(370, 190);
            lblIdiomasTTS.Name = "lblIdiomasTTS";
            lblIdiomasTTS.Size = new Size(196, 15);
            lblIdiomasTTS.TabIndex = 9;
            lblIdiomasTTS.Text = "Código de idioma (ej. es-ES, es-MX)";
            lblIdiomasTTS.Visible = false;
            // 
            // cmbVozTTS
            // 
            cmbVozTTS.BackColor = Color.FromArgb(30, 41, 59);
            cmbVozTTS.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVozTTS.FlatStyle = FlatStyle.Flat;
            cmbVozTTS.ForeColor = Color.White;
            cmbVozTTS.FormattingEnabled = true;
            cmbVozTTS.Location = new Point(24, 208);
            cmbVozTTS.Name = "cmbVozTTS";
            cmbVozTTS.Size = new Size(320, 23);
            cmbVozTTS.TabIndex = 4;
            // 
            // lblVozTTS
            // 
            lblVozTTS.AutoSize = true;
            lblVozTTS.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblVozTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblVozTTS.Location = new Point(24, 190);
            lblVozTTS.Name = "lblVozTTS";
            lblVozTTS.Size = new Size(25, 15);
            lblVozTTS.TabIndex = 10;
            lblVozTTS.Text = "Voz";
            // 
            // pnlApiKeyTTS
            // 
            pnlApiKeyTTS.BackColor = Color.Transparent;
            pnlApiKeyTTS.Controls.Add(lblApiKeyTTS);
            pnlApiKeyTTS.Controls.Add(txtApiKeyTTS);
            pnlApiKeyTTS.Location = new Point(24, 122);
            pnlApiKeyTTS.Name = "pnlApiKeyTTS";
            pnlApiKeyTTS.Size = new Size(680, 52);
            pnlApiKeyTTS.TabIndex = 3;
            pnlApiKeyTTS.Visible = false;
            // 
            // lblApiKeyTTS
            // 
            lblApiKeyTTS.AutoSize = true;
            lblApiKeyTTS.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblApiKeyTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblApiKeyTTS.Location = new Point(0, 0);
            lblApiKeyTTS.Name = "lblApiKeyTTS";
            lblApiKeyTTS.Size = new Size(47, 15);
            lblApiKeyTTS.TabIndex = 0;
            lblApiKeyTTS.Text = "API Key";
            // 
            // txtApiKeyTTS
            // 
            txtApiKeyTTS.BackColor = Color.FromArgb(15, 23, 42);
            txtApiKeyTTS.ForeColor = Color.White;
            txtApiKeyTTS.Location = new Point(0, 18);
            txtApiKeyTTS.Name = "txtApiKeyTTS";
            txtApiKeyTTS.PasswordChar = '●';
            txtApiKeyTTS.Size = new Size(680, 23);
            txtApiKeyTTS.TabIndex = 0;
            // 
            // rbGoogle
            // 
            rbGoogle.AutoSize = true;
            rbGoogle.BackColor = Color.Transparent;
            rbGoogle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbGoogle.ForeColor = Color.FromArgb(200, 215, 240);
            rbGoogle.Location = new Point(410, 92);
            rbGoogle.Name = "rbGoogle";
            rbGoogle.Size = new Size(137, 19);
            rbGoogle.TabIndex = 2;
            rbGoogle.Text = "🌐  Google Cloud TTS";
            rbGoogle.UseVisualStyleBackColor = false;
            rbGoogle.CheckedChanged += rbProveedorTTS_CheckedChanged;
            // 
            // rbOpenAI
            // 
            rbOpenAI.AutoSize = true;
            rbOpenAI.BackColor = Color.Transparent;
            rbOpenAI.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbOpenAI.ForeColor = Color.FromArgb(200, 215, 240);
            rbOpenAI.Location = new Point(240, 92);
            rbOpenAI.Name = "rbOpenAI";
            rbOpenAI.Size = new Size(104, 19);
            rbOpenAI.TabIndex = 1;
            rbOpenAI.Text = "✨  OpenAI TTS";
            rbOpenAI.UseVisualStyleBackColor = false;
            rbOpenAI.CheckedChanged += rbProveedorTTS_CheckedChanged;
            // 
            // rbSystemSpeech
            // 
            rbSystemSpeech.AutoSize = true;
            rbSystemSpeech.BackColor = Color.Transparent;
            rbSystemSpeech.Checked = true;
            rbSystemSpeech.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbSystemSpeech.ForeColor = Color.FromArgb(200, 215, 240);
            rbSystemSpeech.Location = new Point(24, 92);
            rbSystemSpeech.Name = "rbSystemSpeech";
            rbSystemSpeech.Size = new Size(171, 19);
            rbSystemSpeech.TabIndex = 0;
            rbSystemSpeech.TabStop = true;
            rbSystemSpeech.Text = "🖥️  Sistema Windows (SAPI)";
            rbSystemSpeech.UseVisualStyleBackColor = false;
            rbSystemSpeech.CheckedChanged += rbProveedorTTS_CheckedChanged;
            // 
            // lblProveedorTTS
            // 
            lblProveedorTTS.AutoSize = true;
            lblProveedorTTS.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblProveedorTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblProveedorTTS.Location = new Point(24, 66);
            lblProveedorTTS.Name = "lblProveedorTTS";
            lblProveedorTTS.Size = new Size(134, 19);
            lblProveedorTTS.TabIndex = 11;
            lblProveedorTTS.Text = "Proveedor de voz:";
            // 
            // chkActivarTTS
            // 
            chkActivarTTS.AutoSize = true;
            chkActivarTTS.BackColor = Color.Transparent;
            chkActivarTTS.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            chkActivarTTS.ForeColor = Color.FromArgb(74, 222, 128);
            chkActivarTTS.Location = new Point(24, 22);
            chkActivarTTS.Name = "chkActivarTTS";
            chkActivarTTS.Size = new Size(221, 24);
            chkActivarTTS.TabIndex = 10;
            chkActivarTTS.Text = "Activar respuestas en audio";
            chkActivarTTS.UseVisualStyleBackColor = false;
            // 
            // pnlCabeceraAudio
            // 
            pnlCabeceraAudio.BackColor = Color.FromArgb(22, 33, 50);
            pnlCabeceraAudio.Controls.Add(lblConteoAudio);
            pnlCabeceraAudio.Dock = DockStyle.Top;
            pnlCabeceraAudio.Location = new Point(4, 4);
            pnlCabeceraAudio.Name = "pnlCabeceraAudio";
            pnlCabeceraAudio.Padding = new Padding(12, 0, 12, 0);
            pnlCabeceraAudio.Size = new Size(1760, 48);
            pnlCabeceraAudio.TabIndex = 1;
            // 
            // lblConteoAudio
            // 
            lblConteoAudio.BackColor = Color.Black;
            lblConteoAudio.Dock = DockStyle.Fill;
            lblConteoAudio.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConteoAudio.ForeColor = Color.FromArgb(226, 232, 240);
            lblConteoAudio.Location = new Point(12, 0);
            lblConteoAudio.Name = "lblConteoAudio";
            lblConteoAudio.Size = new Size(1736, 48);
            lblConteoAudio.TabIndex = 0;
            lblConteoAudio.Text = "Audio TTS  ·  sin configurar";
            lblConteoAudio.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitulo.ForeColor = Color.FromArgb(100, 116, 139);
            lblSubtitulo.Location = new Point(28, 52);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(371, 15);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Configura las conexiones con Slack, Telegram y otros comunicadores";
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = Color.FromArgb(226, 232, 240);
            lblTitulo.Location = new Point(26, 18);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(180, 31);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Comunicadores";
            // 
            // FrmComunicadores
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1008, 797);
            Controls.Add(panelPrincipal);
            Name = "FrmComunicadores";
            Text = "Comunicadores";
            Load += FrmComunicadores_Load;
            panelPrincipal.ResumeLayout(false);
            panelPrincipal.PerformLayout();
            tabControl.ResumeLayout(false);
            tabSlack.ResumeLayout(false);
            pnlFormSlack.ResumeLayout(false);
            pnlFormSlack.PerformLayout();
            pnlCabeceraSlack.ResumeLayout(false);
            tabTelegram.ResumeLayout(false);
            pnlFormTelegram.ResumeLayout(false);
            pnlFormTelegram.PerformLayout();
            pnlCabeceraTelegram.ResumeLayout(false);
            tabAudio.ResumeLayout(false);
            pnlBodyAudio.ResumeLayout(false);
            pnlBodyAudio.PerformLayout();
            pnlInfoTTS.ResumeLayout(false);
            pnlApiKeyTTS.ResumeLayout(false);
            pnlApiKeyTTS.PerformLayout();
            pnlCabeceraAudio.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // ── Declaraciones ─────────────────────────────────────────────────────
        private Panel           panelPrincipal;
        private Label           lblTitulo;
        private Label           lblSubtitulo;
        private TabControl      tabControl;

        // Slack
        private TabPage         tabSlack;
        private Panel           pnlCabeceraSlack;
        private Label           lblConteoSlack;
        private Button          btnToggleSlack;
        private Panel           pnlFormSlack;
        private Label           lblSlackToken;
        private TextBox         txtSlackToken;
        private Label           lblSlackCanal;
        private TextBox         txtSlackCanal;
        private Label           lblSlackUsuarios;
        private TextBox         txtSlackUsuarios;
        private Button          btnGuardarSlack;
        private Button          btnCancelarSlack;
        private FlowLayoutPanel pnlListaSlack;

        // Telegram
        private TabPage         tabTelegram;
        private Panel           pnlCabeceraTelegram;
        private Label           lblConteoTelegram;
        private Button          btnToggleTelegram;
        private Panel           pnlFormTelegram;
        private Label           lblTelegramChatId;
        private TextBox         txtTelegramChatId;
        private Label           lblTelegramApikey;
        private TextBox         txtTelegramApikey;
        private Button          btnGuardarTelegram;
        private Button          btnCancelarTelegram;
        private FlowLayoutPanel pnlListaTelegram;

        // Audio TTS
        private TabPage         tabAudio;
        private Panel           pnlCabeceraAudio;
        private Label           lblConteoAudio;
        private CheckBox        chkActivarTTS;
        private Button          btnGuardarTTS;
        private Panel           pnlBodyAudio;
        private Label           lblProveedorTTS;
        private RadioButton     rbSystemSpeech;
        private RadioButton     rbOpenAI;
        private RadioButton     rbGoogle;
        private Panel           pnlApiKeyTTS;
        private Label           lblApiKeyTTS;
        private TextBox         txtApiKeyTTS;
        private Label           lblVozTTS;
        private ComboBox        cmbVozTTS;
        private Label           lblIdiomasTTS;
        private TextBox         txtIdiomaTTS;
        private Button          btnTestTTS;
        private Label           lblEstadoTTS;
        private Panel           pnlInfoTTS;
        private Label           lblInfoTTS;
    }
}
