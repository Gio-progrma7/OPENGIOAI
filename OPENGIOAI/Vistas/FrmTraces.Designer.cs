namespace OPENGIOAI.Vistas
{
    partial class FrmTraces
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblFecha;
        private System.Windows.Forms.ComboBox cmbFecha;
        private System.Windows.Forms.Button btnActualizar;
        private System.Windows.Forms.CheckBox chkEnVivo;
        private System.Windows.Forms.Label lblResumen;

        private System.Windows.Forms.SplitContainer splitVertical;     // top vs (tree+detail)
        private System.Windows.Forms.SplitContainer splitHorizontal;   // tree vs detail
        private System.Windows.Forms.ListView lstTraces;
        private System.Windows.Forms.ColumnHeader colHora;
        private System.Windows.Forms.ColumnHeader colInstruccion;
        private System.Windows.Forms.ColumnHeader colDuracion;
        private System.Windows.Forms.ColumnHeader colLlm;
        private System.Windows.Forms.ColumnHeader colTools;
        private System.Windows.Forms.ColumnHeader colTokens;
        private System.Windows.Forms.ColumnHeader colCosto;
        private System.Windows.Forms.ColumnHeader colEstado;

        private System.Windows.Forms.TreeView trvSpans;
        private System.Windows.Forms.TextBox txtDetalle;

        private void InitializeComponent()
        {
            pnlTop          = new System.Windows.Forms.Panel();
            lblFecha        = new System.Windows.Forms.Label();
            cmbFecha        = new System.Windows.Forms.ComboBox();
            btnActualizar   = new System.Windows.Forms.Button();
            chkEnVivo       = new System.Windows.Forms.CheckBox();
            lblResumen      = new System.Windows.Forms.Label();

            splitVertical   = new System.Windows.Forms.SplitContainer();
            splitHorizontal = new System.Windows.Forms.SplitContainer();

            lstTraces       = new System.Windows.Forms.ListView();
            colHora         = new System.Windows.Forms.ColumnHeader();
            colInstruccion  = new System.Windows.Forms.ColumnHeader();
            colDuracion     = new System.Windows.Forms.ColumnHeader();
            colLlm          = new System.Windows.Forms.ColumnHeader();
            colTools        = new System.Windows.Forms.ColumnHeader();
            colTokens       = new System.Windows.Forms.ColumnHeader();
            colCosto        = new System.Windows.Forms.ColumnHeader();
            colEstado       = new System.Windows.Forms.ColumnHeader();

            trvSpans        = new System.Windows.Forms.TreeView();
            txtDetalle      = new System.Windows.Forms.TextBox();

            ((System.ComponentModel.ISupportInitialize)(splitVertical)).BeginInit();
            splitVertical.Panel1.SuspendLayout();
            splitVertical.Panel2.SuspendLayout();
            splitVertical.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(splitHorizontal)).BeginInit();
            splitHorizontal.Panel1.SuspendLayout();
            splitHorizontal.Panel2.SuspendLayout();
            splitHorizontal.SuspendLayout();
            pnlTop.SuspendLayout();
            SuspendLayout();

            // ── pnlTop ──────────────────────────────────────────────────────
            pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            pnlTop.Height = 44;
            pnlTop.BackColor = System.Drawing.Color.FromArgb(30, 41, 59);
            pnlTop.Controls.Add(lblResumen);
            pnlTop.Controls.Add(chkEnVivo);
            pnlTop.Controls.Add(btnActualizar);
            pnlTop.Controls.Add(cmbFecha);
            pnlTop.Controls.Add(lblFecha);

            // ── lblFecha ────────────────────────────────────────────────────
            lblFecha.AutoSize = true;
            lblFecha.Location = new System.Drawing.Point(12, 14);
            lblFecha.Text = "Fecha:";
            lblFecha.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            lblFecha.Font = new System.Drawing.Font("Segoe UI", 9F);

            // ── cmbFecha ────────────────────────────────────────────────────
            cmbFecha.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbFecha.Location = new System.Drawing.Point(60, 11);
            cmbFecha.Width = 130;
            cmbFecha.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            cmbFecha.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            cmbFecha.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
            cmbFecha.SelectedIndexChanged += cmbFecha_SelectedIndexChanged;

            // ── btnActualizar ───────────────────────────────────────────────
            btnActualizar.Text = "⟳ Actualizar";
            btnActualizar.Location = new System.Drawing.Point(200, 10);
            btnActualizar.Size = new System.Drawing.Size(100, 24);
            btnActualizar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnActualizar.FlatAppearance.BorderSize = 0;
            btnActualizar.BackColor = System.Drawing.Color.FromArgb(59, 130, 246);
            btnActualizar.ForeColor = System.Drawing.Color.White;
            btnActualizar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnActualizar.Click += btnActualizar_Click;

            // ── chkEnVivo ───────────────────────────────────────────────────
            chkEnVivo.Text = "● En vivo";
            chkEnVivo.Location = new System.Drawing.Point(310, 13);
            chkEnVivo.AutoSize = true;
            chkEnVivo.Checked = true;
            chkEnVivo.ForeColor = System.Drawing.Color.FromArgb(34, 197, 94);
            chkEnVivo.BackColor = System.Drawing.Color.Transparent;
            chkEnVivo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

            // ── lblResumen ──────────────────────────────────────────────────
            lblResumen.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblResumen.AutoSize = true;
            lblResumen.Location = new System.Drawing.Point(450, 14);
            lblResumen.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            lblResumen.Text = "— sin traces —";
            lblResumen.Font = new System.Drawing.Font("Segoe UI", 9F);

            // ── splitVertical (top=lista, bottom=tree+detalle) ──────────────
            splitVertical.Dock = System.Windows.Forms.DockStyle.Fill;
            splitVertical.Orientation = System.Windows.Forms.Orientation.Horizontal;
            splitVertical.SplitterDistance = 260;
            splitVertical.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            splitVertical.Panel1.Controls.Add(lstTraces);
            splitVertical.Panel2.Controls.Add(splitHorizontal);

            // ── lstTraces ───────────────────────────────────────────────────
            lstTraces.Dock = System.Windows.Forms.DockStyle.Fill;
            lstTraces.View = System.Windows.Forms.View.Details;
            lstTraces.FullRowSelect = true;
            lstTraces.MultiSelect = false;
            lstTraces.GridLines = false;
            lstTraces.HideSelection = false;
            lstTraces.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            lstTraces.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
            lstTraces.Font = new System.Drawing.Font("Consolas", 9F);
            lstTraces.BorderStyle = System.Windows.Forms.BorderStyle.None;
            lstTraces.Columns.AddRange(new System.Windows.Forms.ColumnHeader[]
            {
                colHora, colInstruccion, colDuracion, colLlm, colTools, colTokens, colCosto, colEstado
            });
            lstTraces.SelectedIndexChanged += lstTraces_SelectedIndexChanged;

            colHora.Text        = "Hora";         colHora.Width        = 80;
            colInstruccion.Text = "Instrucción";  colInstruccion.Width = 520;
            colDuracion.Text    = "Duración";     colDuracion.Width    = 90;
            colLlm.Text         = "LLM";          colLlm.Width         = 50;
            colTools.Text       = "Tools";        colTools.Width       = 60;
            colTokens.Text      = "Tokens";       colTokens.Width      = 80;
            colCosto.Text       = "Costo USD";    colCosto.Width       = 90;
            colEstado.Text      = "Estado";       colEstado.Width      = 90;

            // ── splitHorizontal (left=tree, right=detalle) ──────────────────
            splitHorizontal.Dock = System.Windows.Forms.DockStyle.Fill;
            splitHorizontal.Orientation = System.Windows.Forms.Orientation.Vertical;
            splitHorizontal.SplitterDistance = 450;
            splitHorizontal.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            splitHorizontal.Panel1.Controls.Add(trvSpans);
            splitHorizontal.Panel2.Controls.Add(txtDetalle);

            // ── trvSpans ────────────────────────────────────────────────────
            trvSpans.Dock = System.Windows.Forms.DockStyle.Fill;
            trvSpans.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            trvSpans.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
            trvSpans.Font = new System.Drawing.Font("Consolas", 9F);
            trvSpans.BorderStyle = System.Windows.Forms.BorderStyle.None;
            trvSpans.ShowLines = true;
            trvSpans.ShowPlusMinus = true;
            trvSpans.HideSelection = false;
            trvSpans.AfterSelect += trvSpans_AfterSelect;

            // ── txtDetalle ──────────────────────────────────────────────────
            txtDetalle.Dock = System.Windows.Forms.DockStyle.Fill;
            txtDetalle.Multiline = true;
            txtDetalle.ReadOnly = true;
            txtDetalle.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txtDetalle.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            txtDetalle.ForeColor = System.Drawing.Color.FromArgb(226, 232, 240);
            txtDetalle.Font = new System.Drawing.Font("Consolas", 9F);
            txtDetalle.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtDetalle.WordWrap = true;

            // ── FrmTraces ───────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1200, 720);
            Controls.Add(splitVertical);
            Controls.Add(pnlTop);
            BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            Text = "🔬 Traces de ejecución";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(900, 500);

            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            splitHorizontal.Panel1.ResumeLayout(false);
            splitHorizontal.Panel2.ResumeLayout(false);
            splitHorizontal.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(splitHorizontal)).EndInit();
            splitHorizontal.ResumeLayout(false);
            splitVertical.Panel1.ResumeLayout(false);
            splitVertical.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(splitVertical)).EndInit();
            splitVertical.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
