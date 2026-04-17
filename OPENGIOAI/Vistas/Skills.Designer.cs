namespace OPENGIOAI.Vistas
{
    partial class Skills
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
            txtNombre = new TextBox();
            txtCodigo = new TextBox();
            btnGuardar = new Button();
            pnlContenedor = new FlowLayoutPanel();
            btnEjecutar = new Button();
            txtRuta = new TextBox();
            txtDescripcion = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtNombre
            // 
            txtNombre.BackColor = Color.FromArgb(30, 41, 59);
            txtNombre.ForeColor = Color.White;
            txtNombre.Location = new Point(10, 10);
            txtNombre.Multiline = true;
            txtNombre.Name = "txtNombre";
            txtNombre.Size = new Size(372, 35);
            txtNombre.TabIndex = 0;
            // 
            // txtCodigo
            // 
            txtCodigo.BackColor = Color.FromArgb(30, 41, 59);
            txtCodigo.ForeColor = Color.White;
            txtCodigo.Location = new Point(7, 106);
            txtCodigo.Multiline = true;
            txtCodigo.Name = "txtCodigo";
            txtCodigo.ScrollBars = ScrollBars.Both;
            txtCodigo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtCodigo.Size = new Size(798, 190);
            txtCodigo.TabIndex = 1;
            // 
            // btnGuardar
            // 
            btnGuardar.FlatAppearance.BorderColor = Color.SteelBlue;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.ForeColor = Color.White;
            btnGuardar.Location = new Point(47, 354);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(97, 41);
            btnGuardar.TabIndex = 2;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // pnlContenedor
            // 
            pnlContenedor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlContenedor.AutoScroll = true;
            pnlContenedor.Location = new Point(47, 410);
            pnlContenedor.Name = "pnlContenedor";
            pnlContenedor.Size = new Size(847, 390);
            pnlContenedor.TabIndex = 3;
            // 
            // btnEjecutar
            // 
            btnEjecutar.FlatAppearance.BorderColor = Color.GreenYellow;
            btnEjecutar.FlatStyle = FlatStyle.Flat;
            btnEjecutar.ForeColor = Color.White;
            btnEjecutar.Location = new Point(150, 354);
            btnEjecutar.Name = "btnEjecutar";
            btnEjecutar.Size = new Size(97, 41);
            btnEjecutar.TabIndex = 4;
            btnEjecutar.Text = "Ejecutar";
            btnEjecutar.UseVisualStyleBackColor = true;
            btnEjecutar.Click += btnEjecutar_Click;
            // 
            // txtRuta
            // 
            txtRuta.BackColor = Color.FromArgb(30, 41, 59);
            txtRuta.ForeColor = Color.White;
            txtRuta.Location = new Point(10, 51);
            txtRuta.Multiline = true;
            txtRuta.Name = "txtRuta";
            txtRuta.Size = new Size(372, 36);
            txtRuta.TabIndex = 5;
            // 
            // txtDescripcion
            // 
            txtDescripcion.BackColor = Color.FromArgb(30, 41, 59);
            txtDescripcion.ForeColor = Color.White;
            txtDescripcion.Location = new Point(388, 10);
            txtDescripcion.Multiline = true;
            txtDescripcion.Name = "txtDescripcion";
            txtDescripcion.Size = new Size(417, 77);
            txtDescripcion.TabIndex = 6;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Silver;
            label1.Location = new Point(-4, 45);
            label1.Name = "label1";
            label1.Size = new Size(48, 13);
            label1.TabIndex = 7;
            label1.Text = "Nombre";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.Silver;
            label2.Location = new Point(10, 86);
            label2.Name = "label2";
            label2.Size = new Size(31, 13);
            label2.TabIndex = 8;
            label2.Text = "Ruta";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.Silver;
            label3.Location = new Point(388, 90);
            label3.Name = "label3";
            label3.Size = new Size(67, 13);
            label3.TabIndex = 9;
            label3.Text = "Descripcion";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.Silver;
            label4.Location = new Point(-4, 182);
            label4.Name = "label4";
            label4.Size = new Size(45, 13);
            label4.TabIndex = 10;
            label4.Text = "Codigo";
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(30, 41, 59);
            panel1.Controls.Add(txtCodigo);
            panel1.Controls.Add(txtNombre);
            panel1.Controls.Add(txtRuta);
            panel1.Controls.Add(txtDescripcion);
            panel1.Controls.Add(label3);
            panel1.Location = new Point(44, 35);
            panel1.Name = "panel1";
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Size = new Size(812, 308);
            panel1.TabIndex = 11;
            // 
            // Skills
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(939, 830);
            Controls.Add(panel1);
            Controls.Add(btnEjecutar);
            Controls.Add(btnGuardar);
            Controls.Add(label4);
            Controls.Add(pnlContenedor);
            Controls.Add(label1);
            Controls.Add(label2);
            ForeColor = Color.Transparent;
            Name = "Skills";
            Text = "Skills";
            Load += Skills_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtNombre;
        private TextBox txtCodigo;
        private Button btnGuardar;
        private FlowLayoutPanel pnlContenedor;
        private Button btnEjecutar;
        private TextBox txtRuta;
        private TextBox txtDescripcion;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Panel panel1;
    }
}