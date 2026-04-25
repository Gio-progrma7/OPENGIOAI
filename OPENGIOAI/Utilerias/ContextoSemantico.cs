// ============================================================
//  ContextoSemantico.cs  — Fase D (RAG universal de contexto)
//
//  QUÉ HACE:
//    Dada la instrucción del usuario, recupera de los índices
//    paralelos (credenciales / skills / automatizaciones) los
//    fragmentos más relevantes y los devuelve como secciones
//    listas para inyectar al PromptEfectivo, en lugar de volcar
//    TODO el manifest (lo cual crece sin control).
//
//  FLUJO:
//    1. Asegurar índice (best-effort, incremental).
//    2. Embeber la instrucción del usuario.
//    3. Buscar top-K por fuente con score mínimo.
//    4. Formatear bloques markdown por sección + tabla de
//       contenidos ligera como fallback por si el top-K falla.
//
//  DOBLE GATE:
//    · HAB_CONTEXTO_SEMANTICO → master switch
//    · Si se apaga → AgentContext cae al dump clásico completo.
//
//  TOLERANTE A FALLOS:
//    Cualquier error (Ollama caído, dimensión inconsistente,
//    índice vacío) devuelve null y el caller usa el dump clásico.
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
    /// <summary>
    /// Resultado del retrieval de contexto. Todos los campos son secciones
    /// markdown listas para concatenar; si una viene vacía significa que
    /// AgentContext debe omitirla (o caer al dump clásico según el caso).
    /// </summary>
    public sealed class ContextoRecuperado
    {
        /// <summary>Sección de credenciales relevantes (o vacío).</summary>
        public string BloqueCredenciales { get; set; } = "";

        /// <summary>Sección de skills relevantes (o vacío).</summary>
        public string BloqueSkills { get; set; } = "";

        /// <summary>Sección de automatizaciones relevantes (o vacío).</summary>
        public string BloqueAutomatizaciones { get; set; } = "";

        /// <summary>
        /// Tabla de contenidos ligera (solo IDs/nombres sin descripción).
        /// Sirve como fallback para que el LLM sepa QUÉ existe aunque el
        /// top-K no lo haya traído completo. Presupuesto: &lt; 150 tokens.
        /// </summary>
        public string TablaContenidos { get; set; } = "";

        /// <summary>True si el retrieval produjo al menos un hit útil.</summary>
        public bool HayContexto =>
            !string.IsNullOrWhiteSpace(BloqueCredenciales) ||
            !string.IsNullOrWhiteSpace(BloqueSkills) ||
            !string.IsNullOrWhiteSpace(BloqueAutomatizaciones);
    }

    public static class ContextoSemantico
    {
        // Umbral mínimo de similitud para considerar un hit "útil".
        // Coseno normalizado [0,1]: 0.5 = ortogonal, 0.6+ = relacionado.
        private const double ScoreMinimo = 0.55;

        // Presupuesto de chunks por fuente. Se tunea aquí un solo punto.
        private const int TopKCredenciales     = 5;
        private const int TopKSkills           = 6;
        private const int TopKAutomatizaciones = 3;

        /// <summary>
        /// Retrieval principal. Devuelve null si el subsistema no puede
        /// operar (habilidad OFF, índice vacío, error del proveedor) —
        /// el caller debe caer al dump clásico.
        /// </summary>
        public static async Task<ContextoRecuperado?> ObtenerContextoRelevanteAsync(
            string rutaWorkspace,
            string instruccion,
            CancellationToken ct = default)
        {
            using var span = TracerEjecucion.Instancia.AbrirSpan(
                SpanTipo.Memoria, "RAG contexto");
            span.RegistrarInput(instruccion);

            try
            {
                if (string.IsNullOrWhiteSpace(rutaWorkspace)) return null;
                if (string.IsNullOrWhiteSpace(instruccion))   return null;

                if (!HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_CONTEXTO_SEMANTICO))
                    return null;

                // Asegurar índice actualizado (incremental, silencioso).
                try
                {
                    await ContextoIndexer.IndexarAsync(
                        rutaWorkspace,
                        forzarReindexacion: false,
                        progreso: null,
                        ct: ct);
                }
                catch { /* seguimos con el índice que haya */ }

                var store = VectorStore.Crear(
                    RutasProyecto.ObtenerRutaEmbeddingsContexto(rutaWorkspace));
                store.Cargar();

                if (store.Count == 0) return null;

                float[] vectorConsulta;
                try
                {
                    vectorConsulta = await EmbeddingsService.EmbedAsync(instruccion, ct);
                }
                catch
                {
                    return null;
                }

                if (vectorConsulta == null || vectorConsulta.Length == 0)
                    return null;

                var res = new ContextoRecuperado
                {
                    BloqueCredenciales = BuscarYFormatear(
                        store, vectorConsulta,
                        ContextoIndexer.F_CREDENCIAL,
                        TopKCredenciales,
                        tituloSeccion: "CREDENCIALES RELEVANTES",
                        reglas:
                            "· Solo usa una credencial si es estrictamente necesaria.\n" +
                            "· Busca por nombre; extrae siempre el campo \"key\".\n" +
                            "· Si no hay coincidencia → informa. No inventes.\n" +
                            "· Ruta completa del JSON: " + RutasProyecto.ObtenerRutaListApis()),

                    BloqueSkills = BuscarYFormatear(
                        store, vectorConsulta,
                        ContextoIndexer.F_SKILL,
                        TopKSkills,
                        tituloSeccion: "SKILLS RELEVANTES",
                        reglas: "Importar en Python: from skill_runner import skill_run, skill_run_json"),

                    BloqueAutomatizaciones = BuscarYFormatear(
                        store, vectorConsulta,
                        ContextoIndexer.F_AUTOMATIZACION,
                        TopKAutomatizaciones,
                        tituloSeccion: "AUTOMATIZACIONES RELEVANTES",
                        reglas: ""),

                    TablaContenidos = ConstruirTablaContenidos(store),
                };

                span.AgregarAtributo("tiene_contexto", res.HayContexto.ToString());
                return res;
            }
            catch
            {
                return null;
            }
        }

        // ──────────────────────────────────────────────────────────────────

        private static string BuscarYFormatear(
            VectorStore store,
            float[] vectorConsulta,
            string fuente,
            int topK,
            string tituloSeccion,
            string reglas)
        {
            var hits = store.Buscar(vectorConsulta, topK, fuenteFiltro: fuente,
                                     scoreMinimo: ScoreMinimo);
            if (hits.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"================= {tituloSeccion} =================");
            foreach (var h in hits.OrderByDescending(x => x.Score))
            {
                int pct = (int)Math.Round(h.Score * 100);
                sb.AppendLine($"[{pct}%] {h.Chunk.Texto}");
            }
            if (!string.IsNullOrWhiteSpace(reglas))
            {
                sb.AppendLine();
                sb.AppendLine(reglas);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Tabla de contenidos ligera: todos los IDs/nombres disponibles
        /// sin descripción. Presupuesto pequeño (&lt; 150 tokens típicos).
        /// Sirve para que el LLM sepa qué existe aunque el top-K no lo
        /// haya traído — útil para preguntas tipo "¿qué skills tengo?".
        /// </summary>
        private static string ConstruirTablaContenidos(VectorStore store)
        {
            try
            {
                var porFuente = store.ContarPorFuente();
                if (porFuente.Count == 0) return "";

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("================= ÍNDICE DE CONTEXTO =================");
                sb.AppendLine("Recursos disponibles (arriba viste solo los relevantes a esta");
                sb.AppendLine("instrucción; este índice lista todo lo que existe):");

                foreach (var f in porFuente.Keys.OrderBy(k => k))
                {
                    int n = porFuente[f];
                    sb.AppendLine($"  · {f}: {n} disponibles");
                }
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }
    }
}
