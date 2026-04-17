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
            panelPrincipal      = new Panel();
            lblTitulo           = new Label();
            lblSubtitulo        = new Label();
            tabControl          = new TabControl();

            // ── Slack ──────────────────────────────────────────────────────────
            tabSlack            = new TabPage();
            pnlCabeceraSlack    = new Panel();
            lblConteoSlack      = new Label();
            btnToggleSlack      = new Button();
            pnlFormSlack        = new Panel();
            lblSlackToken       = new Label();
            txtSlackToken       = new TextBox();
            lblSlackCanal       = new Label();
            txtSlackCanal       = new TextBox();
            lblSlackUsuarios    = new Label();
            txtSlackUsuarios    = new TextBox();
            btnGuardarSlack     = new Button();
            btnCancelarSlack    = new Button();
            pnlListaSlack       = new FlowLayoutPanel();

            // ── Telegram ───────────────────────────────────────────────────────
            tabTelegram         = new TabPage();

            // ── Audio TTS ──────────────────────────────────────────────────────
            tabAudio            = new TabPage();
            pnlCabeceraAudio    = new Panel();
            lblConteoAudio      = new Label();
            pnlBodyAudio        = new Panel();
            chkActivarTTS       = new CheckBox();
            lblProveedorTTS     = new Label();
            rbSystemSpeech      = new RadioButton();
            rbOpenAI            = new RadioButton();
            rbGoogle            = new RadioButton();
            pnlApiKeyTTS        = new Panel();
            lblApiKeyTTS        = new Label();
            txtApiKeyTTS        = new TextBox();
            lblVozTTS           = new Label();
            cmbVozTTS           = new ComboBox();
            lblIdiomasTTS       = new Label();
            txtIdiomaTTS        = new TextBox();
            btnTestTTS          = new Button();
            btnGuardarTTS       = new Button();
            lblEstadoTTS        = new Label();
            pnlInfoTTS          = new Panel();
            lblInfoTTS          = new Label();
            pnlCabeceraTelegram = new Panel();
            lblConteoTelegram   = new Label();
            btnToggleTelegram   = new Button();
            pnlFormTelegram     = new Panel();
            lblTelegramChatId   = new Label();
            txtTelegramChatId   = new TextBox();
            lblTelegramApikey   = new Label();
            txtTelegramApikey   = new TextBox();
            btnGuardarTelegram  = new Button();
            btnCancelarTelegram = new Button();
            pnlListaTelegram    = new FlowLayoutPanel();

            panelPrincipal.SuspendLayout();
            tabControl.SuspendLayout();
            tabSlack.SuspendLayout();
            pnlCabeceraSlack.SuspendLayout();
            pnlFormSlack.SuspendLayout();
            tabTelegram.SuspendLayout();
            pnlCabeceraTelegram.SuspendLayout();
            pnlFormTelegram.SuspendLayout();
            tabAudio.SuspendLayout();
            pnlCabeceraAudio.SuspendLayout();
            pnlBodyAudio.SuspendLayout();
            pnlApiKeyTTS.SuspendLayout();
            pnlInfoTTS.SuspendLayout();
            SuspendLayout();

            // ── panelPrincipal ────────────────────────────────────────────────
            panelPrincipal.BackColor = Color.Transparent;
            panelPrincipal.Dock = DockStyle.Fill;
            panelPrincipal.Controls.Add(tabControl);
            panelPrincipal.Controls.Add(lblSubtitulo);
            panelPrincipal.Controls.Add(lblTitulo);
            panelPrincipal.Name = "panelPrincipal";
            panelPrincipal.TabIndex = 0;

            // ── lblTitulo ─────────────────────────────────────────────────────
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = Color.FromArgb(226, 232, 240);
            lblTitulo.Location = new Point(26, 18);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Comunicadores";

            // ── lblSubtitulo ──────────────────────────────────────────────────
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitulo.ForeColor = Color.FromArgb(100, 116, 139);
            lblSubtitulo.Location = new Point(28, 52);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Configura las conexiones con Slack, Telegram y otros comunicadores";

            // ── tabControl ────────────────────────────────────────────────────
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Location = new Point(20, 78);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(968, 700);
            tabControl.TabIndex = 2;
            tabControl.Controls.Add(tabSlack);
            tabControl.Controls.Add(tabTelegram);
            tabControl.Controls.Add(tabAudio);

            // ══════════════════════════════════════════════════════════════════
            // TAB SLACK — orden de controles: primero Fill, luego Top (de abajo a arriba)
            // ══════════════════════════════════════════════════════════════════
            tabSlack.BackColor = Color.FromArgb(15, 23, 42);
            tabSlack.Controls.Add(pnlListaSlack);       // Dock=Fill  (primero)
            tabSlack.Controls.Add(pnlFormSlack);         // Dock=Top   (segundo)
            tabSlack.Controls.Add(pnlCabeceraSlack);    // Dock=Top   (último → queda en la cima)
            tabSlack.Name = "tabSlack";
            tabSlack.Padding = new Padding(4);
            tabSlack.Size = new Size(960, 672);
            tabSlack.TabIndex = 0;
            tabSlack.Text = "  💬  Slack  ";

            // pnlCabeceraSlack ─────────────────────────────────────────────────
            pnlCabeceraSlack.BackColor = Color.FromArgb(22, 33, 50);
            pnlCabeceraSlack.Controls.Add(lblConteoSlack);
            pnlCabeceraSlack.Controls.Add(btnToggleSlack);
            pnlCabeceraSlack.Dock = DockStyle.Top;
            pnlCabeceraSlack.Height = 48;
            pnlCabeceraSlack.Name = "pnlCabeceraSlack";
            pnlCabeceraSlack.Padding = new Padding(12, 0, 12, 0);
            pnlCabeceraSlack.TabIndex = 2;

            // lblConteoSlack
            lblConteoSlack.AutoSize = false;
            lblConteoSlack.Dock = DockStyle.Fill;
            lblConteoSlack.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConteoSlack.ForeColor = Color.FromArgb(226, 232, 240);
            lblConteoSlack.Name = "lblConteoSlack";
            lblConteoSlack.TabIndex = 0;
            lblConteoSlack.Text = "Slack  (cargando...)";
            lblConteoSlack.TextAlign = ContentAlignment.MiddleLeft;

            // btnToggleSlack
            btnToggleSlack.Dock = DockStyle.Right;
            btnToggleSlack.Width = 145;
            btnToggleSlack.FlatStyle = FlatStyle.Flat;
            btnToggleSlack.FlatAppearance.BorderColor = Color.FromArgb(74, 181, 130);
            btnToggleSlack.FlatAppearance.BorderSize = 1;
            btnToggleSlack.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 40, 25);
            btnToggleSlack.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnToggleSlack.ForeColor = Color.FromArgb(74, 181, 130);
            btnToggleSlack.Name = "btnToggleSlack";
            btnToggleSlack.TabIndex = 1;
            btnToggleSlack.Text = "➕  Nueva";
            btnToggleSlack.UseVisualStyleBackColor = false;
            btnToggleSlack.Click += btnToggleSlack_Click;

            // pnlFormSlack ─────────────────────────────────────────────────────
            pnlFormSlack.BackColor = Color.FromArgb(30, 41, 59);
            pnlFormSlack.Controls.Add(btnCancelarSlack);
            pnlFormSlack.Controls.Add(btnGuardarSlack);
            pnlFormSlack.Controls.Add(txtSlackUsuarios);
            pnlFormSlack.Controls.Add(lblSlackUsuarios);
            pnlFormSlack.Controls.Add(txtSlackCanal);
            pnlFormSlack.Controls.Add(lblSlackCanal);
            pnlFormSlack.Controls.Add(txtSlackToken);
            pnlFormSlack.Controls.Add(lblSlackToken);
            pnlFormSlack.Dock = DockStyle.Top;
            pnlFormSlack.Height = 215;
            pnlFormSlack.Name = "pnlFormSlack";
            pnlFormSlack.Padding = new Padding(12, 10, 12, 10);
            pnlFormSlack.TabIndex = 1;
            pnlFormSlack.Visible = false;

            // lblSlackToken
            lblSlackToken.AutoSize = true;
            lblSlackToken.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSlackToken.ForeColor = Color.FromArgb(148, 163, 184);
            lblSlackToken.Location = new Point(17, 16);
            lblSlackToken.Name = "lblSlackToken";
            lblSlackToken.TabIndex = 0;
            lblSlackToken.Text = "Token de acceso";

            // txtSlackToken
            txtSlackToken.BackColor = Color.FromArgb(15, 23, 42);
            txtSlackToken.ForeColor = Color.White;
            txtSlackToken.Location = new Point(17, 34);
            txtSlackToken.Name = "txtSlackToken";
            txtSlackToken.Size = new Size(450, 23);
            txtSlackToken.TabIndex = 1;

            // lblSlackCanal
            lblSlackCanal.AutoSize = true;
            lblSlackCanal.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSlackCanal.ForeColor = Color.FromArgb(148, 163, 184);
            lblSlackCanal.Location = new Point(485, 16);
            lblSlackCanal.Name = "lblSlackCanal";
            lblSlackCanal.TabIndex = 2;
            lblSlackCanal.Text = "ID Canal";

            // txtSlackCanal
            txtSlackCanal.BackColor = Color.FromArgb(15, 23, 42);
            txtSlackCanal.ForeColor = Color.White;
            txtSlackCanal.Location = new Point(485, 34);
            txtSlackCanal.Name = "txtSlackCanal";
            txtSlackCanal.Size = new Size(250, 23);
            txtSlackCanal.TabIndex = 3;

            // lblSlackUsuarios
            lblSlackUsuarios.AutoSize = true;
            lblSlackUsuarios.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSlackUsuarios.ForeColor = Color.FromArgb(148, 163, 184);
            lblSlackUsuarios.Location = new Point(17, 70);
            lblSlackUsuarios.Name = "lblSlackUsuarios";
            lblSlackUsuarios.TabIndex = 4;
            lblSlackUsuarios.Text = "IDs de usuarios (separados por coma)";

            // txtSlackUsuarios
            txtSlackUsuarios.BackColor = Color.FromArgb(15, 23, 42);
            txtSlackUsuarios.ForeColor = Color.White;
            txtSlackUsuarios.Location = new Point(17, 88);
            txtSlackUsuarios.Multiline = true;
            txtSlackUsuarios.Name = "txtSlackUsuarios";
            txtSlackUsuarios.Size = new Size(718, 65);
            txtSlackUsuarios.TabIndex = 5;

            // btnGuardarSlack
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

            // btnCancelarSlack
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

            // pnlListaSlack ────────────────────────────────────────────────────
            pnlListaSlack.Dock = DockStyle.Fill;
            pnlListaSlack.Name = "pnlListaSlack";
            pnlListaSlack.TabIndex = 0;

            // ══════════════════════════════════════════════════════════════════
            // TAB TELEGRAM
            // ══════════════════════════════════════════════════════════════════
            tabTelegram.BackColor = Color.FromArgb(15, 23, 42);
            tabTelegram.Controls.Add(pnlListaTelegram);
            tabTelegram.Controls.Add(pnlFormTelegram);
            tabTelegram.Controls.Add(pnlCabeceraTelegram);
            tabTelegram.Name = "tabTelegram";
            tabTelegram.Padding = new Padding(4);
            tabTelegram.Size = new Size(960, 672);
            tabTelegram.TabIndex = 1;
            tabTelegram.Text = "  ✈  Telegram  ";

            // pnlCabeceraTelegram ──────────────────────────────────────────────
            pnlCabeceraTelegram.BackColor = Color.FromArgb(22, 33, 50);
            pnlCabeceraTelegram.Controls.Add(lblConteoTelegram);
            pnlCabeceraTelegram.Controls.Add(btnToggleTelegram);
            pnlCabeceraTelegram.Dock = DockStyle.Top;
            pnlCabeceraTelegram.Height = 48;
            pnlCabeceraTelegram.Name = "pnlCabeceraTelegram";
            pnlCabeceraTelegram.Padding = new Padding(12, 0, 12, 0);
            pnlCabeceraTelegram.TabIndex = 2;

            // lblConteoTelegram
            lblConteoTelegram.AutoSize = false;
            lblConteoTelegram.Dock = DockStyle.Fill;
            lblConteoTelegram.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConteoTelegram.ForeColor = Color.FromArgb(226, 232, 240);
            lblConteoTelegram.Name = "lblConteoTelegram";
            lblConteoTelegram.TabIndex = 0;
            lblConteoTelegram.Text = "Telegram  (cargando...)";
            lblConteoTelegram.TextAlign = ContentAlignment.MiddleLeft;

            // btnToggleTelegram
            btnToggleTelegram.Dock = DockStyle.Right;
            btnToggleTelegram.Width = 145;
            btnToggleTelegram.FlatStyle = FlatStyle.Flat;
            btnToggleTelegram.FlatAppearance.BorderColor = Color.FromArgb(41, 182, 246);
            btnToggleTelegram.FlatAppearance.BorderSize = 1;
            btnToggleTelegram.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 20, 50);
            btnToggleTelegram.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnToggleTelegram.ForeColor = Color.FromArgb(41, 182, 246);
            btnToggleTelegram.Name = "btnToggleTelegram";
            btnToggleTelegram.TabIndex = 1;
            btnToggleTelegram.Text = "➕  Nueva";
            btnToggleTelegram.UseVisualStyleBackColor = false;
            btnToggleTelegram.Click += btnToggleTelegram_Click;

            // pnlFormTelegram ──────────────────────────────────────────────────
            pnlFormTelegram.BackColor = Color.FromArgb(30, 41, 59);
            pnlFormTelegram.Controls.Add(btnCancelarTelegram);
            pnlFormTelegram.Controls.Add(btnGuardarTelegram);
            pnlFormTelegram.Controls.Add(txtTelegramApikey);
            pnlFormTelegram.Controls.Add(lblTelegramApikey);
            pnlFormTelegram.Controls.Add(txtTelegramChatId);
            pnlFormTelegram.Controls.Add(lblTelegramChatId);
            pnlFormTelegram.Dock = DockStyle.Top;
            pnlFormTelegram.Height = 148;
            pnlFormTelegram.Name = "pnlFormTelegram";
            pnlFormTelegram.Padding = new Padding(12, 10, 12, 10);
            pnlFormTelegram.TabIndex = 1;
            pnlFormTelegram.Visible = false;

            // lblTelegramChatId
            lblTelegramChatId.AutoSize = true;
            lblTelegramChatId.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTelegramChatId.ForeColor = Color.FromArgb(148, 163, 184);
            lblTelegramChatId.Location = new Point(17, 16);
            lblTelegramChatId.Name = "lblTelegramChatId";
            lblTelegramChatId.TabIndex = 0;
            lblTelegramChatId.Text = "Chat ID";

            // txtTelegramChatId
            txtTelegramChatId.BackColor = Color.FromArgb(15, 23, 42);
            txtTelegramChatId.ForeColor = Color.White;
            txtTelegramChatId.Location = new Point(17, 34);
            txtTelegramChatId.Name = "txtTelegramChatId";
            txtTelegramChatId.Size = new Size(230, 23);
            txtTelegramChatId.TabIndex = 1;

            // lblTelegramApikey
            lblTelegramApikey.AutoSize = true;
            lblTelegramApikey.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTelegramApikey.ForeColor = Color.FromArgb(148, 163, 184);
            lblTelegramApikey.Location = new Point(265, 16);
            lblTelegramApikey.Name = "lblTelegramApikey";
            lblTelegramApikey.TabIndex = 2;
            lblTelegramApikey.Text = "API Key";

            // txtTelegramApikey
            txtTelegramApikey.BackColor = Color.FromArgb(15, 23, 42);
            txtTelegramApikey.ForeColor = Color.White;
            txtTelegramApikey.Location = new Point(265, 34);
            txtTelegramApikey.Name = "txtTelegramApikey";
            txtTelegramApikey.Size = new Size(480, 23);
            txtTelegramApikey.TabIndex = 3;

            // btnGuardarTelegram
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

            // btnCancelarTelegram
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

            // pnlListaTelegram ─────────────────────────────────────────────────
            pnlListaTelegram.Dock = DockStyle.Fill;
            pnlListaTelegram.Name = "pnlListaTelegram";
            pnlListaTelegram.TabIndex = 0;

            // ══════════════════════════════════════════════════════════════════
            // TAB AUDIO TTS
            // ══════════════════════════════════════════════════════════════════
            tabAudio.BackColor = Color.FromArgb(15, 23, 42);
            tabAudio.Controls.Add(pnlBodyAudio);
            tabAudio.Controls.Add(pnlCabeceraAudio);
            tabAudio.Name = "tabAudio";
            tabAudio.Padding = new Padding(4);
            tabAudio.Size = new Size(960, 672);
            tabAudio.TabIndex = 2;
            tabAudio.Text = "  🔊  Audio TTS  ";

            // pnlCabeceraAudio — solo muestra el estado actual, sin botones ───
            pnlCabeceraAudio.BackColor = Color.FromArgb(22, 33, 50);
            pnlCabeceraAudio.Controls.Add(lblConteoAudio);
            pnlCabeceraAudio.Dock = DockStyle.Top;
            pnlCabeceraAudio.Height = 48;
            pnlCabeceraAudio.Name = "pnlCabeceraAudio";
            pnlCabeceraAudio.Padding = new Padding(12, 0, 12, 0);
            pnlCabeceraAudio.TabIndex = 1;

            // lblConteoAudio
            lblConteoAudio.AutoSize = false;
            lblConteoAudio.Dock = DockStyle.Fill;
            lblConteoAudio.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConteoAudio.ForeColor = Color.FromArgb(226, 232, 240);
            lblConteoAudio.Name = "lblConteoAudio";
            lblConteoAudio.TabIndex = 0;
            lblConteoAudio.Text = "Audio TTS  ·  sin configurar";
            lblConteoAudio.TextAlign = ContentAlignment.MiddleLeft;

            // pnlBodyAudio ─────────────────────────────────────────────────────
            pnlBodyAudio.AutoScroll = true;
            pnlBodyAudio.BackColor = Color.FromArgb(15, 23, 42);
            pnlBodyAudio.Dock = DockStyle.Fill;
            pnlBodyAudio.Name = "pnlBodyAudio";
            pnlBodyAudio.Padding = new Padding(24, 20, 24, 20);
            pnlBodyAudio.TabIndex = 0;
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

            // chkActivarTTS — toggle principal, prominente, arriba del todo ────
            chkActivarTTS.AutoSize = true;
            chkActivarTTS.BackColor = Color.Transparent;
            chkActivarTTS.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            chkActivarTTS.ForeColor = Color.FromArgb(74, 222, 128);
            chkActivarTTS.Location = new Point(24, 22);
            chkActivarTTS.Name = "chkActivarTTS";
            chkActivarTTS.TabIndex = 10;
            chkActivarTTS.Text = "Activar respuestas en audio";
            chkActivarTTS.UseVisualStyleBackColor = false;

            // lblProveedorTTS ──────────────────────────────────────────────────
            lblProveedorTTS.AutoSize = true;
            lblProveedorTTS.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblProveedorTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblProveedorTTS.Location = new Point(24, 66);
            lblProveedorTTS.Name = "lblProveedorTTS";
            lblProveedorTTS.Text = "Proveedor de voz:";

            // rbSystemSpeech ───────────────────────────────────────────────────
            rbSystemSpeech.AutoSize = true;
            rbSystemSpeech.BackColor = Color.Transparent;
            rbSystemSpeech.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbSystemSpeech.ForeColor = Color.FromArgb(200, 215, 240);
            rbSystemSpeech.Location = new Point(24, 92);
            rbSystemSpeech.Name = "rbSystemSpeech";
            rbSystemSpeech.TabIndex = 0;
            rbSystemSpeech.Text = "🖥️  Sistema Windows (SAPI)";
            rbSystemSpeech.Checked = true;
            rbSystemSpeech.UseVisualStyleBackColor = false;
            rbSystemSpeech.CheckedChanged += rbProveedorTTS_CheckedChanged;

            // rbOpenAI ─────────────────────────────────────────────────────────
            rbOpenAI.AutoSize = true;
            rbOpenAI.BackColor = Color.Transparent;
            rbOpenAI.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbOpenAI.ForeColor = Color.FromArgb(200, 215, 240);
            rbOpenAI.Location = new Point(240, 92);
            rbOpenAI.Name = "rbOpenAI";
            rbOpenAI.TabIndex = 1;
            rbOpenAI.Text = "✨  OpenAI TTS";
            rbOpenAI.UseVisualStyleBackColor = false;
            rbOpenAI.CheckedChanged += rbProveedorTTS_CheckedChanged;

            // rbGoogle ─────────────────────────────────────────────────────────
            rbGoogle.AutoSize = true;
            rbGoogle.BackColor = Color.Transparent;
            rbGoogle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rbGoogle.ForeColor = Color.FromArgb(200, 215, 240);
            rbGoogle.Location = new Point(410, 92);
            rbGoogle.Name = "rbGoogle";
            rbGoogle.TabIndex = 2;
            rbGoogle.Text = "🌐  Google Cloud TTS";
            rbGoogle.UseVisualStyleBackColor = false;
            rbGoogle.CheckedChanged += rbProveedorTTS_CheckedChanged;

            // pnlApiKeyTTS ─────────────────────────────────────────────────────
            pnlApiKeyTTS.BackColor = Color.Transparent;
            pnlApiKeyTTS.Controls.Add(lblApiKeyTTS);
            pnlApiKeyTTS.Controls.Add(txtApiKeyTTS);
            pnlApiKeyTTS.Location = new Point(24, 122);
            pnlApiKeyTTS.Name = "pnlApiKeyTTS";
            pnlApiKeyTTS.Size = new Size(680, 52);
            pnlApiKeyTTS.TabIndex = 3;
            pnlApiKeyTTS.Visible = false;

            lblApiKeyTTS.AutoSize = true;
            lblApiKeyTTS.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblApiKeyTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblApiKeyTTS.Location = new Point(0, 0);
            lblApiKeyTTS.Name = "lblApiKeyTTS";
            lblApiKeyTTS.Text = "API Key";

            txtApiKeyTTS.BackColor = Color.FromArgb(15, 23, 42);
            txtApiKeyTTS.ForeColor = Color.White;
            txtApiKeyTTS.Location = new Point(0, 18);
            txtApiKeyTTS.Name = "txtApiKeyTTS";
            txtApiKeyTTS.PasswordChar = '●';
            txtApiKeyTTS.Size = new Size(680, 23);
            txtApiKeyTTS.TabIndex = 0;

            // lblVozTTS ────────────────────────────────────────────────────────
            lblVozTTS.AutoSize = true;
            lblVozTTS.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblVozTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblVozTTS.Location = new Point(24, 190);
            lblVozTTS.Name = "lblVozTTS";
            lblVozTTS.Text = "Voz";

            // cmbVozTTS ────────────────────────────────────────────────────────
            cmbVozTTS.BackColor = Color.FromArgb(30, 41, 59);
            cmbVozTTS.DrawMode = DrawMode.Normal;
            cmbVozTTS.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVozTTS.FlatStyle = FlatStyle.Flat;
            cmbVozTTS.ForeColor = Color.White;
            cmbVozTTS.FormattingEnabled = true;
            cmbVozTTS.Location = new Point(24, 208);
            cmbVozTTS.Name = "cmbVozTTS";
            cmbVozTTS.Size = new Size(320, 23);
            cmbVozTTS.TabIndex = 4;

            // lblIdiomasTTS ────────────────────────────────────────────────────
            lblIdiomasTTS.AutoSize = true;
            lblIdiomasTTS.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblIdiomasTTS.ForeColor = Color.FromArgb(148, 163, 184);
            lblIdiomasTTS.Location = new Point(370, 190);
            lblIdiomasTTS.Name = "lblIdiomasTTS";
            lblIdiomasTTS.Text = "Código de idioma (ej. es-ES, es-MX)";
            lblIdiomasTTS.Visible = false;

            // txtIdiomaTTS ─────────────────────────────────────────────────────
            txtIdiomaTTS.BackColor = Color.FromArgb(15, 23, 42);
            txtIdiomaTTS.ForeColor = Color.White;
            txtIdiomaTTS.Location = new Point(370, 208);
            txtIdiomaTTS.Name = "txtIdiomaTTS";
            txtIdiomaTTS.Size = new Size(160, 23);
            txtIdiomaTTS.TabIndex = 5;
            txtIdiomaTTS.Text = "es-ES";
            txtIdiomaTTS.Visible = false;

            // btnTestTTS ───────────────────────────────────────────────────────
            btnTestTTS.FlatStyle = FlatStyle.Flat;
            btnTestTTS.FlatAppearance.BorderColor = Color.FromArgb(139, 92, 246);
            btnTestTTS.FlatAppearance.BorderSize = 1;
            btnTestTTS.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 10, 60);
            btnTestTTS.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnTestTTS.ForeColor = Color.FromArgb(196, 181, 253);
            btnTestTTS.Location = new Point(24, 250);
            btnTestTTS.Name = "btnTestTTS";
            btnTestTTS.Size = new Size(160, 32);
            btnTestTTS.TabIndex = 6;
            btnTestTTS.Text = "🔊  Probar audio";
            btnTestTTS.UseVisualStyleBackColor = false;
            btnTestTTS.Click += btnTestTTS_Click;

            // btnGuardarTTS — prominente, junto al botón Probar ────────────────
            btnGuardarTTS.FlatStyle = FlatStyle.Flat;
            btnGuardarTTS.FlatAppearance.BorderColor = Color.FromArgb(74, 222, 128);
            btnGuardarTTS.FlatAppearance.BorderSize = 1;
            btnGuardarTTS.FlatAppearance.CheckedBackColor = Color.FromArgb(20, 80, 40);
            btnGuardarTTS.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 50, 25);
            btnGuardarTTS.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnGuardarTTS.ForeColor = Color.FromArgb(74, 222, 128);
            btnGuardarTTS.Location = new Point(200, 250);
            btnGuardarTTS.Name = "btnGuardarTTS";
            btnGuardarTTS.Size = new Size(160, 32);
            btnGuardarTTS.TabIndex = 7;
            btnGuardarTTS.Text = "💾  Guardar";
            btnGuardarTTS.UseVisualStyleBackColor = false;
            btnGuardarTTS.Click += btnGuardarTTS_Click;

            // lblEstadoTTS ─────────────────────────────────────────────────────
            lblEstadoTTS.AutoSize = true;
            lblEstadoTTS.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEstadoTTS.ForeColor = Color.FromArgb(100, 116, 139);
            lblEstadoTTS.Location = new Point(24, 292);
            lblEstadoTTS.Name = "lblEstadoTTS";
            lblEstadoTTS.Text = "";

            // pnlInfoTTS ───────────────────────────────────────────────────────
            pnlInfoTTS.BackColor = Color.FromArgb(22, 30, 48);
            pnlInfoTTS.Location = new Point(24, 320);
            pnlInfoTTS.Name = "pnlInfoTTS";
            pnlInfoTTS.Size = new Size(680, 120);
            pnlInfoTTS.Padding = new Padding(14, 10, 14, 10);
            pnlInfoTTS.Controls.Add(lblInfoTTS);
            pnlInfoTTS.TabIndex = 7;

            lblInfoTTS.AutoSize = false;
            lblInfoTTS.Dock = DockStyle.Fill;
            lblInfoTTS.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblInfoTTS.ForeColor = Color.FromArgb(100, 130, 180);
            lblInfoTTS.Name = "lblInfoTTS";
            lblInfoTTS.Text =
                "🖥️  Sistema Windows (SAPI): sin costo, usa voces instaladas en Windows. " +
                "Calidad básica. El audio se envía como archivo .wav.\r\n\r\n" +
                "✨  OpenAI TTS: requiere API Key de OpenAI. Voces neurales de alta calidad " +
                "(nova, alloy, echo, fable, onyx, shimmer). Audio en formato MP3.\r\n\r\n" +
                "🌐  Google Cloud TTS: requiere API Key de Google Cloud con TTS habilitado. " +
                "Voces Neural2 en múltiples idiomas y acentos. Audio en formato MP3.";

            // ── FrmComunicadores ──────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(1008, 797);
            Controls.Add(panelPrincipal);
            Name = "FrmComunicadores";
            Text = "Comunicadores";
            Load += FrmComunicadores_Load;

            panelPrincipal.ResumeLayout(false);
            panelPrincipal.PerformLayout();
            tabControl.ResumeLayout(false);
            tabSlack.ResumeLayout(false);
            pnlCabeceraSlack.ResumeLayout(false);
            pnlFormSlack.ResumeLayout(false);
            pnlFormSlack.PerformLayout();
            tabTelegram.ResumeLayout(false);
            pnlCabeceraTelegram.ResumeLayout(false);
            pnlFormTelegram.ResumeLayout(false);
            pnlFormTelegram.PerformLayout();
            tabAudio.ResumeLayout(false);
            pnlCabeceraAudio.ResumeLayout(false);
            pnlBodyAudio.ResumeLayout(false);
            pnlBodyAudio.PerformLayout();
            pnlApiKeyTTS.ResumeLayout(false);
            pnlApiKeyTTS.PerformLayout();
            pnlInfoTTS.ResumeLayout(false);
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
