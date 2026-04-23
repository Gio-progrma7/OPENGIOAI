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
using OPENGIOAI.Promts;
using OPENGIOAI.Utilerias;
// PerfilContexto vive en OPENGIOAI.Entidades — ya incluido arriba.
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

        /// <summary>
        /// El Constructor completó su ejecución.
        /// Proporciona la salida técnica cruda (contenido de respuesta.txt)
        /// para que la UI pueda reenviarla formateada (ej. a Telegram).
        /// </summary>
        public event Action<string>? OnConstructorCompletado;

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

            // ── Telemetría de tokens: abrir bucket de ejecución ──────────────
            // Cada llamada al LLM que haga cualquier fase del pipeline
            // quedará agrupada bajo este InstruccionId.
            string instruccionId = ConsumoTokensTracker.Instancia.IniciarEjecucion(instruccion);

            // ── Tracing (Fase 1A): abrir trace correlacionado con el InstruccionId ──
            TracerEjecucion.Instancia.IniciarTrace(
                instruccionId, instruccion, _modelo, _servicio.ToString(), _ruta);

            // Span raíz que cubre todo el pipeline. Se cierra en el finally
            // tras FinalizarTrace, así la UI ve todos los spans hijos cerrados
            // cuando recibe el evento OnTraceFinalizado.
            using var spanPipeline = TracerEjecucion.Instancia.AbrirSpan(
                SpanTipo.Pipeline, "ARIA Pipeline");
            spanPipeline.RegistrarInput(instruccion);
            spanPipeline.AgregarAtributo("modelo", _modelo);
            spanPipeline.AgregarAtributo("servicio", _servicio.ToString());

            try
            {

            // ── FASE 1: ANALISTA (en paralelo con pre-carga de contexto) ──────
            OnFaseIniciada?.Invoke(FaseAgente.Analista, "Analizando tu instrucción...");

            var spanAnalista = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Analista");
            spanAnalista.RegistrarInput(instruccion);
            var taskAnalista    = FaseAnalistaAsync(instruccion, ct);
            // Pre-build del contexto COMPLETO para el Constructor.
            // Se lanza en paralelo con el Analista para ocultar latencia de I/O.
            // Pasamos la instrucción → si memoria_semantica está activa,
            // BuildAsync usará RAG (top-K) en lugar del dump completo.
            var taskCtxPrebuild = AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio, _soloChat, _claves, ct,
                perfil: PerfilContexto.Completo,
                instruccionUsuario: instruccion);

            await Task.WhenAll(taskAnalista, taskCtxPrebuild);

            string planSimple = taskAnalista.Result;
            spanAnalista.RegistrarOutput(planSimple);
            spanAnalista.Dispose();

            OnFaseIniciada?.Invoke(FaseAgente.Analista, planSimple);
            OnFaseCompletada?.Invoke(FaseAgente.Analista, true);

            log.AppendLine("### 📋 Analista");
            log.AppendLine($"> {planSimple.Replace("\n", "\n> ")}");
            log.AppendLine();

            ct.ThrowIfCancellationRequested();

            // ── FASE 2: CONSTRUCTOR ──────────────────────────────────────────
            OnFaseIniciada?.Invoke(FaseAgente.Constructor, "Trabajando en ello...");
            var spanConstructor = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Constructor");
            spanConstructor.RegistrarInput(instruccion);
            var (codigoGenerado, stdoutConstructor) = await FaseConstructorAsync(instruccion, ct);
            OnFaseCompletada?.Invoke(FaseAgente.Constructor, true);

            // ── Determinar la salida real del Constructor ────────────────────
            // Prioridad:
            //   1. respuesta.txt → el script la escribió explícitamente (caso ideal)
            //   2. stdout capturado → el script imprimió a consola pero no escribió respuesta.txt
            //      En este caso lo persistimos en respuesta.txt para que el Analizador y
            //      el Guardián también lo lean (ambos llaman a LeerRespuestaTxt internamente)
            //   3. codigoGenerado → modo soloChat sin script (LLM respondió texto directo)
            string salidaRawConstructor = LeerRespuestaTxt();

            if (string.IsNullOrWhiteSpace(salidaRawConstructor))
            {
                if (!string.IsNullOrWhiteSpace(stdoutConstructor))
                {
                    // El script produjo stdout pero no escribió respuesta.txt
                    // → persistirlo para que todo el pipeline lo use
                    salidaRawConstructor = stdoutConstructor;
                    try
                    {
                        await File.WriteAllTextAsync(
                            Path.Combine(_ruta, "respuesta.txt"),
                            stdoutConstructor,
                            System.Text.Encoding.UTF8);
                    }
                    catch { /* Silenciar — el flujo continúa igual */ }
                }
                else
                {
                    // Sin script (soloChat): el LLM respondió texto directamente
                    salidaRawConstructor = codigoGenerado;
                }
            }

            // Notificar a la UI con la salida real (para reenvío opcional por Telegram, etc.)
            OnConstructorCompletado?.Invoke(salidaRawConstructor);
            spanConstructor.AgregarAtributo("lineas_codigo", codigoGenerado.Split('\n').Length.ToString());
            spanConstructor.RegistrarOutput(salidaRawConstructor);
            spanConstructor.Dispose();

            log.AppendLine("### ⚙️ Constructor");
            log.AppendLine($"- Código: {codigoGenerado.Split('\n').Length} líneas");
            log.AppendLine($"- Salida raw: `{Truncar(salidaRawConstructor, 400)}`");
            log.AppendLine();

            ct.ThrowIfCancellationRequested();

            // ── ANALIZADOR DE SALIDA ─────────────────────────────────────────
            OnFaseIniciada?.Invoke(FaseAgente.Guardian, "Verificando el resultado...");

            var spanGuardian = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Guardian");
            spanGuardian.RegistrarInput(codigoGenerado);

            var (exitoRapido, salidaNormalizada) =
                await AnalizarSalidaRapidoAsync(instruccion, codigoGenerado, ct);

            string resultadoFinal;
            if (exitoRapido)
            {
                spanGuardian.AgregarAtributo("via", "rapido");
                spanGuardian.AgregarAtributo("correcciones", "0");
                OnFaseCompletada?.Invoke(FaseAgente.Guardian, true);
                resultadoFinal = salidaNormalizada;
                log.AppendLine("### 🛡️ Guardián");
                log.AppendLine("- ✅ Datos verificados — sin correcciones necesarias");
                log.AppendLine($"- Resultado: `{Truncar(salidaNormalizada, 400)}`");
                log.AppendLine();
            }
            else
            {
                spanGuardian.AgregarAtributo("via", "correccion");
                resultadoFinal = await FaseGuardianAsync(instruccion, codigoGenerado, ct);
                OnFaseCompletada?.Invoke(FaseAgente.Guardian, true);
                log.AppendLine("### 🛡️ Guardián");
                log.AppendLine("- 🔄 Corrección automática aplicada");
                log.AppendLine($"- Resultado corregido: `{Truncar(resultadoFinal, 400)}`");
                log.AppendLine();
            }
            spanGuardian.RegistrarOutput(resultadoFinal);
            spanGuardian.Dispose();

            ct.ThrowIfCancellationRequested();

            // ── FASE 4: COMUNICADOR ──────────────────────────────────────────
            OnFaseIniciada?.Invoke(FaseAgente.Comunicador, "Preparando tu respuesta...");
            var spanComunicador = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Comunicador");
            spanComunicador.RegistrarInput(resultadoFinal);
            string respuesta = await FaseComunicadorAsync(
                instruccion, codigoGenerado, resultadoFinal, ct);
            spanComunicador.RegistrarOutput(respuesta);
            spanComunicador.Dispose();
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

            // ── FASE 5: MEMORISTA (fire-and-forget) ──────────────────────
            // Corre DESPUÉS de entregar la respuesta. No suma latencia.
            // Si falla, el usuario ni se entera — best-effort por diseño.
            _ = DispararMemoristaAsync(instruccion, respuesta);

            spanPipeline.RegistrarOutput(respuesta);
            return respuesta;
            }
            catch (OperationCanceledException)
            {
                spanPipeline.MarcarCancelado();
                TracerEjecucion.Instancia.FinalizarTrace(SpanEstado.Cancelado);
                throw;
            }
            catch (Exception ex)
            {
                spanPipeline.MarcarError(ex.Message);
                TracerEjecucion.Instancia.FinalizarTrace(SpanEstado.Error, ex.Message);
                throw;
            }
            finally
            {
                // Cerrar el bucket de telemetría — la UI recibe el evento
                // OnEjecucionFinalizada con el total agregado de la instrucción.
                try { ConsumoTokensTracker.Instancia.FinalizarEjecucion(); } catch { }
                // Si no pasó por catch (camino feliz), cerrar el trace normalmente.
                try { TracerEjecucion.Instancia.FinalizarTrace(SpanEstado.Ok); } catch { }
            }
        }

        /// <summary>
        /// Lanza el Memorista en segundo plano con su propio CancellationToken.
        /// Deliberadamente NO usa el ct del caller: el usuario ya recibió su
        /// respuesta, así que cancelar aquí perdería memoria por ruido del UI.
        /// </summary>
        private async Task DispararMemoristaAsync(string instruccion, string respuesta)
        {
            try
            {
                // Memorista: perfil dedicado — no lee su propia memoria
                // (está por escribirla) ni skills/credenciales.
                var ctxMem = await AgentContext.BuildAsync(
                    _ruta, _modelo, _apiKey, _servicio,
                    soloChat: true, _claves,
                    CancellationToken.None,
                    perfil: PerfilContexto.Memorista);

                await AgenteMemorista.EjecutarAsync(
                    instruccion, respuesta, ctxMem, CancellationToken.None);
            }
            catch
            {
                // Fase 5 es best-effort — cualquier error queda silenciado.
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  FASE 1 — ANALISTA
        //  LLM puro (sin script). Devuelve un mensaje amigable para el usuario
        //  explicando qué va a hacer, en lenguaje simple y directo.
        // ════════════════════════════════════════════════════════════════════

        private async Task<string> FaseAnalistaAsync(string instruccion, CancellationToken ct)
        {
            // El Analista SOLO interpreta la instrucción: no necesita credenciales,
            // ni skills, ni memoria, ni rutas. Perfil Mínimo → cero I/O de disco,
            // cero tokens del PromptEfectivo (pasa "" más su propio prompt).
            var ctx = await AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio,
                soloChat: true, _claves, ct,
                perfil: PerfilContexto.Minimo);

            // Prompt del Analista — definido en PromptCatalogo, editable desde FrmPromts.
            string promptAnalista = PromptRegistry.Instancia.Obtener(
                PromptCatalogo.K_ANALISTA,
                new Dictionary<string, string>
                {
                    ["instruccion"] = instruccion,
                });

            var ctxAnalista = ctx.ComoFase("Analista").ConPromptPersonalizado("");
            string raw = await AIModelConector.ObtenerRespuestaLLMAsync(promptAnalista, ctxAnalista, ct);

            return ParsearPlanAnalista(raw);
        }

        private static string ParsearPlanAnalista(string raw)
        {
            try
            {
                // Limpiar bloques de código si el modelo los añadió
                string limpio = ExtraerBloquePuro(raw);

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

            // 4. Verificación LLM rápida — prompt minimalista para latencia baja.
            //    El Analizador solo compara instrucción vs salida, no ejecuta
            //    nada. Perfil Mínimo es suficiente.
            var ctx = await AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio, soloChat: true, _claves, ct,
                perfil: PerfilContexto.Minimo);

            string promptAnalisis = PromptRegistry.Instancia.Obtener(
                PromptCatalogo.K_ANALIZADOR,
                new Dictionary<string, string>
                {
                    ["instruccion"] = instruccion,
                    ["salida"]      = Truncar(salidaNorm, 600),
                });

            var ctxAnalizador = ctx.ComoFase("Analizador").ConPromptPersonalizado("");
            string raw = "";
            try
            {
                raw = await AIModelConector.ObtenerRespuestaLLMAsync(promptAnalisis, ctxAnalizador, ct);

                var json = JObject.Parse(ExtraerBloquePuro(raw));
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

        /// <summary>
        /// Ejecuta el Constructor y captura tanto el código generado como el stdout del script.
        /// El stdout es la salida real de ejecución (lo que el script imprime / escribe en pantalla),
        /// distinto de respuesta.txt que el propio script puede o no escribir.
        /// </summary>
        private async Task<(string codigo, string stdout)> FaseConstructorAsync(
            string instruccion, CancellationToken ct)
        {
            var sbStdout = new StringBuilder();
            string codigo = await AIModelConector.EjecutarInstruccionIAAsync(
                instruccion,
                _modelo, _ruta, _apiKey, _claves, _soloChat, _servicio, ct,
                onInicioScript: () => OnInicioScript?.Invoke(FaseAgente.Constructor),
                onSalidaScript: linea =>
                {
                    sbStdout.AppendLine(linea);
                    OnLineaScript?.Invoke(FaseAgente.Constructor, linea);
                });
            return (codigo, sbStdout.ToString().Trim());
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

                // Guardián: JSON-in/JSON-out de verificación. No necesita más.
                var ctx = await AgentContext.BuildAsync(
                    _ruta, _modelo, _apiKey, _servicio,
                    soloChat: true, _claves, ct,
                    perfil: PerfilContexto.Minimo);

                string promptGuardian = PromptRegistry.Instancia.Obtener(
                    PromptCatalogo.K_GUARDIAN,
                    new Dictionary<string, string>
                    {
                        ["instruccion"]          = instruccion,
                        ["instruccion_escapada"] = instruccion.Replace("\"", "'"),
                        ["resultado"]            = string.IsNullOrWhiteSpace(resultado)
                                                      ? "[Sin resultado — el script no genero salida]"
                                                      : resultado,
                        ["codigo"]               = TruncarCodigo(codigoGenerado, 30),
                    });

                var ctxGuardian = ctx.ComoFase("Guardián").ConPromptPersonalizado("");
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
                var json = JObject.Parse(ExtraerBloquePuro(rawJson));
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
                var sbStdoutCorrector = new StringBuilder();
                await AIModelConector.EjecutarInstruccionIAAsync(
                    instruccionCorrectora,
                    _modelo, _ruta, _apiKey, _claves, _soloChat, _servicio, ct,
                    onInicioScript: () => OnInicioScript?.Invoke(FaseAgente.Guardian),
                    onSalidaScript: linea =>
                    {
                        sbStdoutCorrector.AppendLine(linea);
                        OnLineaScript?.Invoke(FaseAgente.Guardian, linea);
                    });

                // Nuevo resultado: preferir respuesta.txt; fallback a stdout si está vacío
                string nuevoResultado = NormalizarSalidaJSON(LeerRespuestaTxt());
                if (string.IsNullOrWhiteSpace(nuevoResultado))
                {
                    string stdoutCorrector = sbStdoutCorrector.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stdoutCorrector))
                    {
                        nuevoResultado = NormalizarSalidaJSON(stdoutCorrector);
                        try
                        {
                            await File.WriteAllTextAsync(
                                Path.Combine(_ruta, "respuesta.txt"),
                                stdoutCorrector,
                                System.Text.Encoding.UTF8);
                        }
                        catch { }
                    }
                }
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
            // Comunicador: estilo/identidad + memoria (para personalización),
            // pero SIN credenciales, SIN skills, SIN automatizaciones.
            // Ahorra tokens durante el streaming y acelera el primer token.
            // Le pasamos la instrucción para activar RAG si corresponde.
            var ctx = await AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio, _soloChat, _claves, ct,
                perfil: PerfilContexto.Comunicador,
                instruccionUsuario: instruccion);

            // El Comunicador usa el prompt de Agente2 como base de estilo
            // pero con reglas más estrictas de lenguaje amigable
            var ctxComunicador = ctx.ComoFase("Comunicador").ConPromptPersonalizado(ConstruirPromptComunicador());

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

        private static string ConstruirPromptComunicador() =>
            PromptRegistry.Instancia.Obtener(PromptCatalogo.K_COMUNICADOR);

        // ── Utilidades ───────────────────────────────────────────────────────

        /// <summary>
        /// Extrae el bloque JSON puro de una respuesta LLM que puede venir envuelta
        /// en markdown (```json ... ```, ``` ... ```) o con texto adicional.
        /// Estrategia: buscar el primer '{' o '[' y el último '}' o ']' correspondiente.
        /// Es más robusto que hacer Replace/TrimStart porque no depende del orden de los delimitadores.
        /// </summary>
        private static string ExtraerBloquePuro(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw ?? "";

            // Buscar primer carácter de apertura JSON
            int inicio = -1;
            char cierre = '}';
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == '{') { inicio = i; cierre = '}'; break; }
                if (raw[i] == '[') { inicio = i; cierre = ']'; break; }
            }

            if (inicio < 0) return raw.Trim(); // No parece JSON — devolver tal cual

            // Buscar el último carácter de cierre correspondiente
            int fin = raw.LastIndexOf(cierre);
            if (fin <= inicio) return raw.Trim();

            return raw[inicio..(fin + 1)];
        }

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
