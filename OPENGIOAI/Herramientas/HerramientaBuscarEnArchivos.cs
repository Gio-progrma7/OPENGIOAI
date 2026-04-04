using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Herramienta: buscar_en_archivos
    /// Permite al agente buscar texto o patrones dentro de archivos de un directorio.
    /// </summary>
    public class HerramientaBuscarEnArchivos : IHerramienta
    {
        public string Nombre => "buscar_en_archivos";

        public string Descripcion =>
            "Busca texto o expresiones regulares dentro de archivos de un directorio. " +
            "Devuelve los archivos y las líneas donde se encontró la coincidencia. " +
            "Útil para encontrar funciones, variables, errores en logs, patrones en datos, etc.";

        public JObject EsquemaParametros => JObject.Parse("""
            {
              "type": "object",
              "properties": {
                "directorio": {
                  "type": "string",
                  "description": "Directorio raíz donde buscar."
                },
                "patron": {
                  "type": "string",
                  "description": "Texto literal o expresión regular a buscar."
                },
                "extension": {
                  "type": "string",
                  "description": "Extensión de archivos donde buscar (ej: .py, .txt, .cs). Por defecto busca en todos."
                },
                "es_regex": {
                  "type": "boolean",
                  "description": "Si es true, 'patron' se interpreta como expresión regular. Por defecto false."
                },
                "ignorar_mayusculas": {
                  "type": "boolean",
                  "description": "Si es true, la búsqueda ignora mayúsculas/minúsculas. Por defecto true."
                },
                "max_resultados": {
                  "type": "integer",
                  "description": "Máximo número de coincidencias a devolver. Por defecto 50."
                },
                "contexto_lineas": {
                  "type": "integer",
                  "description": "Número de líneas de contexto antes y después de cada coincidencia. Por defecto 0."
                }
              },
              "required": ["directorio", "patron"]
            }
            """);

        public async Task<string> EjecutarAsync(JObject parametros, CancellationToken ct = default)
        {
            string directorio = parametros["directorio"]?.ToString() ?? "";
            string patron = parametros["patron"]?.ToString() ?? "";
            string extension = parametros["extension"]?.ToString() ?? "";
            bool esRegex = parametros["es_regex"]?.Value<bool>() ?? false;
            bool ignorarMayusculas = parametros["ignorar_mayusculas"]?.Value<bool>() ?? true;
            int maxResultados = parametros["max_resultados"]?.Value<int>() ?? 50;
            int contextoLineas = parametros["contexto_lineas"]?.Value<int>() ?? 0;

            if (string.IsNullOrWhiteSpace(directorio) || string.IsNullOrWhiteSpace(patron))
                return "Error: Se requieren los parámetros 'directorio' y 'patron'.";

            if (!Directory.Exists(directorio))
                return $"Error: El directorio '{directorio}' no existe.";

            string globPattern = string.IsNullOrEmpty(extension) ? "*" :
                (extension.StartsWith(".") ? $"*{extension}" : extension);

            var archivos = Directory.GetFiles(directorio, globPattern, SearchOption.AllDirectories);
            var sb = new StringBuilder();
            int totalCoincidencias = 0;
            int archivosConCoincidencias = 0;

            var regexOpciones = ignorarMayusculas
                ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            foreach (var archivo in archivos)
            {
                ct.ThrowIfCancellationRequested();
                if (totalCoincidencias >= maxResultados) break;

                try
                {
                    var lineas = await File.ReadAllLinesAsync(archivo, ct);
                    var coincidenciasArchivo = new List<(int numero, string linea)>();

                    for (int i = 0; i < lineas.Length && totalCoincidencias < maxResultados; i++)
                    {
                        bool coincide = esRegex
                            ? Regex.IsMatch(lineas[i], patron, regexOpciones)
                            : lineas[i].Contains(patron,
                                ignorarMayusculas
                                    ? StringComparison.OrdinalIgnoreCase
                                    : StringComparison.Ordinal);

                        if (coincide)
                        {
                            coincidenciasArchivo.Add((i + 1, lineas[i]));
                            totalCoincidencias++;
                        }
                    }

                    if (coincidenciasArchivo.Count > 0)
                    {
                        archivosConCoincidencias++;
                        sb.AppendLine($"\n📄 {archivo}:");

                        foreach (var (numero, linea) in coincidenciasArchivo)
                        {
                            // Contexto antes
                            if (contextoLineas > 0)
                                for (int k = Math.Max(0, numero - 1 - contextoLineas); k < numero - 1; k++)
                                    sb.AppendLine($"  {k + 1,5}  {lineas[k]}");

                            sb.AppendLine($"► {numero,5}  {linea.Trim()}");

                            // Contexto después
                            if (contextoLineas > 0)
                                for (int k = numero; k < Math.Min(lineas.Length, numero + contextoLineas); k++)
                                    sb.AppendLine($"  {k + 1,5}  {lineas[k]}");
                        }
                    }
                }
                catch { /* Archivo no legible (binario, permisos), ignorar */ }
            }

            if (totalCoincidencias == 0)
                return $"No se encontraron coincidencias para '{patron}' en '{directorio}'.";

            string resumen = $"Encontradas {totalCoincidencias} coincidencias en {archivosConCoincidencias} archivos";
            if (totalCoincidencias >= maxResultados)
                resumen += $" (limitado a {maxResultados})";

            return resumen + ":\n" + sb.ToString();
        }
    }
}
