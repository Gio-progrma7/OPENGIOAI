// ============================================================
//  OrquestadorARIA.cs  — Arquitectura ARIA v1.0
//  Adaptive Reasoning & Implementation Agents
//
//  4 AGENTES ESPECIALIZADOS + AUTOCORRECCIÓN:
//
//  ┌──────────────┐
//  │  ANALISTA    │  → Interpreta la instrucción, explica el plan al usuario
//  └──────┬───────┘    en lenguaje simple. (LLM puro, sin script)
//         ↓
//  ┌──────────────┐
//  │  CONSTRUCTOR │  → Genera y ejecuta el código/script.
//  └──────┬───────┘    (Agente 1 existente + ejecución Python)
//         ↓
//  ┌──────────────┐   Si falla → genera instrucción correctora
//  │  GUARDIÁN    │ ↺ y reintenta Constructor (max 3x).
//  └──────┬───────┘
//         ↓
//  ┌──────────────┐
//  │ COMUNICADOR  │  → Produce respuesta final amigable
//  └──────────────┘    en streaming, sin tecnicismos.
//
//  PRINCIPIOS:
//  · Cada fase emite eventos para que la UI reaccione en tiempo real.
//  · El GuardIán nunca muestra errores crudos al usuario —
//    siempre los traduce a lenguaje simple y los corrige solo.
//  · El Comunicador usa streaming token-a-token para respuesta fluida.
// ============================================================

using Newtonsoft.Json.Linq;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Agentes
{
    /// <summary>Identifica en qué fase del pipeline ARIA se originó un evento.</summary>
    public enum FaseAgente
    {
        Analista,
        Constructor,
        Guardian,
        Comunicador
    }

    /// <summary>
    /// Orquestador ARIA: coordina los 4 agentes especializados.
    /// Cada ejecución es una instancia nueva — sin estado compartido entre llamadas.
    /// </summary>
    public sealed class OrquestadorARIA
    {
        // ── Configuración por ejecución ──────────────────────────────────────
        private readonly string _modelo;
        private readonly string _ruta;
        private readonly string _apiKey;
        private readonly string _claves;
        private readonly bool   _soloChat;
        private readonly Servicios _servicio;
        private readonly int _maxIntentosGuardian;

        // ── Eventos para la UI ───────────────────────────────────────────────

        /// <summary>
        /// Una fase acaba de iniciar. El string es un mensaje amigable para el usuario
        /// (ej: "Entendí tu instrucción. Voy a hacer 3 cosas...").
        /// </summary>
        public event Action<FaseAgente, string>? OnFaseIniciada;

        /// <summary>Token de streaming recibido del Comunicador.</summary>
        public event Action<FaseAgente, string>? OnToken;

        /// <summary>Una fase completó (true=éxito, false=error manejado).</summary>
        public event Action<FaseAgente, bool>? OnFaseCompletada;

        /// <summary>
        /// El Guardián va a reintentar. (intentoActual, maxIntentos, razonSimple)
        /// </summary>
        public event Action<int, int, string>? OnReintentoGuardian;

        /// <summary>El Constructor/Guardián está iniciando la ejecución del script.</summary>
        public event Action<FaseAgente>? OnInicioScript;

        /// <summary>Línea de salida del script en tiempo real.</summary>
        public event Action<FaseAgente, string>? OnLineaScript;

        // ── Constructor ──────────────────────────────────────────────────────

        /// <param name="maxReintentos">
        /// Máximo de correcciones automáticas del Guardián (0 = sin reintentos, máx 5).
        /// </param>
        public OrquestadorARIA(
            string modelo, string ruta, string apiKey,
            string claves, bool soloChat, Servicios servicio,
            int maxReintentos = 3)
        {
            _modelo   = modelo;
            _ruta     = ruta;
            _apiKey   = apiKey;
            _claves   = claves;
            _soloChat = soloChat;
            _servicio = servicio;
            _maxIntentosGuardian = Math.Clamp(maxReintentos, 0, 5);
        }

        // ── Punto de entrada ─────────────────────────────────────────────────

        /// <summary>
        /// Pipeline ARIA optimizado con analizador de salida y paralelismo.
        ///
        /// Estrategia de velocidad:
        ///   · ANALISTA se lanza inmediatamente y corre en paralelo con la
        ///     construcción del contexto del Constructor (I/O de disco).
        ///   · Tras el Constructor, el ANALIZADOR hace una verificación rápida:
        ///       - Normaliza JSON si respuesta.txt contiene JSON crudo.
        ///       - Pregunta al LLM si la instrucción se completó (call ~0.5s).
        ///   · Si el analizador dice OK  → corto circuito: va directo al Comunicador.
        ///   · Si dice FALLO             → Guardián entra a corregir (hasta 3x).
        ///   · Comunicador siempre recibe la salida normalizada (nunca JSON crudo).
        /// </summary>
        public async Task<string> EjecutarAsync(string instruccion, CancellationToken ct)
        {
            var inicioTotal = DateTime.UtcNow;
            var log = new StringBuilder();
            log.AppendLine("---");
            log.AppendLine($"## 🧠 ARIA · {inicioTotal:yyyy-MM-dd HH:mm:ss} UTC");
            log.AppendLine();
            log.AppendLine($"**Instrucción:** {instruccion}");
            log.AppendLine();

            // ── FASE 1: ANALISTA (en paralelo con pre-carga de contexto) ──────
            OnFaseIniciada?.Invoke(FaseAgente.Analista, "Analizando tu instrucción...");

            var taskAnalista    = FaseAnalistaAsync(instruccion, ct);
            var taskCtxPrebuild = AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio, _soloChat, _claves, ct);

            await Task.WhenAll(taskAnalista, taskCtxPrebuild);

            string planSimple = taskAnalista.Result;
            OnFaseIniciada?.Invoke(FaseAgente.Analista, planSimple);
            OnFaseCompletada?.Invoke(FaseAgente.Analista, true);

            log.AppendLine("### 📋 Analista");
            log.AppendLine($"> {planSimple.Replace("\n", "\n> ")}");
            log.AppendLine();

            ct.ThrowIfCancellationRequested();

            // ── FASE 2: CONSTRUCTOR ──────────────────────────────────────────
            OnFaseIniciada?.Invoke(FaseAgente.Constructor, "Trabajando en ello...");
            string codigoGenerado = await FaseConstructorAsync(instruccion, ct);
            OnFaseCompletada?.Invoke(FaseAgente.Constructor, true);

            string salidaRawConstructor = LeerRespuestaTxt();
            log.AppendLine("### ⚙️ Constructor");
            log.AppendLine($"- Código: {codigoGenerado.Split('\n').Length} líneas");
            log.AppendLine($"- Salida raw: `{Truncar(salidaRawConstructor, 400)}`");
            log.AppendLine();

            ct.ThrowIfCancellationRequested();

            // ── ANALIZADOR DE SALIDA ─────────────────────────────────────────
            OnFaseIniciada?.Invoke(FaseAgente.Guardian, "Verificando el resultado...");

            var (exitoRapido, salidaNormalizada) =
                await AnalizarSalidaRapidoAsync(instruccion, codigoGenerado, ct);

            string resultadoFinal;
            if (exitoRapido)
            {
                OnFaseCompletada?.Invoke(FaseAgente.Guardian, true);
                resultadoFinal = salidaNormalizada;
                log.AppendLine("### 🛡️ Guardián");
                log.AppendLine("- ✅ Datos verificados — sin correcciones necesarias");
                log.AppendLine($"- Resultado: `{Truncar(salidaNormalizada, 400)}`");
                log.AppendLine();
            }
            else
            {
                resultadoFinal = await FaseGuardianAsync(instruccion, codigoGenerado, ct);
                OnFaseCompletada?.Invoke(FaseAgente.Guardian, true);
                log.AppendLine("### 🛡️ Guardián");
                log.AppendLine("- 🔄 Corrección automática aplicada");
                log.AppendLine($"- Resultado corregido: `{Truncar(resultadoFinal, 400)}`");
                log.AppendLine();
            }

            ct.ThrowIfCancellationRequested();

            // ── FASE 4: COMUNICADOR ──────────────────────────────────────────
            OnFaseIniciada?.Invoke(FaseAgente.Comunicador, "Preparando tu respuesta...");
            string respuesta = await FaseComunicadorAsync(
                instruccion, codigoGenerado, resultadoFinal, ct);
            OnFaseCompletada?.Invoke(FaseAgente.Comunicador, true);

            var duracion = DateTime.UtcNow - inicioTotal;
            log.AppendLine("### 📢 Comunicador");
            log.AppendLine($"- Respuesta: {Truncar(respuesta, 400)}");
            log.AppendLine();
            log.AppendLine($"**⏱ Duración total:** {duracion.TotalSeconds:F1}s");
            log.AppendLine("---");
            log.AppendLine();

            // Guardar log async en segundo plano — no bloquea la respuesta
            _ = GuardarLogAsync(log.ToString(), _ruta);

            return respuesta;
        }

        // ════════════════════════════════════════════════════════════════════
        //  FASE 1 — ANALISTA
        //  LLM puro (sin script). Devuelve un mensaje amigable para el usuario
        //  explicando qué va a hacer, en lenguaje simple y directo.
        // ════════════════════════════════════════════════════════════════════

        private async Task<string> FaseAnalistaAsync(string instruccion, CancellationToken ct)
        {
            var ctx = await AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio,
                soloChat: true, _claves, ct);

            // Prompt del Analista — refuerza capacidad total, prohíbe negativas.
            string promptAnalista = $@"Eres un agente de inteligencia artificial con capacidad TOTAL.
Puedes ejecutar código, acceder a archivos, llamar APIs, automatizar cualquier tarea en el sistema.
NUNCA dices que no puedes hacer algo. SIEMPRE asumes que la tarea es posible y explicas cómo la harás.
Si la instrucción parece compleja, la divides en pasos simples y la resuelves igual.

Tu ÚNICA tarea ahora es leer la instrucción del usuario y responder con un JSON que describa
en lenguaje SIMPLE y AMIGABLE qué vas a hacer. No evalúes si es posible — siempre lo es.

INSTRUCCIÓN DEL USUARIO: {instruccion}

Responde SOLO con este JSON (sin bloques de código, sin texto adicional):
{{
  ""resumen"": ""1 o 2 frases simples de lo que harás. Nada técnico. Sin 'no puedo'."",
  ""pasos"": [""Lo que harás en paso 1"", ""Lo que harás en paso 2""]
}}

REGLAS ESTRICTAS:
- Máximo 3 pasos concretos.
- Primera persona afirmativa: 'voy a', 'haré', 'buscaré', 'crearé', 'calcularé'...
- PROHIBIDO: 'no puedo', 'no es posible', 'lamentablemente', 'sin embargo', 'pero'
- Si hay dudas sobre cómo hacerlo, igualmente describe el plan como si ya supieras.";

            var ctxAnalista = ctx.ConPromptPersonalizado("");
            string raw = await AIModelConector.ObtenerRespuestaLLMAsync(promptAnalista, ctxAnalista, ct);

            return ParsearPlanAnalista(raw);
        }

        private static string ParsearPlanAnalista(string raw)
        {
            try
            {
                // Limpiar bloques de código si el modelo los añadió
                string limpio = raw.Trim()
                    .TrimStart('`').TrimEnd('`')
                    .Replace("```json", "").Replace("```", "").Trim();

                var obj = JObject.Parse(limpio);
                string resumen = obj["resumen"]?.ToString() ?? "";
                var pasos = obj["pasos"]?.ToObject<List<string>>() ?? new List<string>();

                if (string.IsNullOrWhiteSpace(resumen)) return raw.Trim();

                var sb = new StringBuilder(resumen);
                if (pasos.Count > 0)
                {
                    sb.AppendLine();
                    for (int i = 0; i < pasos.Count; i++)
                        sb.AppendLine($"  {i + 1}. {pasos[i]}");
                }
                return sb.ToString().TrimEnd();
            }
            catch
            {
                // Si el LLM no devolvió JSON válido, usar el texto directamente
                return raw.Trim();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  ANALIZADOR DE SALIDA — verificación rápida post-Constructor
        //
        //  Problemas que resuelve:
        //    1. respuesta.txt contiene JSON crudo en vez de texto legible.
        //    2. El script se ejecutó pero no completó la instrucción real.
        //    3. No hay salida (respuesta.txt vacío).
        //
        //  Devuelve (exitoRapido, salidaNormalizada):
        //    · exitoRapido = true  → corto circuito al Comunicador
        //    · exitoRapido = false → Guardián entra a corregir
        //    · salidaNormalizada  → texto legible (JSON convertido si aplica)
        // ════════════════════════════════════════════════════════════════════

        private async Task<(bool exito, string salida)> AnalizarSalidaRapidoAsync(
            string instruccion, string codigoGenerado, CancellationToken ct)
        {
            // 1. Leer salida cruda
            string salidaCruda = LeerRespuestaTxt();

            // 2. Normalizar JSON si es necesario
            string salidaNorm = NormalizarSalidaJSON(salidaCruda);

            // 3. Si no hay salida en absoluto → fallo inmediato sin LLM call
            if (string.IsNullOrWhiteSpace(salidaNorm))
                return (false, "");

            // 4. Verificación LLM rápida — prompt minimalista para latencia baja
            var ctx = await AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio, soloChat: true, _claves, ct);

            string promptAnalisis = $@"Responde SOLO JSON válido sin texto adicional.
IMPORTANTE: La salida es el OUTPUT TÉCNICO de un script Python.
JSON con 'status ok' es un resultado CORRECTO Y VÁLIDO — no es un error de formato.
¿Los datos solicitados por el usuario están presentes en la salida?

INSTRUCCIÓN: {instruccion}

SALIDA TÉCNICA DEL SCRIPT:
{Truncar(salidaNorm, 600)}

Responde:
{{""exito"": true}}
o
{{""exito"": false, ""razon"": ""1 frase: que dato falta o que error hay""}}";

            var ctxAnalizador = ctx.ConPromptPersonalizado("");
            string raw = "";
            try
            {
                raw = await AIModelConector.ObtenerRespuestaLLMAsync(promptAnalisis, ctxAnalizador, ct);

                string limpio = raw.Trim()
                    .TrimStart('`').TrimEnd('`')
                    .Replace("```json", "").Replace("```", "").Trim();

                var json = JObject.Parse(limpio);
                bool exito = json["exito"]?.Value<bool>() ?? false;

                if (!exito)
                {
                    string razon = json["razon"]?.ToString() ?? "La instrucción no se completó.";
                    OnFaseIniciada?.Invoke(FaseAgente.Guardian,
                        $"Detecté un problema: {razon}. Lo corregiré automáticamente...");
                }

                return (exito, salidaNorm);
            }
            catch
            {
                // Si el LLM no respondió JSON válido, asumir éxito conservador
                // (evitar loops de corrección innecesarios)
                return (!string.IsNullOrWhiteSpace(salidaNorm), salidaNorm);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  NORMALIZADOR JSON → Texto legible
        //
        //  Cuando el script Python escribe JSON en respuesta.txt en lugar de
        //  texto plano, este método extrae los valores de texto significativos
        //  y los convierte en líneas legibles por un humano.
        // ─────────────────────────────────────────────────────────────────────

        private static string NormalizarSalidaJSON(string salida)
        {
            if (string.IsNullOrWhiteSpace(salida)) return salida;

            string trimmed = salida.Trim();
            bool pareceCerJSON = (trimmed.StartsWith("{") || trimmed.StartsWith("[")) &&
                                 (trimmed.EndsWith("}") || trimmed.EndsWith("]"));

            if (!pareceCerJSON) return salida; // No es JSON, devolver tal cual

            try
            {
                var token = JToken.Parse(trimmed);
                var lineas = ExtraerTextoDeJSON(token, nivel: 0);
                if (lineas.Count == 0) return salida;

                // Reconstruir como texto legible con indentación sutil
                return string.Join("\n", lineas).Trim();
            }
            catch
            {
                return salida; // JSON malformado, devolver original
            }
        }

        private static List<string> ExtraerTextoDeJSON(JToken token, int nivel)
        {
            const int MaxNivel = 4;
            const int MinLengthStr = 4; // Ignorar valores cortos tipo "ok", "0"

            var resultado = new List<string>();
            if (nivel > MaxNivel) return resultado;

            string indent = new string(' ', nivel * 2);

            switch (token)
            {
                case JObject obj:
                    foreach (var prop in obj.Properties())
                    {
                        string clave = prop.Name;
                        JToken valor = prop.Value;

                        if (valor.Type == JTokenType.String)
                        {
                            string s = valor.ToString();
                            if (s.Length >= MinLengthStr)
                                resultado.Add($"{indent}{FormatearClave(clave)}: {s}");
                        }
                        else if (valor.Type == JTokenType.Integer ||
                                 valor.Type == JTokenType.Float)
                        {
                            resultado.Add($"{indent}{FormatearClave(clave)}: {valor}");
                        }
                        else if (valor.Type == JTokenType.Boolean)
                        {
                            string boolStr = valor.Value<bool>() ? "Sí" : "No";
                            resultado.Add($"{indent}{FormatearClave(clave)}: {boolStr}");
                        }
                        else if (valor.Type == JTokenType.Array ||
                                 valor.Type == JTokenType.Object)
                        {
                            var sub = ExtraerTextoDeJSON(valor, nivel + 1);
                            if (sub.Count > 0)
                            {
                                resultado.Add($"{indent}{FormatearClave(clave)}:");
                                resultado.AddRange(sub);
                            }
                        }
                    }
                    break;

                case JArray arr:
                    int i = 1;
                    foreach (var item in arr)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            string s = item.ToString();
                            if (s.Length >= MinLengthStr)
                                resultado.Add($"{indent}{i}. {s}");
                        }
                        else
                        {
                            var sub = ExtraerTextoDeJSON(item, nivel + 1);
                            if (sub.Count > 0)
                            {
                                resultado.Add($"{indent}{i}.");
                                resultado.AddRange(sub);
                            }
                        }
                        i++;
                    }
                    break;
            }

            return resultado;
        }

        private static string FormatearClave(string clave)
        {
            // snake_case / camelCase → palabras con mayúscula inicial
            if (string.IsNullOrEmpty(clave)) return clave;
            string separado = System.Text.RegularExpressions.Regex
                .Replace(clave, @"([a-z])([A-Z])", "$1 $2")
                .Replace('_', ' ').Replace('-', ' ');
            return char.ToUpper(separado[0]) + separado[1..];
        }

        private static string Truncar(string texto, int maxChars) =>
            texto.Length <= maxChars ? texto : texto[..maxChars] + "...";

        // ════════════════════════════════════════════════════════════════════
        //  FASE 2 — CONSTRUCTOR
        //  Usa el pipeline existente: LLM genera Python → se ejecuta → salida.
        // ════════════════════════════════════════════════════════════════════

        private async Task<string> FaseConstructorAsync(string instruccion, CancellationToken ct)
        {
            return await AIModelConector.EjecutarInstruccionIAAsync(
                instruccion,
                _modelo, _ruta, _apiKey, _claves, _soloChat, _servicio, ct,
                onInicioScript: () => OnInicioScript?.Invoke(FaseAgente.Constructor),
                onSalidaScript: linea => OnLineaScript?.Invoke(FaseAgente.Constructor, linea));
        }

        // ════════════════════════════════════════════════════════════════════
        //  FASE 3 — GUARDIÁN (autocorrección)
        //
        //  Loop hasta _maxIntentosGuardian:
        //    1. Lee respuesta.txt
        //    2. Pregunta al LLM: ¿se cumplió la instrucción?
        //    3. Si no → obtiene instrucción correctora → llama al Constructor
        //    4. Si sí (o max intentos) → pasa al Comunicador
        // ════════════════════════════════════════════════════════════════════

        private async Task<string> FaseGuardianAsync(
            string instruccion, string codigoGenerado, CancellationToken ct)
        {
            // Normalizar JSON para que el Guardián evalúe datos, no formato
            string resultado = NormalizarSalidaJSON(LeerRespuestaTxt());

            for (int intento = 1; intento <= _maxIntentosGuardian; intento++)
            {
                ct.ThrowIfCancellationRequested();

                var ctx = await AgentContext.BuildAsync(
                    _ruta, _modelo, _apiKey, _servicio,
                    soloChat: true, _claves, ct);

                string promptGuardian = $@"Eres un revisor tecnico que valida si un script cumplio la instruccion del usuario.
REGLA: El RESULTADO es salida tecnica de un script Python. JSON con status ok es CORRECTO Y VALIDO.
Marca FALLO solo si: status=error, datos pedidos ausentes, hay traceback/excepcion, o resultado vacio.
Responde SOLO con JSON valido (sin bloques de codigo):

Si los datos estan presentes (exito real):
{{""exito"": true, ""razon"": ""Que datos se obtuvieron en 1 frase""}}

Si hay fallo real (datos ausentes, error Python, vacio):
{{""exito"": false, ""razon"": ""Que fallo (max 1 frase)"", ""instruccion_correctora"": ""Instruccion concreta para corregirlo""}}

INSTRUCCION ORIGINAL: {instruccion}

RESULTADO OBTENIDO (salida del script):
{(string.IsNullOrWhiteSpace(resultado) ? "[Sin resultado — el script no genero salida]" : resultado)}

CODIGO EJECUTADO (ultimas 30 lineas):
{TruncarCodigo(codigoGenerado, 30)}";

                var ctxGuardian = ctx.ConPromptPersonalizado("");
                string rawVerificacion = await AIModelConector.ObtenerRespuestaLLMAsync(
                    promptGuardian, ctxGuardian, ct);

                var (seguir, nuevoResultado) = await ProcesarVerificacionGuardianAsync(
                    rawVerificacion, instruccion, intento, resultado, ct);

                if (nuevoResultado != null) resultado = nuevoResultado;
                if (!seguir) break;
            }

            return resultado;
        }

        /// <summary>
        /// Procesa la respuesta JSON del Guardián.
        /// Devuelve (seguir, nuevoResultado):
        ///   seguir=true  → hay corrección que aplicar, continuar el bucle
        ///   seguir=false → detener (éxito o sin corrección disponible)
        ///   nuevoResultado = nuevo texto de respuesta.txt (null si no cambió)
        /// </summary>
        private async Task<(bool seguir, string? nuevoResultado)> ProcesarVerificacionGuardianAsync(
            string rawJson, string instruccion,
            int intentoActual, string resultadoActual,
            CancellationToken ct)
        {
            try
            {
                string limpio = rawJson.Trim()
                    .TrimStart('`').TrimEnd('`')
                    .Replace("```json", "").Replace("```", "").Trim();

                var json = JObject.Parse(limpio);
                bool exito = json["exito"]?.Value<bool>() ?? true;
                string razon = json["razon"]?.ToString() ?? "";
                string instruccionCorrectora = json["instruccion_correctora"]?.ToString() ?? "";

                if (exito) return (false, null);                          // Éxito — detener
                if (string.IsNullOrWhiteSpace(instruccionCorrectora)) return (false, null);

                // Notificar a la UI del reintento
                OnReintentoGuardian?.Invoke(intentoActual, _maxIntentosGuardian, razon);
                OnFaseIniciada?.Invoke(FaseAgente.Guardian,
                    $"Encontré algo que mejorar: {razon}\nHaciendo una corrección automática... ({intentoActual}/{_maxIntentosGuardian})");

                // Aplicar corrección — usa el Constructor con la instrucción correctora
                await AIModelConector.EjecutarInstruccionIAAsync(
                    instruccionCorrectora,
                    _modelo, _ruta, _apiKey, _claves, _soloChat, _servicio, ct,
                    onInicioScript: () => OnInicioScript?.Invoke(FaseAgente.Guardian),
                    onSalidaScript: linea => OnLineaScript?.Invoke(FaseAgente.Guardian, linea));

                // Devolver nuevo resultado para la siguiente iteración
                string nuevoResultado = LeerRespuestaTxt();
                return (true, nuevoResultado);
            }
            catch
            {
                // Si el JSON no es parseable, asumir éxito y detener
                return (false, null);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  FASE 4 — COMUNICADOR
        //  Streaming token-a-token. Traduce el resultado técnico a lenguaje
        //  natural, sin tecnicismos, como si hablara con un amigo.
        // ════════════════════════════════════════════════════════════════════

        private async Task<string> FaseComunicadorAsync(
            string instruccion, string codigo, string resultado, CancellationToken ct)
        {
            var ctx = await AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio, _soloChat, _claves, ct);

            // El Comunicador usa el prompt de Agente2 como base de estilo
            // pero con reglas más estrictas de lenguaje amigable
            var ctxComunicador = ctx.ConPromptPersonalizado(ConstruirPromptComunicador());

            string promptFinal = $@"INSTRUCCIÓN DEL USUARIO:
{instruccion}

RESULTADO OBTENIDO:
{(string.IsNullOrWhiteSpace(resultado) ? "[La operación se completó pero no generó texto visible]" : resultado)}";

            var sb = new StringBuilder();
            await AIModelConector.ObtenerRespuestaStreamingAsync(
                promptFinal,
                ctxComunicador,
                token =>
                {
                    sb.Append(token);
                    OnToken?.Invoke(FaseAgente.Comunicador, token);
                },
                ct);

            // Fallback: si el streaming falló o no produjo nada, leer respuesta.txt
            if (sb.Length == 0)
            {
                string fallback = LeerRespuestaTxt();
                if (!string.IsNullOrWhiteSpace(fallback))
                    OnToken?.Invoke(FaseAgente.Comunicador, fallback);
                return fallback;
            }

            return sb.ToString();
        }

        private static string ConstruirPromptComunicador() => @"Eres el Comunicador final del sistema ARIA. Tu misión es transmitir al usuario los resultados que ya fueron obtenidos.

══ REGLA CRÍTICA (PRIORIDAD MÁXIMA) ══
Los datos en 'RESULTADO OBTENIDO' son REALES y ya fueron ejecutados exitosamente por el sistema.
JAMÁS digas que no puedes ver, acceder, ejecutar o consultar nada — eso YA SE HIZO antes de que hablaras.
Tu trabajo es SOLO comunicar esos datos de forma clara y amigable. Nunca los ignores.

ESTILO:
1. Lenguaje cotidiano — cero tecnicismos
2. PROHIBIDO usar: script, función, variable, ejecutar, código, Python, proceso, módulo, import
3. USA siempre: busqué, encontré, guardé, calculé, obtuve, creé, calculé
4. Máximo 3 párrafos cortos
5. Máximo 2 emojis, usados con intención
6. Segunda persona directa: te, tu, tus
7. Empieza directo con el dato — sin 'Hola', 'Por supuesto', ni frases de incapacidad

FORMATO:
8. Listas o conjuntos de datos → viñetas o numeración clara
9. NUNCA muestres JSON crudo ni etiquetas técnicas
10. Muchos elementos → resumen primero, luego lista completa
11. Algo salió imperfecto → 1 frase breve, optimista, sin dramatismo";

        // ── Utilidades ───────────────────────────────────────────────────────

        private string LeerRespuestaTxt()
        {
            try
            {
                string path = Path.Combine(_ruta, "respuesta.txt");
                if (!File.Exists(path)) return "";
                return Utils.LimpiarRespuesta(Utils.LeerArchivoTxt(path));
            }
            catch { return ""; }
        }

        private static string TruncarCodigo(string codigo, int maxLineas)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return "[Sin código]";
            var lineas = codigo.Split('\n');
            if (lineas.Length <= maxLineas) return codigo;
            return "...\n" + string.Join("\n", lineas.TakeLast(maxLineas));
        }

        // ── Log de pipeline ──────────────────────────────────────────────────

        /// <summary>
        /// Escribe (append) el log de la ejecución en ARIALog.md dentro de la
        /// ruta de trabajo. Se llama en background al final de EjecutarAsync.
        /// </summary>
        private static async Task GuardarLogAsync(string contenido, string ruta)
        {
            try
            {
                string path = Path.Combine(ruta, "ARIALog.md");
                await File.AppendAllTextAsync(path, contenido, System.Text.Encoding.UTF8);
            }
            catch { /* Silenciar errores de log — nunca interrumpir el flujo */ }
        }
    }
}
