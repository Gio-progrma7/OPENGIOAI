using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class MarkdownFileManager
    {
        /// <summary>
        /// Escribe contenido en un archivo .md (sobrescribe si ya existe)
        /// </summary>
        public static async Task EscribirAsync(string rutaArchivo, string contenido)
        {
            try
            {
                // Asegura que el directorio exista
                var directorio = Path.GetDirectoryName(rutaArchivo);
                if (!Directory.Exists(directorio))
                    Directory.CreateDirectory(directorio);

                await File.WriteAllTextAsync(rutaArchivo, contenido);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al escribir el archivo MD: {ex.Message}");
            }
        }

        /// <summary>
        /// Lee el contenido de un archivo .md
        /// </summary>
        public static async Task<string> LeerAsync(string rutaArchivo)
        {
            try
            {
                if (!File.Exists(rutaArchivo))
                    return string.Empty;

                return await File.ReadAllTextAsync(rutaArchivo);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al leer el archivo MD: {ex.Message}");
            }
        }

        /// <summary>
        /// Agrega contenido al final del archivo .md
        /// </summary>
        public static async Task AgregarAsync(string rutaArchivo, string contenido)
        {
            try
            {
                await File.AppendAllTextAsync(rutaArchivo, contenido);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al agregar contenido al archivo MD: {ex.Message}");
            }
        }
    }
}
