// ============================================================
//  PasoDelPlan.cs  — Entidades del sistema de planificación
//
//  Usadas por:
//    AgentePlanificador (1.3) — bucle ReAct individual
//    PipelineMultiAgente (1.4) — pipeline de 4 agentes especializados
// ============================================================

namespace OPENGIOAI.Agentes
{
    public enum EstadoPaso
    {
        Pendiente,
        Ejecutando,
        Completado,
        Fallido
    }

    /// <summary>
    /// Representa un paso individual dentro de un plan de ejecución.
    /// Registra descripción, estado, resultado y número de intentos.
    /// </summary>
    public class PasoDelPlan
    {
        public int Numero { get; set; }
        public string Descripcion { get; set; } = "";
        public EstadoPaso Estado { get; set; } = EstadoPaso.Pendiente;
        public string Resultado { get; set; } = "";
        public string Error { get; set; } = "";
        public int Intentos { get; set; } = 0;

        public override string ToString() =>
            $"[{Estado}] Paso {Numero}: {Descripcion}";
    }

    /// <summary>
    /// Plan completo generado por el agente planificador.
    /// Contiene el objetivo global y la lista de pasos a ejecutar.
    /// </summary>
    public class Plan
    {
        public string Objetivo { get; set; } = "";
        public List<PasoDelPlan> Pasos { get; set; } = new();

        public int TotalPasos => Pasos.Count;
        public int PasosCompletados => Pasos.Count(p => p.Estado == EstadoPaso.Completado);
        public int PasosFallidos => Pasos.Count(p => p.Estado == EstadoPaso.Fallido);
        public bool TodosCompletados => Pasos.All(p => p.Estado == EstadoPaso.Completado);
    }

    /// <summary>
    /// Resultado final del pipeline multi-agente (1.4).
    /// Contiene las salidas de cada agente especializado.
    /// </summary>
    public class ResultadoPipeline
    {
        public string PlanGenerado { get; set; } = "";
        public string CodigoEjecutado { get; set; } = "";
        public string ResultadoVerificacion { get; set; } = "";
        public string SalidaFormateada { get; set; } = "";
        public bool Exitoso { get; set; }
        public List<string> Bitacora { get; set; } = new();

        public void RegistrarEvento(string mensaje)
        {
            Bitacora.Add($"[{DateTime.Now:HH:mm:ss}] {mensaje}");
        }
    }
}
