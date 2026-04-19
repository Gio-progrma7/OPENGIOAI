namespace OPENGIOAI.Vistas
{
    partial class FrmHabilidades
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
            pnlRoot = new System.Windows.Forms.Panel();
            pnlLista = new System.Windows.Forms.Panel();
            pnlHeader = new System.Windows.Forms.Panel();
            lblSubtitulo = new System.Windows.Forms.Label();
            lblTitulo = new System.Windows.Forms.Label();
            pnlRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            //
            // pnlRoot
            //
            pnlRoot.Controls.Add(pnlLista);
            pnlRoot.Controls.Add(pnlHeader);
            pnlRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlRoot.Location = new System.Drawing.Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.Size = new System.Drawing.Size(900, 600);
            pnlRoot.TabIndex = 0;
            //
            // pnlHeader
            //
            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHeader.Location = new System.Drawing.Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new System.Windows.Forms.Padding(20, 14, 20, 14);
            pnlHeader.Size = new System.Drawing.Size(900, 78);
            pnlHeader.TabIndex = 0;
            //
            // lblTitulo
            //
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            lblTitulo.Location = new System.Drawing.Point(20, 14);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new System.Drawing.Size(160, 30);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "⚙  Habilidades";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            lblSubtitulo.Location = new System.Drawing.Point(22, 48);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new System.Drawing.Size(520, 17);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Enciende o apaga capacidades cognitivas del agente. Solo lo activado consume tokens.";
            //
            // pnlLista
            //
            pnlLista.AutoScroll = true;
            pnlLista.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlLista.Location = new System.Drawing.Point(0, 78);
            pnlLista.Name = "pnlLista";
            pnlLista.Padding = new System.Windows.Forms.Padding(8);
            pnlLista.Size = new System.Drawing.Size(900, 522);
            pnlLista.TabIndex = 1;
            //
            // FrmHabilidades
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 600);
            Controls.Add(pnlRoot);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "FrmHabilidades";
            Text = "Habilidades";
            pnlRoot.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlRoot;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblSubtitulo;
        private System.Windows.Forms.Panel pnlLista;
    }
}
