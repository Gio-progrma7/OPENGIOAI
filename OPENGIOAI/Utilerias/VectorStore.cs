// ============================================================
//  VectorStore.cs  — Fase C
//
//  Almacén vectorial local, cero dependencias. Una instancia
//  por workspace (atada a {ruta}/Memoria/embeddings.jsonl).
//
//  ¿Por qué JSONL y no SQLite o FAISS?
//    · Grep-able por el usuario (auditable).
//    · Append-only barato (un fopen por chunk nuevo).
//    · Si se corrompe una línea, se descarta esa sola.
//    · La memoria de un workspace raramente supera 2-3k chunks,
//      así que la búsqueda por fuerza bruta en float[] corre
//      en milisegundos. Si algún día escala, se cambia por FAISS
//      sin tocar la API pública.
//
//  SIMILITUD:
//    Coseno normalizada a [0,1] via (1 + cos) / 2 para poder
//    establecer umbrales intuitivos (0.7+ = relevante).
//
//  CONCURRENCIA:
//    El store carga todo en memoria al primer Buscar/Cargar y
//    mantiene una lista mutable. Lock para appends y reescritura
//    del archivo. La búsqueda corre sin lock sobre un snapshot
//    (List<ChunkMemoria> es thread-safe para lectura si no se
//    modifica en paralelo; los writes van en lock).
// ============================================================

using Newtonsoft.Json;
using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public sealed class VectorStore
    {
        private readonly string _ruta;           // embeddings.jsonl
        private readonly object _lock = new();
        private List<ChunkMemoria> _chunks = new();
        private bool _cargado;

        public VectorStore(string rutaWorkspace)
        {
            _ruta = RutasProyecto.ObtenerRutaEmbeddings(rutaWorkspace);
        }

        /// <summary>
        /// Constructor con archivo personalizado (para índices paralelos
        /// como embeddings_contexto.jsonl). Misma semántica que el ctor base,
        /// pero permite a cada subsistema mantener su propio vector store
        /// sin mezclar scopes (memoria vs. contexto del agente).
        /// </summary>
        public static VectorStore Crear(string rutaArchivoJsonl)
        {
            return new VectorStore(rutaArchivoJsonl, _interno: true);
        }

        private VectorStore(string rutaArchivoJsonl, bool _interno)
        {
            _ruta = rutaArchivoJsonl;
        }

        // ══════════════════ API pública ══════════════════

        /// <summary>Fuerza la lectura del disco (lazy by default).</summary>
        public void Cargar()
        {
            lock (_lock)
            {
                if (_cargado) return;
                _chunks = LeerDeDisco();
                _cargado = true;
            }
        }

        /// <summary>Número total de chunks indexados.</summary>
        public int Count
        {
            get
            {
                AsegurarCargado();
                lock (_lock) return _chunks.Count;
            }
        }

        /// <summary>Número de chunks por fuente (p.ej. Hechos=12, Episodios=83).</summary>
        public Dictionary<string, int> ContarPorFuente()
        {
            AsegurarCargado();
            lock (_lock)
            {
                return _chunks
                    .GroupBy(c => c.Fuente)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        /// <summary>
        /// Agrega un chunk al store. Si ya existe un chunk con mismo Id,
        /// lo reemplaza (útil para re-indexación incremental).
        /// Persiste inmediatamente en disco (append o re-escritura).
        /// </summary>
        public void Upsert(ChunkMemoria chunk)
        {
            if (chunk == null || chunk.Vector.Length == 0) return;
            AsegurarCargado();
            lock (_lock)
            {
                int idx = _chunks.FindIndex(c => c.Id == chunk.Id);
                if (idx >= 0)
                {
                    _chunks[idx] = chunk;
                    ReescribirDisco();
                }
                else
                {
                    _chunks.Add(chunk);
                    AppendDisco(chunk);
                }
            }
        }

        /// <summary>Agrega muchos chunks y persiste una sola vez al final.</summary>
        public void UpsertMuchos(IEnumerable<ChunkMemoria> chunks)
        {
            if (chunks == null) return;
            AsegurarCargado();
            lock (_lock)
            {
                bool debeReescribir = false;
                foreach (var chunk in chunks)
                {
                    if (chunk == null || chunk.Vector.Length == 0) continue;
                    int idx = _chunks.FindIndex(c => c.Id == chunk.Id);
                    if (idx >= 0)
                    {
                        _chunks[idx] = chunk;
                        debeReescribir = true;
                    }
                    else
                    {
                        _chunks.Add(chunk);
                    }
                }
                ReescribirDisco();
                _ = debeReescribir; // silenciar warning
            }
        }

        /// <summary>
        /// Elimina todos los chunks cuya fuente coincide. Útil para
        /// reconstruir completo el índice de Hechos o de Episodios
        /// sin tocar los demás.
        /// </summary>
        public void EliminarPorFuente(string fuente)
        {
            AsegurarCargado();
            lock (_lock)
            {
                int antes = _chunks.Count;
                _chunks = _chunks.Where(c => !string.Equals(c.Fuente, fuente, StringComparison.OrdinalIgnoreCase)).ToList();
                if (_chunks.Count != antes) ReescribirDisco();
            }
        }

        /// <summary>Borra TODO y el archivo JSONL.</summary>
        public void Limpiar()
        {
            lock (_lock)
            {
                _chunks = new List<ChunkMemoria>();
                _cargado = true;
                try { if (File.Exists(_ruta)) File.Delete(_ruta); } catch { }
            }
        }

        /// <summary>
        /// Busca los top-K chunks más similares al vector consulta.
        /// Score es coseno normalizado a [0,1].
        /// Opcionalmente filtra por fuente.
        /// </summary>
        public List<ResultadoBusquedaMemoria> Buscar(
            float[] consulta,
            int topK = 5,
            string? fuenteFiltro = null,
            double scoreMinimo = 0.0)
        {
            if (consulta == null || consulta.Length == 0)
                return new List<ResultadoBusquedaMemoria>();

            AsegurarCargado();
            List<ChunkMemoria> snapshot;
            lock (_lock) { snapshot = _chunks.ToList(); }

            double normaC = Norma(consulta);
            if (normaC == 0) return new List<ResultadoBusquedaMemoria>();

            var scored = new List<ResultadoBusquedaMemoria>(snapshot.Count);
            foreach (var c in snapshot)
            {
                if (fuenteFiltro != null &&
                    !string.Equals(c.Fuente, fuenteFiltro, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (c.Vector.Length != consulta.Length) continue; // dimensiones distintas = ignorar

                double cos = CosenoNormalizado(consulta, c.Vector, normaC);
                if (cos < scoreMinimo) continue;

                scored.Add(new ResultadoBusquedaMemoria { Chunk = c, Score = cos });
            }

            return scored
                .OrderByDescending(r => r.Score)
                .Take(Math.Max(1, topK))
                .ToList();
        }

        // ══════════════════ Persistencia ══════════════════

        private List<ChunkMemoria> LeerDeDisco()
        {
            var salida = new List<ChunkMemoria>();
            try
            {
                if (!File.Exists(_ruta)) return salida;

                foreach (var linea in File.ReadLines(_ruta))
                {
                    string t = linea?.Trim() ?? "";
                    if (string.IsNullOrEmpty(t) || !t.StartsWith("{")) continue;
                    try
                    {
                        var c = JsonConvert.DeserializeObject<ChunkMemoria>(t);
                        if (c != null && c.Vector != null && c.Vector.Length > 0)
                            salida.Add(c);
                    }
                    catch
                    {
                        // línea corrupta → skip silencioso
                    }
                }
            }
            catch
            {
                // Si hay problema de permisos o similar, arrancamos con store vacío.
            }
            return salida;
        }

        private void AppendDisco(ChunkMemoria c)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                string json = JsonConvert.SerializeObject(c);
                File.AppendAllText(_ruta, json + "\n");
            }
            catch { /* best-effort */ }
        }

        private void ReescribirDisco()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                string tmp = _ruta + ".tmp";
                using (var sw = new StreamWriter(tmp, append: false))
                {
                    foreach (var c in _chunks)
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(c));
                    }
                }
                if (File.Exists(_ruta)) File.Delete(_ruta);
                File.Move(tmp, _ruta);
            }
            catch { /* best-effort */ }
        }

        private void AsegurarCargado()
        {
            if (_cargado) return;
            lock (_lock)
            {
                if (_cargado) return;
                _chunks = LeerDeDisco();
                _cargado = true;
            }
        }

        // ══════════════════ Matemáticas ══════════════════

        private static double Norma(float[] v)
        {
            double s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i] * v[i];
            return Math.Sqrt(s);
        }

        /// <summary>
        /// Coseno entre dos vectores normalizado a [0,1].
        /// 1.0 = idénticos, 0.5 = ortogonales, 0.0 = opuestos.
        /// Usamos (1 + cos) / 2 para que los umbrales sean más intuitivos.
        /// </summary>
        private static double CosenoNormalizado(float[] a, float[] b, double normaA)
        {
            double dot = 0;
            double normaB2 = 0;
            int n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++)
            {
                dot     += a[i] * b[i];
                normaB2 += b[i] * b[i];
            }
            double normaB = Math.Sqrt(normaB2);
            if (normaA == 0 || normaB == 0) return 0;
            double cos = dot / (normaA * normaB);
            // clamp por error de punto flotante
            if (cos > 1)  cos = 1;
            if (cos < -1) cos = -1;
            return (1.0 + cos) / 2.0;
        }
    }
}
