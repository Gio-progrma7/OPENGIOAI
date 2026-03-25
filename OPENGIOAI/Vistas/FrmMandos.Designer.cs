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
            btnLimpiar = new Button();
            ChkNotiS = new PictureBox();
            ChkConver = new PictureBox();
            ChkRes = new PictureBox();
            ChkEstado = new PictureBox();
            checkBoxRapida = new CheckBox();
            checkBoxSlack = new CheckBox();
            btnCancelar = new Button();
            lblConsumoA = new Label();
            lblTimeR = new Label();
            lblTime = new Label();
            lblConsumo = new Label();
            pnlContenedorArchivos = new FlowLayoutPanel();
            pictureBoxCarga = new PictureBox();
            checkBoxTelegram = new CheckBox();
            textBoxInstrucion = new RichTextBox();
            labelSugerencia = new Label();
            checkBoxSoloChat = new CheckBox();
            btnEnviar = new Button();
            checkBoxRecordar = new CheckBox();
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
            ((System.ComponentModel.ISupportInitialize)pictureBoxCarga).BeginInit();
            panelHead.SuspendLayout();
            SuspendLayout();
            // 
            // pnlContenedorTxt
            // 
            pnlContenedorTxt.BackColor = Color.Transparent;
            pnlContenedorTxt.BackgroundImageLayout = ImageLayout.Stretch;
            pnlContenedorTxt.Controls.Add(ChkNotiT);
            pnlContenedorTxt.Controls.Add(btnLimpiar);
            pnlContenedorTxt.Controls.Add(ChkNotiS);
            pnlContenedorTxt.Controls.Add(ChkConver);
            pnlContenedorTxt.Controls.Add(ChkRes);
            pnlContenedorTxt.Controls.Add(ChkEstado);
            pnlContenedorTxt.Controls.Add(checkBoxRapida);
            pnlContenedorTxt.Controls.Add(checkBoxSlack);
            pnlContenedorTxt.Controls.Add(btnCancelar);
            pnlContenedorTxt.Controls.Add(lblConsumoA);
            pnlContenedorTxt.Controls.Add(lblTimeR);
            pnlContenedorTxt.Controls.Add(lblTime);
            pnlContenedorTxt.Controls.Add(lblConsumo);
            pnlContenedorTxt.Controls.Add(pnlContenedorArchivos);
            pnlContenedorTxt.Controls.Add(pictureBoxCarga);
            pnlContenedorTxt.Controls.Add(checkBoxTelegram);
            pnlContenedorTxt.Controls.Add(textBoxInstrucion);
            pnlContenedorTxt.Controls.Add(labelSugerencia);
            pnlContenedorTxt.Controls.Add(checkBoxSoloChat);
            pnlContenedorTxt.Controls.Add(btnEnviar);
            pnlContenedorTxt.Controls.Add(checkBoxRecordar);
            pnlContenedorTxt.Dock = DockStyle.Bottom;
            pnlContenedorTxt.Location = new Point(0, 429);
            pnlContenedorTxt.Name = "pnlContenedorTxt";
            pnlContenedorTxt.Size = new Size(915, 226);
            pnlContenedorTxt.TabIndex = 0;
            // 
            // ChkNotiT
            // 
            ChkNotiT.Image = Properties.Resources.selec;
            ChkNotiT.Location = new Point(410, 6);
            ChkNotiT.Name = "ChkNotiT";
            ChkNotiT.Size = new Size(15, 15);
            ChkNotiT.TabIndex = 17;
            ChkNotiT.TabStop = false;
            ChkNotiT.Visible = false;
            //ChkNotiT.Click += ChkNotiT_Click;
            // 
            // btnLimpiar
            // 
            btnLimpiar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnLimpiar.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnLimpiar.FlatAppearance.MouseOverBackColor = Color.SteelBlue;
            btnLimpiar.FlatStyle = FlatStyle.Flat;
            btnLimpiar.Image = Properties.Resources.escoba;
            btnLimpiar.ImageAlign = ContentAlignment.MiddleLeft;
            btnLimpiar.Location = new Point(17, 196);
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Size = new Size(213, 27);
            btnLimpiar.TabIndex = 19;
            btnLimpiar.Text = "Limpiar Historial del chat";
            btnLimpiar.UseVisualStyleBackColor = true;
            btnLimpiar.Click += btnLimpiar_Click;
            // 
            // ChkNotiS
            // 
            ChkNotiS.Image = Properties.Resources.selec;
            ChkNotiS.Location = new Point(580, 7);
            ChkNotiS.Name = "ChkNotiS";
            ChkNotiS.Size = new Size(15, 15);
            ChkNotiS.TabIndex = 18;
            ChkNotiS.TabStop = false;
            ChkNotiS.Visible = false;
            //ChkNotiS.Click += ChkNotiS_Click;
            // 
            // ChkConver
            // 
            ChkConver.Image = Properties.Resources.selec;
            ChkConver.Location = new Point(262, 6);
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
            ChkRes.Location = new Point(120, 6);
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
            ChkEstado.Location = new Point(21, 6);
            ChkEstado.Name = "ChkEstado";
            ChkEstado.Size = new Size(15, 15);
            ChkEstado.TabIndex = 14;
            ChkEstado.TabStop = false;
            ChkEstado.Visible = false;
            ChkEstado.Click += ChkEstado_Click;
            // 
            // checkBoxRapida
            // 
            checkBoxRapida.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkBoxRapida.AutoSize = true;
            checkBoxRapida.BackColor = Color.Transparent;
            checkBoxRapida.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxRapida.Location = new Point(122, 7);
            checkBoxRapida.Name = "checkBoxRapida";
            checkBoxRapida.Size = new Size(141, 17);
            checkBoxRapida.TabIndex = 13;
            checkBoxRapida.Text = "Solo respuesta Rapida";
            checkBoxRapida.UseVisualStyleBackColor = false;
            checkBoxRapida.CheckedChanged += checkBoxRapida_CheckedChanged;
            // 
            // checkBoxSlack
            // 
            checkBoxSlack.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkBoxSlack.AutoSize = true;
            checkBoxSlack.BackColor = Color.Transparent;
            checkBoxSlack.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxSlack.Location = new Point(580, 9);
            checkBoxSlack.Name = "checkBoxSlack";
            checkBoxSlack.Size = new Size(149, 17);
            checkBoxSlack.TabIndex = 12;
            checkBoxSlack.Text = "Notificaciones por Slack";
            checkBoxSlack.UseVisualStyleBackColor = false;
            checkBoxSlack.CheckedChanged += checkBoxSlack_CheckedChanged;
            // 
            // btnCancelar
            // 
            btnCancelar.BackgroundImage = Properties.Resources.cancell;
            btnCancelar.BackgroundImageLayout = ImageLayout.Center;
            btnCancelar.FlatAppearance.BorderColor = Color.Crimson;
            btnCancelar.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 255, 192);
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.LightPink;
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.ForeColor = Color.White;
            btnCancelar.Location = new Point(750, 131);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(37, 39);
            btnCancelar.TabIndex = 11;
            btnCancelar.Tag = "#0F5DF7";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // lblConsumoA
            // 
            lblConsumoA.AutoSize = true;
            lblConsumoA.Location = new Point(373, 24);
            lblConsumoA.Name = "lblConsumoA";
            lblConsumoA.Size = new Size(0, 15);
            lblConsumoA.TabIndex = 10;
            // 
            // lblTimeR
            // 
            lblTimeR.AutoSize = true;
            lblTimeR.BackColor = Color.Transparent;
            lblTimeR.ForeColor = Color.MediumSpringGreen;
            lblTimeR.Location = new Point(372, 39);
            lblTimeR.Name = "lblTimeR";
            lblTimeR.Size = new Size(0, 15);
            lblTimeR.TabIndex = 9;
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.BackColor = Color.Transparent;
            lblTime.ForeColor = Color.GreenYellow;
            lblTime.Location = new Point(524, 10);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(0, 15);
            lblTime.TabIndex = 8;
            // 
            // lblConsumo
            // 
            lblConsumo.AutoSize = true;
            lblConsumo.Location = new Point(372, 54);
            lblConsumo.Name = "lblConsumo";
            lblConsumo.Size = new Size(0, 15);
            lblConsumo.TabIndex = 0;
            // 
            // pnlContenedorArchivos
            // 
            pnlContenedorArchivos.Location = new Point(17, 31);
            pnlContenedorArchivos.Name = "pnlContenedorArchivos";
            pnlContenedorArchivos.Size = new Size(288, 34);
            pnlContenedorArchivos.TabIndex = 7;
            // 
            // pictureBoxCarga
            // 
            pictureBoxCarga.Image = Properties.Resources.cargaia;
            pictureBoxCarga.Location = new Point(316, 19);
            pictureBoxCarga.Name = "pictureBoxCarga";
            pictureBoxCarga.Size = new Size(140, 66);
            pictureBoxCarga.TabIndex = 6;
            pictureBoxCarga.TabStop = false;
            pictureBoxCarga.Visible = false;
            // 
            // checkBoxTelegram
            // 
            checkBoxTelegram.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkBoxTelegram.AutoSize = true;
            checkBoxTelegram.BackColor = Color.Transparent;
            checkBoxTelegram.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxTelegram.Location = new Point(410, 8);
            checkBoxTelegram.Name = "checkBoxTelegram";
            checkBoxTelegram.Size = new Size(169, 17);
            checkBoxTelegram.TabIndex = 5;
            checkBoxTelegram.Text = "Notificaciones por Telegram";
            checkBoxTelegram.UseVisualStyleBackColor = false;
            checkBoxTelegram.CheckedChanged += checkBoxTelegram_CheckedChanged;
            // 
            // textBoxInstrucion
            // 
            textBoxInstrucion.BackColor = Color.FromArgb(30, 41, 59);
            textBoxInstrucion.ForeColor = Color.Gainsboro;
            textBoxInstrucion.Location = new Point(17, 86);
            textBoxInstrucion.Name = "textBoxInstrucion";
            textBoxInstrucion.Size = new Size(727, 68);
            textBoxInstrucion.TabIndex = 4;
            textBoxInstrucion.Text = "";
            textBoxInstrucion.TextChanged += textBoxInstrucion_TextChanged;
            textBoxInstrucion.KeyDown += textBoxInstrucion_KeyDown;
            // 
            // labelSugerencia
            // 
            labelSugerencia.AutoSize = true;
            labelSugerencia.BackColor = Color.Transparent;
            labelSugerencia.Location = new Point(384, 9);
            labelSugerencia.Name = "labelSugerencia";
            labelSugerencia.Size = new Size(0, 15);
            labelSugerencia.TabIndex = 3;
            // 
            // checkBoxSoloChat
            // 
            checkBoxSoloChat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkBoxSoloChat.AutoSize = true;
            checkBoxSoloChat.BackColor = Color.Transparent;
            checkBoxSoloChat.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxSoloChat.Location = new Point(21, 7);
            checkBoxSoloChat.Name = "checkBoxSoloChat";
            checkBoxSoloChat.Size = new Size(101, 17);
            checkBoxSoloChat.TabIndex = 2;
            checkBoxSoloChat.Text = "Estado Lectura";
            checkBoxSoloChat.UseVisualStyleBackColor = false;
            checkBoxSoloChat.CheckedChanged += checkBoxSoloChat_CheckedChanged;
            // 
            // btnEnviar
            // 
            btnEnviar.BackgroundImage = Properties.Resources.enviar1;
            btnEnviar.BackgroundImageLayout = ImageLayout.Center;
            btnEnviar.FlatAppearance.BorderColor = Color.SteelBlue;
            btnEnviar.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 255, 192);
            btnEnviar.FlatAppearance.MouseOverBackColor = Color.Navy;
            btnEnviar.FlatStyle = FlatStyle.Flat;
            btnEnviar.ForeColor = Color.White;
            btnEnviar.Location = new Point(750, 86);
            btnEnviar.Name = "btnEnviar";
            btnEnviar.Size = new Size(37, 39);
            btnEnviar.TabIndex = 1;
            btnEnviar.Tag = "#0F5DF7";
            btnEnviar.UseVisualStyleBackColor = true;
            btnEnviar.Click += btnEnviar_Click;
            // 
            // checkBoxRecordar
            // 
            checkBoxRecordar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkBoxRecordar.AutoSize = true;
            checkBoxRecordar.BackColor = Color.Transparent;
            checkBoxRecordar.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxRecordar.Location = new Point(262, 7);
            checkBoxRecordar.Name = "checkBoxRecordar";
            checkBoxRecordar.Size = new Size(146, 17);
            checkBoxRecordar.TabIndex = 1;
            checkBoxRecordar.Text = "Mantener conversación";
            checkBoxRecordar.UseVisualStyleBackColor = false;
            checkBoxRecordar.CheckedChanged += checkBoxRecordar_CheckedChanged;
            // 
            // pnlChat
            // 
            pnlChat.BackColor = Color.Transparent;
            pnlChat.BackgroundImageLayout = ImageLayout.Stretch;
            pnlChat.Dock = DockStyle.Fill;
            pnlChat.Location = new Point(0, 45);
            pnlChat.Name = "pnlChat";
            pnlChat.Size = new Size(915, 384);
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
            panelHead.Size = new Size(915, 45);
            panelHead.TabIndex = 2;
            // 
            // btnGuardar
            // 
            btnGuardar.BackColor = Color.MediumSeaGreen;
            btnGuardar.FlatAppearance.BorderColor = Color.OliveDrab;
            btnGuardar.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 64, 0);
            btnGuardar.FlatAppearance.MouseOverBackColor = Color.DarkSeaGreen;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.Image = Properties.Resources.marcador;
            btnGuardar.ImageAlign = ContentAlignment.MiddleLeft;
            btnGuardar.Location = new Point(713, 12);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(167, 30);
            btnGuardar.TabIndex = 6;
            btnGuardar.Text = "Guardar Configuracion";
            btnGuardar.UseVisualStyleBackColor = false;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.Silver;
            label3.Location = new Point(459, -1);
            label3.Name = "label3";
            label3.Size = new Size(35, 12);
            label3.TabIndex = 5;
            label3.Text = "Modelo";
            // 
            // comboBoxModeloIA
            // 
            comboBoxModeloIA.BackColor = Color.FromArgb(30, 41, 59);
            comboBoxModeloIA.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxModeloIA.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxModeloIA.FlatStyle = FlatStyle.Flat;
            comboBoxModeloIA.ForeColor = Color.White;
            comboBoxModeloIA.FormattingEnabled = true;
            comboBoxModeloIA.ItemHeight = 24;
            comboBoxModeloIA.Location = new Point(459, 12);
            comboBoxModeloIA.Name = "comboBoxModeloIA";
            comboBoxModeloIA.Size = new Size(248, 30);
            comboBoxModeloIA.TabIndex = 4;
            comboBoxModeloIA.SelectedIndexChanged += comboBoxModeloIA_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.Silver;
            label2.Location = new Point(271, -1);
            label2.Name = "label2";
            label2.Size = new Size(34, 12);
            label2.TabIndex = 3;
            label2.Text = "Agente";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Silver;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(65, 12);
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
            comboBoxRuta.Location = new Point(3, 12);
            comboBoxRuta.Name = "comboBoxRuta";
            comboBoxRuta.Size = new Size(247, 30);
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
            comboBoxAgentes.Location = new Point(271, 12);
            comboBoxAgentes.Name = "comboBoxAgentes";
            comboBoxAgentes.Size = new Size(165, 30);
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
            Name = "FrmMandos";
            Text = "FrmMandos";
            FormClosing += FrmMandos_FormClosing;
            Load += FrmMandos_Load;
            //Shown += FrmMandos_Shown;
            Resize += FrmMandos_Resize;
            pnlContenedorTxt.ResumeLayout(false);
            pnlContenedorTxt.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)ChkNotiT).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkNotiS).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkConver).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkRes).EndInit();
            ((System.ComponentModel.ISupportInitialize)ChkEstado).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxCarga).EndInit();
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
        private ModernComboBox comboBoxModeloIA;
        private Label label3;
        private PictureBox pictureBoxCarga;
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
    }
}