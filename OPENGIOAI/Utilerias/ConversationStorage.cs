// ============================================================
//  ConversationStorage.cs
//
//  Persiste sesiones de chat en JSON.
//
//  ESTRUCTURA DE ARCHIVOS (dentro del workspace):
//    {workspace}/Conversaciones/
//      YYYY-MM-DD.index.json    ← lista de sesiones de ese día
//      {sesionId}.json          ← sesión completa (turnos incluidos)
//
//  CONCURRENCIA:
//    Best-effort, mismo contrato que TraceStorage.
//    Fallos de disco no rompen el pipeline.
// ============================================================

using Newtonsoft.Json;
using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class ConversationStorage
    {
        private static readonly JsonSerializerSettings _cfg = new()
        {
            NullValueHandling  = NullValueHandling.Ignore,
            Formatting         = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
        };

        private static readonly object _lock = new();

        // ── Sesión activa en memoria ──────────────────────────────────────────

        private static SesionConversacion? _actual;

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>
        /// Abre una nueva sesión. Llama a esto antes de la primera instrucción.
        /// Devuelve el SesionId generado.
        /// </summary>
        public static string IniciarSesion(string primeraInstruccion, string modelo = "", string proveedor = "")
        {
            FinalizarSesion(); // cerrar la anterior si quedó abierta

            var ses = new SesionConversacion
            {
                SesionId  = Guid.NewGuid().ToString("N")[..14],
                Titulo    = Recortar(primeraInstruccion, 80),
                Modelo    = modelo,
                Proveedor = proveedor,
                Inicio    = DateTime.Now
            };

            lock (_lock) { _actual = ses; }
            return ses.SesionId;
        }

        /// <summary>Añade un turno a la sesión activa (fire-and-forget seguro).</summary>
        public static void AgregarTurno(string instruccion, string respuesta)
        {
            SesionConversacion? ses;
            lock (_lock) { ses = _actual; }
            if (ses == null) return;

            var turno = new TurnoChat
            {
                Instruccion       = instruccion,
                Respuesta         = respuesta,
                Timestamp         = DateTime.Now,
                TokensEstimados   = (instruccion.Length + respuesta.Length) / 4
            };

            lock (_lock) { _actual?.Turnos.Add(turno); }

            _ = PersistirAsync(ses.SesionId);
        }

        /// <summary>Cierra la sesión activa y persiste su estado final.</summary>
        public static void FinalizarSesion()
        {
            SesionConversacion? ses;
            lock (_lock)
            {
                ses = _actual;
                _actual = null;
            }
            if (ses == null) return;

            ses.Fin = DateTime.Now;
            _ = PersistirAsync(ses.SesionId, ses, final: true);
        }

        // ── Lectura ───────────────────────────────────────────────────────────

        /// <summary>Índice de sesiones de un día. Rápido — no carga los turnos.</summary>
        public static List<SesionIndiceItem> LeerIndice(DateTime fecha)
        {
            string ruta = RutasProyecto.ObtenerRutaConversacionesIndexDelDia(fecha);
            if (!File.Exists(ruta)) return new();
            try
            {
                string json = File.ReadAllText(ruta, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<SesionIndiceItem>>(json) ?? new();
            }
            catch { return new(); }
        }

        /// <summary>Carga una sesión completa con todos sus turnos.</summary>
        public static SesionConversacion? LeerSesion(string sesionId)
        {
            string ruta = RutasProyecto.ObtenerRutaSesion(sesionId);
            if (!File.Exists(ruta)) return null;
            try
            {
                string json = File.ReadAllText(ruta, Encoding.UTF8);
                return JsonConvert.DeserializeObject<SesionConversacion>(json);
            }
            catch { return null; }
        }

        /// <summary>Fechas con sesiones disponibles, más reciente primero.</summary>
        public static List<DateTime> FechasDisponibles()
        {
            var resultado = new List<DateTime>();
            try
            {
                string carpeta = RutasProyecto.ObtenerRutaCarpetaConversaciones();
                if (!Directory.Exists(carpeta)) return resultado;

                foreach (var f in Directory.EnumerateFiles(carpeta, "*.index.json"))
                {
                    string nombre = Path.GetFileNameWithoutExtension(f); // "YYYY-MM-DD.index"
                    string solo   = nombre.Replace(".index", "");
                    if (DateTime.TryParse(solo, out var fecha))
                        resultado.Add(fecha.Date);
                }
            }
            catch { }

            resultado.Sort((a, b) => b.CompareTo(a));
            return resultado;
        }

        /// <summary>Elimina una sesión del disco y la quita del índice de su día.</summary>
        public static void Eliminar(string sesionId, DateTime fecha)
        {
            try
            {
                string rutaSes = RutasProyecto.ObtenerRutaSesion(sesionId);
                if (File.Exists(rutaSes)) File.Delete(rutaSes);

                string rutaIdx = RutasProyecto.ObtenerRutaConversacionesIndexDelDia(fecha);
                if (!File.Exists(rutaIdx)) return;
                var items = LeerIndice(fecha);
                items.RemoveAll(i => i.SesionId == sesionId);
                File.WriteAllText(rutaIdx,
                    JsonConvert.SerializeObject(items, _cfg), Encoding.UTF8);
            }
            catch { }
        }

        // ── Internos ──────────────────────────────────────────────────────────

        private static async Task PersistirAsync(string sesionId,
            SesionConversacion? sesSnapshot = null, bool final = false)
        {
            SesionConversacion? ses;
            if (sesSnapshot != null)
            {
                ses = sesSnapshot;
            }
            else
            {
                lock (_lock)
                {
                    if (_actual?.SesionId != sesionId) return;
                    ses = CloneSesion(_actual);
                }
            }
            if (ses == null) return;

            await Task.Run(() =>
            {
                try
                {
                    // 1) Guardar JSON completo de la sesión
                    string carpeta = RutasProyecto.ObtenerRutaCarpetaConversaciones();
                    Directory.CreateDirectory(carpeta);

                    string rutaSes  = RutasProyecto.ObtenerRutaSesion(ses.SesionId);
                    string json     = JsonConvert.SerializeObject(ses, _cfg);
                    File.WriteAllText(rutaSes, json, Encoding.UTF8);

                    // 2) Actualizar índice del día
                    DateTime fecha  = ses.Inicio.Date;
                    string rutaIdx  = RutasProyecto.ObtenerRutaConversacionesIndexDelDia(fecha);
                    var items       = File.Exists(rutaIdx)
                        ? JsonConvert.DeserializeObject<List<SesionIndiceItem>>(
                            File.ReadAllText(rutaIdx, Encoding.UTF8)) ?? new()
                        : new List<SesionIndiceItem>();

                    int idx = items.FindIndex(i => i.SesionId == ses.SesionId);
                    var item = new SesionIndiceItem
                    {
                        SesionId   = ses.SesionId,
                        Titulo     = ses.Titulo,
                        Modelo     = ses.Modelo,
                        Proveedor  = ses.Proveedor,
                        Inicio     = ses.Inicio,
                        Fin        = ses.Fin,
                        TotalTurnos = ses.TotalTurnos,
                        TotalTokens = ses.TotalTokens,
                    };
                    if (idx >= 0) items[idx] = item;
                    else         items.Add(item);

                    File.WriteAllText(rutaIdx,
                        JsonConvert.SerializeObject(items, _cfg), Encoding.UTF8);
                }
                catch { /* best-effort */ }
            });
        }

        private static SesionConversacion CloneSesion(SesionConversacion s)
            => new()
            {
                SesionId  = s.SesionId,
                Titulo    = s.Titulo,
                Modelo    = s.Modelo,
                Proveedor = s.Proveedor,
                Inicio    = s.Inicio,
                Fin       = s.Fin,
                Turnos    = s.Turnos.Select(t => new TurnoChat
                {
                    Instruccion     = t.Instruccion,
                    Respuesta       = t.Respuesta,
                    Timestamp       = t.Timestamp,
                    TokensEstimados = t.TokensEstimados
                }).ToList()
            };

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "(sin título)";
            s = s.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return s.Length <= max ? s : s[..max] + "…";
        }
    }
}
