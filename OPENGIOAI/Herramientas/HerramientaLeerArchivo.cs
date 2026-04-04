using Newtonsoft.Json.Linq;

namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Herramienta: leer_archivo
    /// Permite al agente leer el contenido de cualquier archivo de texto.
    /// </summary>
    public class HerramientaLeerArchivo : IHerramienta
    {
        public string Nombre => "leer_archivo";

        public string Descripcion =>
            "Lee el contenido completo de un archivo de texto. " +
            "Úsala para leer código fuente, configuraciones, logs, datos CSV, JSON, etc. " +
            "Devuelve el contenido como texto plano.";

        public JObject EsquemaParametros => JObject.Parse("""
            {
              "type": "object",
              "properties": {
                "ruta": {
                  "type": "string",
                  "description": "Ruta absoluta o relativa del archivo a leer."
                },
                "encoding": {
                  "type": "string",
                  "description": "Encoding del archivo. Por defecto UTF-8. Opciones: utf-8, latin-1, ascii."
                }
              },
              "required": ["ruta"]
            }
            """);

        public async Task<string> EjecutarAsync(JObject parametros, CancellationToken ct = default)
        {
            string ruta = parametros["ruta"]?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(ruta))
                return "Error: Se requiere el parámetro 'ruta'.";

            if (!File.Exists(ruta))
                return $"Error: El archivo '{ruta}' no existe.";

            try
            {
                string encodingNombre = parametros["encoding"]?.ToString() ?? "utf-8";
                var encoding = encodingNombre.ToLowerInvariant() switch
                {
                    "latin-1" or "latin1" or "iso-8859-1" => System.Text.Encoding.Latin1,
                    "ascii" => System.Text.Encoding.ASCII,
                    _ => System.Text.Encoding.UTF8
                };

                string contenido = await File.ReadAllTextAsync(ruta, encoding, ct);
                long tamano = new FileInfo(ruta).Length;

                return $"Archivo: {ruta} ({FormatearTamano(tamano)})\n" +
                       $"Líneas: {contenido.Split('\n').Length}\n" +
                       $"---\n{contenido}";
            }
            catch (Exception ex)
            {
                return $"Error al leer '{ruta}': {ex.Message}";
            }
        }

        private static string FormatearTamano(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}
