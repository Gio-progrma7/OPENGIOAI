using System.Drawing;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    partial class FrmApis
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
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(5, 5, 5);
            ClientSize = new Size(1100, 760);
            DoubleBuffered = true;
            Name = "FrmApis";
            Text = "Credenciales";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 520);
            Load += FrmApis_Load;
            ResumeLayout(false);
        }

        #endregion
    }
}
