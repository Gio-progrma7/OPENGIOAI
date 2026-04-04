namespace OPENGIOAI.Entidades
{
    /// <summary>
    /// Representa una llamada a herramienta solicitada por el LLM durante
    /// el bucle agéntico (Tool Use / Function Calling).
    /// </summary>
    public class LlamadaHerramienta
    {
        /// <summary>
        /// ID único de la llamada (generado por el proveedor).
        /// Se usa para correlacionar el resultado con la solicitud.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Nombre de la herramienta a ejecutar (ej: "leer_archivo").
        /// </summary>
        public string Nombre { get; set; } = "";

        /// <summary>
        /// Argumentos en formato JSON string tal como los envió el LLM.
        /// </summary>
        public string ArgumentosJson { get; set; } = "{}";
    }
}
