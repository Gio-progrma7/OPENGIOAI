// ============================================================
//  FrmMemoria.cs  — Fase 1 del sistema de Memoria
//
//  QUÉ HACE:
//    Permite ver y editar a mano los dos archivos de memoria
//    durable asociados a la ruta de trabajo activa:
//      - Hechos.md      → verdades sobre el usuario / entorno
//      - Episodios.md   → timeline append-only de ejecuciones
//
//  FLUJO:
//    1. Al abrir: lee ambos archivos desde {rutaTrabajo}/Memoria/
//       (los crea vacíos si no existen, al guardar).
//    2. Usuario edita en el TextBox de cada pestaña.
//    3. Guardar → escribe a disco.
//    4. Footer muestra el tamaño aproximado en tokens que
//       se está inyectando al prompt en cada ejecución.
// ============================================================

using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmMemoria : Form
    {
        private readonly string _rutaTrabajo;
        private bool _cargando;
        private bool _modHechos;
        private bool _modEpisodios;

        public FrmMemoria(string rutaTrabajo)
        {
            InitializeComponent();
            _rutaTrabajo = rutaTrabajo ?? "";
            AplicarTema();
            _ = CargarAsync();
        }

        private void AplicarTema()
        {
            BackColor = EmeraldTheme.BgDeep;
            ForeColor = EmeraldTheme.TextPrimary;

            pnlRoot.BackColor   = EmeraldTheme.BgDeep;
            pnlHeader.BackColor = EmeraldTheme.BgDeep;
            pnlFooter.BackColor = EmeraldTheme.BgDeep;

            lblTitulo.ForeColor    = EmeraldTheme.Emerald400;
            lblSubtitulo.ForeColor = EmeraldTheme.TextMuted;
            lblRuta.ForeColor      = EmeraldTheme.TextSecondary;
            lblEstado.ForeColor    = EmeraldTheme.TextMuted;
            lblTokens.ForeColor    = EmeraldTheme.Emerald400;

            tabControl.BackColor = EmeraldTheme.BgSurface;

            foreach (TabPage tp in tabControl.TabPages)
                tp.BackColor = EmeraldTheme.BgSurface;

            txtHechos.BackColor    = EmeraldTheme.BgCard;
            txtHechos.ForeColor    = EmeraldTheme.TextPrimary;
            txtEpisodios.BackColor = EmeraldTheme.BgCard;
            txtEpisodios.ForeColor = EmeraldTheme.TextPrimary;

            EstilarBoton(btnGuardar, true);
            EstilarBoton(btnOlvidarTodo, false);
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

        private async Task CargarAsync()
        {
            _cargando = true;
            try
            {
                if (string.IsNullOrWhiteSpace(_rutaTrabajo))
                {
                    lblRuta.Text = "⚠ No hay ruta de trabajo activa. Configurala en el menú de Rutas.";
                    txtHechos.ReadOnly = true;
                    txtEpisodios.ReadOnly = true;
                    btnGuardar.Enabled = false;
                    btnOlvidarTodo.Enabled = false;
                    return;
                }

                lblRuta.Text = $"Ruta de memoria: {_rutaTrabajo}\\Memoria\\";

                string hechos = await MemoriaManager.LeerHechosAsync(_rutaTrabajo);
                string eps    = await MemoriaManager.LeerEpisodiosAsync(_rutaTrabajo);

                if (string.IsNullOrEmpty(hechos))
                    hechos = PlantillaHechos();
                if (string.IsNullOrEmpty(eps))
                    eps = PlantillaEpisodios();

                txtHechos.Text    = NormalizarSaltos(hechos);
                txtEpisodios.Text = NormalizarSaltos(eps);

                _modHechos = false;
                _modEpisodios = false;
                await RefrescarMetricaAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la memoria: " + ex.Message,
                    "Memoria", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cargando = false;
                ActualizarEstado();
            }
        }

        private async Task RefrescarMetricaAsync()
        {
            try
            {
                int tokens = await MemoriaManager.EstimarTokensActivosAsync(_rutaTrabajo);
                lblTokens.Text = tokens == 0
                    ? "Memoria activa: vacía"
                    : $"Memoria activa: ~{tokens} tokens";
            }
            catch
            {
                lblTokens.Text = "";
            }
        }

        // ══════════════════ Eventos ══════════════════

        private void txtHechos_TextChanged(object? sender, EventArgs e)
        {
            if (_cargando) return;
            _modHechos = true;
            ActualizarEstado();
        }

        private void txtEpisodios_TextChanged(object? sender, EventArgs e)
        {
            if (_cargando) return;
            _modEpisodios = true;
            ActualizarEstado();
        }

        private async void btnGuardar_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_rutaTrabajo)) return;

            try
            {
                btnGuardar.Enabled = false;

                if (_modHechos)
                    await MemoriaManager.GuardarHechosAsync(_rutaTrabajo, txtHechos.Text);
                if (_modEpisodios)
                    await MemoriaManager.GuardarEpisodiosAsync(_rutaTrabajo, txtEpisodios.Text);

                _modHechos = false;
                _modEpisodios = false;
                await RefrescarMetricaAsync();
                ActualizarEstado();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message,
                    "Memoria", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGuardar.Enabled = true;
            }
        }

        private async void btnOlvidarTodo_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_rutaTrabajo)) return;

            var r = MessageBox.Show(
                "¿Borrar toda la memoria de esta ruta de trabajo?\n" +
                "Esta acción no se puede deshacer.",
                "Olvidar todo",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            try
            {
                await MemoriaManager.GuardarHechosAsync(_rutaTrabajo, "");
                await MemoriaManager.GuardarEpisodiosAsync(_rutaTrabajo, "");
                await CargarAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al borrar: " + ex.Message,
                    "Memoria", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════ Helpers ══════════════════

        private void ActualizarEstado()
        {
            if (_modHechos || _modEpisodios)
                lblEstado.Text = "● cambios sin guardar";
            else
                lblEstado.Text = "· sincronizado con disco";
        }

        private static string NormalizarSaltos(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }

        private static string PlantillaHechos() =>
            "# Hechos" + Environment.NewLine +
            "Anota aquí verdades durables sobre ti y tu entorno." + Environment.NewLine +
            "Cada línea es un bullet que el agente lee antes de cada ejecución." + Environment.NewLine +
            Environment.NewLine +
            "- " + Environment.NewLine;

        private static string PlantillaEpisodios() =>
            "# Episodios" + Environment.NewLine +
            "Timeline de ejecuciones relevantes (append-only)." + Environment.NewLine +
            Environment.NewLine;
    }
}
