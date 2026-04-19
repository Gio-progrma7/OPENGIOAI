namespace OPENGIOAI.Vistas
{
    partial class FrmMemoria
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
            tabControl = new System.Windows.Forms.TabControl();
            tabHechos = new System.Windows.Forms.TabPage();
            txtHechos = new System.Windows.Forms.TextBox();
            tabEpisodios = new System.Windows.Forms.TabPage();
            txtEpisodios = new System.Windows.Forms.TextBox();
            pnlFooter = new System.Windows.Forms.Panel();
            btnGuardar = new System.Windows.Forms.Button();
            btnOlvidarTodo = new System.Windows.Forms.Button();
            lblEstado = new System.Windows.Forms.Label();
            lblTokens = new System.Windows.Forms.Label();
            pnlHeader = new System.Windows.Forms.Panel();
            lblTitulo = new System.Windows.Forms.Label();
            lblSubtitulo = new System.Windows.Forms.Label();
            lblRuta = new System.Windows.Forms.Label();
            pnlRoot.SuspendLayout();
            tabControl.SuspendLayout();
            tabHechos.SuspendLayout();
            tabEpisodios.SuspendLayout();
            pnlFooter.SuspendLayout();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            //
            // pnlRoot
            //
            pnlRoot.Controls.Add(tabControl);
            pnlRoot.Controls.Add(pnlFooter);
            pnlRoot.Controls.Add(pnlHeader);
            pnlRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlRoot.Location = new System.Drawing.Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.Size = new System.Drawing.Size(900, 600);
            pnlRoot.TabIndex = 0;
            //
            // pnlHeader
            //
            pnlHeader.Controls.Add(lblRuta);
            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHeader.Location = new System.Drawing.Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new System.Windows.Forms.Padding(20, 14, 20, 14);
            pnlHeader.Size = new System.Drawing.Size(900, 100);
            pnlHeader.TabIndex = 0;
            //
            // lblTitulo
            //
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            lblTitulo.Location = new System.Drawing.Point(20, 14);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new System.Drawing.Size(130, 30);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "🧠  Memoria";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            lblSubtitulo.Location = new System.Drawing.Point(22, 48);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new System.Drawing.Size(400, 17);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Contexto durable que el agente lee antes de cada ejecución.";
            //
            // lblRuta
            //
            lblRuta.AutoSize = true;
            lblRuta.Font = new System.Drawing.Font("Consolas", 8.5F);
            lblRuta.Location = new System.Drawing.Point(22, 70);
            lblRuta.Name = "lblRuta";
            lblRuta.Size = new System.Drawing.Size(120, 14);
            lblRuta.TabIndex = 2;
            lblRuta.Text = "Ruta de memoria: -";
            //
            // tabControl
            //
            tabControl.Controls.Add(tabHechos);
            tabControl.Controls.Add(tabEpisodios);
            tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            tabControl.Location = new System.Drawing.Point(0, 100);
            tabControl.Name = "tabControl";
            tabControl.Padding = new System.Drawing.Point(18, 6);
            tabControl.SelectedIndex = 0;
            tabControl.Size = new System.Drawing.Size(900, 440);
            tabControl.TabIndex = 1;
            //
            // tabHechos
            //
            tabHechos.Controls.Add(txtHechos);
            tabHechos.Location = new System.Drawing.Point(4, 30);
            tabHechos.Name = "tabHechos";
            tabHechos.Padding = new System.Windows.Forms.Padding(12);
            tabHechos.Size = new System.Drawing.Size(892, 406);
            tabHechos.TabIndex = 0;
            tabHechos.Text = "📌  Hechos";
            //
            // txtHechos
            //
            txtHechos.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtHechos.Dock = System.Windows.Forms.DockStyle.Fill;
            txtHechos.Font = new System.Drawing.Font("Consolas", 10F);
            txtHechos.Location = new System.Drawing.Point(12, 12);
            txtHechos.Multiline = true;
            txtHechos.Name = "txtHechos";
            txtHechos.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtHechos.Size = new System.Drawing.Size(868, 382);
            txtHechos.TabIndex = 0;
            txtHechos.TextChanged += txtHechos_TextChanged;
            //
            // tabEpisodios
            //
            tabEpisodios.Controls.Add(txtEpisodios);
            tabEpisodios.Location = new System.Drawing.Point(4, 30);
            tabEpisodios.Name = "tabEpisodios";
            tabEpisodios.Padding = new System.Windows.Forms.Padding(12);
            tabEpisodios.Size = new System.Drawing.Size(892, 406);
            tabEpisodios.TabIndex = 1;
            tabEpisodios.Text = "🕐  Episodios";
            //
            // txtEpisodios
            //
            txtEpisodios.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtEpisodios.Dock = System.Windows.Forms.DockStyle.Fill;
            txtEpisodios.Font = new System.Drawing.Font("Consolas", 10F);
            txtEpisodios.Location = new System.Drawing.Point(12, 12);
            txtEpisodios.Multiline = true;
            txtEpisodios.Name = "txtEpisodios";
            txtEpisodios.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtEpisodios.Size = new System.Drawing.Size(868, 382);
            txtEpisodios.TabIndex = 0;
            txtEpisodios.TextChanged += txtEpisodios_TextChanged;
            //
            // pnlFooter
            //
            pnlFooter.Controls.Add(lblTokens);
            pnlFooter.Controls.Add(lblEstado);
            pnlFooter.Controls.Add(btnOlvidarTodo);
            pnlFooter.Controls.Add(btnGuardar);
            pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlFooter.Location = new System.Drawing.Point(0, 540);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Padding = new System.Windows.Forms.Padding(20, 12, 20, 12);
            pnlFooter.Size = new System.Drawing.Size(900, 60);
            pnlFooter.TabIndex = 2;
            //
            // btnGuardar
            //
            btnGuardar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnGuardar.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            btnGuardar.Location = new System.Drawing.Point(740, 14);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new System.Drawing.Size(140, 34);
            btnGuardar.TabIndex = 0;
            btnGuardar.Text = "💾  Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            //
            // btnOlvidarTodo
            //
            btnOlvidarTodo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnOlvidarTodo.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnOlvidarTodo.Location = new System.Drawing.Point(590, 14);
            btnOlvidarTodo.Name = "btnOlvidarTodo";
            btnOlvidarTodo.Size = new System.Drawing.Size(140, 34);
            btnOlvidarTodo.TabIndex = 1;
            btnOlvidarTodo.Text = "🗑  Olvidar todo";
            btnOlvidarTodo.UseVisualStyleBackColor = true;
            btnOlvidarTodo.Click += btnOlvidarTodo_Click;
            //
            // lblEstado
            //
            lblEstado.AutoSize = true;
            lblEstado.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblEstado.Location = new System.Drawing.Point(22, 24);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new System.Drawing.Size(140, 15);
            lblEstado.TabIndex = 2;
            lblEstado.Text = "· sincronizado con disco";
            //
            // lblTokens
            //
            lblTokens.AutoSize = true;
            lblTokens.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblTokens.Location = new System.Drawing.Point(260, 24);
            lblTokens.Name = "lblTokens";
            lblTokens.Size = new System.Drawing.Size(120, 15);
            lblTokens.TabIndex = 3;
            lblTokens.Text = "Memoria activa: -";
            //
            // FrmMemoria
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 600);
            Controls.Add(pnlRoot);
            Name = "FrmMemoria";
            Text = "Memoria";
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            pnlRoot.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            tabHechos.ResumeLayout(false);
            tabHechos.PerformLayout();
            tabEpisodios.ResumeLayout(false);
            tabEpisodios.PerformLayout();
            pnlFooter.ResumeLayout(false);
            pnlFooter.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlRoot;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblSubtitulo;
        private System.Windows.Forms.Label lblRuta;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabHechos;
        private System.Windows.Forms.TextBox txtHechos;
        private System.Windows.Forms.TabPage tabEpisodios;
        private System.Windows.Forms.TextBox txtEpisodios;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Button btnGuardar;
        private System.Windows.Forms.Button btnOlvidarTodo;
        private System.Windows.Forms.Label lblEstado;
        private System.Windows.Forms.Label lblTokens;
    }
}
