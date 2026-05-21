namespace OPENGIOAI.Vistas
{
    partial class FrmRutas
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // FrmRutas
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.Black;
            ClientSize = new System.Drawing.Size(1000, 700);
            DoubleBuffered = true;
            Name = "FrmRutas";
            Text = "Rutas de Trabajo";
            Load += FrmRutas_Load;
            ResumeLayout(false);
        }

        #endregion
    }
}