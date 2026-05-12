namespace OPENGIOAI.Modulos.Arnes
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Modelo de datos estándar para todas las peticiones y respuestas
    /// que entran y salen del módulo ARNES.
    /// </summary>
    public class ArnesPayload
    {
        /// <summary>
        /// El origen de la petición (ej. "antigravity", "codex", "claude_extension").
        /// </summary>
        [JsonProperty("source_app")]
        public string SourceApp { get; set; } = string.Empty;

        /// <summary>
        /// La acción a realizar (ej. "execute_aria", "ping", "stop").
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Parámetros específicos de la acción (instrucciones, rutas, configuraciones).
        /// </summary>
        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// (Solo para respuestas) El estado de la ejecución: "success", "error", "processing".
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; } = "success";

        /// <summary>
        /// (Solo para respuestas) El mensaje de resultado o detalles del error.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}
