namespace OPENGIOAI.Vistas
{
    partial class FrmPromts
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
            pnlRoot = new Panel();
            pnlDetalle = new Panel();
            pnlEditor = new Panel();
            pnlEditorInterior = new Panel();
            txtEditor = new TextBox();
            pnlAcciones = new Panel();
            btnGuardar = new Button();
            btnRestaurar = new Button();
            lblEstado = new Label();
            pnlInfo = new Panel();
            lblCategoria = new Label();
            lblNombre = new Label();
            lblDescripcion = new Label();
            lblPlaceholders = new Label();
            splitter1 = new Splitter();
            pnlLista = new Panel();
            lstPromts = new ListBox();
            pnlBuscador = new Panel();
            txtFiltro = new TextBox();
            pnlHeader = new Panel();
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            pnlRoot.SuspendLayout();
            pnlDetalle.SuspendLayout();
            pnlEditor.SuspendLayout();
            pnlEditorInterior.SuspendLayout();
            pnlAcciones.SuspendLayout();
            pnlInfo.SuspendLayout();
            pnlLista.SuspendLayout();
            pnlBuscador.SuspendLayout();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            //
            // pnlRoot
            //
            pnlRoot.BackColor = System.Drawing.Color.Transparent;
            pnlRoot.Controls.Add(pnlDetalle);
            pnlRoot.Controls.Add(splitter1);
            pnlRoot.Controls.Add(pnlLista);
            pnlRoot.Controls.Add(pnlHeader);
            pnlRoot.Dock = DockStyle.Fill;
            pnlRoot.Location = new System.Drawing.Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.Size = new System.Drawing.Size(1008, 797);
            pnlRoot.TabIndex = 0;
            //
            // pnlDetalle
            //
            pnlDetalle.BackColor = System.Drawing.Color.Transparent;
            pnlDetalle.Controls.Add(pnlEditor);
            pnlDetalle.Controls.Add(pnlInfo);
            pnlDetalle.Dock = DockStyle.Fill;
            pnlDetalle.Location = new System.Drawing.Point(353, 88);
            pnlDetalle.Name = "pnlDetalle";
            pnlDetalle.Padding = new Padding(20, 16, 20, 16);
            pnlDetalle.Size = new System.Drawing.Size(655, 709);
            pnlDetalle.TabIndex = 3;
            //
            // pnlEditor
            //
            pnlEditor.BackColor = System.Drawing.Color.Transparent;
            pnlEditor.Controls.Add(pnlEditorInterior);
            pnlEditor.Controls.Add(pnlAcciones);
            pnlEditor.Dock = DockStyle.Fill;
            pnlEditor.Location = new System.Drawing.Point(20, 186);
            pnlEditor.Name = "pnlEditor";
            pnlEditor.Size = new System.Drawing.Size(615, 507);
            pnlEditor.TabIndex = 1;
            //
            // pnlEditorInterior
            //
            pnlEditorInterior.BackColor = System.Drawing.Color.Transparent;
            pnlEditorInterior.Controls.Add(txtEditor);
            pnlEditorInterior.Dock = DockStyle.Fill;
            pnlEditorInterior.Name = "pnlEditorInterior";
            pnlEditorInterior.Padding = new Padding(14, 10, 14, 10);
            pnlEditorInterior.Size = new System.Drawing.Size(615, 447);
            pnlEditorInterior.TabIndex = 0;
            //
            // txtEditor
            //
            txtEditor.BorderStyle = BorderStyle.FixedSingle;
            txtEditor.Dock = DockStyle.Fill;
            txtEditor.Font = new System.Drawing.Font("Consolas", 10.5F);
            txtEditor.Location = new System.Drawing.Point(14, 10);
            txtEditor.Multiline = true;
            txtEditor.Name = "txtEditor";
            txtEditor.ScrollBars = ScrollBars.Vertical;
            txtEditor.Size = new System.Drawing.Size(587, 427);
            txtEditor.TabIndex = 0;
            txtEditor.AcceptsTab = true;
            txtEditor.AcceptsReturn = true;
            txtEditor.WordWrap = true;
            txtEditor.HideSelection = false;
            txtEditor.TextChanged += txtEditor_TextChanged;
            //
            // pnlAcciones
            //
            pnlAcciones.BackColor = System.Drawing.Color.Transparent;
            pnlAcciones.Controls.Add(lblEstado);
            pnlAcciones.Controls.Add(btnRestaurar);
            pnlAcciones.Controls.Add(btnGuardar);
            pnlAcciones.Dock = DockStyle.Bottom;
            pnlAcciones.Location = new System.Drawing.Point(0, 447);
            pnlAcciones.Name = "pnlAcciones";
            pnlAcciones.Padding = new Padding(0, 12, 0, 0);
            pnlAcciones.Size = new System.Drawing.Size(615, 60);
            pnlAcciones.TabIndex = 1;
            //
            // btnGuardar
            //
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            btnGuardar.Location = new System.Drawing.Point(455, 16);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new System.Drawing.Size(160, 36);
            btnGuardar.TabIndex = 0;
            btnGuardar.Text = "💾  Guardar cambios";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            //
            // btnRestaurar
            //
            btnRestaurar.FlatStyle = FlatStyle.Flat;
            btnRestaurar.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            btnRestaurar.Location = new System.Drawing.Point(289, 16);
            btnRestaurar.Name = "btnRestaurar";
            btnRestaurar.Size = new System.Drawing.Size(160, 36);
            btnRestaurar.TabIndex = 1;
            btnRestaurar.Text = "↺  Restaurar default";
            btnRestaurar.UseVisualStyleBackColor = true;
            btnRestaurar.Click += btnRestaurar_Click;
            //
            // lblEstado
            //
            lblEstado.AutoSize = false;
            lblEstado.Dock = DockStyle.Left;
            lblEstado.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            lblEstado.Location = new System.Drawing.Point(0, 12);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new System.Drawing.Size(280, 48);
            lblEstado.TabIndex = 2;
            lblEstado.Text = "";
            lblEstado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pnlInfo
            //
            pnlInfo.BackColor = System.Drawing.Color.Transparent;
            pnlInfo.Controls.Add(lblPlaceholders);
            pnlInfo.Controls.Add(lblDescripcion);
            pnlInfo.Controls.Add(lblCategoria);
            pnlInfo.Controls.Add(lblNombre);
            pnlInfo.Dock = DockStyle.Top;
            pnlInfo.Location = new System.Drawing.Point(20, 16);
            pnlInfo.Name = "pnlInfo";
            pnlInfo.Padding = new Padding(8);
            pnlInfo.Size = new System.Drawing.Size(615, 170);
            pnlInfo.TabIndex = 0;
            //
            // lblNombre
            //
            lblNombre.AutoSize = false;
            lblNombre.Dock = DockStyle.Top;
            lblNombre.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblNombre.Location = new System.Drawing.Point(8, 8);
            lblNombre.Name = "lblNombre";
            lblNombre.Size = new System.Drawing.Size(599, 32);
            lblNombre.TabIndex = 0;
            lblNombre.Text = "Selecciona un prompt";
            //
            // lblCategoria
            //
            lblCategoria.AutoSize = false;
            lblCategoria.Dock = DockStyle.Top;
            lblCategoria.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblCategoria.Location = new System.Drawing.Point(8, 40);
            lblCategoria.Name = "lblCategoria";
            lblCategoria.Size = new System.Drawing.Size(599, 22);
            lblCategoria.TabIndex = 1;
            lblCategoria.Text = "";
            //
            // lblDescripcion
            //
            lblDescripcion.AutoSize = false;
            lblDescripcion.Dock = DockStyle.Top;
            lblDescripcion.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            lblDescripcion.Location = new System.Drawing.Point(8, 62);
            lblDescripcion.Name = "lblDescripcion";
            lblDescripcion.Size = new System.Drawing.Size(599, 60);
            lblDescripcion.TabIndex = 2;
            lblDescripcion.Text = "";
            //
            // lblPlaceholders
            //
            lblPlaceholders.AutoSize = false;
            lblPlaceholders.Dock = DockStyle.Top;
            lblPlaceholders.Font = new System.Drawing.Font("Consolas", 9F);
            lblPlaceholders.Location = new System.Drawing.Point(8, 122);
            lblPlaceholders.Name = "lblPlaceholders";
            lblPlaceholders.Size = new System.Drawing.Size(599, 40);
            lblPlaceholders.TabIndex = 3;
            lblPlaceholders.Text = "";
            //
            // splitter1
            //
            splitter1.Location = new System.Drawing.Point(350, 88);
            splitter1.Name = "splitter1";
            splitter1.Size = new System.Drawing.Size(3, 709);
            splitter1.TabIndex = 2;
            splitter1.TabStop = false;
            //
            // pnlLista
            //
            pnlLista.BackColor = System.Drawing.Color.Transparent;
            pnlLista.Controls.Add(lstPromts);
            pnlLista.Controls.Add(pnlBuscador);
            pnlLista.Dock = DockStyle.Left;
            pnlLista.Location = new System.Drawing.Point(0, 88);
            pnlLista.Name = "pnlLista";
            pnlLista.Padding = new Padding(16, 8, 8, 16);
            pnlLista.Size = new System.Drawing.Size(350, 709);
            pnlLista.TabIndex = 1;
            //
            // lstPromts
            //
            lstPromts.BorderStyle = BorderStyle.FixedSingle;
            lstPromts.Dock = DockStyle.Fill;
            lstPromts.DrawMode = DrawMode.OwnerDrawFixed;
            lstPromts.Font = new System.Drawing.Font("Segoe UI", 10F);
            lstPromts.FormattingEnabled = true;
            lstPromts.ItemHeight = 44;
            lstPromts.IntegralHeight = false;
            lstPromts.Location = new System.Drawing.Point(16, 58);
            lstPromts.Name = "lstPromts";
            lstPromts.Size = new System.Drawing.Size(326, 635);
            lstPromts.TabIndex = 1;
            lstPromts.DrawItem += lstPromts_DrawItem;
            lstPromts.SelectedIndexChanged += lstPromts_SelectedIndexChanged;
            //
            // pnlBuscador
            //
            pnlBuscador.BackColor = System.Drawing.Color.Transparent;
            pnlBuscador.Controls.Add(txtFiltro);
            pnlBuscador.Dock = DockStyle.Top;
            pnlBuscador.Location = new System.Drawing.Point(16, 8);
            pnlBuscador.Name = "pnlBuscador";
            pnlBuscador.Size = new System.Drawing.Size(326, 50);
            pnlBuscador.TabIndex = 0;
            //
            // txtFiltro
            //
            txtFiltro.BorderStyle = BorderStyle.FixedSingle;
            txtFiltro.Dock = DockStyle.Fill;
            txtFiltro.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtFiltro.Location = new System.Drawing.Point(0, 0);
            txtFiltro.Name = "txtFiltro";
            txtFiltro.PlaceholderText = "🔎  Buscar prompt...";
            txtFiltro.Size = new System.Drawing.Size(326, 25);
            txtFiltro.TabIndex = 0;
            txtFiltro.TextChanged += txtFiltro_TextChanged;
            //
            // pnlHeader
            //
            pnlHeader.BackColor = System.Drawing.Color.Transparent;
            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new System.Drawing.Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new Padding(24, 12, 24, 8);
            pnlHeader.Size = new System.Drawing.Size(1008, 88);
            pnlHeader.TabIndex = 0;
            //
            // lblTitulo
            //
            lblTitulo.AutoSize = false;
            lblTitulo.Dock = DockStyle.Top;
            lblTitulo.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            lblTitulo.Location = new System.Drawing.Point(24, 12);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new System.Drawing.Size(960, 40);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "🧠  Prompts del sistema";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = false;
            lblSubtitulo.Dock = DockStyle.Top;
            lblSubtitulo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            lblSubtitulo.Location = new System.Drawing.Point(24, 52);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new System.Drawing.Size(960, 24);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Visualiza y personaliza cómo responde cada agente de tu asistente.";
            //
            // FrmPromts
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1008, 797);
            Controls.Add(pnlRoot);
            Name = "FrmPromts";
            Text = "Prompts";
            pnlRoot.ResumeLayout(false);
            pnlDetalle.ResumeLayout(false);
            pnlEditor.ResumeLayout(false);
            pnlEditorInterior.ResumeLayout(false);
            pnlEditorInterior.PerformLayout();
            pnlAcciones.ResumeLayout(false);
            pnlInfo.ResumeLayout(false);
            pnlLista.ResumeLayout(false);
            pnlBuscador.ResumeLayout(false);
            pnlBuscador.PerformLayout();
            pnlHeader.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlRoot;
        private Panel pnlHeader;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Panel pnlLista;
        private Panel pnlBuscador;
        private TextBox txtFiltro;
        private ListBox lstPromts;
        private Splitter splitter1;
        private Panel pnlDetalle;
        private Panel pnlInfo;
        private Label lblNombre;
        private Label lblCategoria;
        private Label lblDescripcion;
        private Label lblPlaceholders;
        private Panel pnlEditor;
        private Panel pnlEditorInterior;
        private TextBox txtEditor;
        private Panel pnlAcciones;
        private Button btnGuardar;
        private Button btnRestaurar;
        private Label lblEstado;
    }
}
