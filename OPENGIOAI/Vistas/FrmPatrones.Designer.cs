namespace OPENGIOAI.Vistas
{
    partial class FrmPatrones
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
            pnlRoot      = new System.Windows.Forms.Panel();
            pnlLista     = new System.Windows.Forms.Panel();
            pnlAcciones  = new System.Windows.Forms.Panel();
            btnAnalizar  = new System.Windows.Forms.Button();
            lblEstado    = new System.Windows.Forms.Label();
            pnlHeader    = new System.Windows.Forms.Panel();
            lblSubtitulo = new System.Windows.Forms.Label();
            lblTitulo    = new System.Windows.Forms.Label();
            pnlRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            pnlAcciones.SuspendLayout();
            SuspendLayout();
            //
            // pnlRoot
            //
            pnlRoot.Controls.Add(pnlLista);
            pnlRoot.Controls.Add(pnlAcciones);
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
            lblTitulo.Size = new System.Drawing.Size(200, 30);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "🔎  Patrones";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            lblSubtitulo.Location = new System.Drawing.Point(22, 48);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new System.Drawing.Size(620, 17);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Tareas recurrentes detectadas en tu historial que podrían convertirse en Skills.";
            //
            // pnlAcciones
            //
            pnlAcciones.Controls.Add(lblEstado);
            pnlAcciones.Controls.Add(btnAnalizar);
            pnlAcciones.Dock = System.Windows.Forms.DockStyle.Top;
            pnlAcciones.Location = new System.Drawing.Point(0, 78);
            pnlAcciones.Name = "pnlAcciones";
            pnlAcciones.Padding = new System.Windows.Forms.Padding(20, 8, 20, 8);
            pnlAcciones.Size = new System.Drawing.Size(900, 52);
            pnlAcciones.TabIndex = 2;
            //
            // btnAnalizar
            //
            btnAnalizar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnAnalizar.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            btnAnalizar.Location = new System.Drawing.Point(20, 10);
            btnAnalizar.Name = "btnAnalizar";
            btnAnalizar.Size = new System.Drawing.Size(180, 34);
            btnAnalizar.TabIndex = 0;
            btnAnalizar.Text = "🔎  Analizar ahora";
            btnAnalizar.UseVisualStyleBackColor = true;
            btnAnalizar.Cursor = System.Windows.Forms.Cursors.Hand;
            //
            // lblEstado
            //
            lblEstado.AutoSize = true;
            lblEstado.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Italic);
            lblEstado.Location = new System.Drawing.Point(214, 18);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new System.Drawing.Size(10, 17);
            lblEstado.TabIndex = 1;
            lblEstado.Text = "";
            //
            // pnlLista
            //
            pnlLista.AutoScroll = true;
            pnlLista.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlLista.Location = new System.Drawing.Point(0, 130);
            pnlLista.Name = "pnlLista";
            pnlLista.Padding = new System.Windows.Forms.Padding(8);
            pnlLista.Size = new System.Drawing.Size(900, 470);
            pnlLista.TabIndex = 1;
            //
            // FrmPatrones
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 600);
            Controls.Add(pnlRoot);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "FrmPatrones";
            Text = "Patrones";
            pnlRoot.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlAcciones.ResumeLayout(false);
            pnlAcciones.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlRoot;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblSubtitulo;
        private System.Windows.Forms.Panel pnlAcciones;
        private System.Windows.Forms.Button btnAnalizar;
        private System.Windows.Forms.Label lblEstado;
        private System.Windows.Forms.Panel pnlLista;
    }
}
