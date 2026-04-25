// ============================================================
//  HistorialResumidor.cs  — Fase 2 (historial comprimido)
//
//  QUÉ HACE:
//    Toma los turnos que la ventana conversacional está a punto
//    de expulsar y los comprime en ≤5 bullets conservando:
//      · Decisiones tomadas
//      · Archivos/skills tocados
//      · Objetivos pendientes
//    El resumen se FUSIONA con el resumen previo (si existe) —
//    compresión incremental estilo "ConversationSummaryBuffer".
//
//  POR QUÉ:
//    Mantener coherencia multi-turno sin que cada llamada pague
//    el historial completo. Complementa prompt caching (Fase 1):
//    el resumen es estable entre turnos hasta que se expulsa un
//    turno nuevo, así que puede cachearse en Anthropic.
//
//  TOLERANTE A FALLOS:
//    Si la llamada al LLM falla (red caída, rate limit, modelo no
//    configurado), devuelve el resumen previo sin modificar. El
//    caller decide si igualmente expulsa los turnos (perdemos
//    contexto antiguo) o los reintenta en la próxima expulsión.
// ============================================================

using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosAI
{
    public static class HistorialResumidor
    {
        // Presupuesto aproximado del resumen en tokens. Si el LLM lo
        // respeta a rajatabla el bloque cacheable queda < 400 tokens.
        private const int PresupuestoTokensResumen = 350;

        // Chars mínimos de un turno para valer la pena resumirlo.
        // Turnos muy cortos ("ok", "sí") no aportan señal.
        private const int MinCharsTurno = 20;

        /// <summary>
        /// Comprime los turnos expulsados en un nuevo resumen acumulado.
        /// Devuelve el resumen previo sin modificar si el LLM falla o si
        /// los turnos no tienen contenido significativo.
        /// </summary>
        public static async Task<string> ResumirAsync(
            string resumenPrevio,
            IReadOnlyList<ConversationTurn> expulsados,
            Servicios servicio,
            string modelo,
            string apiKey,
            CancellationToken ct = default)
        {
            if (expulsados == null || expulsados.Count == 0)
                return resumenPrevio ?? "";

            // Si todos los turnos son triviales no llamamos al LLM.
            bool haySenal = expulsados.Any(t =>
                (t.Instruccion?.Length ?? 0) + (t.RespuestaAgente?.Length ?? 0) >= MinCharsTurno);
            if (!haySenal) return resumenPrevio ?? "";

            using var span = TracerEjecucion.Instancia.AbrirSpan(
                SpanTipo.Memoria, "Historial resumen");
            span.AgregarAtributo("turnos_expulsados", expulsados.Count.ToString());

            try
            {
                string promptSistema = ConstruirPromptSistema();
                string payload       = ConstruirPayloadUsuario(resumenPrevio ?? "", expulsados);

                var ctxBase = await AgentContext.BuildAsync(
                    rutaArchivo: "",
                    modelo: modelo,
                    apiKey: apiKey,
                    servicio: servicio,
                    soloChat: false,
                    clavesDisponibles: "",
                    ct: ct,
                    perfil: PerfilContexto.Minimo);

                var ctxResumidor = ctxBase
                    .ConPromptPersonalizado(promptSistema)
                    .ComoFase("Resumidor");

                string respuesta = await AIModelConector.ObtenerRespuestaLLMAsync(
                    payload, ctxResumidor, ct);

                string nuevo = LimpiarRespuesta(respuesta);
                if (string.IsNullOrWhiteSpace(nuevo))
                    return resumenPrevio ?? "";

                span.RegistrarOutput(nuevo);
                return nuevo;
            }
            catch (OperationCanceledException)
            {
                span.MarcarCancelado();
                // En cancelación preservamos el resumen previo intacto.
                return resumenPrevio ?? "";
            }
            catch (Exception ex)
            {
                span.MarcarError(ex.Message);
                // Best-effort: si algo falla, el caller sigue con el previo.
                return resumenPrevio ?? "";
            }
        }

        // ──────────────────────────────────────────────────────────────

        private static string ConstruirPromptSistema() =>
$@"Eres un resumidor de conversaciones técnicas. Recibes un resumen previo (puede estar vacío) y turnos nuevos a integrar. Tu salida ES el nuevo resumen fusionado.

REGLAS ESTRICTAS:
· Máximo 5 bullets, cada uno ≤ 25 palabras.
· Presupuesto total: ≈ {PresupuestoTokensResumen} tokens.
· Conservar SOLO: decisiones tomadas, archivos/skills tocados, objetivos pendientes, errores no resueltos.
· Descartar: saludos, confirmaciones, texto que no afecte futuras decisiones.
· Fusionar con el resumen previo — no duplicar hechos que ya aparecen ahí.
· Responder SOLO con los bullets en markdown. Sin preámbulo, sin conclusión, sin comillas.

Formato:
- ...
- ...
- ...
";

        private static string ConstruirPayloadUsuario(
            string resumenPrevio, IReadOnlyList<ConversationTurn> expulsados)
        {
            var sb = new StringBuilder();

            sb.AppendLine("RESUMEN PREVIO:");
            sb.AppendLine(string.IsNullOrWhiteSpace(resumenPrevio)
                ? "(vacío — primera compresión de esta conversación)"
                : resumenPrevio.Trim());
            sb.AppendLine();
            sb.AppendLine("TURNOS NUEVOS A INTEGRAR:");

            int i = 1;
            foreach (var t in expulsados)
            {
                sb.AppendLine($"--- turno {i++} ({t.Timestamp:HH:mm}) ---");
                if (!string.IsNullOrWhiteSpace(t.Instruccion))
                    sb.AppendLine("Usuario: " + Truncar(t.Instruccion, 600));
                if (!string.IsNullOrWhiteSpace(t.RespuestaAgente))
                    sb.AppendLine("Agente:  " + Truncar(t.RespuestaAgente, 800));
            }

            return sb.ToString();
        }

        private static string LimpiarRespuesta(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            // El LLM a veces envuelve en ``` o agrega preámbulo corto.
            string r = raw.Trim();
            if (r.StartsWith("```"))
            {
                int primerSalto = r.IndexOf('\n');
                if (primerSalto > 0) r = r[(primerSalto + 1)..];
                if (r.EndsWith("```")) r = r[..^3];
                r = r.Trim();
            }
            return r;
        }

        private static string Truncar(string texto, int maxChars)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto.Length <= maxChars ? texto : texto[..maxChars] + "…";
        }
    }
}
