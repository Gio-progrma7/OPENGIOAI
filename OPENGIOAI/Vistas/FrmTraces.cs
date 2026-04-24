// ============================================================
//  FrmTraces.cs  — Fase 1A (UI de Tracing)
//
//  Vista de traces: lista de ejecuciones + árbol de spans +
//  panel de detalles. Modo "en vivo" que reacciona a los eventos
//  del Tracer mientras un pipeline corre.
//
//  CAPAS:
//    · Top:    selector de fecha + botones (actualizar / en vivo).
//    · Centro: ListView con los traces del día seleccionado.
//    · Medio:  TreeView con los spans del trace seleccionado.
//    · Abajo:  detalles del span: atributos, input/output, tokens.
//
//  DISEÑO COHERENTE CON EL RESTO:
//    · Paleta dark (fondo 15,23,42 / acentos 59,130,246) igual
//      que FrmPrincipal / FrmConsumoTokens.
//    · Sin GDI+ custom — TreeView + ListView nativos, que WinForms
//      pinta rápido y accesible de teclado.
//    · Modo en vivo: al activar, se recarga al recibir
//      OnTraceFinalizado o OnSpanCerrado.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmTraces : Form
    {
        // Paleta coherente con el resto del proyecto.
        private static readonly Color ColorFondo        = Color.FromArgb(15, 23, 42);
        private static readonly Color ColorPanel        = Color.FromArgb(30, 41, 59);
        private static readonly Color ColorTexto        = Color.FromArgb(226, 232, 240);
        private static readonly Color ColorTextoSuave   = Color.FromArgb(148, 163, 184);
        private static readonly Color ColorAcento       = Color.FromArgb(59, 130, 246);
        private static readonly Color ColorOk           = Color.FromArgb(34, 197, 94);
        private static readonly Color ColorError        = Color.FromArgb(239, 68, 68);
        private static readonly Color ColorWarn         = Color.FromArgb(234, 179, 8);

        private TraceEjecucion? _traceSeleccionado;
        private readonly Dictionary<string, TreeNode> _nodoPorSpan = new();

        // Single instance: mismo patrón que FrmConsumoTokens — el usuario
        // suele tener esto como panel flotante de observación mientras
        // trabaja en otra pantalla, así que evitamos multiplicar ventanas.
        private static FrmTraces? _singleton;

        /// <summary>
        /// Abre la ventana de Traces como panel flotante. Si ya existe,
        /// la trae al frente (y la restaura si estaba minimizada). Se
        /// posiciona pegada al lado derecho del form owner para que
        /// conviva con el principal sin taparlo.
        /// </summary>
        public static void MostrarOTraerAlFrente(IWin32Window? ownerParaPosicion = null)
        {
            if (_singleton == null || _singleton.IsDisposed)
            {
                _singleton = new FrmTraces();
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

        public FrmTraces()
        {
            InitializeComponent();

            // Eventos del tracer para modo en vivo.
            TracerEjecucion.Instancia.OnTraceIniciado   += ManejarTraceIniciado;
            TracerEjecucion.Instancia.OnSpanAbierto     += ManejarSpanAbierto;
            TracerEjecucion.Instancia.OnSpanCerrado     += ManejarSpanCerrado;
            TracerEjecucion.Instancia.OnTraceFinalizado += ManejarTraceFinalizado;

            FormClosed += (_, _) =>
            {
                TracerEjecucion.Instancia.OnTraceIniciado   -= ManejarTraceIniciado;
                TracerEjecucion.Instancia.OnSpanAbierto     -= ManejarSpanAbierto;
                TracerEjecucion.Instancia.OnSpanCerrado     -= ManejarSpanCerrado;
                TracerEjecucion.Instancia.OnTraceFinalizado -= ManejarTraceFinalizado;
            };

            CargarFechas();
            CargarTracesDelDia();
        }

        // ═════════════════════ Carga de datos ═════════════════════

        private void CargarFechas()
        {
            cmbFecha.BeginUpdate();
            cmbFecha.Items.Clear();
            var fechas = TraceStorage.FechasDisponibles();
            if (fechas.Count == 0)
                fechas.Add(DateTime.Today);
            foreach (var f in fechas)
                cmbFecha.Items.Add(f.ToString("yyyy-MM-dd"));
            if (cmbFecha.Items.Count > 0)
                cmbFecha.SelectedIndex = 0;
            cmbFecha.EndUpdate();
        }

        private void CargarTracesDelDia()
        {
            lstTraces.BeginUpdate();
            lstTraces.Items.Clear();

            if (cmbFecha.SelectedItem == null)
            {
                lstTraces.EndUpdate();
                return;
            }

            if (!DateTime.TryParse(cmbFecha.SelectedItem.ToString(), out var fecha))
            {
                lstTraces.EndUpdate();
                return;
            }

            var indice = TraceStorage.LeerIndice(fecha);
            // Si es hoy y hay trace actual aún no persistido, añadirlo al vuelo.
            var actual = TracerEjecucion.Instancia.TraceActual();
            if (actual != null && actual.Inicio.ToLocalTime().Date == fecha &&
                !indice.Any(i => i.InstruccionId == actual.InstruccionId))
            {
                indice.Insert(0, ProyectarActual(actual));
            }

            // Orden: más reciente primero.
            indice.Sort((a, b) => b.Inicio.CompareTo(a.Inicio));

            foreach (var item in indice)
            {
                var li = new ListViewItem(item.Inicio.ToLocalTime().ToString("HH:mm:ss"))
                {
                    Tag = item,
                };
                li.SubItems.Add(Recortar(item.Instruccion, 90));
                li.SubItems.Add($"{item.DuracionMs} ms");
                li.SubItems.Add(item.TotalLlamadasLLM.ToString());
                li.SubItems.Add(item.TotalHerramientas.ToString());
                li.SubItems.Add(item.TotalTokens.ToString());
                li.SubItems.Add(item.TotalCostoUsd.ToString("F4"));
                li.SubItems.Add(EstadoTexto(item.Estado));
                li.ForeColor = ColorPorEstado(item.Estado);
                lstTraces.Items.Add(li);
            }

            lstTraces.EndUpdate();

            lblResumen.Text = $"{indice.Count} traces · " +
                              $"{indice.Sum(i => i.TotalLlamadasLLM)} LLM calls · " +
                              $"{indice.Sum(i => i.TotalTokens):N0} tokens · " +
                              $"${indice.Sum(i => i.TotalCostoUsd):F4}";
        }

        private static TraceIndiceItem ProyectarActual(TraceEjecucion t) => new()
        {
            InstruccionId     = t.InstruccionId,
            Instruccion       = t.Instruccion,
            Inicio            = t.Inicio,
            DuracionMs        = (long)(DateTime.UtcNow - t.Inicio).TotalMilliseconds,
            Estado            = t.Estado,
            TotalSpans        = t.TotalSpans,
            TotalLlamadasLLM  = t.TotalLlamadasLLM,
            TotalHerramientas = t.TotalHerramientas,
            TotalTokens       = t.TotalTokens,
            TotalCostoUsd     = t.TotalCostoUsd,
            Modelo            = t.Modelo,
            Servicio          = t.Servicio,
        };

        private void MostrarTrace(TraceEjecucion trace)
        {
            _traceSeleccionado = trace;
            _nodoPorSpan.Clear();

            trvSpans.BeginUpdate();
            trvSpans.Nodes.Clear();

            // Construir árbol: ParentId == null → raíz, si no, colgar del padre.
            // Orden por Inicio para que el árbol respete el flujo.
            var porId = trace.Spans.ToDictionary(s => s.Id);
            foreach (var s in trace.Spans.OrderBy(s => s.Inicio))
            {
                var nodo = CrearNodoSpan(s);
                _nodoPorSpan[s.Id] = nodo;

                if (!string.IsNullOrEmpty(s.ParentId) && _nodoPorSpan.TryGetValue(s.ParentId, out var padre))
                    padre.Nodes.Add(nodo);
                else
                    trvSpans.Nodes.Add(nodo);
            }

            trvSpans.ExpandAll();
            trvSpans.EndUpdate();

            MostrarDetalleSpan(null);
        }

        private TreeNode CrearNodoSpan(TraceSpan s)
        {
            string prefijo = PrefijoTipo(s.Tipo);
            string etiqueta = $"{prefijo} {s.Nombre}  ·  {s.DuracionMs} ms";
            if (s.TotalTokens.HasValue && s.TotalTokens.Value > 0)
                etiqueta += $"  ·  {s.TotalTokens.Value} tok";
            if (s.CostoUsd.HasValue && s.CostoUsd.Value > 0)
                etiqueta += $"  ·  ${s.CostoUsd.Value:F4}";

            var nodo = new TreeNode(etiqueta)
            {
                Tag = s,
                ForeColor = ColorPorEstado(s.Estado),
            };
            return nodo;
        }

        private void MostrarDetalleSpan(TraceSpan? s)
        {
            if (s == null)
            {
                txtDetalle.Text = _traceSeleccionado == null
                    ? "Selecciona un trace para ver sus spans."
                    : BuildCabeceraTrace(_traceSeleccionado);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{PrefijoTipo(s.Tipo)} {s.Nombre}");
            sb.AppendLine(new string('─', 60));
            sb.AppendLine($"Tipo:     {s.Tipo}");
            sb.AppendLine($"Estado:   {EstadoTexto(s.Estado)}");
            sb.AppendLine($"Inicio:   {s.Inicio.ToLocalTime():HH:mm:ss.fff}");
            sb.AppendLine($"Fin:      {(s.Fin.HasValue ? s.Fin.Value.ToLocalTime().ToString("HH:mm:ss.fff") : "— en curso —")}");
            sb.AppendLine($"Duración: {s.DuracionMs} ms");

            if (s.TotalTokens.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine($"Tokens:   prompt={s.PromptTokens ?? 0}  completion={s.CompletionTokens ?? 0}  total={s.TotalTokens ?? 0}");
                if (s.CostoUsd.HasValue)
                    sb.AppendLine($"Costo:    ${s.CostoUsd.Value:F6}");
            }

            if (s.Atributos.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("ATRIBUTOS");
                sb.AppendLine(new string('─', 60));
                foreach (var kv in s.Atributos)
                    sb.AppendLine($"  {kv.Key}: {kv.Value}");
            }

            if (!string.IsNullOrEmpty(s.InputPreview))
            {
                sb.AppendLine();
                sb.AppendLine($"INPUT (hash: {s.InputHash ?? "—"})");
                sb.AppendLine(new string('─', 60));
                sb.AppendLine(s.InputPreview);
            }

            if (!string.IsNullOrEmpty(s.OutputPreview))
            {
                sb.AppendLine();
                sb.AppendLine($"OUTPUT (hash: {s.OutputHash ?? "—"})");
                sb.AppendLine(new string('─', 60));
                sb.AppendLine(s.OutputPreview);
            }

            if (!string.IsNullOrEmpty(s.Error))
            {
                sb.AppendLine();
                sb.AppendLine("ERROR");
                sb.AppendLine(new string('─', 60));
                sb.AppendLine(s.Error);
            }

            txtDetalle.Text = sb.ToString();
        }

        private string BuildCabeceraTrace(TraceEjecucion t)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"TRACE {t.InstruccionId}");
            sb.AppendLine(new string('═', 60));
            sb.AppendLine($"Instrucción:  {t.Instruccion}");
            sb.AppendLine($"Modelo:       {t.Modelo}");
            sb.AppendLine($"Servicio:     {t.Servicio}");
            sb.AppendLine($"Workspace:    {t.RutaWorkspace}");
            sb.AppendLine();
            sb.AppendLine($"Inicio:       {t.Inicio.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"Duración:     {t.DuracionMs} ms");
            sb.AppendLine($"Estado:       {EstadoTexto(t.Estado)}");
            sb.AppendLine();
            sb.AppendLine($"Spans:        {t.TotalSpans}");
            sb.AppendLine($"LLM calls:    {t.TotalLlamadasLLM}");
            sb.AppendLine($"Herramientas: {t.TotalHerramientas}");
            sb.AppendLine($"Tokens:       {t.TotalTokens:N0}");
            sb.AppendLine($"Costo:        ${t.TotalCostoUsd:F6}");
            if (!string.IsNullOrEmpty(t.Error))
            {
                sb.AppendLine();
                sb.AppendLine($"ERROR: {t.Error}");
            }
            sb.AppendLine();
            sb.AppendLine("— Selecciona un span del árbol para ver su detalle —");
            return sb.ToString();
        }

        // ═════════════════════ Eventos de UI ═════════════════════

        private void cmbFecha_SelectedIndexChanged(object? sender, EventArgs e)
        {
            CargarTracesDelDia();
        }

        private void btnActualizar_Click(object? sender, EventArgs e)
        {
            CargarFechas();
            CargarTracesDelDia();
        }

        private void lstTraces_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstTraces.SelectedItems.Count == 0)
            {
                _traceSeleccionado = null;
                trvSpans.Nodes.Clear();
                MostrarDetalleSpan(null);
                return;
            }

            if (lstTraces.SelectedItems[0].Tag is not TraceIndiceItem item) return;

            // Si es el trace en curso, usamos el snapshot en memoria.
            var actual = TracerEjecucion.Instancia.TraceActual();
            if (actual != null && actual.InstruccionId == item.InstruccionId)
            {
                MostrarTrace(actual);
                return;
            }

            if (!DateTime.TryParse(cmbFecha.SelectedItem?.ToString(), out var fecha)) return;
            var trace = TraceStorage.Leer(fecha, item.InstruccionId);
            if (trace != null) MostrarTrace(trace);
        }

        private void trvSpans_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            MostrarDetalleSpan(e.Node?.Tag as TraceSpan);
        }

        // ═════════════════════ Modo en vivo ═════════════════════

        private void ManejarTraceIniciado(TraceEjecucion t)
        {
            if (!chkEnVivo.Checked) return;
            if (IsDisposed) return;
            BeginInvoke(() => CargarTracesDelDia());
        }

        private void ManejarSpanAbierto(TraceSpan s)
        {
            if (!chkEnVivo.Checked) return;
            if (IsDisposed) return;
            // El árbol solo se refresca al cerrar spans (reduce parpadeo).
        }

        private void ManejarSpanCerrado(TraceSpan s)
        {
            if (!chkEnVivo.Checked) return;
            if (IsDisposed) return;
            var actual = TracerEjecucion.Instancia.TraceActual();
            if (actual == null) return;
            // Solo refrescar si estamos viendo el trace actual.
            if (_traceSeleccionado == null || _traceSeleccionado.InstruccionId != actual.InstruccionId)
                return;
            BeginInvoke(() => MostrarTrace(actual));
        }

        private void ManejarTraceFinalizado(TraceEjecucion t)
        {
            if (!chkEnVivo.Checked) return;
            if (IsDisposed) return;
            BeginInvoke(() => CargarTracesDelDia());
        }

        // ═════════════════════ Helpers de formato ═════════════════════

        private static string EstadoTexto(SpanEstado e) => e switch
        {
            SpanEstado.Ok        => "✓ OK",
            SpanEstado.Error     => "✗ ERROR",
            SpanEstado.Cancelado => "⊘ CANCEL",
            SpanEstado.EnCurso   => "… curso",
            _                    => e.ToString(),
        };

        private static Color ColorPorEstado(SpanEstado e) => e switch
        {
            SpanEstado.Ok        => ColorOk,
            SpanEstado.Error     => ColorError,
            SpanEstado.Cancelado => ColorWarn,
            _                    => ColorTexto,
        };

        private static string PrefijoTipo(SpanTipo t) => t switch
        {
            SpanTipo.Pipeline     => "⟦ Pipeline",
            SpanTipo.Fase         => "▸ Fase",
            SpanTipo.LlamadaLLM   => "⚡ LLM",
            SpanTipo.Herramienta  => "🔧 Tool",
            SpanTipo.Script       => "▶ Script",
            SpanTipo.Memoria      => "🧠 Mem",
            SpanTipo.Verificacion => "✓ Verif",
            _                     => "• ",
        };

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
