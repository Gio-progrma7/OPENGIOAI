// ============================================================
//  FrmPromts.cs
//  Módulo de visualización y edición de prompts.
//
//  FLUJO:
//    1. Al abrir: carga todas las definiciones del PromptRegistry.
//    2. Usuario selecciona un prompt → se muestra metadata + texto efectivo.
//    3. Usuario edita → al guardar, se persiste como override en
//       {AppDir}/PromtsUsuario/{clave}.md
//    4. Si el usuario pulsa "Restaurar default" se borra el override.
//
//  NO SE MODIFICA LA LÓGICA de ningún agente. El registry es el único
//  punto donde se lee el prompt, así que cualquier cambio se refleja
//  en la próxima ejecución del pipeline.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.Promts;
using OPENGIOAI.Themas;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmPromts : Form
    {
        private List<PromptDefinition> _todos = new();
        private PromptDefinition? _seleccionado;
        private bool _cargando;      // evita disparar TextChanged al poblar
        private bool _modificado;    // cuando txtEditor difiere del disco

        public FrmPromts()
        {
            InitializeComponent();
            AplicarTema();
            CargarDefiniciones();
        }

        // ══════════════════ Tema ══════════════════

        private void AplicarTema()
        {
            BackColor = EmeraldTheme.BgDeep;
            ForeColor = EmeraldTheme.TextPrimary;

            pnlRoot.BackColor    = EmeraldTheme.BgDeep;
            pnlHeader.BackColor  = EmeraldTheme.BgDeep;
            pnlLista.BackColor   = EmeraldTheme.BgSurface;
            pnlDetalle.BackColor = EmeraldTheme.BgSurface;
            pnlInfo.BackColor    = EmeraldTheme.BgCard;
            pnlEditor.BackColor  = EmeraldTheme.BgSurface;
            pnlEditorInterior.BackColor = EmeraldTheme.BgCard;

            lblTitulo.ForeColor      = EmeraldTheme.Emerald400;
            lblSubtitulo.ForeColor   = EmeraldTheme.TextMuted;
            lblNombre.ForeColor      = EmeraldTheme.TextPrimary;
            lblCategoria.ForeColor   = EmeraldTheme.Emerald500;
            lblDescripcion.ForeColor = EmeraldTheme.TextSecondary;
            lblPlaceholders.ForeColor= EmeraldTheme.TextMuted;
            lblEstado.ForeColor      = EmeraldTheme.TextMuted;

            lstPromts.BackColor = EmeraldTheme.BgCard;
            lstPromts.ForeColor = EmeraldTheme.TextPrimary;

            txtFiltro.BackColor = EmeraldTheme.BgCard;
            txtFiltro.ForeColor = EmeraldTheme.TextPrimary;

            txtEditor.BackColor = EmeraldTheme.BgCard;
            txtEditor.ForeColor = EmeraldTheme.TextPrimary;

            EstilarBoton(btnGuardar, primario: true);
            EstilarBoton(btnRestaurar, primario: false);
        }

        private static void EstilarBoton(Button b, bool primario)
        {
            b.FlatAppearance.BorderSize  = 1;
            b.FlatAppearance.BorderColor = primario
                ? EmeraldTheme.Emerald500
                : EmeraldTheme.Emerald900;
            b.BackColor = primario ? EmeraldTheme.Emerald900 : EmeraldTheme.BgCard;
            b.ForeColor = primario ? EmeraldTheme.TextPrimary : EmeraldTheme.TextSecondary;
            b.FlatAppearance.MouseOverBackColor = EmeraldTheme.Emerald600;
            b.FlatAppearance.MouseDownBackColor = EmeraldTheme.Emerald900;
            b.Cursor = Cursors.Hand;
        }

        // ══════════════════ Carga ══════════════════

        private void CargarDefiniciones()
        {
            _todos = PromptRegistry.Instancia
                .TodasLasDefiniciones()
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.NombreVisible)
                .ToList();

            RefrescarLista();
        }

        private void RefrescarLista()
        {
            string filtro = (txtFiltro.Text ?? "").Trim().ToLowerInvariant();

            lstPromts.BeginUpdate();
            lstPromts.Items.Clear();

            foreach (var def in _todos)
            {
                if (!string.IsNullOrEmpty(filtro))
                {
                    bool coincide =
                        def.NombreVisible.ToLowerInvariant().Contains(filtro) ||
                        def.Categoria.ToLowerInvariant().Contains(filtro) ||
                        def.Clave.ToLowerInvariant().Contains(filtro);
                    if (!coincide) continue;
                }
                lstPromts.Items.Add(def);
            }
            lstPromts.EndUpdate();

            if (lstPromts.Items.Count > 0 && _seleccionado == null)
                lstPromts.SelectedIndex = 0;
        }

        // ══════════════════ Dibujo custom de items ══════════════════

        private void lstPromts_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var def = (PromptDefinition)lstPromts.Items[e.Index]!;
            bool seleccionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool tieneOverride = PromptRegistry.Instancia.TieneOverride(def.Clave);

            using var bg = new SolidBrush(
                seleccionado ? EmeraldTheme.Emerald900 : EmeraldTheme.BgCard);
            e.Graphics.FillRectangle(bg, e.Bounds);

            // Barra lateral sutil: verde si hay override (personalizado)
            if (tieneOverride)
            {
                using var barra = new SolidBrush(EmeraldTheme.Emerald400);
                e.Graphics.FillRectangle(barra, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }

            // Icono + título
            var rectIcono = new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 6, 26, 26);
            using var fIcono = new Font("Segoe UI Emoji", 13F);
            TextRenderer.DrawText(
                e.Graphics, def.Icono ?? "📝", fIcono, rectIcono,
                EmeraldTheme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            var rectTitulo = new Rectangle(
                e.Bounds.X + 42, e.Bounds.Y + 4,
                e.Bounds.Width - 50, 22);
            using var fTitulo = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics, def.NombreVisible, fTitulo, rectTitulo,
                seleccionado ? EmeraldTheme.TextPrimary : EmeraldTheme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);

            // Categoría
            var rectCat = new Rectangle(
                e.Bounds.X + 42, e.Bounds.Y + 22,
                e.Bounds.Width - 50, 20);
            using var fCat = new Font("Segoe UI", 8.5F);
            string sufijo = tieneOverride ? "  · ✎ personalizado" : "";
            TextRenderer.DrawText(
                e.Graphics, def.Categoria + sufijo, fCat, rectCat,
                tieneOverride ? EmeraldTheme.Emerald400 : EmeraldTheme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();
        }

        // ══════════════════ Selección ══════════════════

        private void lstPromts_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_modificado && _seleccionado != null)
            {
                var r = MessageBox.Show(
                    $"Tienes cambios sin guardar en \"{_seleccionado.NombreVisible}\".\n¿Descartarlos?",
                    "Cambios sin guardar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    // restaurar selección anterior
                    _cargando = true;
                    lstPromts.SelectedItem = _seleccionado;
                    _cargando = false;
                    return;
                }
            }

            _seleccionado = lstPromts.SelectedItem as PromptDefinition;
            MostrarSeleccion();
        }

        private void MostrarSeleccion()
        {
            if (_seleccionado == null)
            {
                lblNombre.Text       = "Selecciona un prompt";
                lblCategoria.Text    = "";
                lblDescripcion.Text  = "";
                lblPlaceholders.Text = "";
                txtEditor.Text       = "";
                lblEstado.Text       = "";
                return;
            }

            lblNombre.Text      = $"{_seleccionado.Icono}  {_seleccionado.NombreVisible}";
            lblCategoria.Text   = $"Categoría: {_seleccionado.Categoria}  ·  clave: {_seleccionado.Clave}";
            lblDescripcion.Text = _seleccionado.Descripcion;

            if (_seleccionado.Placeholders.Count == 0)
                lblPlaceholders.Text = "Sin variables. Este es un prompt fijo.";
            else
                lblPlaceholders.Text = "Variables: " +
                    string.Join(" ", _seleccionado.Placeholders.Select(p => "{{" + p + "}}"));

            _cargando = true;
            // Normalizar saltos de línea: WinForms TextBox solo muestra
            // cortes de línea con CRLF. Si el archivo viene con LF (Unix)
            // el texto aparecería en una sola línea corrida.
            string efectivo = PromptRegistry.Instancia.PlantillaEfectiva(_seleccionado.Clave)
                                 ?? "";
            txtEditor.Text = NormalizarSaltos(efectivo);
            txtEditor.SelectionStart = 0;
            txtEditor.SelectionLength = 0;
            txtEditor.ReadOnly = !_seleccionado.Editable;
            _cargando = false;

            _modificado = false;
            ActualizarEstado();
        }

        private void ActualizarEstado()
        {
            if (_seleccionado == null) { lblEstado.Text = ""; return; }

            bool override_ = PromptRegistry.Instancia.TieneOverride(_seleccionado.Clave);
            if (_modificado)
                lblEstado.Text = "● cambios sin guardar";
            else if (override_)
                lblEstado.Text = "✎ personalizado por ti";
            else
                lblEstado.Text = "· usando default del sistema";
        }

        // ══════════════════ Edición ══════════════════

        private void txtEditor_TextChanged(object? sender, EventArgs e)
        {
            if (_cargando || _seleccionado == null) return;
            _modificado = true;
            ActualizarEstado();
        }

        private async void btnGuardar_Click(object? sender, EventArgs e)
        {
            if (_seleccionado == null) return;
            if (!_seleccionado.Editable)
            {
                MessageBox.Show("Este prompt no es editable.", "Prompt bloqueado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                btnGuardar.Enabled = false;
                await PromptRegistry.Instancia.GuardarOverrideAsync(
                    _seleccionado.Clave, txtEditor.Text);

                _modificado = false;
                ActualizarEstado();
                lstPromts.Invalidate(); // refresca badge "personalizado"
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al guardar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGuardar.Enabled = true;
            }
        }

        private void btnRestaurar_Click(object? sender, EventArgs e)
        {
            if (_seleccionado == null) return;

            var r = MessageBox.Show(
                $"¿Restaurar el default de fábrica para \"{_seleccionado.NombreVisible}\"?\n" +
                "Se borrará tu versión personalizada.",
                "Restaurar default",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            PromptRegistry.Instancia.Restaurar(_seleccionado.Clave);

            _cargando = true;
            txtEditor.Text = NormalizarSaltos(_seleccionado.TemplatePorDefecto);
            txtEditor.SelectionStart = 0;
            txtEditor.SelectionLength = 0;
            _cargando = false;

            _modificado = false;
            ActualizarEstado();
            lstPromts.Invalidate();
        }

        // ══════════════════ Helpers ══════════════════

        /// <summary>
        /// Asegura que los saltos sean CRLF — requisito de System.Windows.Forms.TextBox
        /// para renderizar saltos de línea. Sin esto, archivos con solo LF
        /// aparecen como una sola línea continua.
        /// </summary>
        private static string NormalizarSaltos(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            // Pasar cualquier combinación a \n, luego a \r\n → evita doblarlos.
            return s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }

        // ══════════════════ Filtro ══════════════════

        private void txtFiltro_TextChanged(object? sender, EventArgs e)
        {
            var prev = _seleccionado;
            RefrescarLista();
            if (prev != null && lstPromts.Items.Cast<PromptDefinition>().Any(d => d.Clave == prev.Clave))
                lstPromts.SelectedItem = lstPromts.Items.Cast<PromptDefinition>()
                    .First(d => d.Clave == prev.Clave);
        }
    }
}
