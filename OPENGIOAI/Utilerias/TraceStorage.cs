// ============================================================
//  TraceStorage.cs  — Fase 1A
//
//  Persistencia append-only en JSONL, un trace por línea.
//  Rotación natural por día. Mismo patrón que VectorStore.
//
//  ARCHIVOS:
//    {AppDir}/Traces/YYYY-MM-DD.jsonl       ← trace completo por línea
//    {AppDir}/Traces/YYYY-MM-DD.index.json  ← lista de {id, inicio,
//                                             duracion, estado, llamadasLLM,
//                                             tokens, costo, resumen}
//
//  POR QUÉ DOBLE ARCHIVO:
//    El .jsonl puede crecer mucho (1MB por día de uso intensivo).
//    El .index.json carga en milisegundos y basta para pintar la
//    lista en UI sin parsear los traces completos.
//    Al seleccionar un trace, se lee solo ESA línea del .jsonl.
//
//  CONCURRENCIA:
//    Escritura bajo lock por archivo. Lectura sin lock (snapshot).
//    Mismo contrato best-effort que el resto del proyecto: errores
//    de disco se tragan silenciosamente — perder un trace NUNCA
//    debe romper un pipeline.
// ============================================================

using Newtonsoft.Json;
using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OPENGIOAI.Utilerias
{
    /// <summary>Fila del índice diario — lo mínimo para pintar una lista.</summary>
    public sealed class TraceIndiceItem
    {
        public string InstruccionId { get; set; } = "";
        public string Instruccion { get; set; } = "";
        public DateTime Inicio { get; set; }
        public long DuracionMs { get; set; }
        public SpanEstado Estado { get; set; }
        public int TotalSpans { get; set; }
        public int TotalLlamadasLLM { get; set; }
        public int TotalHerramientas { get; set; }
        public int TotalTokens { get; set; }
        public decimal TotalCostoUsd { get; set; }
        public string Modelo { get; set; } = "";
        public string Servicio { get; set; } = "";
        /// <summary>Offset de byte dentro del JSONL — para lectura directa del trace.</summary>
        public long Offset { get; set; }
        public int Longitud { get; set; }
    }

    /// <summary>
    /// Capa de persistencia de traces. Todos los métodos son best-effort
    /// (tragan errores) salvo los de lectura puntual que propagan si se
    /// pide un archivo inexistente.
    /// </summary>
    public static class TraceStorage
    {
        // Serializador compartido — evita alocar Settings en cada llamada.
        private static readonly JsonSerializerSettings _cfg = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting        = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
        };

        // Lock por día: evita contención global si el proceso corre largo.
        private static readonly Dictionary<string, object> _locks = new();

        private static object LockParaFecha(DateTime fecha)
        {
            string clave = fecha.ToString("yyyy-MM-dd");
            lock (_locks)
            {
                if (!_locks.TryGetValue(clave, out var lk))
                {
                    lk = new object();
                    _locks[clave] = lk;
                }
                return lk;
            }
        }

        // ══════════════════ Escritura ══════════════════

        /// <summary>
        /// Persiste un trace completo: una línea en el JSONL del día +
        /// entrada en el index.json. Silenciosa ante errores de disco.
        /// </summary>
        public static void Guardar(TraceEjecucion trace)
        {
            if (trace == null) return;

            DateTime fecha = trace.Inicio.ToLocalTime().Date;
            string rutaJsonl = RutasProyecto.ObtenerRutaTracesDelDia(fecha);
            string rutaIndex = RutasProyecto.ObtenerRutaTracesIndexDelDia(fecha);

            lock (LockParaFecha(fecha))
            {
                try
                {
                    // 1) Serializar el trace en una sola línea.
                    string linea = JsonConvert.SerializeObject(trace, _cfg);
                    byte[] bytes = Encoding.UTF8.GetBytes(linea + "\n");

                    long offset;
                    using (var fs = new FileStream(
                        rutaJsonl, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        offset = fs.Position;
                        fs.Write(bytes, 0, bytes.Length);
                    }

                    // 2) Actualizar el índice — leer, añadir, reescribir.
                    List<TraceIndiceItem> indice = LeerIndice(fecha);
                    indice.Add(new TraceIndiceItem
                    {
                        InstruccionId     = trace.InstruccionId,
                        Instruccion       = Recortar(trace.Instruccion, 200),
                        Inicio            = trace.Inicio,
                        DuracionMs        = trace.DuracionMs,
                        Estado            = trace.Estado,
                        TotalSpans        = trace.TotalSpans,
                        TotalLlamadasLLM  = trace.TotalLlamadasLLM,
                        TotalHerramientas = trace.TotalHerramientas,
                        TotalTokens       = trace.TotalTokens,
                        TotalCostoUsd     = trace.TotalCostoUsd,
                        Modelo            = trace.Modelo,
                        Servicio          = trace.Servicio,
                        Offset            = offset,
                        Longitud          = bytes.Length,
                    });

                    string jsonIndex = JsonConvert.SerializeObject(indice, Formatting.Indented);
                    File.WriteAllText(rutaIndex, jsonIndex, Encoding.UTF8);
                }
                catch
                {
                    // Best-effort: perder un trace es aceptable, romper un
                    // pipeline no lo es. Si hay un problema de disco el
                    // próximo trace volverá a intentar con suerte distinta.
                }
            }
        }

        // ══════════════════ Lectura ══════════════════

        /// <summary>Lista el índice de un día (lo más barato que hay).</summary>
        public static List<TraceIndiceItem> LeerIndice(DateTime fecha)
        {
            string ruta = RutasProyecto.ObtenerRutaTracesIndexDelDia(fecha);
            if (!File.Exists(ruta)) return new List<TraceIndiceItem>();

            try
            {
                string json = File.ReadAllText(ruta, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return new List<TraceIndiceItem>();
                var lista = JsonConvert.DeserializeObject<List<TraceIndiceItem>>(json);
                return lista ?? new List<TraceIndiceItem>();
            }
            catch
            {
                return new List<TraceIndiceItem>();
            }
        }

        /// <summary>
        /// Lee UN trace puntual desde el JSONL del día usando el offset
        /// del índice. Si el offset está roto o fuera de rango, hace scan
        /// lineal como fallback (lento pero correcto).
        /// </summary>
        public static TraceEjecucion? Leer(DateTime fecha, string instruccionId)
        {
            if (string.IsNullOrWhiteSpace(instruccionId)) return null;

            string rutaJsonl = RutasProyecto.ObtenerRutaTracesDelDia(fecha);
            if (!File.Exists(rutaJsonl)) return null;

            // Intento rápido vía índice.
            var item = LeerIndice(fecha).FirstOrDefault(i => i.InstruccionId == instruccionId);
            if (item != null && item.Longitud > 0)
            {
                try
                {
                    using var fs = new FileStream(
                        rutaJsonl, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fs.Seek(item.Offset, SeekOrigin.Begin);
                    byte[] buf = new byte[item.Longitud];
                    int leidos = fs.Read(buf, 0, buf.Length);
                    string linea = Encoding.UTF8.GetString(buf, 0, leidos).TrimEnd('\n');
                    var t = JsonConvert.DeserializeObject<TraceEjecucion>(linea);
                    if (t != null) return t;
                }
                catch { /* fallback */ }
            }

            // Fallback: scan lineal.
            try
            {
                foreach (string linea in File.ReadLines(rutaJsonl, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;
                    // Match barato por substring antes de parsear.
                    if (!linea.Contains(instruccionId)) continue;
                    try
                    {
                        var t = JsonConvert.DeserializeObject<TraceEjecucion>(linea);
                        if (t?.InstruccionId == instruccionId) return t;
                    }
                    catch { /* línea corrupta: siguiente */ }
                }
            }
            catch { /* no-op */ }

            return null;
        }

        /// <summary>Fechas con traces disponibles, más reciente primero.</summary>
        public static List<DateTime> FechasDisponibles()
        {
            var resultado = new List<DateTime>();
            try
            {
                string carpeta = RutasProyecto.ObtenerRutaCarpetaTraces();
                foreach (var f in Directory.EnumerateFiles(carpeta, "*.index.json"))
                {
                    string nombre = Path.GetFileNameWithoutExtension(f); // "2026-04-19.index"
                    int puntoIdx = nombre.IndexOf('.');
                    if (puntoIdx > 0) nombre = nombre.Substring(0, puntoIdx);
                    if (DateTime.TryParse(nombre, out var d))
                        resultado.Add(d);
                }
            }
            catch { /* carpeta inexistente: lista vacía */ }

            resultado.Sort((a, b) => b.CompareTo(a));
            return resultado;
        }

        // ══════════════════ Utilidades ══════════════════

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
