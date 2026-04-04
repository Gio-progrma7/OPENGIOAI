// ============================================================
//  AgentePlanificador.cs  — 1.3 Agente con Planificación (ReAct/CoT)
//
//  PATRÓN: Pensar → Actuar → Observar → Repetir
//
//  A diferencia del MotorHerramientas (que usa Tool Use nativo del LLM),
//  este agente descompone la tarea en pasos explícitos, ejecuta cada uno
//  verificando el resultado y adapta el plan si algo falla.
//
//  Flujo por iteración:
//    1. PENSAR   — El LLM genera un plan estructurado (lista de pasos)
//    2. ACTUAR   — Ejecuta el paso actual con contexto acumulado
//    3. OBSERVAR — Verifica si el resultado es válido
//    4. REPETIR  — Si falla: adapta el plan y reintenta. Si ok: siguiente paso.
// ============================================================

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using System.Text;

namespace OPENGIOAI.Agentes
{
    public static class AgentePlanificador
    {
        // Máximo de reintentos por paso fallido antes de marcar como fallido y continuar
        private const int MaxIntentosPorPaso = 2;

        // =====================================================================
        //  PUNTO DE ENTRADA PÚBLICO
        // =====================================================================

        /// <summary>
        /// Ejecuta la instrucción usando planificación explícita ReAct/CoT.
        /// El agente primero crea un plan, luego ejecuta cada paso verificando
        /// el resultado y adaptando el plan si algo falla.
        /// </summary>
        /// <param name="instruccion">Tarea en lenguaje natural del usuario.</param>
        /// <param name="ctx">Contexto inmutable del agente (modelo, API key, servicio).</param>
        /// <param name="onProgreso">Callback para notificar progreso en tiempo real.</param>
        /// <param name="ct">Token de cancelación.</param>
        public static async Task<string> EjecutarConPlanificacionAsync(
            string instruccion,
            AgentContext ctx,
            Action<string>? onProgreso = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            // ── FASE 1: PENSAR — Generar el plan ────────────────────────────
            onProgreso?.Invoke("🧠 [Planificador] Analizando tarea y generando plan...");

            Plan plan = await GenerarPlanAsync(instruccion, ctx, ct);

            if (plan.Pasos.Count == 0)
            {
                onProgreso?.Invoke("⚠️ [Planificador] No se pudo generar un plan. Ejecutando directamente...");
                return await EjecutarDirectoAsync(instruccion, ctx, ct);
            }

            onProgreso?.Invoke($"📋 [Planificador] Plan generado: {plan.TotalPasos} pasos");
            onProgreso?.Invoke($"   Objetivo: {plan.Objetivo}");
            for (int i = 0; i < plan.Pasos.Count; i++)
                onProgreso?.Invoke($"   {i + 1}. {plan.Pasos[i].Descripcion}");

            // ── FASE 2-3-4: ACTUAR → OBSERVAR → REPETIR ─────────────────────
            var contextoAcumulado = new StringBuilder();
            contextoAcumulado.AppendLine($"Objetivo global: {plan.Objetivo}");
            contextoAcumulado.AppendLine();

            int indicePaso = 0;
            while (indicePaso < plan.Pasos.Count)
            {
                ct.ThrowIfCancellationRequested();

                var paso = plan.Pasos[indicePaso];
                paso.Estado = EstadoPaso.Ejecutando;
                paso.Intentos++;

                onProgreso?.Invoke($"\n▶ [Paso {paso.Numero}/{plan.TotalPasos}] {paso.Descripcion}");

                // ACTUAR — ejecutar el paso
                string resultado = await EjecutarPasoAsync(paso, plan.Objetivo, contextoAcumulado.ToString(), ctx, ct);
                paso.Resultado = resultado;

                onProgreso?.Invoke($"   Resultado: {Truncar(resultado, 150)}");

                // OBSERVAR — verificar el resultado
                bool exitoso = await VerificarPasoAsync(paso, resultado, ctx, ct);

                if (exitoso)
                {
                    paso.Estado = EstadoPaso.Completado;
                    contextoAcumulado.AppendLine($"Paso {paso.Numero} completado: {paso.Descripcion}");
                    contextoAcumulado.AppendLine($"Resultado: {resultado}");
                    contextoAcumulado.AppendLine();
                    onProgreso?.Invoke($"   ✓ Paso {paso.Numero} completado");
                    indicePaso++;
                }
                else
                {
                    paso.Estado = EstadoPaso.Fallido;
                    paso.Error = resultado;
                    onProgreso?.Invoke($"   ✗ Paso {paso.Numero} falló (intento {paso.Intentos}/{MaxIntentosPorPaso})");

                    if (paso.Intentos < MaxIntentosPorPaso)
                    {
                        // REPETIR con plan adaptado
                        onProgreso?.Invoke("   🔄 Adaptando plan...");
                        List<PasoDelPlan> pasosAdaptados = await AdaptarPlanAsync(
                            paso, plan.Pasos.Skip(indicePaso + 1).ToList(),
                            contextoAcumulado.ToString(), ctx, ct);

                        if (pasosAdaptados.Count > 0)
                        {
                            // Reemplazar pasos restantes con plan adaptado
                            plan.Pasos.RemoveRange(indicePaso, plan.Pasos.Count - indicePaso);
                            plan.Pasos.AddRange(pasosAdaptados);
                            onProgreso?.Invoke($"   📝 Plan adaptado: {pasosAdaptados.Count} pasos nuevos");
                        }
                        // Reintentar el mismo paso (ahora reemplazado si se adaptó)
                    }
                    else
                    {
                        // Demasiados intentos — marcar como fallido y continuar
                        onProgreso?.Invoke($"   ⚠️ Paso {paso.Numero} marcado como fallido después de {MaxIntentosPorPaso} intentos. Continuando...");
                        contextoAcumulado.AppendLine($"Paso {paso.Numero} FALLIDO: {paso.Descripcion}");
                        contextoAcumulado.AppendLine($"Error: {resultado}");
                        contextoAcumulado.AppendLine();
                        indicePaso++;
                    }
                }
            }

            // ── FASE FINAL: Síntesis ─────────────────────────────────────────
            onProgreso?.Invoke($"\n✅ [Planificador] Ejecución completa: {plan.PasosCompletados}/{plan.TotalPasos} pasos exitosos");

            return await SintetizarResultadoAsync(instruccion, plan, contextoAcumulado.ToString(), ctx, ct);
        }

        // =====================================================================
        //  FASE 1 — PENSAR: Generar el plan
        // =====================================================================

        private static async Task<Plan> GenerarPlanAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            const string promptPlanificador = @"Eres un agente de planificación experto. Tu única función es descomponer tareas complejas en pasos concretos y ejecutables.

REGLAS:
- Devuelve ÚNICAMENTE un JSON válido, sin texto adicional, sin markdown.
- Cada paso debe ser específico, atómico y verificable.
- Máximo 8 pasos. Mínimo 2 pasos.
- Si la tarea es simple (1 paso), igual devuelve el JSON con ese único paso.

FORMATO REQUERIDO:
{
  ""objetivo"": ""descripción concisa del objetivo final"",
  ""pasos"": [
    {""numero"": 1, ""descripcion"": ""...""},
    {""numero"": 2, ""descripcion"": ""...""}
  ]
}";

            string instruccionPlan = $"Descompón esta tarea en pasos concretos:\n\n{instruccion}";
            string respuesta = await AIModelConector.ObtenerRespuestaLLMAsync(
                instruccionPlan,
                ctx.ConPromptPersonalizado(promptPlanificador),
                ct);

            return ParsearPlan(respuesta);
        }

        private static Plan ParsearPlan(string respuesta)
        {
            try
            {
                // Extraer JSON si viene envuelto en markdown
                string json = ExtraerJson(respuesta);
                var root = JObject.Parse(json);

                var plan = new Plan
                {
                    Objetivo = root["objetivo"]?.ToString() ?? "Objetivo no especificado"
                };

                var pasosJson = root["pasos"] as JArray ?? [];
                foreach (var pasoJson in pasosJson)
                {
                    plan.Pasos.Add(new PasoDelPlan
                    {
                        Numero = pasoJson["numero"]?.Value<int>() ?? plan.Pasos.Count + 1,
                        Descripcion = pasoJson["descripcion"]?.ToString() ?? ""
                    });
                }

                return plan;
            }
            catch
            {
                // Si falla el parseo, crear plan de 1 paso con la respuesta completa
                return new Plan
                {
                    Objetivo = "Ejecutar tarea directamente",
                    Pasos = new List<PasoDelPlan>
                    {
                        new() { Numero = 1, Descripcion = respuesta.Length > 200 ? respuesta[..200] : respuesta }
                    }
                };
            }
        }

        // =====================================================================
        //  FASE 2 — ACTUAR: Ejecutar un paso
        // =====================================================================

        private static async Task<string> EjecutarPasoAsync(
            PasoDelPlan paso,
            string objetivo,
            string contextoAnterior,
            AgentContext ctx,
            CancellationToken ct)
        {
            string promptEjecutor = $@"Eres un agente ejecutor. Tu tarea es ejecutar el paso que se te indica.

OBJETIVO GLOBAL: {objetivo}

CONTEXTO DE PASOS ANTERIORES:
{(string.IsNullOrWhiteSpace(contextoAnterior) ? "(ninguno aún)" : contextoAnterior)}

INSTRUCCIONES:
- Ejecuta SOLO el paso indicado.
- Sé conciso y directo en tu respuesta.
- Si generas código, inclúyelo completo y listo para ejecutar.
- Reporta qué hiciste y cuál fue el resultado.";

            string instruccionPaso = $"Ejecuta el paso {paso.Numero}: {paso.Descripcion}";

            return await AIModelConector.ObtenerRespuestaLLMAsync(
                instruccionPaso,
                ctx.ConPromptPersonalizado(promptEjecutor),
                ct);
        }

        // =====================================================================
        //  FASE 3 — OBSERVAR: Verificar el resultado
        // =====================================================================

        private static async Task<bool> VerificarPasoAsync(
            PasoDelPlan paso, string resultado, AgentContext ctx, CancellationToken ct)
        {
            const string promptVerificador = @"Eres un agente verificador. Evalúa si un paso de un plan se ejecutó correctamente.

REGLAS:
- Devuelve ÚNICAMENTE un JSON válido: {""exitoso"": true} o {""exitoso"": false}
- Sin texto adicional, sin markdown.
- Sé permisivo: si hay un resultado razonable aunque no sea perfecto, marca como exitoso.
- Solo marca fallido si el resultado es un error claro, vacío o completamente irrelevante.";

            string instruccionVerificar =
                $"Paso a verificar: {paso.Descripcion}\n\nResultado obtenido:\n{resultado}\n\n¿Se completó correctamente?";

            try
            {
                string respuesta = await AIModelConector.ObtenerRespuestaLLMAsync(
                    instruccionVerificar,
                    ctx.ConPromptPersonalizado(promptVerificador),
                    ct);

                string json = ExtraerJson(respuesta);
                var root = JObject.Parse(json);
                return root["exitoso"]?.Value<bool>() ?? true;
            }
            catch
            {
                // En caso de error en la verificación, asumir éxito para no bloquear el flujo
                return true;
            }
        }

        // =====================================================================
        //  FASE 4 — REPETIR: Adaptar el plan si algo falla
        // =====================================================================

        private static async Task<List<PasoDelPlan>> AdaptarPlanAsync(
            PasoDelPlan pasoFallido,
            List<PasoDelPlan> pasosRestantes,
            string contextoActual,
            AgentContext ctx,
            CancellationToken ct)
        {
            const string promptAdaptador = @"Eres un agente de re-planificación. Un paso del plan ha fallado y necesitas adaptar los pasos restantes.

REGLAS:
- Devuelve ÚNICAMENTE un JSON válido, sin texto adicional.
- Genera pasos alternativos para completar el objetivo.
- Máximo 5 pasos nuevos.

FORMATO:
{
  ""pasos"": [
    {""numero"": 1, ""descripcion"": ""...""}
  ]
}";

            string pasosRestantesTexto = string.Join("\n",
                pasosRestantes.Select(p => $"- Paso {p.Numero}: {p.Descripcion}"));

            string instruccionAdaptar =
                $"El paso '{pasoFallido.Descripcion}' falló con error: {pasoFallido.Error}\n\n" +
                $"Contexto actual:\n{contextoActual}\n\n" +
                $"Pasos restantes del plan original:\n{pasosRestantesTexto}\n\n" +
                "Genera pasos alternativos para completar el objetivo:";

            try
            {
                string respuesta = await AIModelConector.ObtenerRespuestaLLMAsync(
                    instruccionAdaptar,
                    ctx.ConPromptPersonalizado(promptAdaptador),
                    ct);

                string json = ExtraerJson(respuesta);
                var root = JObject.Parse(json);
                var pasosJson = root["pasos"] as JArray ?? [];

                return pasosJson.Select((p, i) => new PasoDelPlan
                {
                    Numero = pasoFallido.Numero + i,
                    Descripcion = p["descripcion"]?.ToString() ?? ""
                }).Where(p => !string.IsNullOrWhiteSpace(p.Descripcion)).ToList();
            }
            catch
            {
                return new List<PasoDelPlan>();
            }
        }

        // =====================================================================
        //  FASE FINAL: Sintetizar resultado
        // =====================================================================

        private static async Task<string> SintetizarResultadoAsync(
            string instruccionOriginal,
            Plan plan,
            string contextoCompleto,
            AgentContext ctx,
            CancellationToken ct)
        {
            string promptSintesis = $@"Eres un agente de síntesis. Resume los resultados de la ejecución de un plan de manera clara y útil para el usuario.

INSTRUCCIÓN ORIGINAL DEL USUARIO:
{instruccionOriginal}

PASOS COMPLETADOS: {plan.PasosCompletados}/{plan.TotalPasos}";

            string instruccionSintesis =
                $"Resume los resultados de la siguiente ejecución de plan:\n\n{contextoCompleto}";

            return await AIModelConector.ObtenerRespuestaLLMAsync(
                instruccionSintesis,
                ctx.ConPromptPersonalizado(promptSintesis),
                ct);
        }

        // =====================================================================
        //  FALLBACK — Ejecución directa si no se pudo generar plan
        // =====================================================================

        private static Task<string> EjecutarDirectoAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            return AIModelConector.ObtenerRespuestaLLMAsync(instruccion, ctx, ct);
        }

        // =====================================================================
        //  UTILIDADES
        // =====================================================================

        private static string ExtraerJson(string texto)
        {
            // Quitar bloques markdown ```json ... ```
            int inicio = texto.IndexOf('{');
            int fin = texto.LastIndexOf('}');
            if (inicio >= 0 && fin > inicio)
                return texto[inicio..(fin + 1)];
            return texto.Trim();
        }

        private static string Truncar(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";
    }
}
