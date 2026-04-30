// ============================================================
//  DialogoEditarParametro.cs — Editor de un SkillParametro
//
//  Formulario modal para añadir o editar un único parámetro de
//  un skill. Permite definir: nombre, tipo, descripción, si es
//  requerido, valor por defecto, y opciones (lista cerrada).
//
//  Devuelve el SkillParametro construido en la propiedad Resultado
//  cuando DialogResult == OK.
// ============================================================

using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    /// <summary>
    /// Diálogo para crear o editar un <see cref="SkillParametro"/>.
    /// </summary>
    public sealed class DialogoEditarParametro : Form
    {
        // ── Paleta ───────────────────────────────────────────────────────────
        private static readonly Color ColorFondo            = Color.FromArgb(15,  23,  42);
        private static readonly Color ColorCard             = Color.FromArgb(30,  41,  59);
        private static readonly Color ColorBorde            = Color.FromArgb(51,  65,  85);
        private static readonly Color ColorTextoPrincipal   = Color.FromArgb(241, 245, 249);
        private static readonly Color ColorTextoSecundario  = Color.FromArgb(148, 163, 184);
        private static readonly Color ColorVerde            = Color.FromArgb(52,  211, 153);
        private static readonly Color ColorRojo             = Color.FromArgb(248, 113, 113);
        private static readonly Color ColorAmbar            = Color.FromArgb(251, 191,  36);

        private TextBox    _txtNombre        = null!;
        private ComboBox   _cbTipo           = null!;
        private TextBox    _txtDescripcion   = null!;
        private CheckBox   _chkRequerido     = null!;
        private TextBox    _txtDefault       = null!;
        private TextBox    _txtOpciones      = null!;
        private Label      _lblError         = null!;

        /// <summary>Parámetro resultante. Solo válido tras DialogResult.OK.</summary>
        public SkillParametro Resultado { get; private set; } = new();

        public DialogoEditarParametro(SkillParametro? existente = null)
        {
            ConstruirFormulario();
            if (existente != null) Cargar(existente);
        }

        // ── Helper estático ──────────────────────────────────────────────────

        /// <summary>
        /// Muestra el diálogo. Devuelve el parámetro creado/editado, o null si se canceló.
        /// </summary>
        public static SkillParametro? Mostrar(SkillParametro? existente, IWin32Window? owner = null)
        {
            using var dlg = new DialogoEditarParametro(existente);
            var r = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
            return r == DialogResult.OK ? dlg.Resultado : null;
        }

        // ── Construcción ─────────────────────────────────────────────────────

        private void ConstruirFormulario()
        {
            Text            = "Parámetro de skill";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox     = false;
            MaximizeBox     = false;
            ShowInTaskbar   = false;
            BackColor       = ColorFondo;
            ForeColor       = ColorTextoPrincipal;
            Font            = new Font("Segoe UI", 9f);
            Width           = 520;

            int y = 14;

            var lblTitulo = new Label
            {
                Text      = "🧩  Definir parámetro",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = ColorTextoPrincipal,
                Location  = new Point(16, y),
                AutoSize  = true
            };
            Controls.Add(lblTitulo);
            y += 30;

            // ── Nombre ────────────────────────────────────────────────────────
            _txtNombre = AgregarCampoTexto("Nombre (snake_case) *", ref y,
                "ruta, timeout, formato_salida...", true);

            // ── Tipo ──────────────────────────────────────────────────────────
            AgregarLabel("Tipo *", y);
            y += 18;
            _cbTipo = new ComboBox
            {
                Location      = new Point(16, y),
                Size          = new Size(Width - 48, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = ColorCard,
                ForeColor     = ColorTextoPrincipal,
                Font          = new Font("Segoe UI", 9.5f),
                FlatStyle     = FlatStyle.Flat
            };
            _cbTipo.Items.AddRange(new object[]
                { "string", "number", "integer", "boolean", "array", "object" });
            _cbTipo.SelectedIndex = 0;
            Controls.Add(_cbTipo);
            y += 36;

            // ── Descripción ───────────────────────────────────────────────────
            _txtDescripcion = AgregarCampoTexto("Descripción", ref y,
                "Qué representa este parámetro y cómo se usa.", false);

            // ── Requerido ─────────────────────────────────────────────────────
            _chkRequerido = new CheckBox
            {
                Text      = "Requerido (obligatorio para ejecutar el skill)",
                Location  = new Point(16, y),
                Size      = new Size(Width - 48, 22),
                ForeColor = ColorTextoPrincipal,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 9f),
                Checked   = true
            };
            Controls.Add(_chkRequerido);
            y += 30;

            // ── Valor por defecto ─────────────────────────────────────────────
            _txtDefault = AgregarCampoTexto("Valor por defecto (opcional)", ref y,
                "Se usa si el usuario/agente no provee este parámetro.", false);

            // ── Opciones (enum) ───────────────────────────────────────────────
            _txtOpciones = AgregarCampoTexto("Opciones cerradas (opcional, separadas por coma)",
                ref y, "ej: csv, json, yaml", false);

            // ── Error ─────────────────────────────────────────────────────────
            _lblError = new Label
            {
                Location    = new Point(16, y),
                Size        = new Size(Width - 32, 18),
                ForeColor   = ColorRojo,
                Font        = new Font("Segoe UI", 8.5f),
                AutoSize    = false,
                MaximumSize = new Size(Width - 32, 0)
            };
            Controls.Add(_lblError);
            y += 8;

            // ── Botones ───────────────────────────────────────────────────────
            int yBtn = y + 16;

            var btnOk = new Button
            {
                Text      = "Guardar",
                Size      = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = ColorVerde,
                BackColor = ColorCard,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnOk.FlatAppearance.BorderColor = ColorVerde;
            btnOk.FlatAppearance.BorderSize  = 1;
            btnOk.Click += (_, _) => OnGuardar();

            var btnCancel = new Button
            {
                Text         = "Cancelar",
                Size         = new Size(100, 30),
                FlatStyle    = FlatStyle.Flat,
                ForeColor    = ColorTextoSecundario,
                BackColor    = ColorCard,
                Cursor       = Cursors.Hand,
                Font         = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = ColorBorde;
            btnCancel.FlatAppearance.BorderSize  = 1;

            btnOk.Location     = new Point(Width - 240, yBtn);
            btnCancel.Location = new Point(Width - 122, yBtn);

            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Height = yBtn + 80;
        }

        private TextBox AgregarCampoTexto(string label, ref int y, string placeholder, bool grande)
        {
            AgregarLabel(label, y);
            y += 18;

            var tb = new TextBox
            {
                Location        = new Point(16, y),
                Size            = new Size(Width - 48, 26),
                BackColor       = ColorCard,
                ForeColor       = ColorTextoPrincipal,
                Font            = new Font("Consolas", 9.5f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = placeholder
            };
            Controls.Add(tb);
            y += 36;
            return tb;
        }

        private void AgregarLabel(string texto, int y)
        {
            var lbl = new Label
            {
                Text      = texto,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ColorTextoSecundario,
                Location  = new Point(16, y),
                AutoSize  = true
            };
            Controls.Add(lbl);
        }

        private void Cargar(SkillParametro p)
        {
            _txtNombre.Text      = p.Nombre;
            _cbTipo.SelectedItem = _cbTipo.Items.Cast<object>()
                .FirstOrDefault(o => string.Equals(o.ToString(), p.Tipo, StringComparison.OrdinalIgnoreCase))
                ?? "string";
            _txtDescripcion.Text = p.Descripcion;
            _chkRequerido.Checked = p.Requerido;
            _txtDefault.Text     = p.ValorPorDefecto;
            _txtOpciones.Text    = p.Opciones != null && p.Opciones.Count > 0
                ? string.Join(", ", p.Opciones)
                : "";
        }

        // ── Validación y construcción ────────────────────────────────────────

        private void OnGuardar()
        {
            string nombre = _txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                _lblError.Text = "⚠  El nombre es obligatorio.";
                return;
            }
            if (!Regex.IsMatch(nombre, @"^[a-z][a-z0-9_]*$"))
            {
                _lblError.Text = "⚠  Nombre debe ser snake_case: minúsculas, dígitos y _ (empezando por letra).";
                return;
            }

            var opciones = (_txtOpciones.Text ?? "")
                .Split(',')
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToList();

            Resultado = new SkillParametro
            {
                Nombre          = nombre,
                Tipo            = _cbTipo.SelectedItem?.ToString() ?? "string",
                Descripcion     = _txtDescripcion.Text.Trim(),
                Requerido       = _chkRequerido.Checked,
                ValorPorDefecto = _txtDefault.Text.Trim(),
                Opciones        = opciones,
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
