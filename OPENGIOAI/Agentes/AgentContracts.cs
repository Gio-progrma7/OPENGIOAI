using OPENGIOAI.Data;
using OPENGIOAI.Entidades;

namespace OPENGIOAI.Agentes
{
    /// <summary>
    /// Estado general de una ejecucion agentica. Sirve como contrato comun para UI,
    /// ARNES, tracing y futuros orquestadores.
    /// </summary>
    public enum EstadoEjecucionAgente
    {
        Pendiente,
        Ejecutando,
        Completado,
        Fallido,
        Cancelado
    }

    /// <summary>
    /// Contexto minimo e inmutable que identifica una corrida completa del agente.
    /// No reemplaza a AgentContext: este describe la llamada al LLM; AgentRunContext
    /// describe la ejecucion end-to-end.
    /// </summary>
    public sealed record AgentRunContext(
        string RunId,
        string Instruccion,
        DateTime InicioUtc,
        string RutaTrabajo,
        string Modelo,
        Servicios Servicio);

    /// <summary>
    /// Resultado tipado de una fase. Permite mover datos entre agentes sin depender
    /// de archivos temporales como canal interno.
    /// </summary>
    public sealed record AgentStepResult(
        FaseAgente Fase,
        bool Exitoso,
        string Salida,
        string? Error = null,
        TimeSpan? Duracion = null)
    {
        public static AgentStepResult Ok(
            FaseAgente fase,
            string salida,
            TimeSpan? duracion = null) =>
            new(fase, true, salida, null, duracion);

        public static AgentStepResult Fail(
            FaseAgente fase,
            string error,
            string salida = "",
            TimeSpan? duracion = null) =>
            new(fase, false, salida, error, duracion);
    }

    /// <summary>
    /// Contrato para futuras fases intercambiables del pipeline ARIA.
    /// </summary>
    public interface IAgentStep
    {
        FaseAgente Fase { get; }

        Task<AgentStepResult> EjecutarAsync(
            AgentRunContext run,
            AgentContext ctx,
            CancellationToken ct = default);
    }
}
