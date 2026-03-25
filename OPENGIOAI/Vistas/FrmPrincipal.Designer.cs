namespace OPENGIOAI.Vistas
{
    partial class FrmPrincipal
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmPrincipal));
            pnlMenu = new Panel();
            btnAutomatizacion = new Button();
            label3 = new Label();
            label2 = new Label();
            btnMultiples = new Button();
            button1 = new Button();
            btnSkills = new Button();
            panel2 = new Panel();
            btnSalir = new Button();
            btnModelos = new Button();
            btnRutas = new Button();
            btnApis = new Button();
            btnMando = new Button();
            panel1 = new Panel();
            btnOcultar = new Button();
            pnlContenedor = new Panel();
            pnlConfig = new Panel();
            btnCerrar = new Button();
            pnlMenu.SuspendLayout();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            pnlContenedor.SuspendLayout();
            pnlConfig.SuspendLayout();
            SuspendLayout();
            // 
            // pnlMenu
            // 
            pnlMenu.BackColor = Color.FromArgb(30, 41, 59);
            pnlMenu.Controls.Add(btnAutomatizacion);
            pnlMenu.Controls.Add(label3);
            pnlMenu.Controls.Add(label2);
            pnlMenu.Controls.Add(btnMultiples);
            pnlMenu.Controls.Add(button1);
            pnlMenu.Controls.Add(btnSkills);
            pnlMenu.Controls.Add(panel2);
            pnlMenu.Controls.Add(btnModelos);
            pnlMenu.Controls.Add(btnRutas);
            pnlMenu.Controls.Add(btnApis);
            pnlMenu.Controls.Add(btnMando);
            pnlMenu.Controls.Add(panel1);
            pnlMenu.Dock = DockStyle.Left;
            pnlMenu.Location = new Point(0, 0);
            pnlMenu.Name = "pnlMenu";
            pnlMenu.Size = new Size(203, 744);
            pnlMenu.TabIndex = 0;
            // 
            // btnAutomatizacion
            // 
            btnAutomatizacion.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnAutomatizacion.FlatAppearance.BorderSize = 0;
            btnAutomatizacion.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnAutomatizacion.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnAutomatizacion.FlatStyle = FlatStyle.Flat;
            btnAutomatizacion.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnAutomatizacion.ForeColor = Color.FromArgb(148, 163, 184);
            btnAutomatizacion.Location = new Point(0, 361);
            btnAutomatizacion.Name = "btnAutomatizacion";
            btnAutomatizacion.Size = new Size(203, 41);
            btnAutomatizacion.TabIndex = 11;
            btnAutomatizacion.Text = "🤖  Automatizaciones";
            btnAutomatizacion.TextAlign = ContentAlignment.MiddleLeft;
            btnAutomatizacion.UseVisualStyleBackColor = true;
            btnAutomatizacion.Click += btnAutomatizacion_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.ForeColor = Color.DarkGray;
            label3.Location = new Point(3, 92);
            label3.Name = "label3";
            label3.Size = new Size(64, 15);
            label3.TabIndex = 10;
            label3.Text = "PRINCIPAL";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = Color.DarkGray;
            label2.Location = new Point(6, 296);
            label2.Name = "label2";
            label2.Size = new Size(102, 15);
            label2.TabIndex = 10;
            label2.Text = "CONFIGURACION";
            // 
            // btnMultiples
            // 
            btnMultiples.Enabled = false;
            btnMultiples.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnMultiples.FlatAppearance.BorderSize = 0;
            btnMultiples.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnMultiples.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnMultiples.FlatStyle = FlatStyle.Flat;
            btnMultiples.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnMultiples.ForeColor = Color.FromArgb(148, 163, 184);
            btnMultiples.Location = new Point(0, 227);
            btnMultiples.Name = "btnMultiples";
            btnMultiples.Size = new Size(203, 41);
            btnMultiples.TabIndex = 8;
            btnMultiples.Text = "Multiples Agentes";
            btnMultiples.UseVisualStyleBackColor = true;
            btnMultiples.Visible = false;
            btnMultiples.Click += btnMultiples_Click;
            // 
            // button1
            // 
            button1.FlatAppearance.BorderColor = Color.RoyalBlue;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            button1.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.ForeColor = Color.FromArgb(148, 163, 184);
            button1.Location = new Point(0, 502);
            button1.Name = "button1";
            button1.Size = new Size(203, 41);
            button1.TabIndex = 7;
            button1.Text = "🌳  Árbol del proyecto";
            button1.TextAlign = ContentAlignment.MiddleLeft;
            button1.UseVisualStyleBackColor = true;
            // 
            // btnSkills
            // 
            btnSkills.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnSkills.FlatAppearance.BorderSize = 0;
            btnSkills.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnSkills.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnSkills.FlatStyle = FlatStyle.Flat;
            btnSkills.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSkills.ForeColor = Color.FromArgb(148, 163, 184);
            btnSkills.Location = new Point(0, 314);
            btnSkills.Name = "btnSkills";
            btnSkills.Size = new Size(203, 41);
            btnSkills.TabIndex = 6;
            btnSkills.Text = "⚡  Skills";
            btnSkills.TextAlign = ContentAlignment.MiddleLeft;
            btnSkills.UseVisualStyleBackColor = true;
            btnSkills.Click += btnSkills_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(btnSalir);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 676);
            panel2.Name = "panel2";
            panel2.Size = new Size(203, 68);
            panel2.TabIndex = 5;
            // 
            // btnSalir
            // 
            btnSalir.FlatAppearance.BorderColor = Color.Crimson;
            btnSalir.FlatAppearance.BorderSize = 0;
            btnSalir.FlatAppearance.MouseDownBackColor = Color.Maroon;
            btnSalir.FlatAppearance.MouseOverBackColor = Color.Maroon;
            btnSalir.FlatStyle = FlatStyle.Flat;
            btnSalir.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSalir.ForeColor = Color.Crimson;
            btnSalir.Location = new Point(0, 24);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(200, 41);
            btnSalir.TabIndex = 7;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // btnModelos
            // 
            btnModelos.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnModelos.FlatAppearance.BorderSize = 0;
            btnModelos.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnModelos.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnModelos.FlatStyle = FlatStyle.Flat;
            btnModelos.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnModelos.ForeColor = Color.FromArgb(148, 163, 184);
            btnModelos.ImageAlign = ContentAlignment.MiddleLeft;
            btnModelos.Location = new Point(0, 158);
            btnModelos.Name = "btnModelos";
            btnModelos.Size = new Size(203, 41);
            btnModelos.TabIndex = 4;
            btnModelos.Text = "👥  Provedores";
            btnModelos.TextAlign = ContentAlignment.MiddleLeft;
            btnModelos.UseVisualStyleBackColor = true;
            btnModelos.Click += btnModelos_Click;
            // 
            // btnRutas
            // 
            btnRutas.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnRutas.FlatAppearance.BorderSize = 0;
            btnRutas.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnRutas.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnRutas.FlatStyle = FlatStyle.Flat;
            btnRutas.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRutas.ForeColor = Color.FromArgb(148, 163, 184);
            btnRutas.Location = new Point(0, 455);
            btnRutas.Name = "btnRutas";
            btnRutas.Size = new Size(203, 41);
            btnRutas.TabIndex = 3;
            btnRutas.Text = "📁  Rutas de Trabajo";
            btnRutas.TextAlign = ContentAlignment.MiddleLeft;
            btnRutas.UseVisualStyleBackColor = true;
            btnRutas.Click += btnRutas_Click;
            // 
            // btnApis
            // 
            btnApis.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnApis.FlatAppearance.BorderSize = 0;
            btnApis.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnApis.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnApis.FlatStyle = FlatStyle.Flat;
            btnApis.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnApis.ForeColor = Color.FromArgb(148, 163, 184);
            btnApis.Location = new Point(0, 408);
            btnApis.Name = "btnApis";
            btnApis.Size = new Size(203, 41);
            btnApis.TabIndex = 2;
            btnApis.Text = "🔑  Credenciales";
            btnApis.TextAlign = ContentAlignment.MiddleLeft;
            btnApis.UseVisualStyleBackColor = true;
            btnApis.Click += btnApis_Click;
            // 
            // btnMando
            // 
            btnMando.FlatAppearance.BorderColor = Color.RoyalBlue;
            btnMando.FlatAppearance.BorderSize = 0;
            btnMando.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            btnMando.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);
            btnMando.FlatStyle = FlatStyle.Flat;
            btnMando.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnMando.ForeColor = Color.FromArgb(148, 163, 184);
            btnMando.ImageAlign = ContentAlignment.MiddleLeft;
            btnMando.Location = new Point(0, 110);
            btnMando.Name = "btnMando";
            btnMando.Size = new Size(203, 41);
            btnMando.TabIndex = 1;
            btnMando.Text = "💬  Chat";
            btnMando.TextAlign = ContentAlignment.MiddleLeft;
            btnMando.UseVisualStyleBackColor = true;
            btnMando.Click += btnMando_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnOcultar);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(203, 89);
            panel1.TabIndex = 0;
            // 
            // btnOcultar
            // 
            btnOcultar.BackgroundImage = Properties.Resources.menup;
            btnOcultar.BackgroundImageLayout = ImageLayout.Zoom;
            btnOcultar.FlatAppearance.BorderColor = Color.LightBlue;
            btnOcultar.FlatAppearance.BorderSize = 0;
            btnOcultar.FlatStyle = FlatStyle.Flat;
            btnOcultar.ForeColor = Color.Transparent;
            btnOcultar.Location = new Point(3, 1);
            btnOcultar.Name = "btnOcultar";
            btnOcultar.Size = new Size(32, 30);
            btnOcultar.TabIndex = 1;
            btnOcultar.UseVisualStyleBackColor = true;
            btnOcultar.Click += btnOcultar_Click;
            // 
            // pnlContenedor
            // 
            pnlContenedor.BackColor = Color.FromArgb(15, 23, 42);
            pnlContenedor.Controls.Add(pnlConfig);
            pnlContenedor.Dock = DockStyle.Fill;
            pnlContenedor.ForeColor = Color.Orange;
            pnlContenedor.Location = new Point(203, 0);
            pnlContenedor.Name = "pnlContenedor";
            pnlContenedor.Size = new Size(913, 744);
            pnlContenedor.TabIndex = 1;
            // 
            // pnlConfig
            // 
            pnlConfig.BackColor = Color.FromArgb(30, 41, 59);
            pnlConfig.Controls.Add(btnCerrar);
            pnlConfig.Dock = DockStyle.Top;
            pnlConfig.Location = new Point(0, 0);
            pnlConfig.Name = "pnlConfig";
            pnlConfig.Size = new Size(913, 31);
            pnlConfig.TabIndex = 8;
            // 
            // btnCerrar
            // 
            btnCerrar.BackColor = Color.Transparent;
            btnCerrar.BackgroundImage = Properties.Resources.regres;
            btnCerrar.BackgroundImageLayout = ImageLayout.Zoom;
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseDownBackColor = Color.Black;
            btnCerrar.FlatAppearance.MouseOverBackColor = Color.SteelBlue;
            btnCerrar.FlatStyle = FlatStyle.Flat;
            btnCerrar.Location = new Point(7, 0);
            btnCerrar.Name = "btnCerrar";
            btnCerrar.Size = new Size(30, 28);
            btnCerrar.TabIndex = 0;
            btnCerrar.UseVisualStyleBackColor = false;
            btnCerrar.Click += btnCerrar_Click;
            // 
            // FrmPrincipal
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1116, 744);
            Controls.Add(pnlContenedor);
            Controls.Add(pnlMenu);
            ForeColor = Color.White;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1132, 661);
            Name = "FrmPrincipal";
            Text = "Ada Lovelace AI";
            pnlMenu.ResumeLayout(false);
            pnlMenu.PerformLayout();
            panel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            pnlContenedor.ResumeLayout(false);
            pnlConfig.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlMenu;
        private Button btnRutas;
        private Button btnApis;
        private Button btnMando;
        private Panel panel1;
        private Button btnModelos;
        private Panel panel2;
        private Panel pnlContenedor;
        private Panel pnlConfig;
        private Button btnCerrar;
        private Button btnSkills;
        private Button btnSalir;
        private Button button1;
        private Button btnMultiples;
        private Label label2;
        private Label label3;
        private Button btnOcultar;
        private Button btnAutomatizacion;
    }
}