// ============================================================
//  FrmConsumoTokens.cs
//
//  Panel FLOTANTE (TopMost, FixedToolWindow, draggable) que muestra
//  en tiempo real el consumo de tokens de cada llamada al LLM.
//
//  Dos pestañas:
//    · 🟢 En vivo     — cada llamada de la ejecución actual (Fase/Modelo/Tokens/USD/Hora)
//    · 📜 Historial  — un renglón por instrucción del usuario (agregado)
//
//  Se suscribe a ConsumoTokensTracker.Instancia:
//    · OnConsumoRegistrado     → agrega fila al listado "En vivo"
//    · OnEjecucionIniciada     → limpia el listado "En vivo" y muestra la nueva instrucción
//    · OnEjecucionFinalizada   → refresca el historial con el bucket cerrado
//
//  Thread-safety:
//    El tracker dispara eventos desde cualquier hilo. Todo uso de UI
//    está envuelto en InvokeIfRequired.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmConsumoTokens : Form
    {
        // Single instance: evita que el usuario abra 5 panels flotantes.
        private static FrmConsumoTokens? _singleton;

        public static void MostrarOTraerAlFrente(IWin32Window? ownerParaPosicion = null)
        {
            if (_singleton == null || _singleton.IsDisposed)
            {
                _singleton = new FrmConsumoTokens();
                if (ownerParaPosicion is Form f)
                {
                    _singleton.Location = new Point(
                        f.Location.X + f.Width - _singleton.Width - 40,
                        f.Location.Y + 80);
                }
                _singleton.Show();
            }
            else
            {
                if (_singleton.WindowState == FormWindowState.Minimized)
                    _singleton.WindowState = FormWindowState.Normal;
                _singleton.BringToFront();
                _singleton.Activate();
            }
        }

        public FrmConsumoTokens()
        {
            InitializeComponent();

            // Tema oscuro consistente con el resto de la app
            try { EmeraldTheme.ApplyTheme(this); } catch { }

            // Colores específicos
            pnlHeader.BackColor   = EmeraldTheme.BgCard;
            pnlResumen.BackColor  = EmeraldTheme.BgSurface;
            lblTitulo.ForeColor   = EmeraldTheme.Emerald400;
            lblResumen.ForeColor  = EmeraldTheme.TextSecondary;

            // Eventos de UI
            btnCerrar.Click  += (_, __) => Close();
            btnLimpiar.Click += BtnLimpiar_Click;

            // Drag manual (FormBorderStyle = SizableToolWindow permite mover, pero
            // también habilitamos drag desde el header para UX más natural).
            pnlHeader.MouseDown += Header_MouseDown;
            lblTitulo.MouseDown += Header_MouseDown;

            // Carga inicial del historial por si el form se abre a mitad de sesión
            RefrescarHistorial();
            RefrescarResumen();

            // Suscripción al tracker — lo hacemos al final para no disparar handlers
            // antes de que el form esté totalmente listo.
            ConsumoTokensTracker.Instancia.OnConsumoRegistrado    += Tracker_OnConsumo;
            ConsumoTokensTracker.Instancia.OnEjecucionIniciada    += Tracker_OnIniciada;
            ConsumoTokensTracker.Instancia.OnEjecucionFinalizada  += Tracker_OnFinalizada;

            FormClosed += (_, __) =>
            {
                try
                {
                    ConsumoTokensTracker.Instancia.OnConsumoRegistrado   -= Tracker_OnConsumo;
                    ConsumoTokensTracker.Instancia.OnEjecucionIniciada   -= Tracker_OnIniciada;
                    ConsumoTokensTracker.Instancia.OnEjecucionFinalizada -= Tracker_OnFinalizada;
                }
                catch { }
            };
        }

        // ═════════════════ Drag manual del header ═════════════════

        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int  SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION       = 0x2;

        private void Header_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            try
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
            catch { }
        }

        // ═════════════════ Handlers del tracker ═════════════════

        private void Tracker_OnConsumo(ConsumoTokens c)
        {
            InvokeIfRequired(() =>
            {
                AgregarFilaVivo(c);
                RefrescarResumen();
            });
        }

        private void Tracker_OnIniciada(EjecucionConsumo ejec)
        {
            InvokeIfRequired(() =>
            {
                lstVivo.Items.Clear();
                lblResumen.Text = $"⏱ Ejecución en curso: {Recortar(ejec.Instruccion, 80)}";
            });
        }

        private void Tracker_OnFinalizada(EjecucionConsumo ejec)
        {
            InvokeIfRequired(() =>
            {
                RefrescarHistorial();
                RefrescarResumen();
            });
        }

        // ═════════════════ Pintado ═════════════════

        private void AgregarFilaVivo(ConsumoTokens c)
        {
            var item = new ListViewItem(string.IsNullOrWhiteSpace(c.Fase) ? "General" : c.Fase);
            item.SubItems.Add(string.IsNullOrWhiteSpace(c.Modelo) ? "-" : c.Modelo);
            item.SubItems.Add(c.PromptTokens.ToString("N0"));
            item.SubItems.Add(c.CompletionTokens.ToString("N0"));
            item.SubItems.Add(c.TotalTokens.ToString("N0"));
            item.SubItems.Add(c.CostoEstimadoUsd > 0 ? $"${c.CostoEstimadoUsd:F4}" : "-");
            item.SubItems.Add(c.Instante.ToString("HH:mm:ss"));
            lstVivo.Items.Add(item);
            item.EnsureVisible();
        }

        private void RefrescarHistorial()
        {
            lstHistorial.BeginUpdate();
            lstHistorial.Items.Clear();

            foreach (var ejec in ConsumoTokensTracker.Instancia.Historial())
            {
                var item = new ListViewItem(ejec.Inicio.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(Recortar(ejec.Instruccion, 80));
                item.SubItems.Add(ejec.Llamadas.Count.ToString("N0"));
                item.SubItems.Add(ejec.TotalTokens.ToString("N0"));
                item.SubItems.Add(ejec.CostoEstimadoUsd > 0 ? $"${ejec.CostoEstimadoUsd:F4}" : "-");
                lstHistorial.Items.Add(item);
            }
            lstHistorial.EndUpdate();
        }

        private void RefrescarResumen()
        {
            var (tokens, usd) = ConsumoTokensTracker.Instancia.TotalSesion();
            var actual = ConsumoTokensTracker.Instancia.EjecucionActual();

            string parteActual = actual == null
                ? "— sin ejecución activa —"
                : $"⏱ actual: {actual.Llamadas.Count} llamadas · {actual.TotalTokens:N0} tok · ${actual.CostoEstimadoUsd:F4}";

            lblResumen.Text =
                $"Σ sesión: {tokens:N0} tok · ${usd:F4}    |    {parteActual}";
        }

        // ═════════════════ Acciones ═════════════════

        private void BtnLimpiar_Click(object? sender, EventArgs e)
        {
            var r = MessageBox.Show(
                "¿Limpiar el historial de ejecuciones? La ejecución en curso (si hay) no se toca.",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            ConsumoTokensTracker.Instancia.Limpiar();
            RefrescarHistorial();
            RefrescarResumen();
        }

        // ═════════════════ Utilidades ═════════════════

        private void InvokeIfRequired(Action a)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke(a);
                else                a();
            }
            catch { /* El form puede haberse cerrado justo a mitad */ }
        }

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
