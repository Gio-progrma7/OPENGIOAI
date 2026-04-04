namespace OPENGIOAI.Entidades
{
    /// <summary>
    /// Respuesta del LLM que puede contener texto y/o llamadas a herramientas.
    /// Usado por MotorHerramientas para decidir si continuar el bucle agéntico.
    /// </summary>
    public class RespuestaLLM
    {
        /// <summary>
        /// Texto de respuesta del LLM (puede estar vacío si solo hay tool calls).
        /// </summary>
        public string Texto { get; set; } = "";

        /// <summary>
        /// Lista de herramientas solicitadas por el LLM en esta iteración.
        /// </summary>
        public List<LlamadaHerramienta> LlamadasHerramientas { get; set; } = [];

        /// <summary>
        /// True si el LLM solicitó ejecutar al menos una herramienta.
        /// </summary>
        public bool TieneLlamadasHerramientas => LlamadasHerramientas.Count > 0;
    }
}
