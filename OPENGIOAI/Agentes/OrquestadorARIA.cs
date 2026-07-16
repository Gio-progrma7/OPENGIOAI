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
using OPENGIOAI.Herramientas;
using OPENGIOAI.Promts;
using OPENGIOAI.Utilerias;
using Serilog;
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
            var run = new AgentRunContext(
                Guid.NewGuid().ToString("N"),
                instruccion,
                inicioTotal,
                _ruta,
                _modelo,
                _servicio);
            string workspaceEjecucion = CrearWorkspaceEjecucion(run);

            var log = new StringBuilder();
            log.AppendLine("---");
            log.AppendLine($"## 🧠 ARIA · {inicioTotal:yyyy-MM-dd HH:mm:ss} UTC");
            log.AppendLine($"**RunId:** `{run.RunId}`");
            log.AppendLine($"**Workspace:** `{workspaceEjecucion}`");
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
            spanPipeline.AgregarAtributo("run_id", run.RunId);
            spanPipeline.AgregarAtributo("modelo", _modelo);
            spanPipeline.AgregarAtributo("servicio", _servicio.ToString());

            try
            {
                var decisionRuta = RouterAgentico.Decidir(instruccion);
                spanPipeline.AgregarAtributo("ruta_agentica", decisionRuta.Ruta.ToString());
                spanPipeline.AgregarAtributo("ruta_confianza", decisionRuta.Confianza.ToString("F2"));
                log.AppendLine("### Router agentico");
                log.AppendLine($"- Ruta: `{decisionRuta.Ruta}`");
                log.AppendLine($"- Confianza: `{decisionRuta.Confianza:F2}`");
                log.AppendLine($"- Razon: {decisionRuta.Razon}");
                log.AppendLine();

                AgentContext? ctxConstructor = null;

                if (decisionRuta.Ruta == RutaAgentica.Herramientas)
                {
                    OnFaseIniciada?.Invoke(FaseAgente.Analista, "Detecte que puedo resolverlo mas rapido con herramientas...");
                    ctxConstructor = await AgentContext.BuildAsync(
                        _ruta, _modelo, _apiKey, _servicio, _soloChat, _claves, ct,
                        perfil: PerfilContexto.Completo,
                        instruccionUsuario: instruccion);
                    OnFaseCompletada?.Invoke(FaseAgente.Analista, true);

                    OnFaseIniciada?.Invoke(FaseAgente.Constructor, "Usando herramientas directas...");
                    string salidaHerramientas = await FaseHerramientasAsync(
                        instruccion, ctxConstructor, decisionRuta, ct);
                    OnConstructorCompletado?.Invoke(salidaHerramientas);

                    log.AppendLine("### Herramientas");
                    log.AppendLine($"- Resultado: `{Truncar(salidaHerramientas, 400)}`");
                    log.AppendLine();

                    if (!DebeCaerAlConstructor(salidaHerramientas))
                    {
                        OnFaseCompletada?.Invoke(FaseAgente.Constructor, true);
                        OnFaseCompletada?.Invoke(FaseAgente.Guardian, true);
                        OnFaseIniciada?.Invoke(FaseAgente.Comunicador, "Preparando tu respuesta...");
                        OnToken?.Invoke(FaseAgente.Comunicador, salidaHerramientas);
                        OnFaseCompletada?.Invoke(FaseAgente.Comunicador, true);

                        var duracionRapida = DateTime.UtcNow - inicioTotal;
                        log.AppendLine("### Comunicador");
                        log.AppendLine("- Respuesta entregada desde ruta rapida de herramientas");
                        log.AppendLine($"**Duracion total:** {duracionRapida.TotalSeconds:F1}s");
                        log.AppendLine("---");
                        log.AppendLine();

                        _ = GuardarLogAsync(log.ToString(), _ruta);
                        _ = DispararMemoristaAsync(instruccion, salidaHerramientas);

                        spanPipeline.RegistrarOutput(salidaHerramientas);
                        return salidaHerramientas;
                    }

                    OnFaseCompletada?.Invoke(FaseAgente.Constructor, false);
                    log.AppendLine("### Fallback");
                    log.AppendLine("- La ruta de herramientas no cerro con confianza; continuo con Constructor + Guardian.");
                    log.AppendLine();
                }

            // ── FASE 1: ANALISTA — Análisis inteligente de la instrucción ──────
            OnFaseIniciada?.Invoke(FaseAgente.Analista, "Analizando tu solicitud...");
            
            // Construcción del contexto COMPLETO (paralelo con análisis).
            ctxConstructor ??= await AgentContext.BuildAsync(
                _ruta, _modelo, _apiKey, _servicio, _soloChat, _claves, ct,
                perfil: PerfilContexto.Completo,
                instruccionUsuario: instruccion);

            // Análisis rápido de la instrucción: identificar objetivo, tipo y complejidad
            string analisisRapido = await AnalizarInstruccionRapidoAsync(instruccion, ctxConstructor, ct);
            
            // Log del análisis para trazabilidad
            log.AppendLine("### 📋 Analista");
            log.AppendLine($"- Modelo: `{_modelo}`");
            log.AppendLine($"- Perfil: `Completo` (RAG)");
            log.AppendLine($"- Análisis: {Truncar(analisisRapido, 300)}");
            log.AppendLine();

            OnFaseCompletada?.Invoke(FaseAgente.Analista, true);

            ct.ThrowIfCancellationRequested();

            // ── FASE 2: CONSTRUCTOR (Reusando Contexto) ──────────────────────
            OnFaseIniciada?.Invoke(FaseAgente.Constructor, "Generando y ejecutando la solución...");
            var spanConstructor = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Constructor");
            spanConstructor.RegistrarInput(instruccion);
            
            // Ahora pasamos ctxConstructor para evitar que se reconstruya dentro
            var ejecucionConstructor = await FaseConstructorAsync(instruccion, ctxConstructor, workspaceEjecucion, ct);
            OnFaseCompletada?.Invoke(FaseAgente.Constructor, true);
            string codigoGenerado = ejecucionConstructor.CodigoGenerado;
            string stdoutConstructor = ejecucionConstructor.Stdout;

            // ── Determinar la salida real del Constructor ────────────────────
            string salidaRawConstructor = ejecucionConstructor.SalidaPreferida;

            if (string.IsNullOrWhiteSpace(salidaRawConstructor))
            {
                salidaRawConstructor = codigoGenerado;
            }

            // Normalizar JSON inmediatamente (barato, sin LLM)
            string resultadoFinal = NormalizarSalidaJSON(salidaRawConstructor);

            OnConstructorCompletado?.Invoke(resultadoFinal);
            spanConstructor.AgregarAtributo("lineas_codigo", codigoGenerado.Split('\n').Length.ToString());
            spanConstructor.RegistrarOutput(resultadoFinal);
            spanConstructor.Dispose();

            log.AppendLine("### ⚙️ Constructor");
            log.AppendLine($"- Código: {codigoGenerado.Split('\n').Length} líneas");
            log.AppendLine($"- Salida normalizada: `{Truncar(resultadoFinal, 400)}`");
            log.AppendLine();

            ct.ThrowIfCancellationRequested();

            // ── FASE 3: GUARDIÁN (autocorrección inteligente) ─────────────────
            // Si la salida del Constructor es válida → camino feliz, sin coste extra.
            // Si hay error técnico, datos ausentes o salida vacía → diagnóstico
            // estructurado + reintentos automáticos (hasta _maxIntentosGuardian).
            var spanGuardian = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Guardian");
            spanGuardian.RegistrarInput(instruccion);

            (bool esFallo, string tipoFallo, string detalle) = DiagnosticarFallo(
                resultadoFinal, ejecucionConstructor, stdoutConstructor);

            if (esFallo && _maxIntentosGuardian > 0)
            {
                spanGuardian.AgregarAtributo("tipo_fallo", tipoFallo);
                spanGuardian.AgregarAtributo("detalle_fallo", detalle);

                string mensajeUsuario = tipoFallo switch
                {
                    "sintaxis"      => "El código generado tiene un error de sintaxis. Lo estoy corrigiendo...",
                    "logica"        => "El resultado no coincide con lo que pediste. Ajustando la lógica...",
                    "importacion"   => "Falta una librería necesaria. Agregándola al código...",
                    "runtime"       => "El script falló al ejecutarse. Revisando y corrigiendo...",
                    "datos_ausentes" => "No encontré los datos que buscabas. Intentando con otro enfoque...",
                    "resultado_vacio" => "El script terminó pero no generó salida. Corrigiendo...",
                    _               => "Detecté un inconveniente técnico. Intentando resolverlo automáticamente..."
                };

                OnFaseIniciada?.Invoke(FaseAgente.Guardian, mensajeUsuario);
                resultadoFinal = await FaseGuardianAsync(instruccion, codigoGenerado, resultadoFinal, ctxConstructor, workspaceEjecucion, ct);
                bool exitoGuardian = !string.IsNullOrWhiteSpace(resultadoFinal);
                OnFaseCompletada?.Invoke(FaseAgente.Guardian, exitoGuardian);

                log.AppendLine("### 🛡️ Guardián");
                log.AppendLine($"- 🔄 Fallo detectado: `{tipoFallo}` → {detalle}");
                log.AppendLine($"- Corrección: {(exitoGuardian ? "✅ Aplicada" : "❌ No se pudo corregir automáticamente")}");
                if (exitoGuardian)
                    log.AppendLine($"- Resultado corregido: `{Truncar(resultadoFinal, 400)}`");
            }
            else
            {
                spanGuardian.AgregarAtributo("tipo_fallo", "ninguno");
                OnFaseCompletada?.Invoke(FaseAgente.Guardian, true);
                log.AppendLine("### 🛡️ Guardián");
                log.AppendLine("- ✅ Ejecución correcta — sin correcciones necesarias");
            }
            log.AppendLine();

            spanGuardian.RegistrarOutput(resultadoFinal);
            spanGuardian.Dispose();

            ct.ThrowIfCancellationRequested();

            // ── FASE 4: COMUNICADOR — respuesta final al usuario ─────────────
            OnFaseIniciada?.Invoke(FaseAgente.Comunicador, "Preparando tu respuesta final...");
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
            log.AppendLine($"**⏱ Duración total:** {duracion.TotalSeconds:F1}s · **Fases:** Analista+Contexto ↓ Constructor ↓ Guardián ↓ Comunicador");
            log.AppendLine();
            log.AppendLine($"**Estado:** {((ejecucionConstructor?.ExitCode ?? 0) == 0 ? "✅ Éxito" : "⚠️ Con correcciones")}");
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
                try { ConsumoTokensTracker.Instancia.FinalizarEjecucion(); }
                catch (Exception ex) { Log.Warning(ex, "Error al finalizar bucket de telemetría"); }
                try { TracerEjecucion.Instancia.FinalizarTrace(SpanEstado.Ok); }
                catch (Exception ex) { Log.Warning(ex, "Error al finalizar trace de ejecución"); }
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
                var ctxMem = await AgentContext.BuildAsync(
                    _ruta, _modelo, _apiKey, _servicio,
                    soloChat: true, _claves,
                    CancellationToken.None,
                    perfil: PerfilContexto.Memorista);

                await AgenteMemorista.EjecutarAsync(
                    instruccion, respuesta, ctxMem, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Agente Memorista falló en background (best-effort)");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  FASE 1 — ANALISTA
        //  LLM puro (sin script). Devuelve un mensaje amigable para el usuario
        //  explicando qué va a hacer, en lenguaje simple y directo.
        // ════════════════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────────────────────
        //  NORMALIZADOR JSON → Texto legible
        //
        //  Cuando el script Python escribe JSON en respuesta.txt en lugar de
        //  texto plano, este método extrae los valores de texto significativos
        //  y los convierte en líneas legibles por un humano.
        // ─────────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Análisis rápido y ligero de la instrucción del usuario.
        /// Identifica el tipo de tarea, los datos necesarios y la complejidad
        /// para que el pipeline pueda adaptar su estrategia de ejecución.
        /// Es una llamada LLM rápida (~0.5s) con perfil mínimo de contexto.
        /// Si falla o tarda, el pipeline continúa sin el análisis (best-effort).
        /// </summary>
        private static async Task<string> AnalizarInstruccionRapidoAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                string promptAnalisis = $@"Analiza esta instrucción y responde SOLO JSON (1 línea, sin markdown):

Instrucción: {instruccion}

{{
  ""tipo"": ""lectura|escritura|analisis|ejecucion|consulta|busqueda|transformacion"",
  ""complejidad"": 1-5,
  ""requiere_archivos"": true/false,
  ""requiere_api"": true/false,
  ""datos_clave"": [""dato1"", ""dato2""],
  ""resumen"": ""1 frase de qué hay que hacer""
}}";

                var ctxMinimo = ctx.ComoFase("AnalizadorRapido")
                    .ConPromptPersonalizado("");
                return await AIModelConector.ObtenerRespuestaLLMAsync(
                    promptAnalisis, ctxMinimo, cts.Token);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Analista] Análisis rápido falló (best-effort)");
                return $"{{\"tipo\":\"no_analizado\",\"complejidad\":3}}";
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  FASE 2 — CONSTRUCTOR
        //  Usa el pipeline existente: LLM genera Python → se ejecuta → salida.
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ejecuta el Constructor y captura tanto el código generado como el stdout del script.
        /// El stdout es la salida real de ejecución (lo que el script imprime / escribe en pantalla),
        /// distinto de respuesta.txt que el propio script puede o no escribir.
        /// </summary>
        private async Task<ResultadoEjecucionIA> FaseConstructorAsync(
            string instruccion,
            AgentContext ctx,
            string workspaceEjecucion,
            CancellationToken ct)
        {
            var resultado = await AIModelConector.EjecutarInstruccionIAConResultadoAsync(
                instruccion,
                _modelo, _ruta, _apiKey, _claves, _soloChat, _servicio, ct,
                onInicioScript: () => OnInicioScript?.Invoke(FaseAgente.Constructor),
                onSalidaScript: linea =>
                {
                    OnLineaScript?.Invoke(FaseAgente.Constructor, linea);
                },
                ctxExistente: ctx,
                workspaceEjecucion: workspaceEjecucion); // Reusar contexto
            return resultado;
        }

        // ════════════════════════════════════════════════════════════════════
        //  FASE 3 — GUARDIÁN (autocorrección)
        //
        //  Loop hasta _maxIntentosGuardian:
        //    1. Recibe la salida capturada del Constructor
        //    2. Pregunta al LLM: ¿se cumplió la instrucción?
        //    3. Si no → obtiene instrucción correctora → llama al Constructor
        //    4. Si sí (o max intentos) → pasa al Comunicador
        // ════════════════════════════════════════════════════════════════════

        private async Task<string> FaseGuardianAsync(
            string instruccion,
            string codigoGenerado,
            string resultadoActual,
            AgentContext ctxConstructor,
            string workspaceEjecucion,
            CancellationToken ct)
        {
            // Normalizar JSON para que el Guardián evalúe datos, no formato
            string resultado = NormalizarSalidaJSON(resultadoActual);

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
                    rawVerificacion, instruccion, intento, resultado, ctxConstructor, workspaceEjecucion, ct);

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
        ///   nuevoResultado = nueva salida capturada (null si no cambió)
        /// </summary>
        private async Task<(bool seguir, string? nuevoResultado)> ProcesarVerificacionGuardianAsync(
            string rawJson, string instruccion,
            int intentoActual, string resultadoActual,
            AgentContext ctxConstructor,
            string workspaceEjecucion,
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

                // Notificar a la UI del reintento con detalle estructurado
                OnReintentoGuardian?.Invoke(intentoActual, _maxIntentosGuardian, razon);

                string fallbackEmoji = intentoActual >= _maxIntentosGuardian - 1 ? "⚠️" : "🔄";
                OnFaseIniciada?.Invoke(FaseAgente.Guardian,
                    $"{fallbackEmoji} Intento {intentoActual}/{_maxIntentosGuardian}: {razon}\nAplicando corrección automática...");

                // Aplicar corrección — usa el Constructor con la instrucción correctora
                var ejecucionCorrectora = await AIModelConector.EjecutarInstruccionIAConResultadoAsync(
                    instruccionCorrectora,
                    _modelo, _ruta, _apiKey, _claves, _soloChat, _servicio, ct,
                    onInicioScript: () => OnInicioScript?.Invoke(FaseAgente.Guardian),
                    onSalidaScript: linea =>
                    {
                        OnLineaScript?.Invoke(FaseAgente.Guardian, linea);
                    },
                    ctxExistente: ctxConstructor,
                    workspaceEjecucion: workspaceEjecucion); // Reusar contexto del orquestador

                // Nuevo resultado: preferir salida capturada por el runner; respuesta.txt
                // queda como compatibilidad externa, no como bus del orquestador.
                string nuevoResultado = NormalizarSalidaJSON(ejecucionCorrectora.SalidaPreferida);
                if (string.IsNullOrWhiteSpace(nuevoResultado))
                {
                    nuevoResultado = NormalizarSalidaJSON(ejecucionCorrectora.SalidaTecnica);
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

            // Fallback: si el streaming falló o no produjo nada, devolver el resultado capturado.
            if (sb.Length == 0)
            {
                string fallback = resultado;
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
        /// Diagnostica si la salida del Constructor contiene un fallo y lo clasifica.
        /// Usa heurísticas rápidas (sin LLM) para determinar el tipo de error:
        ///   - sintaxis: traceback de SyntaxError o NameError
        ///   - importacion: ModuleNotFoundError o ImportError
        ///   - runtime: traceback genérico o código de salida != 0
        ///   - datos_ausentes: status:error o datos vacíos
        ///   - resultado_vacio: sin salida ni stdout
        ///   - logica: salida presente pero con advertencias
        /// Devuelve (esFallo, tipoFallo, detalle).
        /// </summary>
        private static (bool esFallo, string tipo, string detalle) DiagnosticarFallo(
            string resultadoFinal,
            ResultadoEjecucionIA ejecucion,
            string stdoutConstructor)
        {
            // 1. Error de ejecución del script
            if (ejecucion.ExitCode.GetValueOrDefault() != 0)
                return (true, "runtime", $"Exit code: {ejecucion.ExitCode}");

            // 2. Error estándar con contenido
            if (!string.IsNullOrWhiteSpace(ejecucion.Stderr))
            {
                string err = ejecucion.Stderr.ToLowerInvariant();
                if (err.Contains("syntaxerror"))
                    return (true, "sintaxis", "Error de sintaxis en el código generado");
                if (err.Contains("modulenotfound") || err.Contains("importerror"))
                    return (true, "importacion", "Falta una dependencia necesaria");
                if (err.Contains("filenotfound") || err.Contains("oserror"))
                    return (true, "runtime", "Error de archivo o directorio no encontrado");
                if (err.Contains("nameerror"))
                    return (true, "sintaxis", "Variable o función no definida");
                if (err.Contains("typeerror") || err.Contains("valueerror"))
                    return (true, "runtime", "Error de tipo o valor en los datos");
                if (err.Contains("keyerror") || err.Contains("indexerror"))
                    return (true, "runtime", "Acceso inválido a datos (key o índice inexistente)");
                if (err.Contains("traceback"))
                    return (true, "runtime", "Excepción no controlada durante la ejecución");
                return (true, "runtime", Truncar(ejecucion.Stderr.Trim(), 120));
            }

            // 3. Indicador de error técnico en la salida
            string resultado = (resultadoFinal ?? "").ToLowerInvariant();
            string stdout = (stdoutConstructor ?? "").ToLowerInvariant();

            if (resultado.Contains("traceback") || stdout.Contains("[err]"))
                return (true, "runtime", "Se detectó un traceback o error explícito en la salida");

            if (resultado.Contains("\"status\": \"error\"") ||
                resultado.Contains("'status': 'error'") ||
                resultado.Contains("status : error"))
                return (true, "datos_ausentes", "El script reportó status:error");

            // 4. Salida vacía o sin datos
            if (string.IsNullOrWhiteSpace(resultadoFinal) && string.IsNullOrWhiteSpace(stdoutConstructor))
                return (true, "resultado_vacio", "El script no produjo ninguna salida");

            if (resultadoFinal?.Length < 10 && stdoutConstructor?.Length < 10)
                return (true, "resultado_vacio", "La salida del script es insuficiente para dar una respuesta");

            // 5. Error genérico en la cadena técnica
            if (ejecucion.SalidaTecnica.Contains("Error al ejecutar el script"))
                return (true, "runtime", "El sistema no pudo ejecutar el script generado");

            return (false, "ninguno", "Ejecución correcta");
        }

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

        private string CrearWorkspaceEjecucion(AgentRunContext run)
        {
            string runsRoot = Path.Combine(_ruta, "Runs");
            string workspace = Path.Combine(runsRoot, run.RunId);
            Directory.CreateDirectory(workspace);
            return workspace;
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
        private async Task<string> FaseHerramientasAsync(
            string instruccion,
            AgentContext ctx,
            DecisionRutaAgentica decision,
            CancellationToken ct)
        {
            using var spanTools = TracerEjecucion.Instancia.AbrirSpan(
                SpanTipo.Fase, "Herramientas");
            spanTools.RegistrarInput(instruccion);
            spanTools.AgregarAtributo("max_iteraciones", decision.MaxIteracionesHerramientas.ToString());

            string resultado = await MotorHerramientas.EjecutarConHerramientasAsync(
                instruccion,
                ctx.ComoFase("Herramientas"),
                onProgreso: linea => OnLineaScript?.Invoke(FaseAgente.Constructor, linea),
                ct: ct,
                maxIteraciones: decision.MaxIteracionesHerramientas);

            spanTools.RegistrarOutput(resultado);
            return resultado;
        }

        private static bool DebeCaerAlConstructor(string salidaHerramientas)
        {
            if (string.IsNullOrWhiteSpace(salidaHerramientas)) return true;

            string s = salidaHerramientas.ToLowerInvariant();
            return (s.Contains("alcanz") && s.Contains("mite")) ||
                   s.Contains("politica de herramientas") ||
                   s.Contains("error inesperado ejecutando") ||
                   s.Contains("registrada");
        }

        private static async Task GuardarLogAsync(string contenido, string ruta)
        {
            try
            {
                string path = Path.Combine(ruta, "ARIALog.md");
                await File.AppendAllTextAsync(path, contenido, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudo guardar el log ARIA en {Ruta}", ruta);
            }
        }
    }
}
