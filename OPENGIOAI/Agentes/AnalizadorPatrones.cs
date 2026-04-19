// ============================================================
//  AnalizadorPatrones.cs  — Fase 3 del sistema de Memoria
//
//  QUÉ HACE:
//    1. Delega a DetectorPatrones para encontrar clusters locales
//       (≥3 ocurrencias) en Episodios.md — operación barata, sin LLM.
//    2. Para cada cluster detectado, llama al LLM (prompt
//       K_ANALIZADOR_PATRONES) pidiendo una sugerencia de Skill:
//       nombre, descripción, categoría, parámetros, ejemplo.
//    3. Devuelve la lista de PatronDetectado enriquecida lista para
//       ser mostrada en FrmPatrones.
//
//  DISPARADOR:
//    NO se ejecuta en el pipeline normal. Solo se invoca cuando el
//    usuario abre FrmPatrones y pulsa "Analizar ahora". De esta forma
//    el coste de tokens queda acotado y visible.
//
//  GATE:
//    Si la habilidad "patrones" está desactivada, devuelve lista vacía
//    sin tocar LLM ni disco.
//
//  BEST-EFFORT:
//    Cualquier fallo al enriquecer un patrón individual mantiene los
//    datos locales (ocurrencias, ejemplos, firma) — el usuario sigue
//    viendo el patrón pero sin sugerencia del LLM.
// ============================================================

using Newtonsoft.Json.Linq;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Promts;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Agentes
{
    public static class AnalizadorPatrones
    {
        /// <summary>
        /// Pipeline completo: detecta localmente + enriquece con LLM.
        /// Usa el AgentContext entregado por el caller (FrmPatrones arma
        /// su propio ctx con la config activa del usuario).
        /// </summary>
        public static async Task<List<PatronDetectado>> AnalizarAsync(
            string rutaTrabajo,
            AgentContext ctx,
            CancellationToken ct = default)
        {
            if (!HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_PATRONES))
                return new List<PatronDetectado>();

            if (string.IsNullOrWhiteSpace(rutaTrabajo))
                return new List<PatronDetectado>();

            // 1. Detección local — barato, sin red.
            var ignoradas = await DetectorPatrones.LeerIgnoradasAsync(rutaTrabajo);
            var patrones = await DetectorPatrones.DetectarAsync(rutaTrabajo, ignoradas);

            if (patrones.Count == 0 || ctx == null)
                return patrones; // nada que enriquecer

            // 2. Enriquecimiento con LLM, uno por uno (clusters suelen ser pocos).
            //    No usamos Parallel.ForEachAsync para no saturar proveedores con rate limits.
            foreach (var p in patrones)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await EnriquecerConLLM(p, ctx, ct);
                }
                catch
                {
                    // Si el LLM falla, el patrón se devuelve "en crudo" —
                    // el usuario igual puede ver ocurrencias/ejemplos.
                }
            }

            return patrones;
        }

        // ══════════════════ Enriquecimiento LLM ══════════════════

        private static async Task EnriquecerConLLM(
            PatronDetectado patron,
            AgentContext ctx,
            CancellationToken ct)
        {
            // Construir el prompt con placeholders resueltos.
            string ejemplos = string.Join(
                Environment.NewLine,
                patron.Ejemplos.Select(e => "- " + Recortar(e, 180)));

            string prompt = PromptRegistry.Instancia.Obtener(
                PromptCatalogo.K_ANALIZADOR_PATRONES,
                new Dictionary<string, string>
                {
                    ["ocurrencias"] = patron.Ocurrencias.ToString(),
                    ["ejemplos"]    = ejemplos,
                });

            // Contexto sin prompt maestro: el Analizador no necesita credenciales,
            // skills ni memoria. Igual que el Memorista, evita ruido innecesario.
            var ctxLlm = ctx.ComoFase("AnalizadorPatrones").ConPromptPersonalizado("");
            string raw = await AIModelConector.ObtenerRespuestaLLMAsync(prompt, ctxLlm, ct);

            if (string.IsNullOrWhiteSpace(raw)) return;

            var (nombre, descripcion, categoria, ejemploInv, parametros) = ParsearRespuesta(raw);

            if (!string.IsNullOrWhiteSpace(nombre))        patron.NombreSugerido    = nombre;
            if (!string.IsNullOrWhiteSpace(descripcion))   patron.Descripcion       = descripcion;
            if (!string.IsNullOrWhiteSpace(categoria))     patron.Categoria         = categoria;
            if (!string.IsNullOrWhiteSpace(ejemploInv))    patron.EjemploInvocacion = ejemploInv;
            if (parametros.Count > 0)                      patron.ParametrosSugeridos = parametros;
        }

        // ══════════════════ Parseo ══════════════════

        private static (string nombre, string descripcion, string categoria,
                        string ejemplo, List<SkillParametro> parametros)
            ParsearRespuesta(string raw)
        {
            string nombre = "", descripcion = "", categoria = "", ejemplo = "";
            var parametros = new List<SkillParametro>();

            try
            {
                string json = ExtraerJson(raw);
                if (string.IsNullOrEmpty(json))
                    return (nombre, descripcion, categoria, ejemplo, parametros);

                var obj = JObject.Parse(json);

                nombre      = (obj["nombre_sugerido"]?.ToString() ?? "").Trim();
                descripcion = (obj["descripcion"]?.ToString() ?? "").Trim();
                categoria   = (obj["categoria"]?.ToString() ?? "").Trim().ToLowerInvariant();
                ejemplo     = (obj["ejemplo_invocacion"]?.ToString() ?? "").Trim();

                var arr = obj["parametros"] as JArray;
                if (arr != null)
                {
                    foreach (var tok in arr)
                    {
                        var o = tok as JObject;
                        if (o == null) continue;

                        var p = new SkillParametro
                        {
                            Nombre      = (o["nombre"]?.ToString() ?? "").Trim(),
                            Tipo        = string.IsNullOrWhiteSpace(o["tipo"]?.ToString())
                                            ? "string"
                                            : o["tipo"]!.ToString().Trim().ToLowerInvariant(),
                            Descripcion = (o["descripcion"]?.ToString() ?? "").Trim(),
                            Requerido   = o["requerido"]?.Type == JTokenType.Boolean
                                            ? o["requerido"]!.ToObject<bool>()
                                            : true,
                        };

                        if (!string.IsNullOrWhiteSpace(p.Nombre))
                            parametros.Add(p);
                    }
                }

                // Normalizar categoría a lista permitida.
                var permitidas = new HashSet<string> { "sistema","archivos","ia","web","datos","general" };
                if (!permitidas.Contains(categoria)) categoria = "general";
            }
            catch
            {
                // JSON roto → devolvemos lo que alcanzamos a parsear (posiblemente vacío).
            }

            return (nombre, descripcion, categoria, ejemplo, parametros);
        }

        private static string ExtraerJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            string s = raw;
            int fenceStart = s.IndexOf("```");
            if (fenceStart >= 0)
            {
                int fenceEnd = s.IndexOf("```", fenceStart + 3);
                if (fenceEnd > fenceStart)
                {
                    s = s.Substring(fenceStart + 3, fenceEnd - fenceStart - 3);
                    if (s.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                        s = s.Substring(4);
                }
            }

            int ini = s.IndexOf('{');
            int fin = s.LastIndexOf('}');
            if (ini < 0 || fin <= ini) return "";
            return s.Substring(ini, fin - ini + 1);
        }

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
