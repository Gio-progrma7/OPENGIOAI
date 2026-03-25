namespace OPENGIOAI.Vistas
{
    partial class FrmMultopleAngent
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
            btnCrear = new Button();
            button2 = new Button();
            numeriCantidad = new NumericUpDown();
            panel1 = new Panel();
            panel2 = new Panel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)numeriCantidad).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // btnCrear
            // 
            btnCrear.Location = new Point(104, 3);
            btnCrear.Name = "btnCrear";
            btnCrear.Size = new Size(105, 23);
            btnCrear.TabIndex = 2;
            btnCrear.Text = "Crear";
            btnCrear.UseVisualStyleBackColor = true;
            btnCrear.Click += btnCrear_Click;
            // 
            // button2
            // 
            button2.Location = new Point(215, 3);
            button2.Name = "button2";
            button2.Size = new Size(97, 23);
            button2.TabIndex = 3;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            // 
            // numeriCantidad
            // 
            numeriCantidad.Location = new Point(13, 3);
            numeriCantidad.Name = "numeriCantidad";
            numeriCantidad.Size = new Size(85, 23);
            numeriCantidad.TabIndex = 5;
            // 
            // panel1
            // 
            panel1.Controls.Add(button2);
            panel1.Controls.Add(numeriCantidad);
            panel1.Controls.Add(btnCrear);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(919, 38);
            panel1.TabIndex = 5;
            // 
            // panel2
            // 
            panel2.Controls.Add(flowLayoutPanel1);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 38);
            panel2.Name = "panel2";
            panel2.Size = new Size(919, 544);
            panel2.TabIndex = 6;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(919, 544);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // FrmMultopleAngent
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(919, 582);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "FrmMultopleAngent";
            Text = "FrmMultopleAngent";
            ((System.ComponentModel.ISupportInitialize)numeriCantidad).EndInit();
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Button btnCrear;
        private Button button2;
        private NumericUpDown numeriCantidad;
        private Panel panel1;
        private Panel panel2;
        private FlowLayoutPanel flowLayoutPanel1;
    }
}