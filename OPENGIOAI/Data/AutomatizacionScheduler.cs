using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Data
{
    /// <summary>
    /// Scheduler en background que revisa cada minuto las automatizaciones activas
    /// y ejecuta las que coincidan con la hora/intervalo programado.
    /// Corre en paralelo al formulario — funciona aunque la UI esté cerrada.
    /// </summary>
    public sealed class AutomatizacionScheduler : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private Task? _tarea;

        // Credenciales necesarias para ejecutar scripts
        private string _modelo   = "";
        private string _apiKey   = "";
        private string _rutaBase = "";
        private string _claves   = "";
        private Servicios _agente = Servicios.Gemenni;

        // Tracking: evita que la misma auto corra dos veces en el mismo minuto
        private readonly ConcurrentDictionary<string, DateTime> _ultimaEjecucion = new();

        // Evento para notificar a la UI
        public event Action<string, string>? OnLog; // (autoId, mensaje)

        public void Configurar(string modelo, string apiKey, string rutaBase,
                               string claves, Servicios agente)
        {
            _modelo   = modelo;
            _apiKey   = apiKey;
            _rutaBase = rutaBase;
            _claves   = claves;
            _agente   = agente;
        }

        public void Iniciar()
        {
            if (_tarea != null) return;
            _tarea = Task.Run(() => BuclePrincipal(_cts.Token), _cts.Token);
        }

        public void Detener()
        {
            _cts.Cancel();
            try
            {
                _tarea?.Wait(3000);
            }
            catch (AggregateException ae)
                when (ae.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // Cancelación esperada — no es un error.
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("SCHEDULER", $"Error al detener: {ex.Message}");
            }
        }

        // ── Bucle principal — se ejecuta cada 30 segundos ─────────────────
        private async Task BuclePrincipal(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var lista = JsonManager.Leer<Automatizacion>(
                        RutasProyecto.ObtenerRutaListAutomatizaciones());

                    var ahora = DateTime.Now;

                    foreach (var auto in lista.Where(a => a.Activa && a.TipoScheduleEnum != TipoSchedule.Manual))
                    {
                        if (ct.IsCancellationRequested) break;
                        if (!DebeEjecutarse(auto, ahora)) continue;

                        // Marcar para no repetir en este minuto
                        string clave = $"{auto.Id}_{ahora:yyyyMMddHHmm}";
                        if (!_ultimaEjecucion.TryAdd(clave, ahora)) continue;

                        // Ejecutar en paralelo (fire-and-forget con logging)
                        _ = EjecutarAutomatizacionAsync(auto, ct);
                    }

                    // Limpiar tracking viejo (>2h)
                    foreach (var kv in _ultimaEjecucion)
                        if ((ahora - kv.Value).TotalHours > 2)
                            _ultimaEjecucion.TryRemove(kv.Key, out _);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke("SCHEDULER", $"Error en bucle: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            }
        }

        // ── Evalúa si la automatización debe correr ahora ─────────────────
        private static bool DebeEjecutarse(Automatizacion auto, DateTime ahora)
        {
            // Verificar día de la semana
            if (auto.DiasActivos.Count > 0 && !auto.DiasActivos.Contains((int)ahora.DayOfWeek))
                return false;

            switch (auto.TipoScheduleEnum)
            {
                case TipoSchedule.Diaria:
                case TipoSchedule.Unica:
                    // Ejecutar si la hora actual coincide con HoraEjecucion (±1 min)
                    if (TimeSpan.TryParse(auto.HoraEjecucion, out var he))
                    {
                        var diff = (ahora.TimeOfDay - he).Duration();
                        return diff.TotalMinutes < 1;
                    }
                    return false;

                case TipoSchedule.Intervalo:
                    if (auto.IntervaloMinutos <= 0) return false;
                    // Verificar si estamos dentro del rango horario (si se definió)
                    if (!string.IsNullOrEmpty(auto.HoraInicio) && !string.IsNullOrEmpty(auto.HoraFin))
                    {
                        if (TimeSpan.TryParse(auto.HoraInicio, out var hi) &&
                            TimeSpan.TryParse(auto.HoraFin, out var hf))
                        {
                            if (ahora.TimeOfDay < hi || ahora.TimeOfDay > hf) return false;
                        }
                    }
                    // ¿Ha pasado suficiente tiempo desde la última ejecución?
                    if (auto.UltimaEjecucion.HasValue)
                    {
                        var minDesde = (ahora - auto.UltimaEjecucion.Value).TotalMinutes;
                        return minDesde >= auto.IntervaloMinutos;
                    }
                    return true; // Primera vez

                case TipoSchedule.Siempre:
                    // Se ejecuta en cada ciclo del scheduler (cada 30s)
                    return true;

                case TipoSchedule.Manual:
                default:
                    return false;
            }
        }

        // ── Validación del grafo: ciclos y nodos huérfanos ────────────────
        /// <summary>
        /// Detecta ciclos en el grafo de nodos vía Kahn (topological sort).
        /// Devuelve la lista de IDs de nodos involucrados en al menos un ciclo;
        /// vacía si el grafo es un DAG válido.
        /// </summary>
        public static List<string> DetectarCiclos(Automatizacion auto)
        {
            if (auto.Nodos == null || auto.Nodos.Count == 0)
                return new List<string>();

            var inDegree = auto.Nodos.ToDictionary(n => n.Id, _ => 0);
            var idsValidos = new HashSet<string>(inDegree.Keys);

            foreach (var nodo in auto.Nodos)
                foreach (var sucId in nodo.ConexionesSalida)
                    if (idsValidos.Contains(sucId))
                        inDegree[sucId]++;

            var cola = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            int procesados = 0;
            var nodosPorId = auto.Nodos.ToDictionary(n => n.Id);

            while (cola.Count > 0)
            {
                string id = cola.Dequeue();
                procesados++;
                foreach (var sucId in nodosPorId[id].ConexionesSalida)
                {
                    if (!inDegree.ContainsKey(sucId)) continue;
                    if (--inDegree[sucId] == 0) cola.Enqueue(sucId);
                }
            }

            // Los nodos con in-degree restante > 0 forman parte de un ciclo
            return procesados == auto.Nodos.Count
                ? new List<string>()
                : inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
        }

        // ── Ejecutar todos los nodos respetando el grafo de dependencias ──────
        private async Task EjecutarAutomatizacionAsync(Automatizacion auto, CancellationToken ct)
        {
            OnLog?.Invoke(auto.Id, $"⏰ Scheduler arrancando: {auto.Nombre}");

            // Validar grafo (ciclos + entradas requeridas) antes de arrancar procesos
            var erroresGrafo = auto.ValidarGrafo();
            if (erroresGrafo.Count > 0)
            {
                auto.UltimoEstado = $"✖ Grafo inválido ({erroresGrafo.Count} error(es))";
                foreach (var e in erroresGrafo)
                    OnLog?.Invoke(auto.Id, $"  ✖ {e}");
                PersistirEstado(auto);
                return;
            }

            string carpeta = ObtenerCarpeta(auto);

            // Variables globales serializadas — disponibles a todos los nodos
            string autoVariablesJson =
                Newtonsoft.Json.JsonConvert.SerializeObject(auto.Variables ?? new());

            // Token combinado: cancelación externa + timeout configurable
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (auto.TimeoutMinutos > 0)
                ctsTimeout.CancelAfter(TimeSpan.FromMinutes(auto.TimeoutMinutos));
            var ctEjec = ctsTimeout.Token;

            // Si existe el orquestador Python generado, usarlo directamente
            string rutaOrq = Path.Combine(carpeta, "_orquestador.py");
            if (File.Exists(rutaOrq))
            {
                OnLog?.Invoke(auto.Id, "▶ Usando orquestador Python (grafo completo)");
                try
                {
                    string logOrq = await EjecutarScriptPython(
                        rutaOrq, carpeta, null, autoVariablesJson, ctEjec);
                    auto.UltimoEstado    = "✔ Completado";
                    auto.UltimaEjecucion = DateTime.Now;
                    OnLog?.Invoke(auto.Id, $"✔ Completado\n{Truncar(logOrq, 500)}");
                }
                catch (OperationCanceledException) when (ctsTimeout.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    auto.UltimoEstado = $"✖ Timeout ({auto.TimeoutMinutos} min) en orquestador";
                    OnLog?.Invoke(auto.Id, auto.UltimoEstado);
                }
                catch (Exception ex)
                {
                    auto.UltimoEstado = $"✖ {ex.Message}";
                    OnLog?.Invoke(auto.Id, $"✖ {ex.Message}");
                }
                PersistirEstado(auto);
                return;
            }

            // Fallback: ejecutar respetando grafo con TCS (mismo algoritmo que la UI)
            var nodosById = auto.Nodos.ToDictionary(n => n.Id);
            var predMap   = auto.Nodos.ToDictionary(n => n.Id, _ => new List<string>());
            foreach (var n in auto.Nodos)
                foreach (var sucId in n.ConexionesSalida)
                    if (predMap.ContainsKey(sucId)) predMap[sucId].Add(n.Id);

            var tcs        = auto.Nodos.ToDictionary(
                n => n.Id, _ => new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
            var resultados = new ConcurrentDictionary<string, string>();
            int errores    = 0;

            async Task EjecutarNodoSched(NodoAutomatizacion nodo)
            {
                try
                {
                    var preds = predMap[nodo.Id];
                    if (preds.Count > 0)
                        await Task.WhenAll(preds.Select(pid => tcs[pid].Task));

                    ctEjec.ThrowIfCancellationRequested();

                    string rutaScript = Path.Combine(carpeta, nodo.NombreScript);
                    if (!File.Exists(rutaScript))
                    {
                        OnLog?.Invoke(auto.Id, $"  ⚠ Script no encontrado: {nodo.NombreScript}");
                        resultados[nodo.Id] = "";
                        tcs[nodo.Id].TrySetResult(false);
                        return;
                    }

                    // Contexto plano (backward compat: NODO_ANTERIOR_RESULTADO)
                    string ctx = preds.Count == 0 ? ""
                               : preds.Count == 1 ? resultados.GetValueOrDefault(preds[0], "")
                               : string.Join("\n", preds
                                   .Where(p => resultados.ContainsKey(p) && !string.IsNullOrEmpty(resultados[p]))
                                   .Select(p => $"=== {nodosById[p].Titulo} ===\n{resultados[p]}"));

                    // Contexto estructurado (NODO_CONTEXTO): JSON con la salida de
                    // cada predecesor parseada cuando es JSON válido, o crudo si no.
                    string ctxJson = ConstruirContextoJson(preds, resultados, nodosById, nodo);

                    OnLog?.Invoke(auto.Id, $"  ▶ {nodo.Titulo}");
                    string resultado = await EjecutarScriptPython(
                        rutaScript, carpeta, ctx, autoVariablesJson, ctEjec, ctxJson);
                    resultados[nodo.Id] = resultado;
                    nodo.UltimaRespuesta = resultado;
                    OnLog?.Invoke(auto.Id, $"  ✔ {nodo.Titulo}: {Truncar(resultado, 100)}");
                    tcs[nodo.Id].TrySetResult(true);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errores);
                    resultados[nodo.Id] = "";
                    OnLog?.Invoke(auto.Id, $"  ✖ {nodo.Titulo}: {ex.Message}");
                    tcs[nodo.Id].TrySetResult(false);
                }
            }

            try
            {
                await Task.WhenAll(auto.Nodos.Select(EjecutarNodoSched));
                auto.UltimoEstado    = errores == 0 ? "✔ Completado" : $"⚠ {errores} error(es)";
                auto.UltimaEjecucion = DateTime.Now;
                OnLog?.Invoke(auto.Id, $"✔ {auto.Nombre}: {auto.UltimoEstado}");
            }
            catch (OperationCanceledException) when (ctsTimeout.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                foreach (var t in tcs.Values) t.TrySetResult(false);
                auto.UltimoEstado = $"✖ Timeout ({auto.TimeoutMinutos} min)";
                OnLog?.Invoke(auto.Id, $"✖ {auto.Nombre}: {auto.UltimoEstado}");
            }
            catch (Exception ex)
            {
                // Señalizar todos los TCS restantes para no dejar tareas bloqueadas
                foreach (var t in tcs.Values) t.TrySetResult(false);
                auto.UltimoEstado = $"✖ {ex.Message}";
                OnLog?.Invoke(auto.Id, $"✖ {ex.Message}");
            }

            PersistirEstado(auto);
        }

        private void PersistirEstado(Automatizacion auto)
        {
            try
            {
                var lista = JsonManager.Leer<Automatizacion>(
                    RutasProyecto.ObtenerRutaListAutomatizaciones());
                var item = lista.FirstOrDefault(a => a.Id == auto.Id);
                if (item != null)
                {
                    item.UltimoEstado    = auto.UltimoEstado;
                    item.UltimaEjecucion = auto.UltimaEjecucion;
                    JsonManager.Guardar(RutasProyecto.ObtenerRutaListAutomatizaciones(), lista);
                }
            }
            catch (Exception ex)
            {
                // No-fatal: solo afecta a la persistencia del último estado.
                // Antes se silenciaba — ahora al menos lo logueamos para diagnóstico.
                OnLog?.Invoke(auto.Id, $"⚠ No se pudo persistir estado: {ex.Message}");
            }
        }

        // ── Construcción del contexto JSON entre nodos ────────────────────
        /// <summary>
        /// Construye el JSON que se inyecta en NODO_CONTEXTO. Estructura:
        ///   {
        ///     "predecesores": { "<titulo_predecesor>": <salida_parseada_o_string>, ... },
        ///     "anterior": <salida_directa_si_solo_hay_un_predecesor>,
        ///     "esperadas": ["nombre1", "nombre2", ...]
        ///   }
        /// El script Python puede `json.loads(os.environ["NODO_CONTEXTO"])` y
        /// trabajar con las salidas tipadas en vez de parsear texto plano.
        /// </summary>
        private static string ConstruirContextoJson(
            List<string> predIds,
            ConcurrentDictionary<string, string> resultados,
            Dictionary<string, NodoAutomatizacion> nodosById,
            NodoAutomatizacion nodoActual)
        {
            var predecesores = new Dictionary<string, object?>();
            foreach (var pid in predIds)
            {
                if (!nodosById.TryGetValue(pid, out var pred)) continue;
                string salida = resultados.GetValueOrDefault(pid, "");
                predecesores[pred.Titulo] = ParsearSalidaSiJson(salida);
            }

            object? anterior = predIds.Count == 1
                ? ParsearSalidaSiJson(resultados.GetValueOrDefault(predIds[0], ""))
                : null;

            var esperadas = (nodoActual.Entradas ?? new List<VariableNodo>())
                .Select(e => e.Nombre).ToList();

            var paquete = new
            {
                predecesores,
                anterior,
                esperadas,
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(paquete);
        }

        /// <summary>
        /// Si la salida del nodo predecesor es un JSON válido, devuelve el
        /// JToken parseado para que el receptor pueda navegarlo. Si no, lo
        /// devuelve como string crudo.
        /// </summary>
        private static object? ParsearSalidaSiJson(string salida)
        {
            string s = (salida ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return "";
            if (s.StartsWith('{') || s.StartsWith('['))
            {
                try { return Newtonsoft.Json.Linq.JToken.Parse(s); }
                catch { /* no es JSON, caer al string */ }
            }
            return s;
        }

        // ── Ejecutar un script .py específico ─────────────────────────────
        private static async Task<string> EjecutarScriptPython(
            string rutaScript, string carpeta, string? contextoAnterior,
            string autoVariablesJson, CancellationToken ct,
            string? nodoContextoJson = null)
        {
            // Limpiar respuesta.txt antes (best-effort: archivo bloqueado o permisos
            // no son fatales — el script siempre puede caer al stdout como fallback).
            string respTxtPath = Path.Combine(carpeta, "respuesta.txt");
            try { File.WriteAllText(respTxtPath, "", Encoding.UTF8); }
            catch (IOException)        { /* el archivo está siendo usado, lo veremos vacío */ }
            catch (UnauthorizedAccessException) { /* sin permisos: usaremos stdout */ }

            var psi = new ProcessStartInfo
            {
                FileName               = EncontrarPythonAbsoluto(),
                Arguments              = $"\"{rutaScript}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                WorkingDirectory       = carpeta,
                // ── Forzar UTF-8 para evitar UnicodeEncodeError en consola Windows ──
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8
            };

            // Variables de entorno: UTF-8 + contexto anterior + contexto estructurado
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            psi.EnvironmentVariables["PYTHONUTF8"]       = "1";

            // Backward compat: salida cruda del último predecesor
            if (!string.IsNullOrWhiteSpace(contextoAnterior))
                psi.EnvironmentVariables["NODO_ANTERIOR_RESULTADO"] = contextoAnterior;

            // Variables globales de la automatización (JSON serializado)
            if (!string.IsNullOrWhiteSpace(autoVariablesJson) && autoVariablesJson != "{}")
                psi.EnvironmentVariables["AUTO_VARIABLES"] = autoVariablesJson;

            // Contexto estructurado de los predecesores (JSON serializado)
            if (!string.IsNullOrWhiteSpace(nodoContextoJson))
                psi.EnvironmentVariables["NODO_CONTEXTO"] = nodoContextoJson;

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) sbOut.AppendLine(e.Data); };
            proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) sbErr.AppendLine(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync(ct);

            // Leer respuesta.txt (fuente principal de resultado)
            string respuesta = "";
            if (File.Exists(respTxtPath))
                respuesta = File.ReadAllText(respTxtPath, Encoding.UTF8).Trim();

            // Si no hay respuesta.txt, usar stdout
            if (string.IsNullOrWhiteSpace(respuesta))
                respuesta = sbOut.ToString().Trim();

            if (proc.ExitCode != 0 && sbErr.Length > 0)
                throw new Exception($"Script falló (exit {proc.ExitCode}): {sbErr}");

            return respuesta;
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static string ObtenerCarpeta(Automatizacion auto)
        {
            if (string.IsNullOrEmpty(auto.CarpetaScripts))
            {
                string basePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Automatizaciones", auto.Id);
                auto.CarpetaScripts = basePath;
            }
            if (!Directory.Exists(auto.CarpetaScripts))
                Directory.CreateDirectory(auto.CarpetaScripts);
            return auto.CarpetaScripts;
        }

        public static string ObtenerCarpetaAuto(Automatizacion auto) => ObtenerCarpeta(auto);

        private static string Truncar(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";

        /// <summary>Busca la ruta absoluta de python.exe (Task Scheduler no hereda PATH).</summary>
        private static string EncontrarPythonAbsoluto()
        {
            try
            {
                var psi = new ProcessStartInfo("where", "python")
                {
                    CreateNoWindow = true, UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                string? salida = p?.StandardOutput.ReadToEnd();
                p?.WaitForExit(5000);
                if (p?.ExitCode == 0 && !string.IsNullOrWhiteSpace(salida))
                {
                    foreach (string linea in salida.Split('\n', '\r'))
                    {
                        string ruta = linea.Trim();
                        if (ruta.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(ruta))
                            return ruta;
                    }
                }
            }
            catch
            {
                // 'where' puede no existir o bloquearse; caemos a candidatos hardcoded.
            }

            string[] candidatos =
            {
                @"C:\Python312\python.exe", @"C:\Python311\python.exe",
                @"C:\Python310\python.exe", @"C:\Python39\python.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Python\Python312\python.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Python\Python311\python.exe"),
            };
            foreach (var c in candidatos)
                if (File.Exists(c)) return c;

            return "python";
        }

        public void Dispose()
        {
            Detener();
            _cts.Dispose();
        }
    }
}
