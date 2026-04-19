// ============================================================
//  MemoriaSemantica.cs  — Fase C (RAG local sobre memoria)
//
//  QUÉ HACE:
//    API de alto nivel que, dada la instrucción del usuario,
//    recupera los top-K chunks más relevantes de Hechos.md /
//    Episodios.md y los devuelve formateados como sección lista
//    para inyectar en el prompt (en lugar de volcar la memoria
//    completa).
//
//  FLUJO:
//    1. Asegurar índice (best-effort, incremental y silencioso).
//    2. Embebe la instrucción del usuario.
//    3. Busca top-K en el VectorStore.
//    4. Devuelve un bloque markdown con la sección "MEMORIA RELEVANTE".
//
//  DOBLE GATE:
//    · HAB_MEMORIA            → memoria activa en absoluto
//    · HAB_MEMORIA_SEMANTICA  → modo RAG en lugar de dump completo
//    Si cualquiera está OFF, se devuelve "" y AgentContext cae de
//    vuelta al camino clásico (MemoriaManager.FormatearParaPrompt).
//
//  TOLERANTE A FALLOS:
//    Cualquier error (Ollama apagado, API key inválida, disco lleno)
//    se traga silenciosamente y devuelve "". El pipeline sigue
//    funcionando sin memoria — preferible a romperlo.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class MemoriaSemantica
    {
        /// <summary>
        /// Devuelve una sección markdown con los top-K fragmentos más
        /// relevantes a la instrucción. Si no hay nada que devolver
        /// (habilidad OFF, instrucción vacía, índice vacío, error),
        /// devuelve "".
        /// </summary>
        public static async Task<string> ObtenerContextoRelevanteAsync(
            string rutaWorkspace,
            string instruccion,
            CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rutaWorkspace)) return "";
                if (string.IsNullOrWhiteSpace(instruccion))   return "";

                // Gate 1: memoria general activa
                if (!HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_MEMORIA))
                    return "";

                // Gate 2: modo semántico activo
                if (!HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_MEMORIA_SEMANTICA))
                    return "";

                // Asegurar índice: si Hechos/Episodios cambiaron desde la última
                // vez, se re-indexa aquí. Es incremental: si no cambió nada
                // salta rápido sin llamar al proveedor.
                // Best-effort — si falla, seguimos con lo que haya en el store.
                try
                {
                    await MemoriaIndexer.IndexarAsync(
                        rutaWorkspace,
                        forzarReindexacion: false,
                        progreso: null,
                        ct: ct);
                }
                catch
                {
                    // Seguimos con el índice existente (puede estar vacío).
                }

                var cfg = EmbeddingsService.CargarConfig();
                var store = new VectorStore(rutaWorkspace);
                store.Cargar();

                if (store.Count == 0) return "";

                // Embeber la instrucción del usuario
                float[] vectorConsulta;
                try
                {
                    vectorConsulta = await EmbeddingsService.EmbedAsync(instruccion, ct);
                }
                catch
                {
                    return "";
                }

                if (vectorConsulta == null || vectorConsulta.Length == 0)
                    return "";

                int topK = cfg.TopK > 0 ? cfg.TopK : 5;
                var resultados = store.Buscar(
                    vectorConsulta,
                    topK: topK,
                    fuenteFiltro: null,
                    scoreMinimo: 0.0);

                if (resultados.Count == 0) return "";

                return FormatearBloqueMarkdown(resultados);
            }
            catch
            {
                // Cualquier error → el pipeline sigue sin memoria semántica.
                return "";
            }
        }

        /// <summary>
        /// Formatea los resultados como sección lista para concatenar al
        /// PromptEfectivo. Se agrupan por fuente (Hechos primero,
        /// Episodios después) y se etiqueta cada bloque con el score.
        /// </summary>
        private static string FormatearBloqueMarkdown(IReadOnlyList<ResultadoBusquedaMemoria> resultados)
        {
            if (resultados == null || resultados.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================= MEMORIA RELEVANTE (RAG) =================");
            sb.AppendLine("Estos fragmentos fueron seleccionados por similitud semántica");
            sb.AppendLine("con la instrucción actual. Úsalos como contexto — no son");
            sb.AppendLine("necesariamente exhaustivos.");
            sb.AppendLine();

            // Ordenar: primero por fuente (Hechos antes), luego por score desc
            var ordenados = resultados
                .OrderBy(r => r.Chunk.Fuente == "Hechos" ? 0 : 1)
                .ThenByDescending(r => r.Score)
                .ToList();

            string? fuenteActual = null;
            foreach (var r in ordenados)
            {
                if (r.Chunk.Fuente != fuenteActual)
                {
                    if (fuenteActual != null) sb.AppendLine();
                    sb.AppendLine($"--- {r.Chunk.Fuente} ---");
                    fuenteActual = r.Chunk.Fuente;
                }

                int pct = (int)Math.Round(r.Score * 100);
                sb.AppendLine($"[{pct}%] {r.Chunk.Texto.Trim()}");
            }

            sb.AppendLine();
            sb.AppendLine("===========================================================");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
