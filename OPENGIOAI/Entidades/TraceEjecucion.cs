// ============================================================
//  TraceEjecucion.cs  — Fase 1A (Tracing estructurado)
//
//  QUÉ ES:
//    Registro completo y estructurado de una ejecución del agente.
//    Hermano mayor de ConsumoTokens: mientras éste captura solo
//    tokens por LLM call, el Trace captura TODAS las unidades de
//    trabajo (fases, LLM calls, tool calls, scripts, memoria, etc.)
//    formando un árbol con relaciones padre-hijo.
//
//  CORRELACIÓN:
//    InstruccionId es el mismo que usa ConsumoTokensTracker.
//    Así, en la UI se pueden cruzar tokens y spans sin acoplar los
//    dos sistemas (cada uno es independiente y best-effort).
//
//  PERSISTENCIA:
//    JSONL append-only por día en {AppDir}/Traces/YYYY-MM-DD.jsonl
//    (ver TraceStorage). Un trace completo por línea.
//
//  DISEÑO:
//    · Spans forman un árbol vía ParentId (null = raíz).
//    · Cada span tiene Estado (Ok/Error/Cancelado) auto-deducido.
//    · Input/Output se guardan como preview recortado + hash SHA1,
//      suficientes para diff/replay sin explotar el tamaño del JSON.
// ============================================================

using System;
using System.Collections.Generic;

namespace OPENGIOAI.Entidades
{
    /// <summary>Naturaleza de un span dentro del árbol de ejecución.</summary>
    public enum SpanTipo
    {
        Pipeline,       // Raíz lógica: cubre toda la instrucción del usuario.
        Fase,           // Una fase del pipeline (Analista, Constructor, ...).
        LlamadaLLM,     // Un hit al proveedor de LLM.
        Herramienta,    // Una invocación de IHerramienta.
        Script,         // Ejecución de python u otro subproceso.
        Memoria,        // Lectura/escritura/RAG sobre la memoria durable.
        Verificacion,   // Check determinista (AnalizarSalidaRapido, etc.).
        Custom,         // Punto de extensión genérico.
    }

    /// <summary>Estado final de un span. Se fija cuando se cierra.</summary>
    public enum SpanEstado
    {
        EnCurso,
        Ok,
        Error,
        Cancelado,
    }

    /// <summary>
    /// Unidad de trabajo dentro de un Trace. Forma un árbol vía ParentId.
    /// Los spans se cierran (y serializan) en orden de apertura gracias
    /// al IDisposable SpanHandle del tracer.
    /// </summary>
    public sealed class TraceSpan
    {
        public string Id { get; set; } = "";
        public string? ParentId { get; set; }
        public SpanTipo Tipo { get; set; } = SpanTipo.Custom;
        public string Nombre { get; set; } = "";

        public DateTime Inicio { get; set; } = DateTime.UtcNow;
        public DateTime? Fin { get; set; }
        public long DuracionMs { get; set; }

        public SpanEstado Estado { get; set; } = SpanEstado.EnCurso;
        public string? Error { get; set; }

        // Resumen de entrada/salida — suficiente para UI y diffs.
        public string InputPreview { get; set; } = "";
        public string OutputPreview { get; set; } = "";
        public string? InputHash { get; set; }
        public string? OutputHash { get; set; }

        // Métricas económicas (nullable: solo aplican a spans LLM).
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public decimal? CostoUsd { get; set; }

        // Metadata libre (modelo, temperatura, iteración ReAct, etc.).
        public Dictionary<string, string> Atributos { get; set; } = new();
    }

    /// <summary>
    /// Raíz: representa una ejecución completa (una instrucción del usuario).
    /// Contiene todos los spans recolectados, ordenados por Inicio.
    /// </summary>
    public sealed class TraceEjecucion
    {
        public string InstruccionId { get; set; } = "";
        public string Instruccion { get; set; } = "";
        public string Modelo { get; set; } = "";
        public string Servicio { get; set; } = "";
        public string RutaWorkspace { get; set; } = "";

        public DateTime Inicio { get; set; } = DateTime.UtcNow;
        public DateTime? Fin { get; set; }
        public long DuracionMs { get; set; }

        public SpanEstado Estado { get; set; } = SpanEstado.EnCurso;
        public string? Error { get; set; }

        public List<TraceSpan> Spans { get; set; } = new();

        /// <summary>Totales derivados, útiles para la lista en UI.</summary>
        public int TotalSpans => Spans.Count;
        public int TotalLlamadasLLM
        {
            get
            {
                int c = 0;
                foreach (var s in Spans) if (s.Tipo == SpanTipo.LlamadaLLM) c++;
                return c;
            }
        }
        public int TotalHerramientas
        {
            get
            {
                int c = 0;
                foreach (var s in Spans) if (s.Tipo == SpanTipo.Herramienta) c++;
                return c;
            }
        }
        public int TotalTokens
        {
            get
            {
                int t = 0;
                foreach (var s in Spans) if (s.TotalTokens.HasValue) t += s.TotalTokens.Value;
                return t;
            }
        }
        public decimal TotalCostoUsd
        {
            get
            {
                decimal t = 0m;
                foreach (var s in Spans) if (s.CostoUsd.HasValue) t += s.CostoUsd.Value;
                return t;
            }
        }
    }
}
