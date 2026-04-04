using Newtonsoft.Json.Linq;
using System.Text;

namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Herramienta: listar_directorio
    /// Permite al agente explorar la estructura de carpetas del sistema de archivos.
    /// </summary>
    public class HerramientaListarDirectorio : IHerramienta
    {
        public string Nombre => "listar_directorio";

        public string Descripcion =>
            "Lista archivos y subdirectorios de una carpeta. " +
            "Muestra nombres, tamaños y fechas de modificación. " +
            "Úsala para explorar proyectos, encontrar archivos y entender la estructura del sistema.";

        public JObject EsquemaParametros => JObject.Parse("""
            {
              "type": "object",
              "properties": {
                "ruta": {
                  "type": "string",
                  "description": "Ruta del directorio a listar."
                },
                "patron": {
                  "type": "string",
                  "description": "Patrón de búsqueda opcional (ej: *.py, *.txt, *.cs). Por defecto lista todo."
                },
                "recursivo": {
                  "type": "boolean",
                  "description": "Si es true, incluye subdirectorios recursivamente. Por defecto false."
                },
                "incluir_ocultos": {
                  "type": "boolean",
                  "description": "Si es true, incluye archivos ocultos (que empiezan con punto). Por defecto false."
                }
              },
              "required": ["ruta"]
            }
            """);

        public Task<string> EjecutarAsync(JObject parametros, CancellationToken ct = default)
        {
            string ruta = parametros["ruta"]?.ToString() ?? "";
            string patron = parametros["patron"]?.ToString() ?? "*";
            bool recursivo = parametros["recursivo"]?.Value<bool>() ?? false;
            bool incluirOcultos = parametros["incluir_ocultos"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(ruta))
                return Task.FromResult("Error: Se requiere el parámetro 'ruta'.");

            if (!Directory.Exists(ruta))
                return Task.FromResult($"Error: El directorio '{ruta}' no existe.");

            try
            {
                var opcion = recursivo ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var sb = new StringBuilder();
                sb.AppendLine($"Directorio: {ruta}");
                sb.AppendLine(new string('-', 60));

                // Subdirectorios
                var dirs = Directory.GetDirectories(ruta, "*", opcion)
                    .Where(d => incluirOcultos || !Path.GetFileName(d).StartsWith('.'))
                    .OrderBy(d => d);

                foreach (var dir in dirs)
                {
                    string relativo = Path.GetRelativePath(ruta, dir);
                    sb.AppendLine($"[DIR]  {relativo}/");
                }

                // Archivos
                var archivos = Directory.GetFiles(ruta, patron, opcion)
                    .Where(f => incluirOcultos || !Path.GetFileName(f).StartsWith('.'))
                    .OrderBy(f => f);

                int countArchivos = 0;
                foreach (var archivo in archivos)
                {
                    var info = new FileInfo(archivo);
                    string relativo = Path.GetRelativePath(ruta, archivo);
                    sb.AppendLine($"[FILE] {relativo,-50} {FormatearTamano(info.Length),8}  {info.LastWriteTime:yyyy-MM-dd HH:mm}");
                    countArchivos++;
                }

                sb.AppendLine(new string('-', 60));
                sb.AppendLine($"Total: {dirs.Count()} directorios, {countArchivos} archivos.");

                return Task.FromResult(sb.ToString());
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Error al listar '{ruta}': {ex.Message}");
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
