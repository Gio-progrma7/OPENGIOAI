using OPENGIOAI.Themas;

namespace OPENGIOAI.Vistas
{
    partial class FrmMandos
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlContenedorTxt = new Panel();
            ChkNotiT = new PictureBox();
            ChkNotiS = new PictureBox();
            ChkConver = new PictureBox();
            ChkRes = new PictureBox();
            ChkEstado = new PictureBox();
            lblConsumoA = new Label();
            lblTimeR = new Label();
            lblTime = new Label();
            lblConsumo = new Label();
            pnlContenedorArchivos = new FlowLayoutPanel();
            labelSugerencia = new Label();
            textBoxInstrucion = new RichTextBox();
            btnEnviar = new Button();
            btnCancelar = new Button();
            checkBoxSoloChat = new CheckBox();
            checkBoxRapida = new CheckBox();
            checkBoxRecordar = new CheckBox();
            checkBoxTelegram = new CheckBox();
            checkBoxConstructorTelegram = new CheckBox();
            checkBoxArchivosTelegram = new CheckBox();
            checkBoxSlack = new CheckBox();
            checkBoxConstructorSlack = new CheckBox();
            checkBoxArchivosSlack = new CheckBox();
            checkBoxAudio = new CheckBox();
            btnLimpiar = new Button();
            btnInfo = new Button();
            pnlChat = new FlowLayoutPanel();
            panelHead = new Panel();
            btnGuardar = new Button();
            label3 = new Label();
            comboBoxModeloIA = new ModernComboBox();
            label2 = new Label();
            label1 = new Label();
            comboBoxRuta = new ModernComboBox();
            comboBoxAgentes = new ModernComboBox();
            pnlContenedorTxt.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ChkNotiT).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ChkNotiS).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ChkConver).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ChkRes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ChkEstado).BeginInit();
            panelHead.SuspendLayout();
            SuspendLayout();
            // 
            // pnlContenedorTxt
            // 
            pnlContenedorTxt.BackColor = Color.Transparent;
            pnlContenedorTxt.BackgroundImageLayout = ImageLayout.Stretch;
            pnlContenedorTxt.Controls.Add(ChkNotiT);
            pnlContenedorTxt.Controls.Add(ChkNotiS);
            pnlContenedorTxt.Controls.Add(ChkConver);
            pnlContenedorTxt.Controls.Add(ChkRes);
            pnlContenedorTxt.Controls.Add(ChkEstado);
            pnlContenedorTxt.Controls.Add(lblConsumoA);
            pnlContenedorTxt.Controls.Add(lblTimeR);
            pnlContenedorTxt.Controls.Add(lblTime);
            pnlContenedorTxt.Controls.Add(lblConsumo);
            pnlContenedorTxt.Controls.Add(pnlContenedorArchivos);
            pnlContenedorTxt.Controls.Add(labelSugerencia);
            pnlContenedorTxt.Controls.Add(textBoxInstrucion);
            pnlContenedorTxt.Controls.Add(btnEnviar);
            pnlContenedorTxt.Controls.Add(btnCancelar);
            pnlContenedorTxt.Controls.Add(checkBoxSoloChat);
            pnlContenedorTxt.Controls.Add(checkBoxRapida);
            pnlContenedorTxt.Controls.Add(checkBoxRecordar);
            pnlContenedorTxt.Controls.Add(checkBoxTelegram);
            pnlContenedorTxt.Controls.Add(checkBoxConstructorTelegram);
            pnlContenedorTxt.Controls.Add(checkBoxArchivosTelegram);
            pnlContenedorTxt.Controls.Add(checkBoxSlack);
            pnlContenedorTxt.Controls.Add(checkBoxConstructorSlack);
            pnlContenedorTxt.Controls.Add(checkBoxArchivosSlack);
            pnlContenedorTxt.Controls.Add(checkBoxAudio);
            pnlContenedorTxt.Controls.Add(btnLimpiar);
            pnlContenedorTxt.Controls.Add(btnInfo);
            pnlContenedorTxt.Dock = DockStyle.Bottom;
            pnlContenedorTxt.Location = new Point(0, 487);
            pnlContenedorTxt.Name = "pnlContenedorTxt";
            pnlContenedorTxt.Size = new Size(915, 168);
            pnlContenedorTxt.TabIndex = 0;
            // 
            // ChkNotiT
            // 
            ChkNotiT.Image = Properties.Resources.selec;
            ChkNotiT.Location = new Point(8, 5);
            ChkNotiT.Name = "ChkNotiT";
            ChkNotiT.Size = new Size(15, 15);
            ChkNotiT.TabIndex = 17;
            ChkNotiT.TabStop = false;
            ChkNotiT.Visible = false;
            // 
            // ChkNotiS
            // 
            ChkNotiS.Image = Properties.Resources.selec;
            ChkNotiS.Location = new Point(115, 7);
            ChkNotiS.Name = "ChkNotiS";
            ChkNotiS.Size = new Size(15, 15);
            ChkNotiS.TabIndex = 18;
            ChkNotiS.TabStop = false;
            ChkNotiS.Visible = false;
            // 
            // ChkConver
            // 
            ChkConver.Image = Properties.Resources.selec;
            ChkConver.Location = new Point(262, 5);
            ChkConver.Name = "ChkConver";
            ChkConver.Size = new Size(15, 15);
            ChkConver.TabIndex = 16;
            ChkConver.TabStop = false;
            ChkConver.Visible = false;
            ChkConver.Click += ChkConver_Click;
            // 
            // ChkRes
            // 
            ChkRes.Image = Properties.Resources.selec;
            ChkRes.Location = new Point(88, 136);
            ChkRes.Name = "ChkRes";
            ChkRes.Size = new Size(15, 15);
            ChkRes.TabIndex = 15;
            ChkRes.TabStop = false;
            ChkRes.Visible = false;
            ChkRes.Click += ChkRes_Click;
            // 
            // ChkEstado
            // 
            ChkEstado.Image = Properties.Resources.selec;
            ChkEstado.Location = new Point(200, 136);
            ChkEstado.Name = "ChkEstado";
            ChkEstado.Size = new Size(15, 15);
            ChkEstado.TabIndex = 14;
            ChkEstado.TabStop = false;
            ChkEstado.Visible = false;
            ChkEstado.Click += ChkEstado_Click;
            // 
            // lblConsumoA
            // 
            lblConsumoA.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblConsumoA.AutoSize = true;
            lblConsumoA.ForeColor = Color.FromArgb(140, 160, 190);
            lblConsumoA.Location = new Point(626, 3);
            lblConsumoA.Name = "lblConsumoA";
            lblConsumoA.Size = new Size(0, 15);
            lblConsumoA.TabIndex = 10;
            // 
            // lblTimeR
            // 
            lblTimeR.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTimeR.AutoSize = true;
            lblTimeR.BackColor = Color.Transparent;
            lblTimeR.ForeColor = Color.MediumSpringGreen;
            lblTimeR.Location = new Point(620, 18);
            lblTimeR.Name = "lblTimeR";
            lblTimeR.Size = new Size(0, 15);
            lblTimeR.TabIndex = 9;
            // 
            // lblTime
            // 
            lblTime.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTime.AutoSize = true;
            lblTime.BackColor = Color.Transparent;
            lblTime.ForeColor = Color.GreenYellow;
            lblTime.Location = new Point(620, 5);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(0, 15);
            lblTime.TabIndex = 8;
            // 
            // lblConsumo
            // 
            lblConsumo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblConsumo.AutoSize = true;
            lblConsumo.ForeColor = Color.FromArgb(140, 160, 190);
            lblConsumo.Location = new Point(644, 5);
            lblConsumo.Name = "lblConsumo";
            lblConsumo.Size = new Size(0, 15);
            lblConsumo.TabIndex = 0;
            // 
            // pnlContenedorArchivos
            // 
            pnlContenedorArchivos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlContenedorArchivos.Location = new Point(8, 24);
            pnlContenedorArchivos.Name = "pnlContenedorArchivos";
            pnlContenedorArchivos.Size = new Size(560, 0);
            pnlContenedorArchivos.TabIndex = 7;
            // 
            // labelSugerencia
            // 
            labelSugerencia.AutoSize = true;
            labelSugerencia.BackColor = Color.Transparent;
            labelSugerencia.Location = new Point(4, 4);
            labelSugerencia.Name = "labelSugerencia";
            labelSugerencia.Size = new Size(0, 15);
            labelSugerencia.TabIndex = 3;
            // 
            // textBoxInstrucion
            // 
            textBoxInstrucion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxInstrucion.BackColor = Color.FromArgb(30, 41, 59);
            textBoxInstrucion.ForeColor = Color.Gainsboro;
            textBoxInstrucion.Location = new Point(8, 26);
            textBoxInstrucion.Name = "textBoxInstrucion";
            textBoxInstrucion.Size = new Size(851, 78);
            textBoxInstrucion.TabIndex = 4;
            textBoxInstrucion.Text = "";
            textBoxInstrucion.TextChanged += textBoxInstrucion_TextChanged;
            textBoxInstrucion.KeyDown += textBoxInstrucion_KeyDown;
            // 
            // btnEnviar
            // 
            btnEnviar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEnviar.BackgroundImage = Properties.Resources.enviar1;
            btnEnviar.BackgroundImageLayout = ImageLayout.Center;
            btnEnviar.FlatAppearance.BorderColor = Color.SteelBlue;
            btnEnviar.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 80, 180);
            btnEnviar.FlatAppearance.MouseOverBackColor = Color.Navy;
            btnEnviar.FlatStyle = FlatStyle.Flat;
            btnEnviar.ForeColor = Color.White;
            btnEnviar.Location = new Point(866, 26);
            btnEnviar.Name = "btnEnviar";
            btnEnviar.Size = new Size(40, 38);
            btnEnviar.TabIndex = 1;
            btnEnviar.Tag = "#0F5DF7";
            btnEnviar.UseVisualStyleBackColor = true;
            btnEnviar.Click += btnEnviar_Click;
            // 
            // btnCancelar
            // 
            btnCancelar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancelar.BackgroundImage = Properties.Resources.cancell;
            btnCancelar.BackgroundImageLayout = ImageLayout.Center;
            btnCancelar.FlatAppearance.BorderColor = Color.Crimson;
            btnCancelar.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 30, 30);
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.LightPink;
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.ForeColor = Color.White;
            btnCancelar.Location = new Point(866, 68);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(40, 36);
            btnCancelar.TabIndex = 11;
            btnCancelar.Tag = "#0F5DF7";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // checkBoxSoloChat
            // 
            checkBoxSoloChat.AutoSize = true;
            checkBoxSoloChat.BackColor = Color.Transparent;
            checkBoxSoloChat.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxSoloChat.ForeColor = Color.FromArgb(180, 195, 220);
            checkBoxSoloChat.Location = new Point(8, 5);
            checkBoxSoloChat.Name = "checkBoxSoloChat";
            checkBoxSoloChat.Size = new Size(101, 17);
            checkBoxSoloChat.TabIndex = 2;
            checkBoxSoloChat.Text = "Estado Lectura";
            checkBoxSoloChat.UseVisualStyleBackColor = false;
            checkBoxSoloChat.CheckedChanged += checkBoxSoloChat_CheckedChanged;
            // 
            // checkBoxRapida
            // 
            checkBoxRapida.AutoSize = true;
            checkBoxRapida.BackColor = Color.Transparent;
            checkBoxRapida.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxRapida.ForeColor = Color.FromArgb(180, 195, 220);
            checkBoxRapida.Location = new Point(116, 5);
            checkBoxRapida.Name = "checkBoxRapida";
            checkBoxRapida.Size = new Size(141, 17);
            checkBoxRapida.TabIndex = 13;
            checkBoxRapida.Text = "Solo respuesta Rapida";
            checkBoxRapida.UseVisualStyleBackColor = false;
            checkBoxRapida.CheckedChanged += checkBoxRapida_CheckedChanged;
            // 
            // checkBoxRecordar
            // 
            checkBoxRecordar.AutoSize = true;
            checkBoxRecordar.BackColor = Color.Transparent;
            checkBoxRecordar.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxRecordar.ForeColor = Color.FromArgb(180, 195, 220);
            checkBoxRecordar.Location = new Point(262, 5);
            checkBoxRecordar.Name = "checkBoxRecordar";
            checkBoxRecordar.Size = new Size(146, 17);
            checkBoxRecordar.TabIndex = 1;
            checkBoxRecordar.Text = "Mantener conversación";
            checkBoxRecordar.UseVisualStyleBackColor = false;
            checkBoxRecordar.CheckedChanged += checkBoxRecordar_CheckedChanged;
            // 
            // checkBoxTelegram
            // 
            checkBoxTelegram.Appearance = Appearance.Button;
            checkBoxTelegram.BackColor = Color.FromArgb(22, 34, 54);
            checkBoxTelegram.FlatAppearance.BorderColor = Color.FromArgb(40, 100, 180);
            checkBoxTelegram.FlatAppearance.CheckedBackColor = Color.FromArgb(20, 60, 130);
            checkBoxTelegram.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 60, 110);
            checkBoxTelegram.FlatStyle = FlatStyle.Flat;
            checkBoxTelegram.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxTelegram.ForeColor = Color.FromArgb(140, 180, 240);
            checkBoxTelegram.Location = new Point(88, 109);
            checkBoxTelegram.Name = "checkBoxTelegram";
            checkBoxTelegram.Padding = new Padding(4, 0, 4, 0);
            checkBoxTelegram.Size = new Size(85, 24);
            checkBoxTelegram.TabIndex = 5;
            checkBoxTelegram.Text = "📱 Telegram";
            checkBoxTelegram.UseVisualStyleBackColor = false;
            checkBoxTelegram.CheckedChanged += checkBoxTelegram_CheckedChanged;
            // 
            // checkBoxConstructorTelegram
            // 
            checkBoxConstructorTelegram.AutoSize = true;
            checkBoxConstructorTelegram.BackColor = Color.Transparent;
            checkBoxConstructorTelegram.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxConstructorTelegram.ForeColor = Color.FromArgb(100, 130, 175);
            checkBoxConstructorTelegram.Location = new Point(90, 136);
            checkBoxConstructorTelegram.Name = "checkBoxConstructorTelegram";
            checkBoxConstructorTelegram.Size = new Size(96, 16);
            checkBoxConstructorTelegram.TabIndex = 15;
            checkBoxConstructorTelegram.Text = "↳ datos técnicos";
            checkBoxConstructorTelegram.UseVisualStyleBackColor = false;
            checkBoxConstructorTelegram.CheckedChanged += checkBoxConstructorTelegram_CheckedChanged;
            // 
            // checkBoxArchivosTelegram
            // 
            checkBoxArchivosTelegram.AutoSize = true;
            checkBoxArchivosTelegram.BackColor = Color.Transparent;
            checkBoxArchivosTelegram.Enabled = false;
            checkBoxArchivosTelegram.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxArchivosTelegram.ForeColor = Color.FromArgb(100, 130, 175);
            checkBoxArchivosTelegram.Location = new Point(90, 152);
            checkBoxArchivosTelegram.Name = "checkBoxArchivosTelegram";
            checkBoxArchivosTelegram.Size = new Size(98, 16);
            checkBoxArchivosTelegram.TabIndex = 22;
            checkBoxArchivosTelegram.Text = "↳ enviar archivos";
            checkBoxArchivosTelegram.UseVisualStyleBackColor = false;
            checkBoxArchivosTelegram.CheckedChanged += checkBoxArchivosTelegram_CheckedChanged;
            // 
            // checkBoxSlack
            // 
            checkBoxSlack.Appearance = Appearance.Button;
            checkBoxSlack.BackColor = Color.FromArgb(22, 34, 54);
            checkBoxSlack.FlatAppearance.BorderColor = Color.FromArgb(80, 55, 130);
            checkBoxSlack.FlatAppearance.CheckedBackColor = Color.FromArgb(55, 30, 100);
            checkBoxSlack.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 35, 85);
            checkBoxSlack.FlatStyle = FlatStyle.Flat;
            checkBoxSlack.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxSlack.ForeColor = Color.FromArgb(170, 140, 240);
            checkBoxSlack.Location = new Point(198, 109);
            checkBoxSlack.Name = "checkBoxSlack";
            checkBoxSlack.Padding = new Padding(4, 0, 4, 0);
            checkBoxSlack.Size = new Size(92, 24);
            checkBoxSlack.TabIndex = 12;
            checkBoxSlack.Text = "💬 Slack";
            checkBoxSlack.TextAlign = ContentAlignment.MiddleCenter;
            checkBoxSlack.UseVisualStyleBackColor = false;
            checkBoxSlack.CheckedChanged += checkBoxSlack_CheckedChanged;
            // 
            // checkBoxConstructorSlack
            // 
            checkBoxConstructorSlack.AutoSize = true;
            checkBoxConstructorSlack.BackColor = Color.Transparent;
            checkBoxConstructorSlack.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxConstructorSlack.ForeColor = Color.FromArgb(100, 130, 175);
            checkBoxConstructorSlack.Location = new Point(200, 136);
            checkBoxConstructorSlack.Name = "checkBoxConstructorSlack";
            checkBoxConstructorSlack.Size = new Size(96, 16);
            checkBoxConstructorSlack.TabIndex = 20;
            checkBoxConstructorSlack.Text = "↳ datos técnicos";
            checkBoxConstructorSlack.UseVisualStyleBackColor = false;
            checkBoxConstructorSlack.CheckedChanged += checkBoxConstructorSlack_CheckedChanged;
            // 
            // checkBoxArchivosSlack
            // 
            checkBoxArchivosSlack.AutoSize = true;
            checkBoxArchivosSlack.BackColor = Color.Transparent;
            checkBoxArchivosSlack.Enabled = false;
            checkBoxArchivosSlack.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxArchivosSlack.ForeColor = Color.FromArgb(100, 130, 175);
            checkBoxArchivosSlack.Location = new Point(200, 152);
            checkBoxArchivosSlack.Name = "checkBoxArchivosSlack";
            checkBoxArchivosSlack.Size = new Size(98, 16);
            checkBoxArchivosSlack.TabIndex = 23;
            checkBoxArchivosSlack.Text = "↳ enviar archivos";
            checkBoxArchivosSlack.UseVisualStyleBackColor = false;
            checkBoxArchivosSlack.CheckedChanged += checkBoxArchivosSlack_CheckedChanged;
            // 
            // checkBoxAudio
            // 
            checkBoxAudio.Appearance = Appearance.Button;
            checkBoxAudio.BackColor = Color.FromArgb(22, 34, 54);
            checkBoxAudio.FlatAppearance.BorderColor = Color.FromArgb(109, 40, 217);
            checkBoxAudio.FlatAppearance.CheckedBackColor = Color.FromArgb(60, 20, 120);
            checkBoxAudio.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 15, 85);
            checkBoxAudio.FlatStyle = FlatStyle.Flat;
            checkBoxAudio.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxAudio.ForeColor = Color.FromArgb(196, 181, 253);
            checkBoxAudio.Location = new Point(310, 109);
            checkBoxAudio.Name = "checkBoxAudio";
            checkBoxAudio.Padding = new Padding(4, 0, 4, 0);
            checkBoxAudio.Size = new Size(72, 24);
            checkBoxAudio.TabIndex = 26;
            checkBoxAudio.Text = "🔊 Audio";
            checkBoxAudio.TextAlign = ContentAlignment.MiddleCenter;
            checkBoxAudio.UseVisualStyleBackColor = false;
            checkBoxAudio.CheckedChanged += checkBoxAudio_CheckedChanged;
            // 
            // btnLimpiar
            // 
            btnLimpiar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnLimpiar.FlatAppearance.BorderColor = Color.FromArgb(50, 65, 90);
            btnLimpiar.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 55, 80);
            btnLimpiar.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 50, 75);
            btnLimpiar.FlatStyle = FlatStyle.Flat;
            btnLimpiar.Image = Properties.Resources.escoba;
            btnLimpiar.ImageAlign = ContentAlignment.MiddleLeft;
            btnLimpiar.Location = new Point(725, 108);
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Size = new Size(140, 28);
            btnLimpiar.TabIndex = 19;
            btnLimpiar.Text = "Limpiar histo.";
            btnLimpiar.TextAlign = ContentAlignment.MiddleRight;
            btnLimpiar.UseVisualStyleBackColor = true;
            btnLimpiar.Click += btnLimpiar_Click;
            // 
            // btnInfo
            // 
            btnInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnInfo.FlatAppearance.BorderColor = Color.FromArgb(50, 65, 90);
            btnInfo.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 55, 80);
            btnInfo.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 50, 75);
            btnInfo.FlatStyle = FlatStyle.Flat;
            btnInfo.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnInfo.ForeColor = Color.FromArgb(120, 150, 200);
            btnInfo.Location = new Point(869, 108);
            btnInfo.Name = "btnInfo";
            btnInfo.Size = new Size(38, 28);
            btnInfo.TabIndex = 21;
            btnInfo.Text = "ℹ";
            btnInfo.UseVisualStyleBackColor = true;
            btnInfo.Click += btnInfo_Click;
            // 
            // pnlChat
            // 
            pnlChat.BackColor = Color.Transparent;
            pnlChat.BackgroundImageLayout = ImageLayout.Stretch;
            pnlChat.Dock = DockStyle.Fill;
            pnlChat.Location = new Point(0, 50);
            pnlChat.Name = "pnlChat";
            pnlChat.Size = new Size(915, 437);
            pnlChat.TabIndex = 1;
            // 
            // panelHead
            // 
            panelHead.BackColor = Color.FromArgb(30, 41, 59);
            panelHead.Controls.Add(btnGuardar);
            panelHead.Controls.Add(label3);
            panelHead.Controls.Add(comboBoxModeloIA);
            panelHead.Controls.Add(label2);
            panelHead.Controls.Add(label1);
            panelHead.Controls.Add(comboBoxRuta);
            panelHead.Controls.Add(comboBoxAgentes);
            panelHead.Dock = DockStyle.Top;
            panelHead.Location = new Point(0, 0);
            panelHead.Name = "panelHead";
            panelHead.Size = new Size(915, 50);
            panelHead.TabIndex = 2;
            // 
            // btnGuardar
            // 
            btnGuardar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnGuardar.BackColor = Color.MediumSeaGreen;
            btnGuardar.FlatAppearance.BorderColor = Color.OliveDrab;
            btnGuardar.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 64, 0);
            btnGuardar.FlatAppearance.MouseOverBackColor = Color.DarkSeaGreen;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.Image = Properties.Resources.marcador;
            btnGuardar.ImageAlign = ContentAlignment.MiddleLeft;
            btnGuardar.Location = new Point(674, 16);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(178, 30);
            btnGuardar.TabIndex = 6;
            btnGuardar.Text = "Guardar Configuracion";
            btnGuardar.UseVisualStyleBackColor = false;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.FromArgb(150, 170, 200);
            label3.Location = new Point(425, 2);
            label3.Name = "label3";
            label3.Size = new Size(39, 12);
            label3.TabIndex = 5;
            label3.Text = "Modelo";
            // 
            // comboBoxModeloIA
            // 
            comboBoxModeloIA.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxModeloIA.BackColor = Color.FromArgb(30, 41, 59);
            comboBoxModeloIA.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxModeloIA.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxModeloIA.FlatStyle = FlatStyle.Flat;
            comboBoxModeloIA.ForeColor = Color.White;
            comboBoxModeloIA.FormattingEnabled = true;
            comboBoxModeloIA.ItemHeight = 24;
            comboBoxModeloIA.Location = new Point(425, 16);
            comboBoxModeloIA.Name = "comboBoxModeloIA";
            comboBoxModeloIA.Size = new Size(243, 30);
            comboBoxModeloIA.TabIndex = 4;
            comboBoxModeloIA.SelectedIndexChanged += comboBoxModeloIA_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(150, 170, 200);
            label2.Location = new Point(254, 2);
            label2.Name = "label2";
            label2.Size = new Size(36, 12);
            label2.TabIndex = 3;
            label2.Text = "Agente";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(150, 170, 200);
            label1.Location = new Point(6, 2);
            label1.Name = "label1";
            label1.Size = new Size(72, 12);
            label1.TabIndex = 2;
            label1.Text = "Ruta de trabajo";
            // 
            // comboBoxRuta
            // 
            comboBoxRuta.BackColor = Color.FromArgb(30, 41, 59);
            comboBoxRuta.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxRuta.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxRuta.FlatStyle = FlatStyle.Flat;
            comboBoxRuta.ForeColor = Color.White;
            comboBoxRuta.FormattingEnabled = true;
            comboBoxRuta.ItemHeight = 24;
            comboBoxRuta.Location = new Point(3, 16);
            comboBoxRuta.Name = "comboBoxRuta";
            comboBoxRuta.Size = new Size(246, 30);
            comboBoxRuta.TabIndex = 1;
            comboBoxRuta.SelectedIndexChanged += comboBoxRuta_SelectedIndexChanged;
            // 
            // comboBoxAgentes
            // 
            comboBoxAgentes.BackColor = Color.FromArgb(30, 41, 59);
            comboBoxAgentes.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxAgentes.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxAgentes.FlatStyle = FlatStyle.Flat;
            comboBoxAgentes.ForeColor = Color.White;
            comboBoxAgentes.FormattingEnabled = true;
            comboBoxAgentes.ItemHeight = 24;
            comboBoxAgentes.Location = new Point(254, 16);
            comboBoxAgentes.Name = "comboBoxAgentes";
            comboBoxAgentes.Size = new Size(166, 30);
            comboBoxAgentes.TabIndex = 0;
            comboBoxAgentes.SelectedIndexChanged += comboBoxAgentes_SelectedIndexChanged;
            // 
            // FrmMandos
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(915, 655);
            Controls.Add(pnlChat);
            Controls.Add(panelHead);
            Controls.Add(pnlContenedorTxt);
            ForeColor = Color.Transparent;
            MinimumSize = new Size(680, 500);
            Name = "FrmMandos";
            Text = "FrmMandos";
            FormClosing += FrmMandos_FormClosing;
            Load += FrmMandos_Load;
            Resize += FrmMandos_Resize;
            pnlContenedorTxt.ResumeLayout(false);
            pnlContenedorTxt.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)ChkNotiT).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkNotiS).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkConver).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkRes).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkEstado).EndInit();
            panelHead.ResumeLayout(false);
            panelHead.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlContenedorTxt;
        private FlowLayoutPanel pnlChat;
        private Panel panelHead;
        private ModernComboBox comboBoxAgentes;
        private CheckBox checkBoxRecordar;
        private CheckBox checkBoxSoloChat;
        private Label label2;
        private Label label1;
        private ModernComboBox comboBoxRuta;
        private Button btnEnviar;
        private Label labelSugerencia;
        private RichTextBox textBoxInstrucion;
        private CheckBox checkBoxTelegram;
        private CheckBox checkBoxConstructorTelegram;
        private CheckBox checkBoxArchivosTelegram;
        private CheckBox checkBoxConstructorSlack;
        private CheckBox checkBoxArchivosSlack;
        private CheckBox checkBoxAudio;
        private ModernComboBox comboBoxModeloIA;
        private Label label3;
        private FlowLayoutPanel pnlContenedorArchivos;
        private Button btnGuardar;
        private Label lblConsumo;
        private Label lblTime;
        private Label lblTimeR;
        private Label lblConsumoA;
        private Button btnCancelar;
        private CheckBox checkBoxSlack;
        private CheckBox checkBoxRapida;
        private PictureBox ChkEstado;
        private PictureBox ChkNotiS;
        private PictureBox ChkNotiT;
        private PictureBox ChkConver;
        private PictureBox ChkRes;
        private Button btnLimpiar;
        private Button btnInfo;
    }
}
