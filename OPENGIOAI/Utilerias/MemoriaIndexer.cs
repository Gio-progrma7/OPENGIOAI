// ============================================================
//  MemoriaIndexer.cs  — Fase C
//
//  Orquesta la indexación:
//    1. Lee Hechos.md + Episodios.md del workspace.
//    2. Compara el hash actual contra embeddings.manifest.json.
//    3. Si el hash cambió, borra los chunks de esa fuente y re-embebe.
//    4. Actualiza el manifest.
//
//  SE LLAMA:
//    · Bajo demanda desde FrmEmbeddings (botón "Re-indexar").
//    · Arriba del pipeline, best-effort, en background, sin bloquear
//      (si está la habilidad memoria_semantica ON y hay archivos nuevos).
//
//  PROTEGIDO POR HABILIDAD:
//    Si HAB_MEMORIA_SEMANTICA está OFF → IndexarAsync no hace nada.
//    Así un usuario que solo quiere memoria textual no paga tokens
//    de embeddings sin darse cuenta.
// ============================================================

using Newtonsoft.Json;
using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class MemoriaIndexer
    {
        /// <summary>
        /// Ejecuta indexación incremental. Devuelve un resumen con el trabajo hecho.
        /// Si no hay nada que indexar, devuelve rápido sin llamar al proveedor.
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

            // Gate de habilidad: si memoria_semantica NO está activa, el
            // indexer no arranca. (Se puede forzar desde FrmEmbeddings.)
            if (!forzarReindexacion &&
                !HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_MEMORIA_SEMANTICA))
            {
                r.Mensaje = "Habilidad 'memoria_semantica' está desactivada.";
                return r;
            }

            var cfg = EmbeddingsService.CargarConfig();
            var store = new VectorStore(rutaWorkspace);
            store.Cargar();

            var manifest = LeerManifest(rutaWorkspace);

            // Si cambió el proveedor o modelo, el índice ENTERO queda
            // incompatible (vectores con distinta dimensión / espacio).
            bool cambioProveedor =
                !string.Equals(manifest.Proveedor, cfg.Proveedor.ToString(), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(manifest.Modelo,    cfg.Modelo,              StringComparison.OrdinalIgnoreCase);

            if (cambioProveedor)
            {
                progreso?.Report("Cambio de proveedor/modelo detectado. Limpiando índice previo…");
                store.Limpiar();
                manifest = new ManifestEmbeddings
                {
                    Proveedor = cfg.Proveedor.ToString(),
                    Modelo    = cfg.Modelo,
                    Dimension = cfg.DimensionEsperada,
                };
            }

            // Fuentes conocidas y cómo chunkearlas.
            var fuentes = new List<(string fuente, string ruta, Func<string, List<(string, int)>> chunker)>
            {
                ("Hechos",     RutasProyecto.ObtenerRutaMemoriaHechos(rutaWorkspace),     MemoriaChunker.ChunkearHechos),
                ("Episodios",  RutasProyecto.ObtenerRutaMemoriaEpisodios(rutaWorkspace),  MemoriaChunker.ChunkearEpisodios),
            };

            foreach (var (fuente, ruta, chunker) in fuentes)
            {
                ct.ThrowIfCancellationRequested();
                if (!File.Exists(ruta))
                {
                    progreso?.Report($"· {fuente}: archivo no existe, saltando.");
                    continue;
                }

                string contenido = "";
                try { contenido = await File.ReadAllTextAsync(ruta, ct); } catch { }
                string hash = MemoriaChunker.HashContenido(contenido);

                bool yaHecho = manifest.HashPorFuente.TryGetValue(fuente, out var hashPrevio)
                               && hashPrevio == hash
                               && !forzarReindexacion;

                if (yaHecho)
                {
                    progreso?.Report($"· {fuente}: sin cambios, saltando.");
                    continue;
                }

                progreso?.Report($"· {fuente}: re-indexando…");

                // Borrar chunks antiguos de esa fuente (garantiza consistencia).
                store.EliminarPorFuente(fuente);

                var trozos = chunker(contenido);
                r.ChunksGenerados += trozos.Count;

                // Embeber en batches para eficiencia (OpenAI) o uno por uno (Ollama).
                var textos = new List<string>(trozos.Count);
                foreach (var (texto, _) in trozos) textos.Add(texto);

                var vectores = await EmbeddingsService.EmbedManyAsync(textos, ct);

                // Ensamblar ChunkMemoria y upsert
                var nuevos = new List<ChunkMemoria>(trozos.Count);
                for (int i = 0; i < trozos.Count; i++)
                {
                    var (texto, offset) = trozos[i];
                    float[] v = i < vectores.Count ? vectores[i] : Array.Empty<float>();
                    if (v.Length == 0) { r.ChunksFallidos++; continue; }

                    nuevos.Add(new ChunkMemoria
                    {
                        Id        = MemoriaChunker.ComputarId(fuente, offset, texto),
                        Fuente    = fuente,
                        Texto     = texto,
                        Vector    = v,
                        Modelo    = cfg.Modelo,
                        Proveedor = cfg.Proveedor.ToString(),
                        Fecha     = DateTime.UtcNow,
                        Offset    = offset,
                    });
                }

                store.UpsertMuchos(nuevos);
                r.ChunksIndexados += nuevos.Count;

                manifest.HashPorFuente[fuente] = hash;
                progreso?.Report($"· {fuente}: {nuevos.Count} chunks OK.");
            }

            manifest.Proveedor         = cfg.Proveedor.ToString();
            manifest.Modelo            = cfg.Modelo;
            manifest.Dimension         = cfg.DimensionEsperada;
            manifest.UltimaIndexacion  = DateTime.UtcNow;
            GuardarManifest(rutaWorkspace, manifest);

            r.TotalEnStore = store.Count;
            r.Mensaje      = $"Listo. Indexados {r.ChunksIndexados} chunks ({r.ChunksFallidos} fallos).";
            return r;
        }

        // ──────── Manifest ────────

        private static ManifestEmbeddings LeerManifest(string rutaWorkspace)
        {
            try
            {
                string path = RutasProyecto.ObtenerRutaEmbeddingsManifest(rutaWorkspace);
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
                string path = RutasProyecto.ObtenerRutaEmbeddingsManifest(rutaWorkspace);
                File.WriteAllText(path, JsonConvert.SerializeObject(m, Formatting.Indented));
            }
            catch { }
        }
    }

    public sealed class ResultadoIndexado
    {
        public int    ChunksGenerados { get; set; }
        public int    ChunksIndexados { get; set; }
        public int    ChunksFallidos  { get; set; }
        public int    TotalEnStore    { get; set; }
        public string Mensaje         { get; set; } = "";
    }
}
