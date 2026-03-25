using OPENGIOAI.Entidades;
using OPENGIOAI.Properties;
using OPENGIOAI.ServiciosTelegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class Utils
    {

        public static int ObtenerServicio_Nombre(string nombre)
        {
            foreach (var item in Enum.GetValues<Servicios>())
            {
                if (item.ToString().Equals(nombre, StringComparison.OrdinalIgnoreCase))
                {
                    return (int)item;
                }
            }

            return -1;

        }

        public static string LeerArchivoTxt(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
                return string.Empty;

            return File.ReadAllText(rutaArchivo, Encoding.UTF8);
        }


        public static string ObtenerNombresApis(List<Api> ApisDipsonibles)
        {
            string resultado = string.Empty;
            foreach (var item in ApisDipsonibles)
            {
                resultado += "[ Nombre : " + item.Nombre + "/  Descripcion : " + item.Descripcion + " ] ,  ";
            }

            return resultado;
        }


        public static Image ObtenerTipoArchivo(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo))
                return Resources.zip;

            string extension = Path.GetExtension(rutaArchivo).ToLower();

            switch (extension)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":
                    return Resources.archivopng;

                case ".txt":
                    return Resources.txt;

                case ".doc":
                case ".docx":
                    return Resources.doc;

                case ".xls":
                case ".xlsx":
                    return Resources.xlsx;

                case ".pdf":
                    return Resources.pdf;

                case ".zip":
                case ".rar":
                case ".7z":
                    return Resources.zip;

                case ".mp3":
                case ".wav":
                    return Resources.doc;

                case ".mp4":
                case ".avi":
                case ".mkv":
                    return Resources.video;


                default:
                    return Resources.carpeta;
            }
        }


        public static T LeerConfig<T>(string ruta) where T : new()
        {
            if (!File.Exists(ruta))
            {
                T configVacia = new T();
                GuardarConfig(ruta, configVacia); // crea el archivo
                return configVacia;
            }

            string json = File.ReadAllText(ruta);
            return JsonSerializer.Deserialize<T>(json);
        }
        public static void GuardarConfig<T>(string ruta, T configuracion)
        {
            var opciones = new JsonSerializerOptions
            {
                WriteIndented = true // para que el JSON quede bonito y legible
            };

            string json = JsonSerializer.Serialize(configuracion, opciones);
            File.WriteAllText(ruta, json);
        }

        public static object CrearBlocksDesdeTexto(string mensaje)
        {
            var bloques = new List<object>();

            // Texto principal
            bloques.Add(new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = mensaje
                }
            });

            // Botón por defecto
            var botones = new List<object>
            {
                new
                {
                    type = "button",
                    text = new
                    {
                        type = "plain_text",
                        text = "⚙️ Configuraciones"
                    },
                    value = "#CONFIGURACIONES"
                }
            };

            const string marcador = "<!--BOTONES-->";
            int index = mensaje.IndexOf(marcador, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                var bloque = mensaje[(index + marcador.Length)..];

                var matches = Regex.Matches(
                    bloque,
                    @"\[(.*?)\]\s*\|\s*(#\S+)"
                );

                foreach (Match m in matches)
                {
                    botones.Add(new
                    {
                        type = "button",
                        text = new
                        {
                            type = "plain_text",
                            text = m.Groups[1].Value.Trim()
                        },
                        value = m.Groups[2].Value.Trim()
                    });
                }
            }

            bloques.Add(new
            {
                type = "actions",
                elements = botones
            });

            return bloques;
        }
        public static object? CrearKeyboardDesdeTexto(string mensaje)
        {

            // Botón por defecto SIEMPRE disponible
            var botonDefault = new[]
            {
                new[]
                {
                    ("⚙️ Configuraciones", "#CONFIGURACIONES")

                },
                new[]
                {
                    ("Mantener Comversacion", "#RECORDAR")
                }
             };

            if (string.IsNullOrWhiteSpace(mensaje))
                return TelegramSender.CrearInlineKeyboard(botonDefault);

            const string marcador = "<!--BOTONES-->";
            int index = mensaje.IndexOf(marcador, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
                return TelegramSender.CrearInlineKeyboard(botonDefault);

            var bloque = mensaje[(index + marcador.Length)..];

            var matches = Regex.Matches(
                bloque,
                @"\[(.*?)\]\s*\|\s*(#\S+)",
                RegexOptions.Multiline
            );

            if (matches.Count == 0)
                return TelegramSender.CrearInlineKeyboard(botonDefault);

            var filas = matches
                .Select(m => new[]
                {
            (m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim())
                })
                .ToArray();

            return TelegramSender.CrearInlineKeyboard(filas);
        }

        public static List<string> DividirMensajeTelegram(string mensaje, int limite = 4096)
        {
            var partes = new List<string>();

            if (string.IsNullOrEmpty(mensaje))
                return partes;

            while (mensaje.Length > limite)
            {
                int corte = mensaje.LastIndexOf('\n', limite);

                // Si no encuentra salto de línea, corta exacto
                if (corte <= 0)
                    corte = limite;

                partes.Add(mensaje.Substring(0, corte).Trim());
                mensaje = mensaje.Substring(corte).Trim();
            }

            if (mensaje.Length > 0)
                partes.Add(mensaje);

            return partes;
        }


        public static async Task<string> DescargarArchivoTelegram(
           string token,
           string fileId,
           string fileName,
           string fileType,
           string rutaBase,
           long fileSize)
        {
            try
            {
                //  Limitar tamaño máximo (20MB ejemplo)
                const long maxSize = 20 * 1024 * 1024;
                if (fileSize > maxSize)
                {

                    return string.Empty;
                }

                using var http = new HttpClient();

                //  Obtener file_path
                var getFileUrl = $"https://api.telegram.org/bot{token}/getFile?file_id={fileId}";
                var response = await http.GetStringAsync(getFileUrl);

                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                string filePath = json.result.file_path;

                if (string.IsNullOrEmpty(filePath))
                    return string.Empty;

                // Descargar archivo
                var fileUrl = $"https://api.telegram.org/file/bot{token}/{filePath}";
                byte[] fileBytes = await http.GetByteArrayAsync(fileUrl);

                //  Crear carpeta por tipo
                string basePath = Path.Combine(rutaBase, "TelegramFiles");
                string typeFolder = Path.Combine(basePath, fileType);

                Directory.CreateDirectory(typeFolder);

                // Generar nombre seguro
                string extension = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(extension))
                    extension = Path.GetExtension(filePath);

                string safeName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{extension}";
                string fullPath = Path.Combine(typeFolder, safeName);

                // Guardar archivo
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                return fullPath;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }

        }

        public static string LimpiarRespuesta(string texto)
        {
            return Regex.Replace(
                texto,
                @"RESPUESTA\s+respuesta\.txt\s*:\s*\{([\s\S]*?)\}",
                "$1"
            ).Trim();
        }
    }
}
