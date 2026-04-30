// ============================================================
//  DialogoParametrosSkill.cs — Diálogo dinámico de parámetros
//
//  Construye un formulario en runtime a partir de Skill.Parametros
//  para que el usuario introduzca los valores antes de probar el
//  skill. Marca requeridos en rojo, prellena valores por defecto,
//  usa ComboBox cuando hay Opciones, y CheckBox para booleanos.
//
//  Devuelve un JObject listo para inyectar como SKILL_PARAMS.
// ============================================================

using Newtonsoft.Json.Linq;
using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    /// <summary>
    /// Diálogo modal que recoge los valores de los parámetros de un skill
    /// antes de ejecutarlo desde la UI de prueba.
    /// </summary>
    public sealed class DialogoParametrosSkill : Form
    {
        // ── Paleta (alineada con Skills.cs) ──────────────────────────────────
        private static readonly Color ColorFondo            = Color.FromArgb(15,  23,  42);
        private static readonly Color ColorCard             = Color.FromArgb(30,  41,  59);
        private static readonly Color ColorBorde            = Color.FromArgb(51,  65,  85);
        private static readonly Color ColorTextoPrincipal   = Color.FromArgb(241, 245, 249);
        private static readonly Color ColorTextoSecundario  = Color.FromArgb(148, 163, 184);
        private static readonly Color ColorAcento           = Color.FromArgb(37,  99,  235);
        private static readonly Color ColorVerde            = Color.FromArgb(52,  211, 153);
        private static readonly Color ColorRojo             = Color.FromArgb(248, 113, 113);
        private static readonly Color ColorAmbar            = Color.FromArgb(251, 191,  36);

        private readonly Skill _skill;
        private readonly Dictionary<string, Control> _editores = new();
        private Label _lblErrores = null!;

        /// <summary>JObject con los valores capturados. Solo válido tras DialogResult.OK.</summary>
        public JObject Valores { get; private set; } = new JObject();

        public DialogoParametrosSkill(Skill skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
            ConstruirFormulario();
        }

        // ── Helper estático para uso desde Skills.cs ─────────────────────────

        /// <summary>
        /// Muestra el diálogo si el skill tiene parámetros. Devuelve los valores
        /// como JObject, o null si el usuario canceló o no había parámetros.
        /// Si <paramref name="skill"/> no tiene parámetros, devuelve un JObject vacío
        /// directamente (no abre diálogo).
        /// </summary>
        public static JObject? MostrarYObtener(Skill skill, IWin32Window? owner = null)
        {
            if (skill?.Parametros == null || skill.Parametros.Count == 0)
                return new JObject();

            using var dlg = new DialogoParametrosSkill(skill);
            var r = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
            return r == DialogResult.OK ? dlg.Valores : null;
        }

        // ── Construcción del formulario ──────────────────────────────────────

        private void ConstruirFormulario()
        {
            Text            = $"Parámetros: {_skill.NombreEfectivo}";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox     = false;
            MaximizeBox     = false;
            ShowInTaskbar   = false;
            BackColor       = ColorFondo;
            ForeColor       = ColorTextoPrincipal;
            Font            = new Font("Segoe UI", 9f);
            Width           = 560;

            int y = 12;

            // ── Encabezado ────────────────────────────────────────────────────
            var lblTitulo = new Label
            {
                Text      = $"🧩  {_skill.NombreEfectivo}",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = ColorTextoPrincipal,
                Location  = new Point(16, y),
                AutoSize  = true
            };
            Controls.Add(lblTitulo);
            y += 24;

            int reqs = _skill.Parametros.Count(p => p.Requerido);
            var lblHint = new Label
            {
                Text      = $"{_skill.Parametros.Count} parámetros · {reqs} requeridos · " +
                            "los marcados en rojo son obligatorios.",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = ColorTextoSecundario,
                Location  = new Point(16, y),
                AutoSize  = true
            };
            Controls.Add(lblHint);
            y += 22;

            // Separador
            var sep = new Panel
            {
                Location  = new Point(0, y),
                Size      = new Size(Width, 1),
                BackColor = ColorBorde,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(sep);
            y += 12;

            // ── Campos por parámetro ──────────────────────────────────────────
            foreach (var p in _skill.Parametros)
            {
                y = AgregarCampoParametro(p, y);
            }

            // ── Label de errores ──────────────────────────────────────────────
            _lblErrores = new Label
            {
                Location  = new Point(16, y),
                Size      = new Size(Width - 32, 0),
                AutoSize  = false,
                ForeColor = ColorRojo,
                Font      = new Font("Segoe UI", 8.5f),
                MaximumSize = new Size(Width - 32, 0)
            };
            Controls.Add(_lblErrores);
            y += 4;

            // ── Botones ───────────────────────────────────────────────────────
            int yBtn = y + 12;

            var btnCancelar = new Button
            {
                Text      = "Cancelar",
                Size      = new Size(100, 30),
                Location  = new Point(Width - 230, yBtn),
                FlatStyle = FlatStyle.Flat,
                ForeColor = ColorTextoSecundario,
                BackColor = ColorCard,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderColor = ColorBorde;
            btnCancelar.FlatAppearance.BorderSize  = 1;
            Controls.Add(btnCancelar);

            var btnEjecutar = new Button
            {
                Text      = "▶  Ejecutar con estos valores",
                Size      = new Size(220, 30),
                Location  = new Point(Width - 120 - 220 + 10, yBtn),
                FlatStyle = FlatStyle.Flat,
                ForeColor = ColorVerde,
                BackColor = ColorCard,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            // Reposicionar para alinear a la derecha sin solapar
            btnEjecutar.Location = new Point(Width - 240, yBtn);
            btnCancelar.Location = new Point(btnEjecutar.Left - 110, yBtn);
            btnEjecutar.FlatAppearance.BorderColor = ColorVerde;
            btnEjecutar.FlatAppearance.BorderSize  = 1;
            btnEjecutar.Click += (_, _) => OnEjecutar();
            Controls.Add(btnEjecutar);

            AcceptButton = btnEjecutar;
            CancelButton = btnCancelar;

            Height = yBtn + 80;
        }

        private int AgregarCampoParametro(SkillParametro p, int y)
        {
            // ── Etiqueta ──────────────────────────────────────────────────────
            string sufijo = p.Requerido ? " *" : " (opcional)";
            var lblNombre = new Label
            {
                Text      = $"{p.Nombre}{sufijo}",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = p.Requerido ? ColorRojo : ColorTextoPrincipal,
                Location  = new Point(16, y),
                AutoSize  = true
            };
            Controls.Add(lblNombre);

            // Tipo a la derecha
            var lblTipo = new Label
            {
                Text      = $"<{p.Tipo}>",
                Font      = new Font("Consolas", 8f),
                ForeColor = ColorAmbar,
                Location  = new Point(lblNombre.Right + 6, y + 2),
                AutoSize  = true
            };
            Controls.Add(lblTipo);

            y += 20;

            // ── Descripción (si existe) ───────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(p.Descripcion))
            {
                var lblDesc = new Label
                {
                    Text      = p.Descripcion,
                    Font      = new Font("Segoe UI", 8f, FontStyle.Italic),
                    ForeColor = ColorTextoSecundario,
                    Location  = new Point(16, y),
                    Size      = new Size(Width - 48, 18),
                    AutoEllipsis = true
                };
                Controls.Add(lblDesc);
                y += 18;
            }

            // ── Editor según tipo ─────────────────────────────────────────────
            Control editor;

            if (p.Opciones != null && p.Opciones.Count > 0)
            {
                var combo = new ComboBox
                {
                    Location      = new Point(16, y),
                    Size          = new Size(Width - 48, 26),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor     = ColorCard,
                    ForeColor     = ColorTextoPrincipal,
                    Font          = new Font("Segoe UI", 9.5f),
                    FlatStyle     = FlatStyle.Flat
                };
                foreach (var op in p.Opciones) combo.Items.Add(op);
                if (!string.IsNullOrWhiteSpace(p.ValorPorDefecto) &&
                    p.Opciones.Any(o => o.Equals(p.ValorPorDefecto, StringComparison.OrdinalIgnoreCase)))
                {
                    combo.SelectedItem = p.Opciones.First(
                        o => o.Equals(p.ValorPorDefecto, StringComparison.OrdinalIgnoreCase));
                }
                else if (!p.Requerido && combo.Items.Count > 0)
                {
                    combo.SelectedIndex = 0;
                }
                editor = combo;
            }
            else if (p.Tipo.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
                     p.Tipo.Equals("bool",    StringComparison.OrdinalIgnoreCase))
            {
                var chk = new CheckBox
                {
                    Text      = "true / activado",
                    Location  = new Point(16, y),
                    Size      = new Size(Width - 48, 24),
                    ForeColor = ColorTextoPrincipal,
                    BackColor = Color.Transparent,
                    Font      = new Font("Segoe UI", 9.5f),
                    Checked   = p.ValorPorDefecto.Equals("true", StringComparison.OrdinalIgnoreCase)
                };
                editor = chk;
            }
            else
            {
                var tb = new TextBox
                {
                    Location    = new Point(16, y),
                    Size        = new Size(Width - 48, 26),
                    BackColor   = ColorCard,
                    ForeColor   = ColorTextoPrincipal,
                    Font        = new Font("Consolas", 9.5f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Text        = p.ValorPorDefecto ?? ""
                };
                if (string.IsNullOrEmpty(tb.Text))
                {
                    tb.PlaceholderText = p.Tipo.Equals("number", StringComparison.OrdinalIgnoreCase)
                        ? "ej: 42"
                        : p.Tipo.Equals("array", StringComparison.OrdinalIgnoreCase)
                            ? "JSON array — ej: [\"a\",\"b\"]"
                            : "";
                }
                editor = tb;
            }

            Controls.Add(editor);
            _editores[p.Nombre] = editor;

            return y + 36;
        }

        // ── Validación y captura ─────────────────────────────────────────────

        private void OnEjecutar()
        {
            var jo = new JObject();

            foreach (var p in _skill.Parametros)
            {
                if (!_editores.TryGetValue(p.Nombre, out var ed)) continue;

                string raw;
                if (ed is CheckBox chk)
                    raw = chk.Checked ? "true" : "false";
                else if (ed is ComboBox cb)
                    raw = cb.SelectedItem?.ToString() ?? "";
                else
                    raw = ed.Text ?? "";

                // Skip vacíos no requeridos
                if (string.IsNullOrWhiteSpace(raw) && !p.Requerido) continue;

                // Convertir al token JSON apropiado
                if (p.Tipo.Equals("number", StringComparison.OrdinalIgnoreCase) ||
                    p.Tipo.Equals("integer", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(raw,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var num))
                        jo[p.Nombre] = num;
                    else
                        jo[p.Nombre] = raw; // dejar que el validador lo capture
                }
                else if (p.Tipo.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
                         p.Tipo.Equals("bool",    StringComparison.OrdinalIgnoreCase))
                {
                    jo[p.Nombre] = raw.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                else if (p.Tipo.Equals("array", StringComparison.OrdinalIgnoreCase))
                {
                    try { jo[p.Nombre] = JArray.Parse(raw); }
                    catch { jo[p.Nombre] = raw; }
                }
                else
                {
                    jo[p.Nombre] = raw;
                }
            }

            // Validar antes de cerrar
            var errores = _skill.ValidarParametros(jo);
            if (errores.Count > 0)
            {
                _lblErrores.Text = "⚠  " + string.Join(Environment.NewLine + "⚠  ", errores);
                _lblErrores.AutoSize = true;
                _lblErrores.MaximumSize = new Size(Width - 32, 0);
                return;
            }

            Valores      = jo;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
