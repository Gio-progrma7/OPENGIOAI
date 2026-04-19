// ============================================================
//  AgenteMemorista.cs  — Fase 2 del sistema de Memoria
//
//  QUÉ HACE:
//    5ª fase del pipeline ARIA. Corre SIEMPRE en modo fire-and-forget,
//    después de que el usuario ya recibió su respuesta. Por eso no
//    suma latencia: si falla, no pasa nada; si tarda, no afecta la UX.
//
//    Recibe: instrucción + respuesta + memoria actual.
//    Pregunta al LLM: "¿hay algo nuevo que valga la pena recordar?"
//    Recibe un JSON con hechos_nuevos[] y episodio (o null).
//    Persiste via MemoriaManager.
//
//  DISEÑO:
//    - Prompt editable desde PromptCatalogo.K_MEMORISTA (heredable por el
//      usuario en FrmPromts, como los otros agentes).
//    - Cero side-effects fuera de la carpeta {ruta}/Memoria/.
//    - Deduplicación: no añade un hecho si el texto ya existe en Hechos.md
//      (comparación case-insensitive, tras trim).
//    - Tolerante a fallos: cualquier excepción (LLM caído, JSON inválido,
//      I/O denegado) se silencia. Fase 2 es best-effort.
// ============================================================

using Newtonsoft.Json.Linq;
using OPENGIOAI.Data;
using OPENGIOAI.Promts;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Agentes
{
    public static class AgenteMemorista
    {
        /// <summary>
        /// Se llama como fire-and-forget al final de cada pipeline.
        /// NUNCA lanza excepciones — cualquier fallo queda registrado
        /// en la memoria misma (archivo de debug opcional) y el caller
        /// sigue su flujo normal.
        /// </summary>
        public static async Task EjecutarAsync(
            string instruccion,
            string respuestaFinal,
            AgentContext ctx,
            CancellationToken ct = default)
        {
            try
            {
                // GATE por habilidad: si el usuario apagó la Memoria, el Memorista
                // ni siquiera arranca — ahorra la llamada al LLM y cero I/O.
                if (!HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_MEMORIA))
                    return;

                if (string.IsNullOrWhiteSpace(ctx?.RutaArchivo)) return;
                if (string.IsNullOrWhiteSpace(instruccion)) return;
                if (string.IsNullOrWhiteSpace(respuestaFinal)) return;

                // ── 1. Cargar hechos actuales para que el LLM evite duplicar ─
                string hechosActuales = await MemoriaManager.LeerHechosAsync(ctx.RutaArchivo);
                if (string.IsNullOrWhiteSpace(hechosActuales))
                    hechosActuales = "(sin hechos previos)";

                // ── 2. Resolver el prompt del Memorista (con override del usuario) ─
                string promptMemorista = PromptRegistry.Instancia.Obtener(
                    PromptCatalogo.K_MEMORISTA,
                    new Dictionary<string, string>
                    {
                        ["instruccion"]      = Recortar(instruccion,     2000),
                        ["respuesta"]        = Recortar(respuestaFinal,  2000),
                        ["hechos_actuales"]  = Recortar(hechosActuales,  2500),
                    });

                // ── 3. Llamar al LLM con un contexto sin prompt maestro ──────
                // Usamos un contexto mínimo: el Memorista NO necesita ver
                // credenciales, rutas, skills, ni la memoria ya inyectada;
                // solo debe decidir sobre la conversación que acaba de pasar.
                var ctxMemorista = ctx.ComoFase("Memorista").ConPromptPersonalizado("");
                string raw = await AIModelConector.ObtenerRespuestaLLMAsync(
                    promptMemorista, ctxMemorista, ct);

                if (string.IsNullOrWhiteSpace(raw)) return;

                // ── 4. Parsear JSON tolerando envoltorio ─────────────────────
                var (hechosNuevos, episodio) = ParsearRespuesta(raw);

                // ── 5. Persistir — solo si hay algo que persistir ────────────
                if (hechosNuevos.Count > 0)
                    await AgregarHechosSinDuplicarAsync(ctx.RutaArchivo, hechosNuevos);

                if (!string.IsNullOrWhiteSpace(episodio))
                    await MemoriaManager.AgregarEpisodioAsync(ctx.RutaArchivo, episodio!);
            }
            catch
            {
                // Best-effort. Si el LLM devolvió basura o I/O falla,
                // el usuario ni se entera — ya recibió su respuesta real.
            }
        }

        // ══════════════════ Parseo ══════════════════

        private static (List<string> hechos, string? episodio) ParsearRespuesta(string raw)
        {
            var hechos = new List<string>();
            string? episodio = null;

            try
            {
                string json = ExtraerJson(raw);
                if (string.IsNullOrEmpty(json)) return (hechos, null);

                var obj = JObject.Parse(json);

                var arr = obj["hechos_nuevos"] as JArray;
                if (arr != null)
                {
                    foreach (var tok in arr)
                    {
                        string valor = (tok?.ToString() ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(valor)) continue;
                        // Normalizar: asegurar que empieza con "- "
                        if (!valor.StartsWith("-")) valor = "- " + valor;
                        if (valor.Length > 200) valor = valor[..200];
                        hechos.Add(valor);
                    }
                }

                var epTok = obj["episodio"];
                if (epTok != null && epTok.Type != JTokenType.Null)
                {
                    string ep = epTok.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(ep) && ep.Length <= 300)
                        episodio = ep;
                }
            }
            catch
            {
                // JSON roto → ignorar silenciosamente
            }

            return (hechos, episodio);
        }

        /// <summary>
        /// Extrae el primer objeto JSON del texto (tolera bloques de código,
        /// texto antes/después, etc.). Si no encuentra algo plausible, devuelve "".
        /// </summary>
        private static string ExtraerJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            // Quitar bloques ```json ... ```
            string s = raw;
            int fenceStart = s.IndexOf("```");
            if (fenceStart >= 0)
            {
                int fenceEnd = s.IndexOf("```", fenceStart + 3);
                if (fenceEnd > fenceStart)
                {
                    s = s.Substring(fenceStart + 3, fenceEnd - fenceStart - 3);
                    // Si empieza con "json\n", quitarlo
                    if (s.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                        s = s.Substring(4);
                }
            }

            int ini = s.IndexOf('{');
            int fin = s.LastIndexOf('}');
            if (ini < 0 || fin <= ini) return "";
            return s.Substring(ini, fin - ini + 1);
        }

        // ══════════════════ Persistencia con dedup ══════════════════

        private static async Task AgregarHechosSinDuplicarAsync(string rutaBase, List<string> nuevos)
        {
            string hechos = await MemoriaManager.LeerHechosAsync(rutaBase);

            // Set de líneas ya presentes (normalizado para comparar)
            var yaPresentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var linea in (hechos ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                string norm = NormalizarParaComparar(linea);
                if (!string.IsNullOrWhiteSpace(norm))
                    yaPresentes.Add(norm);
            }

            var sb = new StringBuilder();
            if (string.IsNullOrWhiteSpace(hechos))
            {
                sb.AppendLine("# Hechos");
                sb.AppendLine("Anota aquí verdades durables sobre ti y tu entorno.");
                sb.AppendLine();
            }
            else
            {
                sb.Append(hechos);
                if (!hechos.EndsWith("\n")) sb.AppendLine();
            }

            bool agregadoAlgo = false;
            foreach (var hecho in nuevos)
            {
                string norm = NormalizarParaComparar(hecho);
                if (string.IsNullOrEmpty(norm)) continue;
                if (yaPresentes.Contains(norm)) continue;

                sb.AppendLine(hecho);
                yaPresentes.Add(norm);
                agregadoAlgo = true;
            }

            if (agregadoAlgo)
                await MemoriaManager.GuardarHechosAsync(rutaBase, sb.ToString());
        }

        private static string NormalizarParaComparar(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea)) return "";
            string s = linea.Trim();
            if (s.StartsWith("#")) return ""; // encabezados
            if (s.StartsWith("-")) s = s.Substring(1).Trim();
            return s.ToLowerInvariant();
        }

        // ══════════════════ Utilidad ══════════════════

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
