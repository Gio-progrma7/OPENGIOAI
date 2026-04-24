// ============================================================
//  TracerEjecucion.cs  — Fase 1A
//
//  Singleton que captura el árbol de ejecución del agente.
//  Hermano de ConsumoTokensTracker: mismo ciclo de vida,
//  mismo InstruccionId para correlación gratis.
//
//  API DE USO (pattern-based, cero boilerplate en call-site):
//
//      using (var span = TracerEjecucion.Instancia.AbrirSpan(
//          SpanTipo.Fase, "Analista"))
//      {
//          span.RegistrarInput(instruccion);
//          var r = await FaseAnalistaAsync(instruccion, ct);
//          span.RegistrarOutput(r);
//      }
//
//  El SpanHandle : IDisposable:
//    · Detecta excepciones vía el pattern using → marca Estado=Error.
//    · Auto-cierra con duración precisa.
//    · Mantiene un Stack por AsyncLocal para resolver ParentId solo.
//
//  ACTIVACIÓN:
//    La habilidad HAB_TRACING controla la PERSISTENCIA y el EVENTO.
//    Si está OFF:
//      · AbrirSpan devuelve un handle no-op (cero allocs significativos).
//      · No se escribe a disco ni se emiten eventos.
//    Los .AbrirSpan() siguen siendo seguros de llamar desde cualquier
//    parte del código — la decisión se hace una vez por trace.
//
//  BEST-EFFORT:
//    Cualquier error del tracer (disco, serialización, UI handler) se
//    come en silencio. Romper un pipeline por un trace es un bug peor
//    que no tener el trace.
// ============================================================

using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace OPENGIOAI.Utilerias
{
    /// <summary>
    /// Handle de un span abierto. Se cierra al disponer (using {}).
    /// Captura excepciones automáticamente vía el pattern using.
    /// </summary>
    public sealed class SpanHandle : IDisposable
    {
        private readonly TracerEjecucion _tracer;
        private readonly bool _noop;
        private bool _cerrado;

        internal TraceSpan Span { get; }

        internal SpanHandle(TracerEjecucion tracer, TraceSpan span, bool noop)
        {
            _tracer = tracer;
            Span    = span;
            _noop   = noop;
        }

        // ── API pública del call-site ────────────────────────────────────

        public void RegistrarInput(string? texto)
        {
            if (_noop || texto == null) return;
            Span.InputPreview = Recortar(texto, 500);
            Span.InputHash    = Sha1Corto(texto);
        }

        public void RegistrarOutput(string? texto)
        {
            if (_noop || texto == null) return;
            Span.OutputPreview = Recortar(texto, 500);
            Span.OutputHash    = Sha1Corto(texto);
        }

        public void AgregarAtributo(string clave, string? valor)
        {
            if (_noop || string.IsNullOrEmpty(clave)) return;
            Span.Atributos[clave] = valor ?? "";
        }

        /// <summary>
        /// Adjunta tokens/costo a un span. Típicamente invocado por
        /// AIModelConector al recibir la respuesta de un LLM.
        /// </summary>
        public void RegistrarTokens(int? prompt, int? completion, int? total, decimal? costoUsd)
        {
            if (_noop) return;
            Span.PromptTokens     = prompt;
            Span.CompletionTokens = completion;
            Span.TotalTokens      = total;
            Span.CostoUsd         = costoUsd;
        }

        public void MarcarError(string mensaje)
        {
            if (_noop) return;
            Span.Estado = SpanEstado.Error;
            Span.Error  = mensaje;
        }

        public void MarcarCancelado()
        {
            if (_noop) return;
            Span.Estado = SpanEstado.Cancelado;
        }

        public void Dispose()
        {
            if (_cerrado) return;
            _cerrado = true;
            if (_noop) return;

            try { _tracer.CerrarSpanInterno(Span); } catch { }
        }

        // ── Utilidades ───────────────────────────────────────────────────

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        private static string Sha1Corto(string s)
        {
            try
            {
                byte[] buf  = Encoding.UTF8.GetBytes(s);
                byte[] hash = SHA1.HashData(buf);
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8 && i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
            catch { return ""; }
        }
    }

    public sealed class TracerEjecucion
    {
        private static readonly TracerEjecucion _instancia = new();
        public static TracerEjecucion Instancia => _instancia;

        // Stack por async-flow — permite inferir ParentId sin pasar parámetros.
        private static readonly AsyncLocal<Stack<TraceSpan>?> _pila = new();

        private readonly object _lock = new();
        private TraceEjecucion? _traceActual;
        private bool _activoEnEjecucion;

        // ══════════════════ Eventos (para UI en vivo) ══════════════════

        public event Action<TraceEjecucion>? OnTraceIniciado;
        public event Action<TraceSpan>?      OnSpanAbierto;
        public event Action<TraceSpan>?      OnSpanCerrado;
        public event Action<TraceEjecucion>? OnTraceFinalizado;

        private TracerEjecucion()
        {
            // Puente automático: cuando ConsumoTokensTracker registre un consumo,
            // adjuntar los tokens/costo al span LLM activo (si hay). Así no hay
            // que duplicar la extracción del JSON del proveedor en dos sitios.
            try
            {
                ConsumoTokensTracker.Instancia.OnConsumoRegistrado += c =>
                {
                    try
                    {
                        AnotarTokensEnSpanActual(
                            c.PromptTokens,
                            c.CompletionTokens,
                            c.TotalTokens,
                            c.CostoEstimadoUsd == 0m ? (decimal?)null : c.CostoEstimadoUsd);
                    }
                    catch { /* best-effort */ }
                };
            }
            catch { /* best-effort */ }
        }

        // ══════════════════ Ciclo de vida ══════════════════

        /// <summary>
        /// Abre un nuevo trace asociado a una instrucción. El InstruccionId
        /// es provisto por el caller (típicamente el mismo que usó
        /// ConsumoTokensTracker.IniciarEjecucion) para correlación.
        /// </summary>
        public void IniciarTrace(
            string instruccionId,
            string instruccion,
            string modelo,
            string servicio,
            string rutaWorkspace)
        {
            // La habilidad controla si PERSISTIMOS. El trace en memoria se
            // mantiene siempre (barato) para que la UI en vivo funcione
            // aunque el usuario tenga el tracing apagado.
            _activoEnEjecucion =
                HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_TRACING);

            var trace = new TraceEjecucion
            {
                InstruccionId = string.IsNullOrWhiteSpace(instruccionId)
                    ? Guid.NewGuid().ToString("N").Substring(0, 12)
                    : instruccionId,
                Instruccion   = Recortar(instruccion, 400),
                Modelo        = modelo ?? "",
                Servicio      = servicio ?? "",
                RutaWorkspace = rutaWorkspace ?? "",
                Inicio        = DateTime.UtcNow,
                Estado        = SpanEstado.EnCurso,
            };

            lock (_lock)
            {
                // Si quedó un trace abierto sin cerrar (ej. crash previo),
                // lo cerramos marcándolo como error y lo archivamos.
                if (_traceActual != null)
                    FinalizarInterno(SpanEstado.Error, "Trace previo abandonado");

                _traceActual = trace;
                _pila.Value  = new Stack<TraceSpan>();
            }

            try { OnTraceIniciado?.Invoke(trace); } catch { }
        }

        /// <summary>
        /// Abre un span. El ParentId se infiere del último span abierto
        /// en el mismo flujo async. El handle implementa IDisposable para
        /// auto-cierre con `using`.
        /// </summary>
        public SpanHandle AbrirSpan(SpanTipo tipo, string nombre)
        {
            TraceEjecucion? trace;
            Stack<TraceSpan>? pila;

            lock (_lock)
            {
                trace = _traceActual;
                pila  = _pila.Value;
            }

            // Sin trace activo → no-op. Esto permite llamar AbrirSpan
            // desde cualquier capa sin pensarlo; si no hay trace, no pasa nada.
            if (trace == null || pila == null)
                return new SpanHandle(this, new TraceSpan(), noop: true);

            var span = new TraceSpan
            {
                Id       = Guid.NewGuid().ToString("N").Substring(0, 10),
                ParentId = pila.Count > 0 ? pila.Peek().Id : null,
                Tipo     = tipo,
                Nombre   = nombre ?? "",
                Inicio   = DateTime.UtcNow,
                Estado   = SpanEstado.EnCurso,
            };

            lock (_lock)
            {
                if (_traceActual == null) // doble check
                    return new SpanHandle(this, span, noop: true);
                _traceActual.Spans.Add(span);
            }

            pila.Push(span);

            try { OnSpanAbierto?.Invoke(span); } catch { }

            return new SpanHandle(this, span, noop: false);
        }

        /// <summary>
        /// Adjunta tokens al último span LLM abierto en el flujo actual.
        /// Pensado para llamarse desde AIModelConector cuando extrae el
        /// consumo de la respuesta del proveedor.
        /// </summary>
        public void AnotarTokensEnSpanActual(
            int? promptTokens, int? completionTokens, int? totalTokens, decimal? costoUsd)
        {
            var pila = _pila.Value;
            if (pila == null || pila.Count == 0) return;

            // Preferimos un span LlamadaLLM si está en la cima; si no, el tope.
            TraceSpan span = pila.Peek();
            foreach (var s in pila)
            {
                if (s.Tipo == SpanTipo.LlamadaLLM) { span = s; break; }
            }

            span.PromptTokens     = promptTokens;
            span.CompletionTokens = completionTokens;
            span.TotalTokens      = totalTokens;
            span.CostoUsd         = costoUsd;
        }

        /// <summary>Cierra el trace actual y lo persiste (si la habilidad lo permite).</summary>
        public void FinalizarTrace(SpanEstado estado = SpanEstado.Ok, string? error = null)
        {
            FinalizarInterno(estado, error);
        }

        public TraceEjecucion? TraceActual()
        {
            lock (_lock) { return _traceActual; }
        }

        // ══════════════════ Internos ══════════════════

        internal void CerrarSpanInterno(TraceSpan span)
        {
            if (span.Fin.HasValue) return; // idempotente

            span.Fin        = DateTime.UtcNow;
            span.DuracionMs = (long)(span.Fin.Value - span.Inicio).TotalMilliseconds;

            // Si nadie marcó estado, se asume Ok — las excepciones las pinta el caller
            // antes de Dispose vía MarcarError.
            if (span.Estado == SpanEstado.EnCurso)
                span.Estado = SpanEstado.Ok;

            var pila = _pila.Value;
            if (pila != null && pila.Count > 0 && ReferenceEquals(pila.Peek(), span))
                pila.Pop();
            else if (pila != null)
            {
                // El span no está en la cima — limpiar la pila hasta quitarlo.
                // Esto puede pasar si alguien dispara awaits sin alinear using.
                var tmp = new Stack<TraceSpan>();
                while (pila.Count > 0 && !ReferenceEquals(pila.Peek(), span))
                    tmp.Push(pila.Pop());
                if (pila.Count > 0) pila.Pop();
                while (tmp.Count > 0) pila.Push(tmp.Pop());
            }

            try { OnSpanCerrado?.Invoke(span); } catch { }
        }

        private void FinalizarInterno(SpanEstado estado, string? error)
        {
            TraceEjecucion? cerrado = null;
            bool persistir = false;

            lock (_lock)
            {
                if (_traceActual == null) return;

                _traceActual.Fin        = DateTime.UtcNow;
                _traceActual.DuracionMs = (long)(_traceActual.Fin.Value - _traceActual.Inicio).TotalMilliseconds;
                _traceActual.Estado     = estado;
                _traceActual.Error      = error;

                // Cerrar spans huérfanos que quedaron abiertos (defensivo).
                foreach (var s in _traceActual.Spans)
                {
                    if (!s.Fin.HasValue)
                    {
                        s.Fin        = _traceActual.Fin;
                        s.DuracionMs = (long)(s.Fin.Value - s.Inicio).TotalMilliseconds;
                        if (s.Estado == SpanEstado.EnCurso)
                            s.Estado = SpanEstado.Error;
                    }
                }

                cerrado    = _traceActual;
                persistir  = _activoEnEjecucion;
                _traceActual = null;
                _pila.Value  = null;
            }

            if (cerrado == null) return;

            if (persistir)
            {
                try { TraceStorage.Guardar(cerrado); } catch { }
            }

            try { OnTraceFinalizado?.Invoke(cerrado); } catch { }
        }

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
