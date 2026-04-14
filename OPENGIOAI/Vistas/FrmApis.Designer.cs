namespace OPENGIOAI.Vistas
{
    partial class FrmApis
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
            panel1 = new Panel();
            pnlApis = new FlowLayoutPanel();
            pnlBuscador = new Panel();
            label5 = new Label();
            textBoxFiltro = new TextBox();
            label4 = new Label();
            lblSubtitulo = new Label();
            pnlControl = new Panel();
            pictureBox1 = new PictureBox();
            label7 = new Label();
            label1 = new Label();
            textBoxNombre = new TextBox();
            label2 = new Label();
            textBoxKey = new TextBox();
            label3 = new Label();
            textBoxDescripcion = new TextBox();
            btnGuadar = new Button();
            btnCancelar = new Button();
            panel1.SuspendLayout();
            pnlBuscador.SuspendLayout();
            pnlControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.Transparent;
            panel1.Controls.Add(pnlApis);
            panel1.Controls.Add(pnlBuscador);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(lblSubtitulo);
            panel1.Controls.Add(pnlControl);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1008, 797);
            panel1.TabIndex = 0;
            // 
            // pnlApis
            // 
            pnlApis.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlApis.Location = new Point(26, 387);
            pnlApis.Name = "pnlApis";
            pnlApis.Size = new Size(955, 391);
            pnlApis.TabIndex = 3;
            // 
            // pnlBuscador
            // 
            pnlBuscador.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlBuscador.BackColor = Color.Transparent;
            pnlBuscador.Controls.Add(label5);
            pnlBuscador.Controls.Add(textBoxFiltro);
            pnlBuscador.Location = new Point(26, 330);
            pnlBuscador.Name = "pnlBuscador";
            pnlBuscador.Size = new Size(955, 38);
            pnlBuscador.TabIndex = 14;
            // 
            // label5
            // 
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.FromArgb(148, 163, 184);
            label5.Location = new Point(0, 10);
            label5.Name = "label5";
            label5.Size = new Size(115, 22);
            label5.TabIndex = 12;
            label5.Text = "Buscar credencial:";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxFiltro
            // 
            textBoxFiltro.BackColor = Color.FromArgb(30, 41, 59);
            textBoxFiltro.ForeColor = Color.White;
            textBoxFiltro.Location = new Point(120, 8);
            textBoxFiltro.Name = "textBoxFiltro";
            textBoxFiltro.Size = new Size(320, 23);
            textBoxFiltro.TabIndex = 11;
            textBoxFiltro.TextChanged += textBoxFiltro_TextChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.FromArgb(226, 232, 240);
            label4.Location = new Point(26, 18);
            label4.Name = "label4";
            label4.Size = new Size(339, 31);
            label4.TabIndex = 2;
            label4.Text = "Configuración de Credenciales";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitulo.ForeColor = Color.FromArgb(100, 116, 139);
            lblSubtitulo.Location = new Point(28, 50);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(258, 15);
            lblSubtitulo.TabIndex = 13;
            lblSubtitulo.Text = "Administra tus claves de API y tokens de acceso";
            // 
            // pnlControl
            // 
            pnlControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlControl.BackColor = Color.FromArgb(30, 41, 59);
            pnlControl.Controls.Add(pictureBox1);
            pnlControl.Controls.Add(label7);
            pnlControl.Controls.Add(label1);
            pnlControl.Controls.Add(textBoxNombre);
            pnlControl.Controls.Add(label2);
            pnlControl.Controls.Add(textBoxKey);
            pnlControl.Controls.Add(label3);
            pnlControl.Controls.Add(textBoxDescripcion);
            pnlControl.Controls.Add(btnGuadar);
            pnlControl.Controls.Add(btnCancelar);
            pnlControl.Location = new Point(26, 72);
            pnlControl.Name = "pnlControl";
            pnlControl.Size = new Size(955, 258);
            pnlControl.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(17, 16);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(32, 32);
            pictureBox1.TabIndex = 7;
            pictureBox1.TabStop = false;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.FromArgb(226, 232, 240);
            label7.Location = new Point(57, 20);
            label7.Name = "label7";
            label7.Size = new Size(130, 20);
            label7.TabIndex = 6;
            label7.Text = "Nueva Credencial";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(148, 163, 184);
            label1.Location = new Point(17, 62);
            label1.Name = "label1";
            label1.Size = new Size(100, 15);
            label1.TabIndex = 3;
            label1.Text = "Nombre de la API";
            // 
            // textBoxNombre
            // 
            textBoxNombre.BackColor = Color.FromArgb(15, 23, 42);
            textBoxNombre.ForeColor = Color.White;
            textBoxNombre.Location = new Point(17, 80);
            textBoxNombre.Name = "textBoxNombre";
            textBoxNombre.Size = new Size(347, 23);
            textBoxNombre.TabIndex = 0;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(148, 163, 184);
            label2.Location = new Point(400, 62);
            label2.Name = "label2";
            label2.Size = new Size(68, 15);
            label2.TabIndex = 5;
            label2.Text = "Token / Key";
            // 
            // textBoxKey
            // 
            textBoxKey.BackColor = Color.FromArgb(15, 23, 42);
            textBoxKey.ForeColor = Color.White;
            textBoxKey.Location = new Point(400, 80);
            textBoxKey.Name = "textBoxKey";
            textBoxKey.Size = new Size(393, 23);
            textBoxKey.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.FromArgb(148, 163, 184);
            label3.Location = new Point(17, 124);
            label3.Name = "label3";
            label3.Size = new Size(69, 15);
            label3.TabIndex = 9;
            label3.Text = "Descripción";
            // 
            // textBoxDescripcion
            // 
            textBoxDescripcion.BackColor = Color.FromArgb(15, 23, 42);
            textBoxDescripcion.ForeColor = Color.White;
            textBoxDescripcion.Location = new Point(17, 142);
            textBoxDescripcion.Multiline = true;
            textBoxDescripcion.Name = "textBoxDescripcion";
            textBoxDescripcion.Size = new Size(776, 65);
            textBoxDescripcion.TabIndex = 8;
            // 
            // btnGuadar
            // 
            btnGuadar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnGuadar.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 128, 255);
            btnGuadar.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuadar.FlatStyle = FlatStyle.Flat;
            btnGuadar.ForeColor = Color.White;
            btnGuadar.Location = new Point(17, 217);
            btnGuadar.Name = "btnGuadar";
            btnGuadar.Size = new Size(150, 28);
            btnGuadar.TabIndex = 2;
            btnGuadar.Text = "Guardar";
            btnGuadar.UseVisualStyleBackColor = true;
            btnGuadar.Click += btnGuadar_Click;
            // 
            // btnCancelar
            // 
            btnCancelar.FlatAppearance.BorderColor = Color.DarkRed;
            btnCancelar.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 0, 0);
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 0, 0);
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.ForeColor = Color.White;
            btnCancelar.Location = new Point(180, 217);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(150, 28);
            btnCancelar.TabIndex = 10;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // FrmApis
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(1008, 797);
            Controls.Add(panel1);
            Name = "FrmApis";
            Text = "FrmApis";
            Load += FrmApis_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            pnlBuscador.ResumeLayout(false);
            pnlBuscador.PerformLayout();
            pnlControl.ResumeLayout(false);
            pnlControl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel pnlControl;
        private Panel pnlBuscador;
        private Label label1;
        private Button btnGuadar;
        private TextBox textBoxNombre;
        private TextBox textBoxKey;
        private Label label2;
        private Label label4;
        private Label lblSubtitulo;
        private PictureBox pictureBox1;
        private Label label7;
        private FlowLayoutPanel pnlApis;
        private TextBox textBoxDescripcion;
        private Label label3;
        private Button btnCancelar;
        private Label label5;
        private TextBox textBoxFiltro;
    }
}
