// ============================================================
//  MotorHerramientas.cs  — Bucle Agéntico con Tool Use
//
//  PATRÓN ReAct: Pensar → Llamar Herramienta → Observar → Repetir
//
//  Proveedores soportados:
//    OpenAI / ChatGPT  → formato estándar tool_calls
//    Claude (Anthropic) → formato tool_use / tool_result
//    Gemini (Google)   → formato functionCall / functionResponse
//    DeepSeek          → compatible OpenAI
//    OpenRouter        → compatible OpenAI
//    Ollama            → compatible OpenAI (modelos que soporten tools)
//
//  Flujo por iteración:
//    1. Construir request con definiciones de herramientas
//    2. Enviar al LLM
//    3. Parsear respuesta → RespuestaLLM
//    4. Si sin tool_calls → devolver texto final
//    5. Ejecutar cada herramienta solicitada
//    6. Agregar resultados al historial de mensajes
//    7. Repetir hasta maxIteraciones
// ============================================================

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using System.Net.Http.Headers;
using System.Text;

namespace OPENGIOAI.Herramientas
{
    public static class MotorHerramientas
    {
        // Registro global de herramientas (lazy init, thread-safe con Lazy<T>)
        private static readonly Lazy<RegistroHerramientas> _registro =
            new(RegistroHerramientas.CrearPorDefecto);

        // HttpClient dedicado para el motor de herramientas
        private static readonly HttpClient _cliente = new()
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        // =====================================================================
        //  PUNTO DE ENTRADA PÚBLICO
        // =====================================================================

        /// <summary>
        /// Ejecuta el agente con acceso completo a herramientas.
        /// El bucle continúa hasta que el LLM devuelva una respuesta
        /// sin tool_calls o se alcance el límite de iteraciones.
        /// </summary>
        /// <param name="instruccion">Instrucción del usuario.</param>
        /// <param name="ctx">Contexto inmutable del agente.</param>
        /// <param name="onProgreso">Callback opcional para progreso en tiempo real.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <param name="maxIteraciones">Máximo de ciclos Pensar→Actuar. Por defecto 10.</param>
        public static async Task<string> EjecutarConHerramientasAsync(
            string instruccion,
            AgentContext ctx,
            Action<string>? onProgreso = null,
            CancellationToken ct = default,
            int maxIteraciones = 10)
        {
            ct.ThrowIfCancellationRequested();

            var herramientas = _registro.Value.ObtenerTodas();
            var mensajes = InicializarMensajes(instruccion, ctx);

            for (int iteracion = 1; iteracion <= maxIteraciones; iteracion++)
            {
                ct.ThrowIfCancellationRequested();
                onProgreso?.Invoke($"[Iteración {iteracion}/{maxIteraciones}] Consultando {ctx.Servicio}...");

                // 1. Enviar al LLM con definiciones de herramientas
                string rawResponse = await EnviarConHerramientasAsync(mensajes, herramientas, ctx, ct);

                // 2. Parsear respuesta
                var respuesta = ExtraerRespuestaConHerramientas(rawResponse, ctx.Servicio);

                // 3. Sin tool_calls → respuesta final del agente
                if (!respuesta.TieneLlamadasHerramientas)
                {
                    onProgreso?.Invoke($"[Respuesta final en iteración {iteracion}]");
                    return respuesta.Texto;
                }

                onProgreso?.Invoke($"  → El LLM solicita {respuesta.LlamadasHerramientas.Count} herramienta(s)");

                // 4. Agregar respuesta del asistente (con tool_calls) al historial
                AgregarMensajeAsistente(mensajes, rawResponse, ctx.Servicio);

                // 5. Ejecutar cada herramienta y agregar resultados
                foreach (var llamada in respuesta.LlamadasHerramientas)
                {
                    ct.ThrowIfCancellationRequested();
                    onProgreso?.Invoke($"  → Ejecutando: {llamada.Nombre}({TruncatarJson(llamada.ArgumentosJson)})");

                    string resultado = await EjecutarHerramientaAsync(llamada, ct);
                    onProgreso?.Invoke($"  ✓ {llamada.Nombre}: {Truncar(resultado, 120)}");

                    AgregarResultadoHerramienta(mensajes, llamada, resultado, ctx.Servicio);
                }
            }

            return $"El agente alcanzó el límite de {maxIteraciones} iteraciones sin concluir.";
        }

        // =====================================================================
        //  INICIALIZACIÓN DE MENSAJES
        // =====================================================================

        private static List<JObject> InicializarMensajes(string instruccion, AgentContext ctx)
        {
            var mensajes = new List<JObject>();

            switch (ctx.Servicio)
            {
                case Servicios.Gemenni:
                    // Gemini: no tiene "system" en messages, el prompt va en la primera parte del user
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "user",
                        parts = new[] { new { text = ctx.PromptEfectivo + "\n\nInstrucción del usuario:\n" + instruccion } }
                    }));
                    break;

                case Servicios.Claude:
                    // Claude: system es property separada del request, messages solo tienen user/assistant
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "user",
                        content = new[] { new { type = "text", text = instruccion } }
                    }));
                    break;

                default:
                    // OpenAI, DeepSeek, OpenRouter, Ollama: sistema + usuario en messages
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "system",
                        content = ctx.PromptEfectivo
                    }));
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "user",
                        content = instruccion
                    }));
                    break;
            }

            return mensajes;
        }

        // =====================================================================
        //  ENVÍO DE REQUEST CON HERRAMIENTAS
        // =====================================================================

        private static async Task<string> EnviarConHerramientasAsync(
            List<JObject> mensajes,
            IReadOnlyCollection<IHerramienta> herramientas,
            AgentContext ctx,
            CancellationToken ct)
        {
            var (url, body) = ConstruirRequestConHerramientas(mensajes, herramientas, ctx);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            AgregarHeaders(req, ctx);
            req.Content = new StringContent(
                JsonConvert.SerializeObject(body, Formatting.None),
                Encoding.UTF8, "application/json");

            var resp = await _cliente.SendAsync(req, ct);
            string raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Error {(int)resp.StatusCode} de {ctx.Servicio}: {raw}");

            return raw;
        }

        // =====================================================================
        //  CONSTRUCCIÓN DE REQUESTS POR PROVEEDOR
        // =====================================================================

        private static (string url, object body) ConstruirRequestConHerramientas(
            List<JObject> mensajes,
            IReadOnlyCollection<IHerramienta> herramientas,
            AgentContext ctx)
        {
            string modelo = string.IsNullOrEmpty(ctx.Modelo) ? ModeloPorDefecto(ctx.Servicio) : ctx.Modelo;

            return ctx.Servicio switch
            {
                Servicios.Gemenni => ConstruirRequestGemini(mensajes, herramientas, ctx, modelo),
                Servicios.Claude => ConstruirRequestClaude(mensajes, herramientas, ctx, modelo),
                Servicios.Ollama => ConstruirRequestOllama(mensajes, herramientas, ctx),
                _ => ConstruirRequestOpenAI(mensajes, herramientas, ctx, modelo)
            };
        }

        // ── OpenAI / DeepSeek / OpenRouter ──────────────────────────────────

        private static (string url, object body) ConstruirRequestOpenAI(
            List<JObject> mensajes,
            IReadOnlyCollection<IHerramienta> herramientas,
            AgentContext ctx,
            string modelo)
        {
            string url = ctx.Servicio switch
            {
                Servicios.Deespeek => "https://api.deepseek.com/v1/chat/completions",
                Servicios.OpenRouter => "https://openrouter.ai/api/v1/chat/completions",
                _ => "https://api.openai.com/v1/chat/completions"
            };

            var tools = herramientas.Select(h => new
            {
                type = "function",
                function = new
                {
                    name = h.Nombre,
                    description = h.Descripcion,
                    parameters = h.EsquemaParametros
                }
            }).ToArray();

            var body = new
            {
                model = modelo,
                messages = mensajes,
                tools,
                tool_choice = "auto",
                temperature = 0.7
            };

            return (url, body);
        }

        // ── Claude (Anthropic) ───────────────────────────────────────────────

        private static (string url, object body) ConstruirRequestClaude(
            List<JObject> mensajes,
            IReadOnlyCollection<IHerramienta> herramientas,
            AgentContext ctx,
            string modelo)
        {
            var tools = herramientas.Select(h => new
            {
                name = h.Nombre,
                description = h.Descripcion,
                input_schema = h.EsquemaParametros
            }).ToArray();

            var body = new
            {
                model = modelo,
                max_tokens = 9000,
                system = ctx.PromptEfectivo,
                tools,
                messages = mensajes
            };

            return ("https://api.anthropic.com/v1/messages", body);
        }

        // ── Gemini (Google) ──────────────────────────────────────────────────

        private static (string url, object body) ConstruirRequestGemini(
            List<JObject> mensajes,
            IReadOnlyCollection<IHerramienta> herramientas,
            AgentContext ctx,
            string modelo)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                         $"{modelo}:generateContent?key={ctx.ApiKey}";

            var functionDeclarations = herramientas.Select(h => new
            {
                name = h.Nombre,
                description = h.Descripcion,
                parameters = h.EsquemaParametros
            }).ToArray();

            var body = new
            {
                contents = mensajes,
                tools = new[] { new { functionDeclarations } },
                generationConfig = new { temperature = 0.7 }
            };

            return (url, body);
        }

        // ── Ollama (local) ───────────────────────────────────────────────────

        private static (string url, object body) ConstruirRequestOllama(
            List<JObject> mensajes,
            IReadOnlyCollection<IHerramienta> herramientas,
            AgentContext ctx)
        {
            // Ollama compatible OpenAI en formato tools (requiere modelo que soporte tool_calls)
            var tools = herramientas.Select(h => new
            {
                type = "function",
                function = new
                {
                    name = h.Nombre,
                    description = h.Descripcion,
                    parameters = h.EsquemaParametros
                }
            }).ToArray();

            var body = new
            {
                model = ctx.Modelo,
                messages = mensajes,
                tools,
                stream = false
            };

            return ("http://localhost:11434/api/chat", body);
        }

        // =====================================================================
        //  EXTRACCIÓN DE RESPUESTA CON TOOL CALLS
        // =====================================================================

        private static RespuestaLLM ExtraerRespuestaConHerramientas(string raw, Servicios servicio)
        {
            try
            {
                return servicio switch
                {
                    Servicios.Gemenni => ExtraerGemini(raw),
                    Servicios.Claude => ExtraerClaude(raw),
                    _ => ExtraerOpenAI(raw)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MotorHerramientas] Error parseando respuesta: {ex.Message}");
                return new RespuestaLLM { Texto = raw };
            }
        }

        // ── OpenAI compatible ────────────────────────────────────────────────

        private static RespuestaLLM ExtraerOpenAI(string raw)
        {
            var root = JsonConvert.DeserializeObject<JObject>(raw)!;
            var message = root["choices"]?[0]?["message"];
            string texto = message?["content"]?.ToString() ?? "";
            var toolCallsJson = message?["tool_calls"] as JArray;

            if (toolCallsJson == null || toolCallsJson.Count == 0)
                return new RespuestaLLM { Texto = texto };

            var llamadas = toolCallsJson.Select(tc => new LlamadaHerramienta
            {
                Id = tc["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Nombre = tc["function"]?["name"]?.ToString() ?? "",
                ArgumentosJson = tc["function"]?["arguments"]?.ToString() ?? "{}"
            }).ToList();

            return new RespuestaLLM { Texto = texto, LlamadasHerramientas = llamadas };
        }

        // ── Claude ───────────────────────────────────────────────────────────

        private static RespuestaLLM ExtraerClaude(string raw)
        {
            var root = JsonConvert.DeserializeObject<JObject>(raw)!;
            var content = root["content"] as JArray ?? [];

            string texto = content
                .Where(c => c["type"]?.ToString() == "text")
                .Select(c => c["text"]?.ToString() ?? "")
                .FirstOrDefault() ?? "";

            var llamadas = content
                .Where(c => c["type"]?.ToString() == "tool_use")
                .Select(c => new LlamadaHerramienta
                {
                    Id = c["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                    Nombre = c["name"]?.ToString() ?? "",
                    // Claude devuelve "input" como JObject, no como string
                    ArgumentosJson = c["input"]?.ToString(Formatting.None) ?? "{}"
                })
                .ToList();

            return new RespuestaLLM { Texto = texto, LlamadasHerramientas = llamadas };
        }

        // ── Gemini ───────────────────────────────────────────────────────────

        private static RespuestaLLM ExtraerGemini(string raw)
        {
            var root = JsonConvert.DeserializeObject<JObject>(raw)!;
            var parts = root["candidates"]?[0]?["content"]?["parts"] as JArray ?? [];

            string texto = parts
                .Where(p => p["text"] != null)
                .Select(p => p["text"]?.ToString() ?? "")
                .FirstOrDefault() ?? "";

            var llamadas = parts
                .Where(p => p["functionCall"] != null)
                .Select(p => new LlamadaHerramienta
                {
                    Id = Guid.NewGuid().ToString(),
                    Nombre = p["functionCall"]?["name"]?.ToString() ?? "",
                    // Gemini devuelve "args" como JObject
                    ArgumentosJson = p["functionCall"]?["args"]?.ToString(Formatting.None) ?? "{}"
                })
                .ToList();

            return new RespuestaLLM { Texto = texto, LlamadasHerramientas = llamadas };
        }

        // =====================================================================
        //  GESTIÓN DEL HISTORIAL DE MENSAJES
        // =====================================================================

        private static void AgregarMensajeAsistente(
            List<JObject> mensajes, string rawResponse, Servicios servicio)
        {
            switch (servicio)
            {
                case Servicios.Gemenni:
                {
                    // Gemini: agregar la parte "model" con el functionCall completo
                    var root = JsonConvert.DeserializeObject<JObject>(rawResponse)!;
                    var contentModel = root["candidates"]?[0]?["content"];
                    if (contentModel != null)
                        mensajes.Add(contentModel.ToObject<JObject>()!);
                    break;
                }

                case Servicios.Claude:
                {
                    // Claude: agregar el content completo del assistant (incluye tool_use blocks)
                    var root = JsonConvert.DeserializeObject<JObject>(rawResponse)!;
                    var content = root["content"];
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "assistant",
                        content = content
                    }));
                    break;
                }

                default:
                {
                    // OpenAI: agregar el message completo con tool_calls
                    var root = JsonConvert.DeserializeObject<JObject>(rawResponse)!;
                    var message = root["choices"]?[0]?["message"];
                    if (message != null)
                        mensajes.Add(message.ToObject<JObject>()!);
                    break;
                }
            }
        }

        private static void AgregarResultadoHerramienta(
            List<JObject> mensajes,
            LlamadaHerramienta llamada,
            string resultado,
            Servicios servicio)
        {
            switch (servicio)
            {
                case Servicios.Gemenni:
                    // Gemini: functionResponse dentro de parts del rol "user"
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = llamada.Nombre,
                                    response = new { output = resultado }
                                }
                            }
                        }
                    }));
                    break;

                case Servicios.Claude:
                    // Claude: tool_result dentro del content del rol "user"
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "user",
                        content = new[]
                        {
                            new
                            {
                                type = "tool_result",
                                tool_use_id = llamada.Id,
                                content = resultado
                            }
                        }
                    }));
                    break;

                default:
                    // OpenAI: mensaje con role "tool"
                    mensajes.Add(JObject.FromObject(new
                    {
                        role = "tool",
                        tool_call_id = llamada.Id,
                        content = resultado
                    }));
                    break;
            }
        }

        // =====================================================================
        //  EJECUCIÓN DE HERRAMIENTAS
        // =====================================================================

        private static async Task<string> EjecutarHerramientaAsync(
            LlamadaHerramienta llamada, CancellationToken ct)
        {
            var herramienta = _registro.Value.Obtener(llamada.Nombre);

            if (herramienta == null)
                return $"Error: La herramienta '{llamada.Nombre}' no está registrada. " +
                       $"Herramientas disponibles: {string.Join(", ", _registro.Value.ObtenerTodas().Select(h => h.Nombre))}";

            try
            {
                JObject parametros;
                try
                {
                    parametros = JObject.Parse(llamada.ArgumentosJson);
                }
                catch
                {
                    parametros = new JObject();
                }

                return await herramienta.EjecutarAsync(parametros, ct);
            }
            catch (Exception ex)
            {
                return $"Error inesperado ejecutando '{llamada.Nombre}': {ex.Message}";
            }
        }

        // =====================================================================
        //  HEADERS (igual que AIModelConector pero aquí para no romper privacidad)
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
                    // API key va en query param de la URL
                    break;

                case Servicios.OpenRouter:
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.ApiKey);
                    req.Headers.Add("HTTP-Referer", "https://tuapp.local");
                    req.Headers.Add("X-Title", "OPENGIOAI");
                    break;

                case Servicios.Ollama:
                    // Sin autenticación para Ollama local
                    break;

                default:
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.ApiKey);
                    break;
            }
        }

        // =====================================================================
        //  UTILIDADES
        // =====================================================================

        private static string ModeloPorDefecto(Servicios servicio) => servicio switch
        {
            Servicios.Claude => "claude-3-haiku-20240307",
            Servicios.Gemenni => "gemini-1.5-flash",
            Servicios.Deespeek => "deepseek-chat",
            Servicios.OpenRouter => "openai/gpt-4.1-mini",
            Servicios.Ollama => "llama3.1",
            _ => "gpt-4.1-mini"
        };

        private static string Truncar(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";

        private static string TruncatarJson(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                // Mostrar solo keys con valores truncados
                var partes = obj.Properties()
                    .Select(p => $"{p.Name}: {Truncar(p.Value.ToString(), 30)}");
                return string.Join(", ", partes);
            }
            catch { return Truncar(json, 60); }
        }
    }
}
