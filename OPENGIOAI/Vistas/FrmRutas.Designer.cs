namespace OPENGIOAI.Vistas
{
    partial class FrmRutas
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
            textBoxRuta = new TextBox();
            btnAgregar = new Button();
            textBoxDescripcion = new TextBox();
            label2 = new Label();
            panelFormulario = new Panel();
            btnCancelar = new Button();
            label4 = new Label();
            label3 = new Label();
            pnlContenedor = new FlowLayoutPanel();
            panelFormulario.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxRuta
            // 
            textBoxRuta.BackColor = Color.FromArgb(30, 41, 59);
            textBoxRuta.ForeColor = Color.White;
            textBoxRuta.Location = new Point(7, 33);
            textBoxRuta.Name = "textBoxRuta";
            textBoxRuta.Size = new Size(530, 23);
            textBoxRuta.TabIndex = 0;
            // 
            // btnAgregar
            // 
            btnAgregar.FlatAppearance.BorderColor = Color.Blue;
            btnAgregar.FlatStyle = FlatStyle.Flat;
            btnAgregar.ForeColor = Color.White;
            btnAgregar.Location = new Point(7, 158);
            btnAgregar.Name = "btnAgregar";
            btnAgregar.Size = new Size(103, 28);
            btnAgregar.TabIndex = 1;
            btnAgregar.Text = "Agregar";
            btnAgregar.UseVisualStyleBackColor = true;
            btnAgregar.Click += btnAgregar_Click;
            // 
            // textBoxDescripcion
            // 
            textBoxDescripcion.BackColor = Color.FromArgb(30, 41, 59);
            textBoxDescripcion.ForeColor = Color.White;
            textBoxDescripcion.Location = new Point(7, 84);
            textBoxDescripcion.Multiline = true;
            textBoxDescripcion.Name = "textBoxDescripcion";
            textBoxDescripcion.Size = new Size(530, 67);
            textBoxDescripcion.TabIndex = 5;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.White;
            label2.Location = new Point(19, 8);
            label2.Name = "label2";
            label2.Size = new Size(125, 21);
            label2.TabIndex = 6;
            label2.Text = "Rutas de Trabajo";
            // 
            // panelFormulario
            // 
            panelFormulario.BackColor = Color.FromArgb(30, 41, 59);
            panelFormulario.Controls.Add(btnCancelar);
            panelFormulario.Controls.Add(label4);
            panelFormulario.Controls.Add(label3);
            panelFormulario.Controls.Add(textBoxDescripcion);
            panelFormulario.Controls.Add(btnAgregar);
            panelFormulario.Controls.Add(textBoxRuta);
            panelFormulario.Location = new Point(12, 32);
            panelFormulario.Name = "panelFormulario";
            panelFormulario.Size = new Size(554, 190);
            panelFormulario.TabIndex = 7;
            // 
            // btnCancelar
            // 
            btnCancelar.FlatAppearance.BorderColor = Color.Crimson;
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.ForeColor = Color.White;
            btnCancelar.Location = new Point(138, 159);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(103, 28);
            btnCancelar.TabIndex = 8;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.ForeColor = Color.White;
            label4.Location = new Point(7, 66);
            label4.Name = "label4";
            label4.Size = new Size(69, 15);
            label4.TabIndex = 7;
            label4.Text = "Descripcion";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.White;
            label3.Location = new Point(9, 12);
            label3.Name = "label3";
            label3.Size = new Size(31, 15);
            label3.TabIndex = 6;
            label3.Text = "Ruta";
            // 
            // pnlContenedor
            // 
            pnlContenedor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlContenedor.BackColor = Color.Black;
            pnlContenedor.Location = new Point(12, 245);
            pnlContenedor.Name = "pnlContenedor";
            pnlContenedor.Size = new Size(796, 405);
            pnlContenedor.TabIndex = 8;
            // 
            // FrmRutas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(820, 662);
            Controls.Add(pnlContenedor);
            Controls.Add(panelFormulario);
            Controls.Add(label2);
            Name = "FrmRutas";
            Text = "FrmRutas";
            Load += FrmRutas_Load;
            panelFormulario.ResumeLayout(false);
            panelFormulario.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxRuta;
        private Button btnAgregar;
        private TextBox textBoxDescripcion;
        private Label label2;
        private Panel panelFormulario;
        private Label label4;
        private Label label3;
        private FlowLayoutPanel pnlContenedor;
        private Button btnCancelar;
    }
}