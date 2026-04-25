// ============================================================
//  ContextoIndexer.cs  — Fase D (RAG universal de contexto)
//
//  Hermano de MemoriaIndexer, pero para la metadata del agente:
//    · Credenciales  (ListApis.json)        → fuente "credencial"
//    · Skills        (skills/*.md + JSON)   → fuente "skill"
//    · Automatizaciones (ListAutomatizaciones.json) → fuente "automatizacion"
//
//  ¿Por qué?
//    El prompt del Constructor crece linealmente con cada credencial,
//    skill y automatización. En workspaces reales eso acumula fácilmente
//    4k–8k tokens de manifest que el LLM casi nunca necesita entero.
//
//    Con indexación + retrieval top-K por similitud semántica,
//    el prompt baja a ~1k tokens y sólo trae lo relevante a la
//    instrucción actual.
//
//  DISEÑO:
//    · Índice paralelo al de memoria (embeddings_contexto.jsonl).
//    · Manifest aparte (hashes por fuente, como MemoriaIndexer).
//    · Indexación incremental: re-embebe solo lo que cambió.
//    · Best-effort: si falla el proveedor de embeddings, el pipeline
//      cae al dump completo clásico.
//
//  GATE:
//    HAB_CONTEXTO_SEMANTICO = OFF → IndexarAsync no arranca.
//    Así un usuario que no quiere pagar embeddings por skills
//    mantiene el comportamiento clásico.
// ============================================================

using Newtonsoft.Json;
using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.Skills;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class ContextoIndexer
    {
        // Claves de fuente — deben ser estables (el índice las persiste).
        public const string F_CREDENCIAL     = "credencial";
        public const string F_SKILL          = "skill";
        public const string F_AUTOMATIZACION = "automatizacion";

        /// <summary>
        /// Indexa (o re-indexa incrementalmente) credenciales, skills y
        /// automatizaciones del workspace. Devuelve un resumen del trabajo.
        /// Seguro de llamar en background: si cualquier fuente cambió,
        /// se re-embebe solo esa.
        /// </summary>
        public static async Task<ResultadoIndexado> IndexarAsync(
            string rutaWorkspace,
            bool forzarReindexacion = false,
            IProgress<string>? progreso = null,
            CancellationToken ct = default)
        {
            var r = new ResultadoIndexado();

            if (string.IsNullOrWhiteSpace(rutaWorkspace))
            {
                r.Mensaje = "Ruta de workspace vacía.";
                return r;
            }

            if (!forzarReindexacion &&
                !HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_CONTEXTO_SEMANTICO))
            {
                r.Mensaje = "Habilidad 'contexto_semantico' está desactivada.";
                return r;
            }

            var cfg = EmbeddingsService.CargarConfig();
            var store = VectorStore.Crear(
                RutasProyecto.ObtenerRutaEmbeddingsContexto(rutaWorkspace));
            store.Cargar();

            var manifest = LeerManifest(rutaWorkspace);

            // Si cambió proveedor/modelo → vectores incompatibles: limpiar todo.
            bool cambioProveedor =
                !string.Equals(manifest.Proveedor, cfg.Proveedor.ToString(), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(manifest.Modelo,    cfg.Modelo,              StringComparison.OrdinalIgnoreCase);

            if (cambioProveedor)
            {
                progreso?.Report("Cambio de proveedor/modelo. Limpiando índice de contexto…");
                store.Limpiar();
                manifest = new ManifestEmbeddings
                {
                    Proveedor = cfg.Proveedor.ToString(),
                    Modelo    = cfg.Modelo,
                    Dimension = cfg.DimensionEsperada,
                };
            }

            // ── Extraer chunks de cada fuente (en memoria, con hash) ──
            var fuentes = new List<(string fuente, string hash, List<(string texto, string id)> chunks)>
            {
                ExtraerCredenciales(),
                ExtraerSkills(rutaWorkspace),
                ExtraerAutomatizaciones(),
            };

            foreach (var (fuente, hash, chunks) in fuentes)
            {
                ct.ThrowIfCancellationRequested();

                bool yaHecho = manifest.HashPorFuente.TryGetValue(fuente, out var hashPrevio)
                               && hashPrevio == hash
                               && !forzarReindexacion;

                if (yaHecho)
                {
                    progreso?.Report($"· {fuente}: sin cambios, saltando.");
                    continue;
                }

                progreso?.Report($"· {fuente}: re-indexando {chunks.Count} entradas…");

                // Reconstruir esa fuente desde cero — garantiza consistencia
                // (si el usuario borró una credencial, desaparece del índice).
                store.EliminarPorFuente(fuente);
                r.ChunksGenerados += chunks.Count;

                if (chunks.Count == 0)
                {
                    manifest.HashPorFuente[fuente] = hash;
                    continue;
                }

                var textos = chunks.Select(c => c.texto).ToList();
                var vectores = await EmbeddingsService.EmbedManyAsync(textos, ct);

                var nuevos = new List<ChunkMemoria>(chunks.Count);
                for (int i = 0; i < chunks.Count; i++)
                {
                    float[] v = i < vectores.Count ? vectores[i] : Array.Empty<float>();
                    if (v.Length == 0) { r.ChunksFallidos++; continue; }

                    nuevos.Add(new ChunkMemoria
                    {
                        Id        = chunks[i].id,
                        Fuente    = fuente,
                        Texto     = chunks[i].texto,
                        Vector    = v,
                        Modelo    = cfg.Modelo,
                        Proveedor = cfg.Proveedor.ToString(),
                        Fecha     = DateTime.UtcNow,
                        Offset    = i,
                    });
                }

                store.UpsertMuchos(nuevos);
                r.ChunksIndexados += nuevos.Count;
                manifest.HashPorFuente[fuente] = hash;
                progreso?.Report($"· {fuente}: {nuevos.Count} chunks OK.");
            }

            manifest.Proveedor        = cfg.Proveedor.ToString();
            manifest.Modelo           = cfg.Modelo;
            manifest.Dimension        = cfg.DimensionEsperada;
            manifest.UltimaIndexacion = DateTime.UtcNow;
            GuardarManifest(rutaWorkspace, manifest);

            r.TotalEnStore = store.Count;
            r.Mensaje      = $"Listo. Indexados {r.ChunksIndexados} chunks ({r.ChunksFallidos} fallos).";
            return r;
        }

        // ══════════════════ Extractores por fuente ══════════════════

        /// <summary>
        /// Cada credencial de ListApis.json → 1 chunk con
        /// "NOMBRE — DESCRIPCION" (no incluimos la key por seguridad).
        /// </summary>
        private static (string, string, List<(string texto, string id)>) ExtraerCredenciales()
        {
            var chunks = new List<(string texto, string id)>();
            var sbHash = new StringBuilder();

            try
            {
                string ruta = RutasProyecto.ObtenerRutaListApis();
                if (!File.Exists(ruta))
                    return (F_CREDENCIAL, "", chunks);

                string json = File.ReadAllText(ruta);
                sbHash.Append(json);

                var apis = JsonConvert.DeserializeObject<List<Api>>(json) ?? new List<Api>();
                foreach (var api in apis)
                {
                    string nombre = (api.Nombre ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(nombre)) continue;

                    string desc = (api.Descripcion ?? "").Trim();
                    // Texto para embedding: enriquecido para que la similitud
                    // funcione bien con instrucciones en lenguaje natural.
                    string texto = string.IsNullOrWhiteSpace(desc)
                        ? $"Credencial {nombre}"
                        : $"Credencial {nombre}: {desc}";

                    string id = MemoriaChunker.ComputarId(F_CREDENCIAL, 0, nombre);
                    chunks.Add((texto, id));
                }
            }
            catch { /* best-effort */ }

            return (F_CREDENCIAL, MemoriaChunker.HashContenido(sbHash.ToString()), chunks);
        }

        /// <summary>
        /// Cada skill activo → 1 chunk con "nombre · categoría · descripción · params".
        /// Solo se indexan los activos (los mismos que verían el LLM).
        /// </summary>
        private static (string, string, List<(string texto, string id)>) ExtraerSkills(string rutaWorkspace)
        {
            var chunks = new List<(string texto, string id)>();
            var sbHash = new StringBuilder();

            try
            {
                var skills = SkillLoader.CargarActivas(rutaWorkspace);
                foreach (var s in skills.OrderBy(x => x.IdEfectivo))
                {
                    string id = s.IdEfectivo;
                    string nombre = s.NombreEfectivo;
                    string categoria = string.IsNullOrWhiteSpace(s.Categoria) ? "general" : s.Categoria;
                    string desc = (s.Descripcion ?? "").Trim();

                    string paramsTxt = "";
                    if (s.Parametros != null && s.Parametros.Count > 0)
                    {
                        paramsTxt = " · params: " + string.Join(", ",
                            s.Parametros.Select(p => p.Nombre));
                    }

                    string texto = $"Skill {id} ({categoria}): {nombre}. {desc}{paramsTxt}";
                    sbHash.Append(texto);
                    sbHash.Append('\n');

                    string chunkId = MemoriaChunker.ComputarId(F_SKILL, 0, id);
                    chunks.Add((texto, chunkId));
                }
            }
            catch { /* best-effort */ }

            return (F_SKILL, MemoriaChunker.HashContenido(sbHash.ToString()), chunks);
        }

        /// <summary>
        /// Cada automatización activa → 1 chunk con nombre, descripción y
        /// programación (útil para "mi reporte diario", "la automatización
        /// de slack", etc.).
        /// </summary>
        private static (string, string, List<(string texto, string id)>) ExtraerAutomatizaciones()
        {
            var chunks = new List<(string texto, string id)>();
            var sbHash = new StringBuilder();

            try
            {
                string ruta = RutasProyecto.ObtenerRutaListAutomatizaciones();
                if (!File.Exists(ruta))
                    return (F_AUTOMATIZACION, "", chunks);

                string json = File.ReadAllText(ruta);
                sbHash.Append(json);

                var autos = JsonConvert.DeserializeObject<List<Automatizacion>>(json)
                          ?? new List<Automatizacion>();

                foreach (var a in autos.Where(x => x.Activa))
                {
                    string nombre = (a.Nombre ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(nombre)) continue;

                    string desc = (a.Descripcion ?? "").Trim();
                    string prog = FormatearProgramacion(a);

                    string texto = $"Automatización \"{nombre}\""
                                 + (string.IsNullOrWhiteSpace(desc) ? "" : $": {desc}")
                                 + (string.IsNullOrWhiteSpace(prog)  ? "" : $" [{prog}]");

                    string id = MemoriaChunker.ComputarId(F_AUTOMATIZACION, 0, a.Id);
                    chunks.Add((texto, id));
                }
            }
            catch { /* best-effort */ }

            return (F_AUTOMATIZACION, MemoriaChunker.HashContenido(sbHash.ToString()), chunks);
        }

        private static string FormatearProgramacion(Automatizacion a)
        {
            switch ((a.TipoProgramacion ?? "").ToLowerInvariant())
            {
                case "diaria":    return $"diaria a las {a.HoraEjecucion}";
                case "unica":     return $"una vez {a.FechaUnica:yyyy-MM-dd} {a.HoraEjecucion}";
                case "intervalo": return $"cada {a.IntervaloMinutos} min";
                case "siempre":   return "siempre activa";
                default:          return "";
            }
        }

        // ══════════════════ Manifest ══════════════════

        private static ManifestEmbeddings LeerManifest(string rutaWorkspace)
        {
            try
            {
                string path = RutasProyecto.ObtenerRutaEmbeddingsContextoManifest(rutaWorkspace);
                if (!File.Exists(path)) return new ManifestEmbeddings();
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ManifestEmbeddings>(json) ?? new ManifestEmbeddings();
            }
            catch
            {
                return new ManifestEmbeddings();
            }
        }

        private static void GuardarManifest(string rutaWorkspace, ManifestEmbeddings m)
        {
            try
            {
                string path = RutasProyecto.ObtenerRutaEmbeddingsContextoManifest(rutaWorkspace);
                File.WriteAllText(path, JsonConvert.SerializeObject(m, Formatting.Indented));
            }
            catch { }
        }
    }
}
