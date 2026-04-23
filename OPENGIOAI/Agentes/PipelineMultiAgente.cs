// ============================================================
//  PipelineMultiAgente.cs  — 1.4 Pipeline Multi-Agente Avanzado
//
//  4 agentes especializados en cadena:
//
//  ┌─────────────────┐    ┌──────────────────┐
//  │ AgentePlanif.   │ →  │ AgenteEjecutor   │
//  │ (descompone)    │    │ (genera código)  │
//  └─────────────────┘    └──────────────────┘
//           ↓                      ↓
//  ┌─────────────────┐    ┌──────────────────┐
//  │ AgenteVerif.    │ ←  │ AgenteFormateador│
//  │ (valida result) │    │ (salida final)   │
//  └─────────────────┘    └──────────────────┘
//
//  Cada agente recibe el output del anterior como contexto.
//  El pipeline es tolerante a fallos: si un agente falla,
//  el siguiente recibe el error como contexto y lo maneja.
// ============================================================

using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System.Threading;

namespace OPENGIOAI.Agentes
{
    public static class PipelineMultiAgente
    {
        // =====================================================================
        //  PUNTO DE ENTRADA PÚBLICO
        // =====================================================================

        /// <summary>
        /// Ejecuta la instrucción a través del pipeline de 4 agentes especializados.
        /// Cada agente recibe el output del agente anterior como contexto.
        /// </summary>
        /// <param name="instruccion">Tarea del usuario en lenguaje natural.</param>
        /// <param name="ctx">Contexto del agente (modelo, API key, servicio).</param>
        /// <param name="onProgreso">Callback para notificar progreso en tiempo real.</param>
        /// <param name="ct">Token de cancelación.</param>
        public static async Task<ResultadoPipeline> EjecutarPipelineAsync(
            string instruccion,
            AgentContext ctx,
            Action<string>? onProgreso = null,
            CancellationToken ct = default)
        {
            var resultado = new ResultadoPipeline();

            // Telemetría de tokens: abrir bucket por instrucción completa.
            string instruccionId = ConsumoTokensTracker.Instancia.IniciarEjecucion(instruccion);

            // Tracing (Fase 1A): correlación 1-a-1 con el bucket de tokens.
            TracerEjecucion.Instancia.IniciarTrace(
                instruccionId, instruccion, ctx.Modelo, ctx.Servicio.ToString(), ctx.RutaArchivo);

            using var spanPipeline = TracerEjecucion.Instancia.AbrirSpan(
                SpanTipo.Pipeline, "Pipeline 4-Agentes");
            spanPipeline.RegistrarInput(instruccion);
            spanPipeline.AgregarAtributo("modelo", ctx.Modelo);
            spanPipeline.AgregarAtributo("servicio", ctx.Servicio.ToString());

            try
            {
                // ── AGENTE 1: PLANIFICADOR ───────────────────────────────────
                resultado.RegistrarEvento("AGENTE 1 [Planificador] → Iniciando...");
                onProgreso?.Invoke("\n╔══════════════════════════════════════╗");
                onProgreso?.Invoke("║  AGENTE 1: PLANIFICADOR               ║");
                onProgreso?.Invoke("╚══════════════════════════════════════╝");

                var spanPlan = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Planificador");
                spanPlan.RegistrarInput(instruccion);
                string planTexto = await EjecutarAgentePlanificadorAsync(instruccion, ctx, ct);
                spanPlan.RegistrarOutput(planTexto);
                spanPlan.Dispose();
                resultado.PlanGenerado = planTexto;
                resultado.RegistrarEvento($"AGENTE 1 completado. Plan: {Truncar(planTexto, 100)}");
                onProgreso?.Invoke($"✓ Plan generado:\n{planTexto}");

                ct.ThrowIfCancellationRequested();

                // ── AGENTE 2: EJECUTOR DE CÓDIGO ─────────────────────────────
                resultado.RegistrarEvento("AGENTE 2 [Ejecutor] → Iniciando...");
                onProgreso?.Invoke("\n╔══════════════════════════════════════╗");
                onProgreso?.Invoke("║  AGENTE 2: EJECUTOR DE CÓDIGO         ║");
                onProgreso?.Invoke("╚══════════════════════════════════════╝");

                var spanEjec = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Ejecutor");
                spanEjec.RegistrarInput(planTexto);
                string codigoGenerado = await EjecutarAgenteEjecutorAsync(
                    instruccion, planTexto, ctx, ct);
                spanEjec.RegistrarOutput(codigoGenerado);
                spanEjec.Dispose();
                resultado.CodigoEjecutado = codigoGenerado;
                resultado.RegistrarEvento($"AGENTE 2 completado. Código: {Truncar(codigoGenerado, 100)}");
                onProgreso?.Invoke($"✓ Código/acciones generadas:\n{codigoGenerado}");

                ct.ThrowIfCancellationRequested();

                // ── AGENTE 3: VERIFICADOR DE RESULTADOS ──────────────────────
                resultado.RegistrarEvento("AGENTE 3 [Verificador] → Iniciando...");
                onProgreso?.Invoke("\n╔══════════════════════════════════════╗");
                onProgreso?.Invoke("║  AGENTE 3: VERIFICADOR DE RESULTADOS  ║");
                onProgreso?.Invoke("╚══════════════════════════════════════╝");

                var spanVerif = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Verificador");
                spanVerif.RegistrarInput(codigoGenerado);
                string verificacion = await EjecutarAgenteVerificadorAsync(
                    instruccion, planTexto, codigoGenerado, ctx, ct);
                spanVerif.RegistrarOutput(verificacion);
                spanVerif.Dispose();
                resultado.ResultadoVerificacion = verificacion;
                resultado.RegistrarEvento($"AGENTE 3 completado. Verificación: {Truncar(verificacion, 100)}");
                onProgreso?.Invoke($"✓ Verificación:\n{verificacion}");

                ct.ThrowIfCancellationRequested();

                // ── AGENTE 4: FORMATEADOR DE SALIDA ──────────────────────────
                resultado.RegistrarEvento("AGENTE 4 [Formateador] → Iniciando...");
                onProgreso?.Invoke("\n╔══════════════════════════════════════╗");
                onProgreso?.Invoke("║  AGENTE 4: FORMATEADOR DE SALIDA      ║");
                onProgreso?.Invoke("╚══════════════════════════════════════╝");

                var spanFmt = TracerEjecucion.Instancia.AbrirSpan(SpanTipo.Fase, "Formateador");
                spanFmt.RegistrarInput(verificacion);
                string salidaFinal = await EjecutarAgenteFormateadorAsync(
                    instruccion, planTexto, codigoGenerado, verificacion, ctx, ct);
                spanFmt.RegistrarOutput(salidaFinal);
                spanFmt.Dispose();
                resultado.SalidaFormateada = salidaFinal;
                resultado.RegistrarEvento("AGENTE 4 completado. Pipeline finalizado.");
                onProgreso?.Invoke($"\n{'═',40}");
                onProgreso?.Invoke("RESULTADO FINAL DEL PIPELINE:");
                onProgreso?.Invoke($"{'═',40}");
                onProgreso?.Invoke(salidaFinal);

                resultado.Exitoso = true;

                // ── AGENTE 5: MEMORISTA (fire-and-forget) ────────────────
                // Corre después de que el usuario ya tiene su respuesta.
                // No bloquea el retorno del pipeline ni suma latencia.
                // Usa CancellationToken.None para no perder memoria si la
                // UI cancela justo al terminar.
                _ = AgenteMemorista.EjecutarAsync(
                    instruccion, salidaFinal, ctx, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                resultado.RegistrarEvento("Pipeline cancelado por el usuario.");
                resultado.SalidaFormateada = "Pipeline cancelado.";
                spanPipeline.MarcarCancelado();
                try { TracerEjecucion.Instancia.FinalizarTrace(SpanEstado.Cancelado); } catch { }
            }
            catch (Exception ex)
            {
                resultado.RegistrarEvento($"Error en pipeline: {ex.Message}");
                resultado.SalidaFormateada = $"Error en pipeline: {ex.Message}";
                onProgreso?.Invoke($"❌ Error: {ex.Message}");
                spanPipeline.MarcarError(ex.Message);
                try { TracerEjecucion.Instancia.FinalizarTrace(SpanEstado.Error, ex.Message); } catch { }
            }
            finally
            {
                try { ConsumoTokensTracker.Instancia.FinalizarEjecucion(); } catch { }
                spanPipeline.RegistrarOutput(resultado.SalidaFormateada);
                // Si no pasó por catch (camino feliz), cerrar el trace normalmente.
                try { TracerEjecucion.Instancia.FinalizarTrace(SpanEstado.Ok); } catch { }
            }

            return resultado;
        }

        // =====================================================================
        //  AGENTE 1 — PLANIFICADOR
        //  Descompone la tarea en un plan estructurado detallado
        // =====================================================================

        private static async Task<string> EjecutarAgentePlanificadorAsync(
            string instruccion, AgentContext ctx, CancellationToken ct)
        {
            const string sistemaPlanificador = @"Eres el AGENTE PLANIFICADOR dentro de un pipeline de IA multi-agente.

Tu responsabilidad EXCLUSIVA es crear un plan de ejecución claro y detallado.
El plan será procesado por otros agentes especializados del pipeline.

FORMATO DE SALIDA:
## PLAN DE EJECUCIÓN

**Objetivo:** [objetivo claro en 1 oración]

**Análisis de la tarea:**
[2-3 oraciones analizando qué se necesita hacer]

**Pasos del plan:**
1. [Paso concreto y específico]
2. [Paso concreto y específico]
...

**Consideraciones técnicas:**
[Herramientas, librerías, APIs o recursos necesarios]

**Criterios de éxito:**
[Cómo saber que la tarea se completó correctamente]";

            return await AIModelConector.ObtenerRespuestaLLMAsync(
                instruccion,
                ctx.ComoFase("Planificador").ConPromptPersonalizado(sistemaPlanificador),
                ct);
        }

        // =====================================================================
        //  AGENTE 2 — EJECUTOR DE CÓDIGO
        //  Recibe el plan y genera el código/acciones necesarias
        // =====================================================================

        private static async Task<string> EjecutarAgenteEjecutorAsync(
            string instruccionOriginal,
            string planGenerado,
            AgentContext ctx,
            CancellationToken ct)
        {
            string sistemaEjecutor = $@"Eres el AGENTE EJECUTOR dentro de un pipeline de IA multi-agente.

Recibes un plan del AGENTE PLANIFICADOR y debes implementarlo generando
el código Python, scripts o acciones concretas necesarias.

INSTRUCCIÓN ORIGINAL DEL USUARIO:
{instruccionOriginal}

PLAN RECIBIDO DEL AGENTE PLANIFICADOR:
{planGenerado}

TU RESPONSABILIDAD:
- Implementa CADA paso del plan con código concreto y funcional.
- El código debe ser Python, completo y ejecutable.
- Incluye manejo de errores básico.
- Comenta brevemente las secciones principales.
- Si no se necesita código (tarea de análisis/texto), entrega la implementación en texto estructurado.";

            return await AIModelConector.ObtenerRespuestaLLMAsync(
                "Implementa el plan recibido del Agente Planificador.",
                ctx.ComoFase("Ejecutor").ConPromptPersonalizado(sistemaEjecutor),
                ct);
        }

        // =====================================================================
        //  AGENTE 3 — VERIFICADOR DE RESULTADOS
        //  Valida que el código/output del Ejecutor sea correcto
        // =====================================================================

        private static async Task<string> EjecutarAgenteVerificadorAsync(
            string instruccionOriginal,
            string plan,
            string codigoEjecutor,
            AgentContext ctx,
            CancellationToken ct)
        {
            string sistemaVerificador = $@"Eres el AGENTE VERIFICADOR dentro de un pipeline de IA multi-agente.

Recibes el plan original y el código/output del AGENTE EJECUTOR.
Tu responsabilidad es verificar la calidad y corrección del trabajo.

INSTRUCCIÓN ORIGINAL DEL USUARIO:
{instruccionOriginal}

PLAN DEL AGENTE PLANIFICADOR:
{plan}

OUTPUT DEL AGENTE EJECUTOR:
{codigoEjecutor}

TAREAS DE VERIFICACIÓN:
1. ¿El código/output responde a la instrucción original?
2. ¿Se cubren todos los pasos del plan?
3. ¿Hay errores evidentes, bugs o problemas de lógica?
4. ¿Falta algo importante?
5. ¿Qué mejoras se podrían hacer?

FORMATO DE SALIDA:
## VERIFICACIÓN

**Estado general:** ✅ Correcto / ⚠️ Con observaciones / ❌ Con errores

**Cobertura del plan:** [X/Y pasos cubiertos]

**Problemas detectados:**
- [Problema 1 o 'Ninguno']

**Correcciones aplicadas:**
[Si hay errores, proporciona la versión corregida]

**Recomendaciones:**
[Mejoras opcionales]";

            return await AIModelConector.ObtenerRespuestaLLMAsync(
                "Verifica el output del Agente Ejecutor.",
                ctx.ComoFase("Verificador").ConPromptPersonalizado(sistemaVerificador),
                ct);
        }

        // =====================================================================
        //  AGENTE 4 — FORMATEADOR DE SALIDA
        //  Toma todos los outputs y genera una respuesta final pulida
        // =====================================================================

        private static async Task<string> EjecutarAgenteFormateadorAsync(
            string instruccionOriginal,
            string plan,
            string codigoEjecutor,
            string verificacion,
            AgentContext ctx,
            CancellationToken ct)
        {
            string sistemaFormateador = $@"Eres el AGENTE FORMATEADOR, el último eslabón del pipeline de IA multi-agente.

Recibes los outputs de los 3 agentes anteriores y debes producir
la RESPUESTA FINAL que verá el usuario. Debe ser clara, completa y profesional.

INSTRUCCIÓN ORIGINAL DEL USUARIO:
{instruccionOriginal}

PLAN (Agente 1):
{plan}

IMPLEMENTACIÓN (Agente 2):
{codigoEjecutor}

VERIFICACIÓN (Agente 3):
{verificacion}

TU RESPONSABILIDAD:
- Sintetiza toda la información en una respuesta final limpia y útil.
- Si hay código, preséntalos correctamente formateado.
- Si hay correcciones del verificador, usa la versión corregida.
- Sé directo y útil. El usuario quiere ver el RESULTADO, no el proceso interno.
- Puedes usar emojis moderadamente para mejorar la legibilidad.";

            return await AIModelConector.ObtenerRespuestaLLMAsync(
                "Genera la respuesta final del pipeline para el usuario.",
                ctx.ComoFase("Formateador").ConPromptPersonalizado(sistemaFormateador),
                ct);
        }

        // =====================================================================
        //  UTILIDADES
        // =====================================================================

        private static string Truncar(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";
    }
}
