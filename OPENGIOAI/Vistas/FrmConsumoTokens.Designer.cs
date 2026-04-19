namespace OPENGIOAI.Vistas
{
    partial class FrmConsumoTokens
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
            pnlRoot         = new System.Windows.Forms.Panel();
            pnlHeader       = new System.Windows.Forms.Panel();
            lblTitulo       = new System.Windows.Forms.Label();
            btnCerrar       = new System.Windows.Forms.Button();
            btnLimpiar      = new System.Windows.Forms.Button();
            pnlResumen      = new System.Windows.Forms.Panel();
            lblResumen      = new System.Windows.Forms.Label();
            tabControl      = new System.Windows.Forms.TabControl();
            tabVivo         = new System.Windows.Forms.TabPage();
            tabHistorial    = new System.Windows.Forms.TabPage();
            lstVivo         = new System.Windows.Forms.ListView();
            lstHistorial    = new System.Windows.Forms.ListView();
            colV_Fase       = new System.Windows.Forms.ColumnHeader();
            colV_Modelo     = new System.Windows.Forms.ColumnHeader();
            colV_Prompt     = new System.Windows.Forms.ColumnHeader();
            colV_Compl      = new System.Windows.Forms.ColumnHeader();
            colV_Total      = new System.Windows.Forms.ColumnHeader();
            colV_Usd        = new System.Windows.Forms.ColumnHeader();
            colV_Hora       = new System.Windows.Forms.ColumnHeader();
            colH_Inicio     = new System.Windows.Forms.ColumnHeader();
            colH_Instr      = new System.Windows.Forms.ColumnHeader();
            colH_Llamadas   = new System.Windows.Forms.ColumnHeader();
            colH_Tokens     = new System.Windows.Forms.ColumnHeader();
            colH_Usd        = new System.Windows.Forms.ColumnHeader();

            pnlRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            pnlResumen.SuspendLayout();
            tabControl.SuspendLayout();
            tabVivo.SuspendLayout();
            tabHistorial.SuspendLayout();
            SuspendLayout();

            //
            // pnlRoot
            //
            pnlRoot.Controls.Add(tabControl);
            pnlRoot.Controls.Add(pnlResumen);
            pnlRoot.Controls.Add(pnlHeader);
            pnlRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlRoot.Location = new System.Drawing.Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.Size = new System.Drawing.Size(640, 480);
            pnlRoot.TabIndex = 0;

            //
            // pnlHeader  (draggable)
            //
            pnlHeader.Controls.Add(btnCerrar);
            pnlHeader.Controls.Add(btnLimpiar);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHeader.Location = new System.Drawing.Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new System.Drawing.Size(640, 44);
            pnlHeader.Cursor = System.Windows.Forms.Cursors.SizeAll;

            //
            // lblTitulo
            //
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            lblTitulo.Location = new System.Drawing.Point(14, 12);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Text = "📊  Consumo de Tokens";

            //
            // btnCerrar
            //
            btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCerrar.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnCerrar.Location = new System.Drawing.Point(600, 8);
            btnCerrar.Name = "btnCerrar";
            btnCerrar.Size = new System.Drawing.Size(30, 28);
            btnCerrar.Text = "✕";
            btnCerrar.Cursor = System.Windows.Forms.Cursors.Hand;
            btnCerrar.UseVisualStyleBackColor = true;

            //
            // btnLimpiar
            //
            btnLimpiar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnLimpiar.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnLimpiar.Location = new System.Drawing.Point(510, 8);
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Size = new System.Drawing.Size(80, 28);
            btnLimpiar.Text = "🗑 Limpiar";
            btnLimpiar.Cursor = System.Windows.Forms.Cursors.Hand;
            btnLimpiar.UseVisualStyleBackColor = true;

            //
            // pnlResumen
            //
            pnlResumen.Controls.Add(lblResumen);
            pnlResumen.Dock = System.Windows.Forms.DockStyle.Top;
            pnlResumen.Location = new System.Drawing.Point(0, 44);
            pnlResumen.Name = "pnlResumen";
            pnlResumen.Padding = new System.Windows.Forms.Padding(14, 6, 14, 6);
            pnlResumen.Size = new System.Drawing.Size(640, 46);

            //
            // lblResumen
            //
            lblResumen.AutoSize = false;
            lblResumen.Dock = System.Windows.Forms.DockStyle.Fill;
            lblResumen.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            lblResumen.Name = "lblResumen";
            lblResumen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblResumen.Text = "Esperando llamadas al LLM…";

            //
            // tabControl
            //
            tabControl.Controls.Add(tabVivo);
            tabControl.Controls.Add(tabHistorial);
            tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            tabControl.Location = new System.Drawing.Point(0, 90);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new System.Drawing.Size(640, 390);
            tabControl.TabIndex = 2;

            //
            // tabVivo
            //
            tabVivo.Controls.Add(lstVivo);
            tabVivo.Name = "tabVivo";
            tabVivo.Padding = new System.Windows.Forms.Padding(3);
            tabVivo.Text = "🟢 En vivo";

            //
            // lstVivo
            //
            lstVivo.Dock = System.Windows.Forms.DockStyle.Fill;
            lstVivo.FullRowSelect = true;
            lstVivo.GridLines = false;
            lstVivo.Name = "lstVivo";
            lstVivo.View = System.Windows.Forms.View.Details;
            lstVivo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                colV_Fase, colV_Modelo, colV_Prompt, colV_Compl, colV_Total, colV_Usd, colV_Hora
            });

            colV_Fase.Text = "Fase";       colV_Fase.Width = 110;
            colV_Modelo.Text = "Modelo";   colV_Modelo.Width = 150;
            colV_Prompt.Text = "Prompt";   colV_Prompt.Width = 70; colV_Prompt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            colV_Compl.Text = "Compl.";    colV_Compl.Width = 70;  colV_Compl.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            colV_Total.Text = "Total";     colV_Total.Width = 70;  colV_Total.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            colV_Usd.Text = "USD";         colV_Usd.Width = 75;    colV_Usd.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            colV_Hora.Text = "Hora";       colV_Hora.Width = 85;

            //
            // tabHistorial
            //
            tabHistorial.Controls.Add(lstHistorial);
            tabHistorial.Name = "tabHistorial";
            tabHistorial.Padding = new System.Windows.Forms.Padding(3);
            tabHistorial.Text = "📜 Historial";

            //
            // lstHistorial
            //
            lstHistorial.Dock = System.Windows.Forms.DockStyle.Fill;
            lstHistorial.FullRowSelect = true;
            lstHistorial.GridLines = false;
            lstHistorial.Name = "lstHistorial";
            lstHistorial.View = System.Windows.Forms.View.Details;
            lstHistorial.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                colH_Inicio, colH_Instr, colH_Llamadas, colH_Tokens, colH_Usd
            });

            colH_Inicio.Text   = "Inicio";     colH_Inicio.Width   = 130;
            colH_Instr.Text    = "Instrucción"; colH_Instr.Width   = 260;
            colH_Llamadas.Text = "Llamadas";   colH_Llamadas.Width = 80;  colH_Llamadas.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            colH_Tokens.Text   = "Tokens";     colH_Tokens.Width   = 80;  colH_Tokens.TextAlign   = System.Windows.Forms.HorizontalAlignment.Right;
            colH_Usd.Text      = "USD";        colH_Usd.Width      = 80;  colH_Usd.TextAlign      = System.Windows.Forms.HorizontalAlignment.Right;

            //
            // FrmConsumoTokens
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(640, 480);
            Controls.Add(pnlRoot);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            Name = "FrmConsumoTokens";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "Consumo de Tokens";
            TopMost = true;

            pnlRoot.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlResumen.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            tabVivo.ResumeLayout(false);
            tabHistorial.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel     pnlRoot;
        private System.Windows.Forms.Panel     pnlHeader;
        private System.Windows.Forms.Label     lblTitulo;
        private System.Windows.Forms.Button    btnCerrar;
        private System.Windows.Forms.Button    btnLimpiar;
        private System.Windows.Forms.Panel     pnlResumen;
        private System.Windows.Forms.Label     lblResumen;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage   tabVivo;
        private System.Windows.Forms.TabPage   tabHistorial;
        private System.Windows.Forms.ListView  lstVivo;
        private System.Windows.Forms.ListView  lstHistorial;
        private System.Windows.Forms.ColumnHeader colV_Fase;
        private System.Windows.Forms.ColumnHeader colV_Modelo;
        private System.Windows.Forms.ColumnHeader colV_Prompt;
        private System.Windows.Forms.ColumnHeader colV_Compl;
        private System.Windows.Forms.ColumnHeader colV_Total;
        private System.Windows.Forms.ColumnHeader colV_Usd;
        private System.Windows.Forms.ColumnHeader colV_Hora;
        private System.Windows.Forms.ColumnHeader colH_Inicio;
        private System.Windows.Forms.ColumnHeader colH_Instr;
        private System.Windows.Forms.ColumnHeader colH_Llamadas;
        private System.Windows.Forms.ColumnHeader colH_Tokens;
        private System.Windows.Forms.ColumnHeader colH_Usd;
    }
}
