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
            label4 = new Label();
            pnlControl = new Panel();
            label5 = new Label();
            textBoxFiltro = new TextBox();
            btnCancelar = new Button();
            label3 = new Label();
            textBoxDescripcion = new TextBox();
            pictureBox1 = new PictureBox();
            label7 = new Label();
            label2 = new Label();
            textBoxKey = new TextBox();
            label1 = new Label();
            btnGuadar = new Button();
            textBoxNombre = new TextBox();
            panel1.SuspendLayout();
            pnlControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.Transparent;
            panel1.Controls.Add(pnlApis);
            panel1.Controls.Add(label4);
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
            pnlApis.Location = new Point(26, 381);
            pnlApis.Name = "pnlApis";
            pnlApis.Size = new Size(955, 404);
            pnlApis.TabIndex = 3;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Arial Narrow", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.Lavender;
            label4.Location = new Point(26, 28);
            label4.Name = "label4";
            label4.Size = new Size(275, 25);
            label4.TabIndex = 2;
            label4.Text = "Configuracion de Credenciales";
            // 
            // pnlControl
            // 
            pnlControl.BackColor = Color.FromArgb(30, 41, 59);
            pnlControl.BackgroundImageLayout = ImageLayout.Stretch;
            pnlControl.Controls.Add(label5);
            pnlControl.Controls.Add(textBoxFiltro);
            pnlControl.Controls.Add(btnCancelar);
            pnlControl.Controls.Add(label3);
            pnlControl.Controls.Add(textBoxDescripcion);
            pnlControl.Controls.Add(pictureBox1);
            pnlControl.Controls.Add(label7);
            pnlControl.Controls.Add(label2);
            pnlControl.Controls.Add(textBoxKey);
            pnlControl.Controls.Add(label1);
            pnlControl.Controls.Add(btnGuadar);
            pnlControl.Controls.Add(textBoxNombre);
            pnlControl.Location = new Point(26, 60);
            pnlControl.Name = "pnlControl";
            pnlControl.Size = new Size(743, 315);
            pnlControl.TabIndex = 0;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.ForeColor = SystemColors.AppWorkspace;
            label5.Location = new Point(434, 269);
            label5.Name = "label5";
            label5.Size = new Size(42, 15);
            label5.TabIndex = 12;
            label5.Text = "Buscar";
            // 
            // textBoxFiltro
            // 
            textBoxFiltro.BackColor = Color.FromArgb(30, 41, 59);
            textBoxFiltro.ForeColor = Color.White;
            textBoxFiltro.Location = new Point(433, 284);
            textBoxFiltro.Name = "textBoxFiltro";
            textBoxFiltro.Size = new Size(298, 23);
            textBoxFiltro.TabIndex = 11;
            textBoxFiltro.TextChanged += textBoxFiltro_TextChanged;
            // 
            // btnCancelar
            // 
            btnCancelar.BackgroundImageLayout = ImageLayout.Stretch;
            btnCancelar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnCancelar.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 128, 255);
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.ForeColor = Color.White;
            btnCancelar.Location = new Point(207, 248);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(166, 42);
            btnCancelar.TabIndex = 10;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.White;
            label3.Location = new Point(17, 133);
            label3.Name = "label3";
            label3.Size = new Size(69, 15);
            label3.TabIndex = 9;
            label3.Text = "Descripcion";
            // 
            // textBoxDescripcion
            // 
            textBoxDescripcion.BackColor = Color.FromArgb(30, 41, 59);
            textBoxDescripcion.ForeColor = Color.White;
            textBoxDescripcion.Location = new Point(17, 152);
            textBoxDescripcion.Multiline = true;
            textBoxDescripcion.Name = "textBoxDescripcion";
            textBoxDescripcion.Size = new Size(714, 88);
            textBoxDescripcion.TabIndex = 8;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(17, 13);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(39, 40);
            pictureBox1.TabIndex = 7;
            pictureBox1.TabStop = false;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.White;
            label7.Location = new Point(73, 21);
            label7.Name = "label7";
            label7.Size = new Size(132, 21);
            label7.TabIndex = 6;
            label7.Text = "Nueva Credencial";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = Color.White;
            label2.Location = new Point(418, 73);
            label2.Name = "label2";
            label2.Size = new Size(68, 15);
            label2.TabIndex = 5;
            label2.Text = "Token / Key";
            // 
            // textBoxKey
            // 
            textBoxKey.BackColor = Color.FromArgb(30, 41, 59);
            textBoxKey.ForeColor = Color.White;
            textBoxKey.Location = new Point(418, 91);
            textBoxKey.Multiline = true;
            textBoxKey.Name = "textBoxKey";
            textBoxKey.Size = new Size(313, 39);
            textBoxKey.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.ForeColor = Color.White;
            label1.Location = new Point(17, 73);
            label1.Name = "label1";
            label1.Size = new Size(100, 15);
            label1.TabIndex = 3;
            label1.Text = "Nombre de la API";
            // 
            // btnGuadar
            // 
            btnGuadar.BackgroundImageLayout = ImageLayout.Stretch;
            btnGuadar.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnGuadar.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 128, 255);
            btnGuadar.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuadar.FlatStyle = FlatStyle.Flat;
            btnGuadar.ForeColor = Color.White;
            btnGuadar.Location = new Point(17, 248);
            btnGuadar.Name = "btnGuadar";
            btnGuadar.Size = new Size(166, 42);
            btnGuadar.TabIndex = 2;
            btnGuadar.Text = "Guardar";
            btnGuadar.UseVisualStyleBackColor = true;
            btnGuadar.Click += btnGuadar_Click;
            // 
            // textBoxNombre
            // 
            textBoxNombre.BackColor = Color.FromArgb(30, 41, 59);
            textBoxNombre.ForeColor = Color.White;
            textBoxNombre.Location = new Point(17, 91);
            textBoxNombre.Multiline = true;
            textBoxNombre.Name = "textBoxNombre";
            textBoxNombre.Size = new Size(347, 39);
            textBoxNombre.TabIndex = 0;
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
            pnlControl.ResumeLayout(false);
            pnlControl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel pnlControl;
        private Label label1;
        private Button btnGuadar;
        private TextBox textBoxNombre;
        private TextBox textBoxKey;
        private Label label2;
        private Label label4;
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