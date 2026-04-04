using Newtonsoft.Json.Linq;
using System.Text;

namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Herramienta: escribir_archivo
    /// Permite al agente crear o sobreescribir archivos de texto.
    /// </summary>
    public class HerramientaEscribirArchivo : IHerramienta
    {
        public string Nombre => "escribir_archivo";

        public string Descripcion =>
            "Crea o sobreescribe un archivo con el contenido especificado. " +
            "Si el directorio no existe, lo crea automáticamente. " +
            "Úsala para guardar resultados, crear scripts, generar reportes, etc.";

        public JObject EsquemaParametros => JObject.Parse("""
            {
              "type": "object",
              "properties": {
                "ruta": {
                  "type": "string",
                  "description": "Ruta absoluta o relativa donde crear/sobreescribir el archivo."
                },
                "contenido": {
                  "type": "string",
                  "description": "Contenido a escribir en el archivo."
                },
                "agregar": {
                  "type": "boolean",
                  "description": "Si es true, agrega al final del archivo en lugar de sobreescribir. Por defecto false."
                }
              },
              "required": ["ruta", "contenido"]
            }
            """);

        public async Task<string> EjecutarAsync(JObject parametros, CancellationToken ct = default)
        {
            string ruta = parametros["ruta"]?.ToString() ?? "";
            string contenido = parametros["contenido"]?.ToString() ?? "";
            bool agregar = parametros["agregar"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(ruta))
                return "Error: Se requiere el parámetro 'ruta'.";

            try
            {
                string? directorio = Path.GetDirectoryName(ruta);
                if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
                    Directory.CreateDirectory(directorio);

                if (agregar)
                    await File.AppendAllTextAsync(ruta, contenido, Encoding.UTF8, ct);
                else
                    await File.WriteAllTextAsync(ruta, contenido, Encoding.UTF8, ct);

                string accion = agregar ? "agregado" : "escrito";
                return $"Archivo '{ruta}' {accion} exitosamente. " +
                       $"Tamaño: {new FileInfo(ruta).Length} bytes.";
            }
            catch (Exception ex)
            {
                return $"Error al escribir '{ruta}': {ex.Message}";
            }
        }
    }
}
