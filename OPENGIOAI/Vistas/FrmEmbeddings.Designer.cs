namespace OPENGIOAI.Vistas
{
    partial class FrmEmbeddings
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
            pnlContenido = new System.Windows.Forms.Panel();
            pnlHeader = new System.Windows.Forms.Panel();
            lblSubtitulo = new System.Windows.Forms.Label();
            lblTitulo = new System.Windows.Forms.Label();
            pnlRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            //
            // pnlRoot
            //
            pnlRoot.Controls.Add(pnlContenido);
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
            lblTitulo.Size = new System.Drawing.Size(240, 30);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "\U0001F9EC  Embeddings (RAG)";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            lblSubtitulo.Location = new System.Drawing.Point(22, 48);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new System.Drawing.Size(620, 17);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Configura el proveedor de embeddings y re-indexa la memoria para que el agente use RAG.";
            //
            // pnlContenido
            //
            pnlContenido.AutoScroll = true;
            pnlContenido.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlContenido.Location = new System.Drawing.Point(0, 78);
            pnlContenido.Name = "pnlContenido";
            pnlContenido.Padding = new System.Windows.Forms.Padding(20);
            pnlContenido.Size = new System.Drawing.Size(900, 522);
            pnlContenido.TabIndex = 1;
            //
            // FrmEmbeddings
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 600);
            Controls.Add(pnlRoot);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "FrmEmbeddings";
            Text = "Embeddings";
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
        private System.Windows.Forms.Panel pnlContenido;
    }
}
