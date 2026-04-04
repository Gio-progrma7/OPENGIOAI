using Newtonsoft.Json.Linq;

namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Contrato que debe implementar cada herramienta disponible para el agente.
    /// El LLM ve Nombre + Descripcion + EsquemaParametros para decidir cuándo usarla.
    /// </summary>
    public interface IHerramienta
    {
        /// <summary>
        /// Identificador único de la herramienta. El LLM lo usa en las tool_calls.
        /// Solo letras, números y guiones bajos. Ej: "leer_archivo".
        /// </summary>
        string Nombre { get; }

        /// <summary>
        /// Descripción clara de qué hace la herramienta y cuándo usarla.
        /// El LLM la lee para decidir si esta herramienta es la apropiada.
        /// </summary>
        string Descripcion { get; }

        /// <summary>
        /// JSON Schema que describe los parámetros de la herramienta.
        /// Formato estándar OpenAPI / JSON Schema Draft 7.
        /// </summary>
        JObject EsquemaParametros { get; }

        /// <summary>
        /// Ejecuta la herramienta con los parámetros dados.
        /// Siempre devuelve string — errores incluidos (nunca lanza excepciones al caller).
        /// </summary>
        Task<string> EjecutarAsync(JObject parametros, CancellationToken ct = default);
    }
}
