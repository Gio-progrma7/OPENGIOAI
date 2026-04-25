// ============================================================
//  AIModelConector.cs  — VERSIÓN FINAL (Pasos 1, 2, 3, 4)
//
//  PASO 1: PromtAgente eliminado como estado global mutable.
//          Todos los métodos reciben AgentContext como parámetro.
//          El prompt viaja con el contexto, nunca se "restaura".
//
//  PASO 2: HttpClient singleton por proveedor.
//          Antes: new HttpClient() en cada llamada → socket exhaustion.
//          Ahora: static readonly HttpClient reutilizado → una sola
//          conexión TCP por proveedor, headers por HttpRequestMessage.
//
//  PASO 4: RetryPolicy integrado en ObtenerRespuestaAPIAsync y
//          ObtenerRespuestaOllamaAsync. Errores 429/5xx se reintentan
//          hasta 3 veces con backoff exponencial + jitter.
//          LlmErrorException reemplaza el return "Error 429: ..." para
//          que RetryPolicy pueda leer el código HTTP y decidir.
// ============================================================

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OPENGIOAI.Entidades;
using OPENGIOAI.Herramientas;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.Utilerias;
using System.Net.Http.Headers;
using System.Text;

namespace OPENGIOAI.Data
{
    public static class AIModelConector
    {
        // =====================================================================
        //  PASO 2: HttpClient singleton por proveedor
        //  Un solo cliente por dominio. Nunca se dispone.
        //  Headers de auth van en HttpRequestMessage (thread-safe).
        // =====================================================================
        private static readonly HttpClient _clienteGeneral = new()
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        private static readonly HttpClient _clienteStreaming = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        private static readonly HttpClient _clienteOllama = new()
        {
            Timeout = TimeSpan.FromMinutes(3),
            BaseAddress = new Uri("http://localhost:11434")
        };

        // ── Token + Project ID cache para Antigravity (gcloud ADC) ──────────────
        // Token caduca ~60 min → cacheamos 55 min para renovar antes del vencimiento.
        // Project ID se detecta automáticamente desde "gcloud config get-value project".
        private static string   _antigravityToken       = "";
        private static DateTime _antigravityTokenExpiry = DateTime.MinValue;
        private static string   _antigravityProjectId   = "";   // auto-detectado
        private static readonly SemaphoreSlim _antigravityTokenLock = new(1, 1);

        /// <summary>
        /// Devuelve un token ADC vigente. Si expiró lo renueva.
        /// Aprovecha la misma llamada para refrescar el Project ID cacheado.
        /// </summary>
        private static async Task<string> ObtenerTokenAntigravityAsync()
        {
            // Fast-path: token vigente
            if (!string.IsNullOrWhiteSpace(_antigravityToken) &&
                DateTime.UtcNow < _antigravityTokenExpiry)
                return _antigravityToken;

            await _antigravityTokenLock.WaitAsync();
            try
            {
                // Segunda comprobación dentro del lock (double-checked)
                if (!string.IsNullOrWhiteSpace(_antigravityToken) &&
                    DateTime.UtcNow < _antigravityTokenExpiry)
                    return _antigravityToken;

                // Obtener token y project ID en paralelo
                var tokenTask   = ServiciosAI.AIServicios.ObtenerTokenGcloudAsync();
                var projectTask = ServiciosAI.AIServicios.ObtenerProyectoGcloudAsync();
                await Task.WhenAll(tokenTask, projectTask);

                _antigravityToken       = tokenTask.Result;
                _antigravityTokenExpiry = DateTime.UtcNow.AddMinutes(55);

                // Actualizar project ID solo si se obtuvo uno válido
                if (!string.IsNullOrWhiteSpace(projectTask.Result))
                    _antigravityProjectId = projectTask.Result;

                return _antigravityToken;
            }
            finally
            {
                _antigravityTokenLock.Release();
            }
        }

        /// <summary>
        /// Devuelve el Project ID a usar para Vertex AI.
        /// Prioridad: 1) ApiKey del contexto  2) Project ID auto-detectado de gcloud.
        /// </summary>
        internal static async Task<string> ObtenerProjectIdAntigravityAsync(string apiKeyContexto)
        {
            // Si el contexto ya trae un project ID válido, usarlo
            if (!string.IsNullOrWhiteSpace(apiKeyContexto))
            {
                string pid = apiKeyContexto.Contains(':')
                    ? apiKeyContexto.Split(':', 2)[0]
                    : apiKeyContexto;
                if (EsProjectIdValido(pid)) return apiKeyContexto; // devolver completo (puede tener :region)
            }

            // Si no, intentar usar el cacheado
            if (!string.IsNullOrWhiteSpace(_antigravityProjectId))
                return _antigravityProjectId;

            // Último recurso: pedir a gcloud ahora mismo
            string detectado = await ServiciosAI.AIServicios.ObtenerProyectoGcloudAsync();
            if (!string.IsNullOrWhiteSpace(detectado))
            {
                _antigravityProjectId = detectado;
                return detectado;
            }

            return "";
        }

        // Valida que un string tenga la forma básica de un GCP Project ID
        // (letras minúsculas, números, guiones; 6-30 caracteres)
        private static bool EsProjectIdValido(string s) =>
            !string.IsNullOrWhiteSpace(s) &&
            s.Length >= 6 && s.Length <= 30 &&
            System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-z0-9][a-z0-9\-]*[a-z0-9]$");

        // =====================================================================
        //  PUNTO DE ENTRADA PRINCIPAL — Agente 1 (generador)
        // =====================================================================

        /// <summary>
        /// Construye el AgentContext (PASO 1), obtiene el código Python
        /// del LLM (PASO 4: con retry), lo guarda y lo ejecuta.
        /// </summary>
        public static async Task<string> EjecutarInstruccionIAAsync(
            string instruccion,
            string modelo = "",
            string rutaArchivo = "",
            string apiKey = "",
            string clavesDisponibles = "",
            bool soloChat = false,
            Servicios servicio = Servicios.Gemenni,
            CancellationToken ct = default,
            Action? onInicioScript = null,
            Action<string>? onSalidaScript = null)
        {
            ct.ThrowIfCancellationRequested();

            // PASO 1: contexto inmutable por ejecución.
            // Lee los .md del disco una sola vez y arma el prompt efectivo.
            AgentContext ctx = await AgentContext.BuildAsync(
                rutaArchivo, modelo, apiKey, servicio,
                soloChat, clavesDisponibles, ct);

            ct.ThrowIfCancellationRequested();

            string codigoPython = await ObtenerRespuestaLLMAsync(instruccion, ctx, ct);

            ct.ThrowIfCancellationRequested();

            return await GenerarScriptIA(codigoPython, rutaArchivo, soloChat, ct,
                onInicioScript, onSalidaScript);
        }

        // =====================================================================
        //  PUNTO DE ENTRADA — Agente 2 (validador / bienvenida)
        // =====================================================================

        /// <summary>
        /// Versión para el Agente 2.
        /// ComoAgente2() devuelve una copia inmutable del contexto con el
        /// prompt de error/respuesta como base — sin mutar nada global.
        /// </summary>
        public static async Task<string> EjecutarInstruccionIAAsyncRespuesta(
            string instruccion,
            string codigoRespuesta,
            string respuestaTxt = "",
            string modelo = "",
            string rutaArchivo = "",
            string apiKey = "",
            bool conInstruccion = false,
            Servicios servicio = Servicios.Gemenni,
            CancellationToken ct = default,
            Action<string>? onTokenReceived = null)
        {
            ct.ThrowIfCancellationRequested();

            AgentContext ctxBase = await AgentContext.BuildAsync(
                rutaArchivo, modelo, apiKey, servicio,
                soloChat: false, clavesDisponibles: "", ct);

            // PASO 1: copia inmutable — sin restauración de static.
            AgentContext ctx = conInstruccion
                ? ctxBase.ComoAgente2()
                : ctxBase.ComoInicio();

            string promptFinal = conInstruccion
                ? ConstruirPromptValidacion(instruccion, codigoRespuesta, respuestaTxt)
                : "Hola, recomiéndame qué podemos hacer hoy";

            if (onTokenReceived == null)
                return await ObtenerRespuestaLLMAsync(promptFinal, ctx, ct);

            // Modo streaming
            var sb = new StringBuilder();
            await ObtenerRespuestaStreamingAsync(
                promptFinal, ctx,
                token => { sb.Append(token); onTokenReceived(token); },
                ct);

            return sb.ToString();
        }

        // =====================================================================
        //  PUNTO DE ENTRADA — Agente con Herramientas (Tool Use)
        // =====================================================================

        /// <summary>
        /// Ejecuta el agente en modo Tool Use.
        /// El LLM puede llamar herramientas (leer archivos, HTTP, comandos, etc.)
        /// en un bucle ReAct hasta producir una respuesta final.
        ///
        /// Proveedores soportados con tools nativos:
        ///   OpenAI, Claude, Gemini, DeepSeek, OpenRouter, Ollama (modelos compatibles).
        /// </summary>
        /// <param name="instruccion">Instrucción del usuario en lenguaje natural.</param>
        /// <param name="onProgreso">Callback opcional invocado en cada paso del bucle.</param>
        /// <param name="maxIteraciones">Máximo de ciclos Pensar→Actuar. Por defecto 10.</param>
        public static async Task<string> EjecutarConHerramientasAsync(
            string instruccion,
            string modelo = "",
            string rutaArchivo = "",
            string apiKey = "",
            string clavesDisponibles = "",
            bool soloChat = false,
            Servicios servicio = Servicios.ChatGpt,
            Action<string>? onProgreso = null,
            CancellationToken ct = default,
            int maxIteraciones = 10)
        {
            ct.ThrowIfCancellationRequested();

            AgentContext ctx = await AgentContext.BuildAsync(
                rutaArchivo, modelo, apiKey, servicio,
                soloChat, clavesDisponibles, ct);

            return await MotorHerramientas.EjecutarConHerramientasAsync(
                instruccion, ctx, onProgreso, ct, maxIteraciones);
        }

        // =====================================================================
        //  PUNTO DE ENTRADA — 1.3 Agente con Planificación (ReAct / CoT)
        // =====================================================================

        /// <summary>
        /// Ejecuta la instrucción usando planificación explícita ReAct/CoT.
        /// El agente descompone la tarea en pasos, ejecuta cada uno,
        /// verifica el resultado y adapta el plan si algo falla.
        /// Bucle: Pensar → Actuar → Observar → Repetir.
        /// </summary>
        /// <param name="onProgreso">Callback para recibir actualizaciones en tiempo real.</param>
        public static async Task<string> EjecutarConPlanificacionAsync(
            string instruccion,
            string modelo = "",
            string rutaArchivo = "",
            string apiKey = "",
            string clavesDisponibles = "",
            bool soloChat = false,
            Servicios servicio = Servicios.Gemenni,
            Action<string>? onProgreso = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            AgentContext ctx = await AgentContext.BuildAsync(
                rutaArchivo, modelo, apiKey, servicio,
                soloChat, clavesDisponibles, ct);

            return await OPENGIOAI.Agentes.AgentePlanificador.EjecutarConPlanificacionAsync(
                instruccion, ctx, onProgreso, ct);
        }

        // =====================================================================
        //  PUNTO DE ENTRADA — 1.4 Pipeline Multi-Agente Avanzado
        // =====================================================================

        /// <summary>
        /// Ejecuta la instrucción a través del pipeline de 4 agentes especializados:
        ///   1. Planificador  — descompone la tarea en un plan detallado
        ///   2. Ejecutor      — implementa el plan con código/acciones
        ///   3. Verificador   — valida el output del ejecutor
        ///   4. Formateador   — produce la respuesta final pulida
        /// Cada agente recibe el output del anterior como contexto.
        /// </summary>
        /// <param name="onProgreso">Callback para recibir actualizaciones de cada agente.</param>
        public static async Task<OPENGIOAI.Agentes.ResultadoPipeline> EjecutarPipelineMultiAgenteAsync(
            string instruccion,
            string modelo = "",
            string rutaArchivo = "",
            string apiKey = "",
            string clavesDisponibles = "",
            bool soloChat = false,
            Servicios servicio = Servicios.Gemenni,
            Action<string>? onProgreso = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            AgentContext ctx = await AgentContext.BuildAsync(
                rutaArchivo, modelo, apiKey, servicio,
                soloChat, clavesDisponibles, ct);

            return await OPENGIOAI.Agentes.PipelineMultiAgente.EjecutarPipelineAsync(
                instruccion, ctx, onProgreso, ct);
        }

        // =====================================================================
        //  PARALELO
        // =====================================================================

        public static async Task<List<string>> EjecutarIAEnParaleloAsync(
            List<ConfiguracionIA> configuraciones)
        {
            var tareas = configuraciones.Select(c =>
                EjecutarInstruccionIAAsync(
                    c.Instruccion, c.Modelo, c.RutaArchivo,
                    c.ApiKey, c.ClavesKeyConfiguradas, c.Chat, c.Servicio));

            return (await Task.WhenAll(tareas)).ToList();
        }

        // =====================================================================
        //  CAPA LLM — enruta al proveedor correcto
        // =====================================================================

        // internal para que AgentePlanificador y PipelineMultiAgente puedan usarlo
        // sin duplicar la lógica de enrutamiento por proveedor.
        internal static async Task<string> ObtenerRespuestaLLMAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            // Tracing (Fase 1A): cada hit al LLM es un span — se cuelga de la fase
            // activa (Analista/Constructor/Planificador/...) vía AsyncLocal.
            // Si no hay trace activo, AbrirSpan devuelve un no-op transparente.
            using var span = OPENGIOAI.Utilerias.TracerEjecucion.Instancia.AbrirSpan(
                OPENGIOAI.Entidades.SpanTipo.LlamadaLLM,
                $"LLM:{ctx.Servicio}/{(string.IsNullOrEmpty(ctx.Modelo) ? "default" : ctx.Modelo)}");
            span.AgregarAtributo("servicio", ctx.Servicio.ToString());
            span.AgregarAtributo("modelo", ctx.Modelo);
            span.AgregarAtributo("fase", ctx.NombreFase);
            span.RegistrarInput(instruccion);

            try
            {
                string resultado = ctx.Servicio switch
                {
                    Servicios.Ollama      => await ObtenerRespuestaOllamaAsync(instruccion, ctx, ct),
                    Servicios.Antigravity => await ObtenerRespuestaAntigravityAsync(instruccion, ctx, ct),
                    _                     => await ObtenerRespuestaAPIAsync(instruccion, ctx, ct)
                };
                span.RegistrarOutput(resultado);
                return resultado;
            }
            catch (OperationCanceledException)
            {
                span.MarcarCancelado();
                throw;
            }
            catch (Exception ex)
            {
                span.MarcarError(ex.Message);
                throw;
            }
        }

        // =====================================================================
        //  LLAMADA HTTP — APIs externas (OpenAI, DeepSeek, Gemini, Claude, OpenRouter)
        //
        //  PASO 2: usa _clienteGeneral (singleton).
        //  PASO 4: envuelto con RetryPolicy — reintenta en 429 / 5xx.
        //          LlmErrorException en lugar de return string de error para
        //          que RetryPolicy pueda leer el HttpStatusCode.
        // =====================================================================

        private static Task<string> ObtenerRespuestaAPIAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            return RetryPolicy.EjecutarAsync(
                operacion: async () =>
                {
                    var (apiUrl, body) = ConstruirRequest(instruccion, ctx);

                    using var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                    AgregarHeaders(req, ctx);
                    req.Content = new StringContent(
                        JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                    var resp = await _clienteGeneral.SendAsync(req, ct);
                    string raw = await resp.Content.ReadAsStringAsync(ct);

                    // PASO 4: lanzar excepción tipada para que RetryPolicy
                    // identifique si el error es reintentable (429, 5xx).
                    // Antes era: return $"Error {resp.StatusCode}: {raw}";
                    // Con ese return, RetryPolicy nunca veía el error.
                    if (!resp.IsSuccessStatusCode)
                       

                    await AIServicios.MostrarConsumoTokens(ctx.Servicio, raw, ctx.NombreFase, ctx.Modelo);

                    return ExtraerContenido(ctx.Servicio, raw);
                },
                ct: ct,
                onReintento: (intento, ex, espera) =>
                    System.Diagnostics.Debug.WriteLine(
                        $"[Retry] Intento {intento} — {ex.Message} " +
                        $"— próximo en {espera.TotalSeconds:F1}s")
            );
        }

        // =====================================================================
        //  LLAMADA HTTP — Ollama local
        //
        //  PASO 2: usa _clienteOllama (singleton con BaseAddress).
        //  PASO 4: RetryPolicy cubre el caso de Ollama cargando modelo (500).
        // =====================================================================

        private static Task<string> ObtenerRespuestaOllamaAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            return RetryPolicy.EjecutarAsync(
                operacion: async () =>
                {
                    var body = new
                    {
                        model = ctx.Modelo,
                        prompt = ctx.PromptEfectivo + "\nInstruccion: " + instruccion
                    };

                    using var req = new HttpRequestMessage(
                        HttpMethod.Post, "/api/generate");
                    req.Content = new StringContent(
                        JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                    var resp = await _clienteOllama.SendAsync(req, ct);

                    if (!resp.IsSuccessStatusCode)
                    {
                        string err = await resp.Content.ReadAsStringAsync(ct);
                        throw new LlmErrorException(
                            $"Ollama {(int)resp.StatusCode}: {err}", resp.StatusCode);
                    }

                    string raw = await resp.Content.ReadAsStringAsync(ct);

                    var sb = new StringBuilder();
                    foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var root = JsonConvert.DeserializeObject<JObject>(line);
                            if (root?.TryGetValue("response", out var r) == true) sb.Append(r);
                            if (root?.TryGetValue("done", out var d) == true &&
                                d.Value<bool>()) break;
                        }
                        catch { /* línea inválida, ignorar */ }
                    }

                    // Telemetría (Fase A): Ollama reporta tokens en la última línea NDJSON.
                    await AIServicios.MostrarConsumoTokens(ctx.Servicio, raw, ctx.NombreFase, ctx.Modelo);

                    return Limpiar(sb.ToString());
                },
                ct: ct,
                onReintento: (intento, ex, espera) =>
                    System.Diagnostics.Debug.WriteLine(
                        $"[Retry/Ollama] Intento {intento} — {ex.Message} " +
                        $"— próximo en {espera.TotalSeconds:F1}s")
            );
        }

        // =====================================================================
        //  LLAMADA HTTP — Antigravity (Google Vertex AI con gcloud ADC)
        //
        //  La URL sigue el formato:
        //    https://{region}-aiplatform.googleapis.com/v1/projects/{projectId}/
        //      locations/{region}/publishers/google/models/{model}:generateContent
        //
        //  El formato de request/response es idéntico a Gemini API.
        //  ctx.ApiKey almacena el Project ID de GCP (ej. "my-project-123").
        //  Opcionalmente "projectId:region" para sobrescribir la región.
        // =====================================================================

        private static Task<string> ObtenerRespuestaAntigravityAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            return RetryPolicy.EjecutarAsync(
                operacion: async () =>
                {
                    string token = await ObtenerTokenAntigravityAsync();
                    if (string.IsNullOrWhiteSpace(token))
                        throw new InvalidOperationException(
                            "No se pudo obtener el token de gcloud. " +
                            "Ejecuta: gcloud auth application-default login");

                    // Resolver el Project ID (contexto → cache → gcloud)
                    string projectIdResuelto = await ObtenerProjectIdAntigravityAsync(ctx.ApiKey);
                    if (string.IsNullOrWhiteSpace(projectIdResuelto))
                        throw new InvalidOperationException(
                            "No se encontró un Project ID de GCP. " +
                            "Configúralo en Proveedores → Antigravity → PROJECT ID, " +
                            "o ejecuta: gcloud config set project TU-PROYECTO");

                    var (apiUrl, body) = ConstruirRequestAntigravity(instruccion, ctx, projectIdResuelto);

                    using var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                    req.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                    req.Content = new StringContent(
                        JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                    var resp = await _clienteGeneral.SendAsync(req, ct);
                    string raw = await resp.Content.ReadAsStringAsync(ct);

                    if (!resp.IsSuccessStatusCode)
                        throw new LlmErrorException(
                            $"Antigravity {(int)resp.StatusCode}: {raw}", resp.StatusCode);

                    await AIServicios.MostrarConsumoTokens(ctx.Servicio, raw, ctx.NombreFase, ctx.Modelo);
                    // Vertex AI usa el mismo formato de respuesta que Gemini
                    return ExtraerContenido(Servicios.Gemenni, raw);
                },
                ct: ct,
                onReintento: (intento, ex, espera) =>
                    System.Diagnostics.Debug.WriteLine(
                        $"[Retry/Antigravity] Intento {intento} — {ex.Message} " +
                        $"— próximo en {espera.TotalSeconds:F1}s")
            );
        }

        /// <summary>
        /// Construye la URL y el body para Vertex AI.
        /// projectIdResuelto puede tener formato "projectId" o "projectId:region".
        /// </summary>
        private static (string url, object body) ConstruirRequestAntigravity(
            string instruccion, AgentContext ctx, string? projectIdResuelto = null)
        {
            // Descomponer "projectId" o "projectId:region"
            string raw      = projectIdResuelto ?? ctx.ApiKey;
            string projectId;
            string region;

            if (!string.IsNullOrWhiteSpace(raw) && raw.Contains(':'))
            {
                var parts = raw.Split(':', 2);
                projectId = parts[0].Trim();
                region    = parts[1].Trim();
            }
            else
            {
                projectId = raw?.Trim() ?? "";
                region    = "us-central1";
            }

            // Modelo: usar el configurado o el más reciente por defecto
            string modelo = string.IsNullOrEmpty(ctx.Modelo)
                ? "gemini-2.0-flash-001"
                : ctx.Modelo;

            // Vertex AI requiere solo el nombre del modelo sin prefijos
            // (si viene "publishers/google/models/gemini-x" → limpiar)
            if (modelo.Contains('/'))
                modelo = modelo.Split('/').Last();

            string url = $"https://{region}-aiplatform.googleapis.com/v1/projects/{projectId}" +
                         $"/locations/{region}/publishers/google/models/{modelo}:generateContent";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        role  = "user",
                        parts = new[] { new { text = ctx.PromptEfectivo + "\nInstruccion: " + instruccion } }
                    }
                },
                generationConfig = new { temperature = 0.7 }
            };

            return (url, body);
        }

        // =====================================================================
        //  STREAMING
        // =====================================================================

        public static async Task ObtenerRespuestaStreamingAsync(
            string instruccion,
            AgentContext ctx,
            Action<string> onToken,
            CancellationToken ct = default)
        {
            // Antigravity: streaming via Vertex AI (mismo formato SSE que Gemini)
            if (ctx.Servicio == Servicios.Antigravity)
            {
                await ObtenerRespuestaStreamingAntigravityAsync(instruccion, ctx, onToken, ct);
                return;
            }

            bool esOllama = ctx.Servicio == Servicios.Ollama;

            var (apiUrl, bodyObj) = esOllama
                ? ConstruirRequestOllamaStream(instruccion, ctx)
                : ConstruirRequestStream(instruccion, ctx);

            using var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            if (!esOllama) AgregarHeaders(req, ctx);
            req.Content = new StringContent(
                JsonConvert.SerializeObject(bodyObj), Encoding.UTF8, "application/json");

            var resp = await _clienteStreaming.SendAsync(
                req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!resp.IsSuccessStatusCode)
                throw new LlmErrorException(
                    await resp.Content.ReadAsStringAsync(ct), resp.StatusCode);

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string? token = esOllama
                    ? ExtraerTokenOllama(line, out _)
                    : ExtraerTokenSSE(line, ctx.Servicio);

                if (token != null) onToken(token);

                if (esOllama)
                {
                    var root = JsonConvert.DeserializeObject<JObject>(line);
                    if (root?["done"]?.Value<bool>() == true) break;
                }
                else if (line.TrimStart().StartsWith("data:") && line.Contains("[DONE]"))
                    break;
            }
        }

        /// <summary>
        /// Streaming para Vertex AI (Antigravity).
        /// Vertex devuelve la respuesta completa en JSON (no SSE), por lo que
        /// hacemos la llamada normal y emitimos el texto en un único token.
        /// Si Vertex añade soporte SSE en el futuro, aquí es donde se extiende.
        /// </summary>
        private static async Task ObtenerRespuestaStreamingAntigravityAsync(
            string instruccion,
            AgentContext ctx,
            Action<string> onToken,
            CancellationToken ct)
        {
            string token = await ObtenerTokenAntigravityAsync();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException(
                    "No se pudo obtener el token de gcloud. " +
                    "Ejecuta: gcloud auth application-default login");

            string projectIdResuelto = await ObtenerProjectIdAntigravityAsync(ctx.ApiKey);
            if (string.IsNullOrWhiteSpace(projectIdResuelto))
                throw new InvalidOperationException(
                    "No se encontró un Project ID de GCP. " +
                    "Configúralo en Proveedores → Antigravity → PROJECT ID.");

            var (apiUrl, body) = ConstruirRequestAntigravity(instruccion, ctx, projectIdResuelto);

            // Vertex AI soporta streaming SSE añadiendo ?alt=sse a la URL
            string streamUrl = apiUrl.Contains('?')
                ? apiUrl + "&alt=sse"
                : apiUrl + "?alt=sse";

            using var req = new HttpRequestMessage(HttpMethod.Post, streamUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(
                JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var resp = await _clienteStreaming.SendAsync(
                req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!resp.IsSuccessStatusCode)
                throw new LlmErrorException(
                    await resp.Content.ReadAsStringAsync(ct), resp.StatusCode);

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Vertex SSE: "data: {json}" donde json tiene el mismo schema que Gemini
                string? chunk = ExtraerTokenSSE(line, Servicios.Gemenni);
                if (chunk != null) onToken(chunk);
            }
        }

        // =====================================================================
        //  CONSTRUCCIÓN DE REQUESTS
        // =====================================================================

        /// <summary>
        /// Construye el campo <c>system</c> de la API de Anthropic como un
        /// array de bloques de texto, marcando el prefijo estable con
        /// <c>cache_control: ephemeral</c> para activar prompt caching.
        ///
        /// RESULTADO POSIBLE:
        ///   · Dos bloques: [estable con cache_control, variable sin cache].
        ///   · Un bloque: solo estable o solo variable (según disponibilidad).
        ///   · Fallback string: si el contexto no particionó el prompt
        ///     (legacy), enviamos el PromptEfectivo como string plano —
        ///     Anthropic acepta ambos formatos.
        ///
        /// NOTAS:
        ///   · Anthropic exige mínimo 1024 tokens (≈4 KB) en el bloque para
        ///     activar el caché. Si el estable es muy corto, cache_control
        ///     es silenciosamente ignorado — no es un error.
        ///   · TTL del caché: 5 minutos desde el último hit.
        /// </summary>
        private static object ConstruirSystemClaude(AgentContext ctx)
        {
            string estable  = ctx.PromptEstable  ?? "";
            string variable = ctx.PromptVariable ?? "";

            // Retrocompatibilidad: contextos antiguos sin particionar.
            if (string.IsNullOrEmpty(estable) && string.IsNullOrEmpty(variable))
                return ctx.PromptEfectivo ?? "";

            var bloques = new List<object>();

            if (!string.IsNullOrEmpty(estable))
            {
                bloques.Add(new
                {
                    type = "text",
                    text = estable,
                    cache_control = new { type = "ephemeral" }
                });
            }

            if (!string.IsNullOrEmpty(variable))
            {
                bloques.Add(new
                {
                    type = "text",
                    text = variable,
                });
            }

            return bloques.ToArray();
        }

        private static (string url, object body) ConstruirRequest(
            string instruccion, AgentContext ctx)
        {
            string modelo = ctx.Modelo;
            string prompt = ctx.PromptEfectivo;

            return ctx.Servicio switch
            {
                Servicios.Gemenni => (
                    $"https://generativelanguage.googleapis.com/v1beta/models/" +
                    $"{(string.IsNullOrEmpty(modelo) ? "gemini-1.5-flash" : modelo)}" +
                    $":generateContent?key={ctx.ApiKey}",
                    (object)new
                    {
                        contents = new[]
                        {
                            new
                            {
                                role  = "user",
                                parts = new[] { new { text = prompt + "\nInstruccion: " + instruccion } }
                            }
                        },
                        generationConfig = new { temperature = 0.7 }
                    }
                ),

                Servicios.Claude => (
                    "https://api.anthropic.com/v1/messages",
                    (object)new
                    {
                        model = string.IsNullOrEmpty(modelo) ? "claude-3-haiku-20240307" : modelo,
                        max_tokens = 9000,
                        temperature = 1,
                        // Prompt caching (Fase 1): bloque estable marcado
                        // con cache_control → Anthropic lo reutiliza 5 min
                        // con ~90% descuento en tokens repetidos. El bloque
                        // variable (RAG retrievals) queda sin cache_control.
                        system = ConstruirSystemClaude(ctx),
                        messages = new[]
                        {
                            new { role = "user", content = new[] { new { type = "text", text = instruccion } } }
                        }
                    }
                ),

                Servicios.Deespeek => (
                    "https://api.deepseek.com/v1/chat/completions",
                    (object)new
                    {
                        model = string.IsNullOrEmpty(modelo) ? "deepseek-chat" : modelo,
                        messages = new[]
                        {
                            new { role = "system", content = prompt },
                            new { role = "user",   content = instruccion }
                        },
                        temperature = 1,
                        stream = false
                    }
                ),

                Servicios.OpenRouter => (
                    "https://openrouter.ai/api/v1/chat/completions",
                    (object)new
                    {
                        model = string.IsNullOrEmpty(modelo) ? "openai/gpt-4.1-mini" : modelo,
                        messages = new[]
                        {
                            new { role = "system", content = prompt },
                            new { role = "user",   content = instruccion }
                        },
                        temperature = 1
                    }
                ),

                _ => ( // ChatGPT y cualquier otro compatible con OpenAI
                    "https://api.openai.com/v1/chat/completions",
                    (object)new
                    {
                        model = string.IsNullOrEmpty(modelo) ? "gpt-3.5-turbo" : modelo,
                        messages = new[]
                        {
                            new { role = "system", content = prompt },
                            new { role = "user",   content = instruccion }
                        },
                        temperature = 1
                    }
                )
            };
        }

        private static (string url, object body) ConstruirRequestStream(
            string instruccion, AgentContext ctx)
        {
            string modelo = ctx.Modelo;
            string prompt = ctx.PromptEfectivo;

            return ctx.Servicio switch
            {
                Servicios.Gemenni => (
                    $"https://generativelanguage.googleapis.com/v1beta/models/" +
                    $"{(string.IsNullOrEmpty(modelo) ? "gemini-1.5-flash" : modelo)}" +
                    $":streamGenerateContent?key={ctx.ApiKey}&alt=sse",
                    (object)new
                    {
                        contents = new[]
                        {
                            new
                            {
                                role  = "user",
                                parts = new[] { new { text = prompt + "\nInstruccion: " + instruccion } }
                            }
                        },
                        generationConfig = new { temperature = 0.7 }
                    }
                ),

                Servicios.Claude => (
                    "https://api.anthropic.com/v1/messages",
                    (object)new
                    {
                        model = modelo,
                        max_tokens = 2048,
                        stream = true,
                        // Igual que el path no-stream: prefijo estable cacheado.
                        system = ConstruirSystemClaude(ctx),
                        messages = new[] { new { role = "user", content = instruccion } }
                    }
                ),

                _ => (
                    ConstruirRequest(instruccion, ctx).url,
                    (object)new
                    {
                        model = modelo,
                        messages = new[]
                        {
                            new { role = "system", content = prompt },
                            new { role = "user",   content = instruccion }
                        },
                        temperature = 1,
                        stream = true
                    }
                )
            };
        }

        private static (string url, object body) ConstruirRequestOllamaStream(
            string instruccion, AgentContext ctx) => (
            "http://localhost:11434/api/chat",
            (object)new
            {
                model = ctx.Modelo,
                messages = new[]
                {
                    new { role = "system", content = ctx.PromptEfectivo },
                    new { role = "user",   content = instruccion }
                },
                stream = true
            });

        // =====================================================================
        //  HEADERS — por request, no por cliente (thread-safe con singleton)
        // =====================================================================

        private static void AgregarHeaders(HttpRequestMessage req, AgentContext ctx)
        {
            switch (ctx.Servicio)
            {
                case Servicios.Claude:
                    req.Headers.Add("x-api-key", ctx.ApiKey);
                    req.Headers.Add("anthropic-version", "2023-06-01");
                    break;

                case Servicios.Gemenni:
                    // API key va en query param de la URL, no en headers
                    break;

                case Servicios.OpenRouter:
                    req.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", ctx.ApiKey);
                    req.Headers.Add("HTTP-Referer", "https://tuapp.local");
                    req.Headers.Add("X-Title", "OPENGIOAI");
                    break;

                default:
                    req.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", ctx.ApiKey);
                    break;
            }
        }

        // =====================================================================
        //  EXTRACCIÓN DE RESPUESTA
        // =====================================================================

        private static string ExtraerContenido(Servicios servicio, string raw)
        {
            var root = JsonConvert.DeserializeObject<JObject>(raw);

            string output = servicio switch
            {
                Servicios.Gemenni =>
                    root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "",
                Servicios.Claude =>
                    root?["content"]?[0]?["text"]?.ToString() ?? "",
                _ =>
                    root?["choices"]?[0]?["message"]?["content"]?.ToString() ?? ""
            };

            return Limpiar(output);
        }

        private static string? ExtraerTokenOllama(string line, out bool done)
        {
            done = false;
            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(line);
                done = root?["done"]?.Value<bool>() ?? false;
                return root?["message"]?["content"]?.ToString();
            }
            catch { return null; }
        }

        private static string? ExtraerTokenSSE(string line, Servicios servicio)
        {
            if (!line.StartsWith("data:")) return null;
            var json = line[5..].Trim();
            if (json == "[DONE]") return null;
            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(json);
                return servicio switch
                {
                    Servicios.Claude =>
                        root?["delta"]?["text"]?.ToString(),
                    Servicios.Gemenni =>
                        root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString(),
                    _ =>
                        root?["choices"]?[0]?["delta"]?["content"]?.ToString()
                };
            }
            catch { return null; }
        }

        private static string Limpiar(string s) =>
            s.Replace("```python", "")
             .Replace("```", "")
             .Replace("\ufeff", "")
             .Trim();

        // =====================================================================
        //  PROMPT AGENTE 2
        // =====================================================================

        private static string ConstruirPromptValidacion(
            string instruccion, string codigoRespuesta, string respuestaTxt)
        {
            return $@"INSTRUCCIÓN PRINCIPAL:
{instruccion}

CÓDIGO GENERADO POR AGENTE 1:
{codigoRespuesta}

CONTENIDO DE respuesta.txt:
{respuestaTxt}

REGLAS:
- Presenta el resultado de forma elegante. Puedes usar emojis moderadamente.
- Si hay errores: explica cómo solucionarlos. Si puedes corregirlos, hazlo.
- Si no hay respuesta válida, responde tú directamente.
- Prioriza siempre ayudar al usuario.";
        }

        // =====================================================================
        //  EJECUCIÓN DEL SCRIPT PYTHON
        // =====================================================================

        private static async Task<string> GenerarScriptIA(
            string script, string rutaArchivo, bool chat, CancellationToken ct,
            Action? onInicioScript = null,
            Action<string>? onLinea = null)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new Exception("La IA no devolvió ningún script válido.");

            string pythonFile = Path.Combine(rutaArchivo, "script_ia.py");
            GuardarScript(rutaArchivo, script);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{pythonFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (ScriptEstaEjecutandose(pythonFile))
                CerrarScriptSiEstaEjecutandose(pythonFile);

            if (chat)
            {
                onInicioScript?.Invoke();

                var sbSalida = new StringBuilder();

                // ── Watcher sobre respuesta.txt para mostrar cambios en tiempo real ──
                string respuestaTxtPath = Path.Combine(rutaArchivo, "respuesta.txt");
                long _ultimaPosRespuesta = 0;
                // Limpiar el archivo antes de empezar para no acumular ejecuciones anteriores
                if (File.Exists(respuestaTxtPath))
                    try { File.WriteAllText(respuestaTxtPath, "", Encoding.UTF8); } catch { }

                FileSystemWatcher? watcher = null;
                if (onLinea != null)
                {
                    watcher = new FileSystemWatcher(rutaArchivo)
                    {
                        Filter = "respuesta.txt",
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                        EnableRaisingEvents = false
                    };
                    watcher.Changed += (_, _) =>
                    {
                        try
                        {
                            using var fs = new FileStream(
                                respuestaTxtPath, FileMode.Open,
                                FileAccess.Read, FileShare.ReadWrite);
                            fs.Seek(_ultimaPosRespuesta, SeekOrigin.Begin);
                            using var reader = new StreamReader(fs, Encoding.UTF8);
                            string nuevaData = reader.ReadToEnd();
                            _ultimaPosRespuesta = fs.Position;
                            if (!string.IsNullOrEmpty(nuevaData))
                                onLinea(nuevaData);
                        }
                        catch { /* ignorar errores de acceso simultáneo */ }
                    };
                }

                using var process = new System.Diagnostics.Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null) return;
                    sbSalida.AppendLine(e.Data);
                    onLinea?.Invoke(e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data == null) return;
                    string linea = $"[ERR] {e.Data}";
                    sbSalida.AppendLine(linea);
                    onLinea?.Invoke(linea);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (watcher != null)
                    watcher.EnableRaisingEvents = true;

                await process.WaitForExitAsync(ct);

                watcher?.Dispose();

                string salida = sbSalida.ToString().Trim();
                return process.ExitCode != 0
                    ? script + "\n\nError al ejecutar el script:\n" + salida
                    : string.IsNullOrEmpty(salida)
                        ? script
                        : script + "\n\nSalida del script:\n" + salida;
            }

            _ = Task.Run(() =>
                System.Diagnostics.Process.Start(psi)?.WaitForExit(), ct);

            return script;
        }

        private static void GuardarScript(string rutaArchivo, string script)
        {
            if (!Directory.Exists(rutaArchivo))
                Directory.CreateDirectory(rutaArchivo);

            File.WriteAllText(
                Path.Combine(rutaArchivo, "script_ia.py"), script, Encoding.UTF8);
        }

        // =====================================================================
        //  GESTIÓN DE PROCESOS PYTHON
        // =====================================================================

        public static bool ScriptEstaEjecutandose(string rutaScript) =>
            BuscarProcesoPython(rutaScript) != null;

        public static bool CerrarScriptSiEstaEjecutandose(string rutaScript)
        {
            var proc = BuscarProcesoPython(rutaScript);
            if (proc == null) return false;
            proc.Kill(true);
            proc.WaitForExit();
            return true;
        }

        private static System.Diagnostics.Process? BuscarProcesoPython(string rutaScript)
        {
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("python"))
            {
                try
                {
                    using var s = new System.Management.ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {p.Id}");

                    foreach (System.Management.ManagementObject o in s.Get())
                    {
                        string? cmd = o["CommandLine"]?.ToString();
                        if (!string.IsNullOrEmpty(cmd) &&
                            cmd.Contains(rutaScript, StringComparison.OrdinalIgnoreCase))
                            return p;
                    }
                }
                catch { /* proceso protegido, ignorar */ }
            }
            return null;
        }
    }
}