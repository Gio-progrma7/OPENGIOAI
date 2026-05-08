using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace OPENGIOAI.ServiciosTelegram
{
    /// <summary>
    /// Proporciona utilidades estáticas para el envío de mensajes y la construcción de interfaces interactivas para Telegram.
    /// Contiene métodos auxiliares para formatear, enviar mensajes, crear teclados inline y manejar la comunicación con la API del bot.
    /// </summary>
    public static class TelegramSender
    {
        private const int MenuPageSize = 8;

        /// <summary>
        /// Envía mensajes de texto a un chat de Telegram de forma asíncrona utilizando la API HTTP del bot.
        /// Permite incluir teclados interactivos en formato JSON y utiliza parseo HTML para el formato del mensaje.
        /// Maneja las respuestas HTTP y las validaciones de estado de la API, registrando errores en consola o mostrando el detalle del problema en pantalla.
        /// </summary>
        public static async Task<bool> EnviarMensajeAsync(
        string token,
        long chatId,
        string mensaje,
        object? inlineKeyboard = null)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{token}/sendMessage";

                // Usar JSON + UTF-8 explícito en lugar de FormUrlEncodedContent
                // para que los emojis (🔍 ⚙️ 🛡️ ✨) lleguen intactos en lugar de "??".
                object payload = inlineKeyboard != null
                    ? new
                    {
                        chat_id      = chatId,
                        text         = mensaje ?? string.Empty,
                        parse_mode   = "HTML",
                        reply_markup = inlineKeyboard
                    }
                    : new
                    {
                        chat_id    = chatId,
                        text       = mensaje ?? string.Empty,
                        parse_mode = "HTML"
                    };

                string jsonBody = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                using var client  = new HttpClient();
                using var content = new StringContent(
                    jsonBody, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(errorBody);

                    Console.WriteLine($"Error HTTP Telegram: {(int)response.StatusCode} {response.ReasonPhrase}");
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean())
                    return true;

                Console.WriteLine("Telegram devolvió ok=false: " + json);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enviando mensaje: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Envía la acción "escribiendo..." (typing) a un chat de Telegram.
        /// El indicador dura ~5 s en la UI del receptor.
        /// Llamar cada ≤4 s mientras el bot esté procesando para mantenerlo visible.
        /// </summary>
        public static async Task EnviarAccionEscribiendoAsync(string token, long chatId)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{token}/sendChatAction";
                var parameters = new Dictionary<string, string>
                {
                    { "chat_id", chatId.ToString() },
                    { "action",  "typing"           }
                };
                using var client  = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                using var content = new FormUrlEncodedContent(parameters);
                await client.PostAsync(url, content).ConfigureAwait(false);
            }
            catch { /* ignorar errores de red en acción secundaria */ }
        }

        /// <summary>
        /// Crea un teclado inline de Telegram en formato JSON dinámico.
        /// Permite generar filas y columnas de botones interactivos, donde cada botón contiene el texto visible y el valor de callback asociado.
        /// El resultado puede ser enviado como parte del parámetro reply_markup en los mensajes del bot.
        /// </summary>
        public static object CrearInlineKeyboard(params (string text, string callback)[][] botones)
        {
            return new
            {
                inline_keyboard = botones.Select(row =>
                    row.Select(b => new { text = b.text, callback_data = b.callback })
                )
            };
        }

        private static object CrearMenuPaginado<T>(
            IEnumerable<T> items,
            int pagina,
            string claveMenu,
            Func<T, (string text, string callback)> map)
        {
            var lista = (items ?? Enumerable.Empty<T>()).ToList();
            int totalPaginas = Math.Max(1, (int)Math.Ceiling(lista.Count / (double)MenuPageSize));
            pagina = Math.Clamp(pagina, 0, totalPaginas - 1);

            var botones = new List<object[]>();
            var visibles = lista
                .Skip(pagina * MenuPageSize)
                .Take(MenuPageSize)
                .ToList();

            if (visibles.Count == 0)
            {
                botones.Add(new object[]
                {
                    new { text = "Sin opciones disponibles", callback_data = "#CONFIGURACIONES" }
                });
            }
            else
            {
                var filaTemporal = new List<object>();
                foreach (var item in visibles)
                {
                    var (texto, callback) = map(item);
                    filaTemporal.Add(new
                    {
                        text = RecortarBoton(texto, 34),
                        callback_data = callback
                    });

                    if (filaTemporal.Count == 2)
                    {
                        botones.Add(filaTemporal.ToArray());
                        filaTemporal.Clear();
                    }
                }

                if (filaTemporal.Count > 0)
                    botones.Add(filaTemporal.ToArray());
            }

            var navegacion = new List<object>();
            if (pagina > 0)
            {
                navegacion.Add(new
                {
                    text = "‹ Ver menos",
                    callback_data = $"#VERMENOS_{claveMenu}_{pagina - 1}"
                });
            }

            if (pagina < totalPaginas - 1)
            {
                navegacion.Add(new
                {
                    text = "Ver más ›",
                    callback_data = $"#VERMAS_{claveMenu}_{pagina + 1}"
                });
            }

            if (navegacion.Count > 0)
                botones.Add(navegacion.ToArray());

            botones.Add(new object[]
            {
                new { text = "⚙ Configuraciones", callback_data = "#CONFIGURACIONES" }
            });

            return new { inline_keyboard = botones };
        }

        private static string RecortarBoton(string texto, int max)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "Opción";
            texto = texto.Trim();
            return texto.Length <= max ? texto : texto[..Math.Max(1, max - 1)] + "…";
        }

        /// <summary>
        /// Genera un teclado inline dinámico de Telegram a partir de una lista de agentes.
        /// Organiza los botones en filas de dos elementos y agrega una fila adicional con opciones de configuración.
        /// Cada botón contiene el nombre del agente visible y el callback asociado para su identificación en el sistema.
        /// </summary>
        public static object CrearKeyboardDesdeListaAgentes(IEnumerable<Modelo> listaAgentes, int pagina = 0)
        {
            return CrearMenuPaginado(
                listaAgentes,
                pagina,
                "AGENTES",
                item => ($"🤖 {item.Agente}", $"#AGENTE_{item.Agente}"));
        }

        /// <summary>
        /// Genera un teclado inline dinámico de Telegram a partir de una lista de modelos de IA disponibles.
        /// Organiza los botones en filas de dos elementos y agrega una fila adicional con la opción de configuraciones.
        /// Cada botón contiene el nombre del modelo visible al usuario y el callback asociado para su procesamiento interno.
        /// </summary>
        public static object CrearKeyboardDesdeListaModelos(IEnumerable<ModeloAgente> listaAgentes, int pagina = 0)
        {
            return CrearMenuPaginado(
                listaAgentes,
                pagina,
                "MODELOS",
                item => ($"⚡ {item.Nombre}", $"#MODELO_{item.Nombre}"));
        }

        /// <summary>
        /// Genera un teclado inline dinámico de Telegram basado en una lista de rutas de archivos.
        /// Organiza los botones en filas de dos elementos y agrega una fila adicional para acceder a las configuraciones.
        /// Cada botón muestra la ruta del archivo y contiene un callback asociado para su procesamiento posterior.
        /// </summary>
        public static object CrearKeyboardDesdeListaRutas(IEnumerable<Archivo> listaAgentes, int pagina = 0)
        {
            return CrearMenuPaginado(
                listaAgentes,
                pagina,
                "RUTAS",
                item =>
                {
                    var nombre = Path.GetFileName(item.Ruta.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (string.IsNullOrWhiteSpace(nombre)) nombre = item.Ruta;
                    return ($"📁 {nombre}", $"#RUTA_{item.Ruta}");
                });
        }


        /// <summary>
        /// Genera el menú de configuraciones del bot de Telegram en formato de teclado inline.
        /// Incluye opciones para activar o desactivar Telegram, controlar el modo recordar tema, limitar el chat, gestionar APIs y cambiar parámetros del sistema como agente, modelo o ruta de trabajo.
        /// Retorna la estructura compatible con reply_markup para su envío mediante el bot.
        /// </summary>
        public static object Configuraciones_Menu(bool avanzado = false)
        {
            if (avanzado)
            {
                return new
                {
                    inline_keyboard = new[]
                    {
                        new[]
                        {
                            new { text = "🤖 Cambiar agente", callback_data = "#CAMBIARAGENTE" },
                            new { text = "⚡ Cambiar modelo", callback_data = "#CAMBIARMODELO" }
                        },
                        new[]
                        {
                            new { text = "📁 Ruta de trabajo", callback_data = "#CAMBIARRUTA" },
                            new { text = "🔌 APIs", callback_data = "#APIS" }
                        },
                        new[]
                        {
                            new { text = "‹ Ver menos", callback_data = "#VERMENOS_CONFIG" }
                        }
                    }
                };
            }

            var objeto = new
            {
                inline_keyboard = new[]
                        {
                            new[]
                            {
                                new { text = "✅ Activar Telegram", callback_data = "#ACTIVATELEGRAM" },
                                new { text = "⛔ Desactivar Telegram", callback_data = "#DESACTIVATELEGRAM" }
                            },
                            new[]
                            {
                                new { text = "🧠 Recordar tema", callback_data = "#RECORDAR" },
                                new { text = "💬 Solo Chat", callback_data = "#SOLOCHAT" }
                            },
                            new[]
                            {
                                new { text = "Ver más ›", callback_data = "#VERMAS_CONFIG" }
                            }
                        }
            };

            return objeto;
        }

        /// <summary>
        /// Genera un teclado inline con opciones rápidas para acceder a configuraciones o cancelar la instrucción actual.
        /// Se utiliza principalmente para ofrecer al usuario acciones de control durante la interacción con el bot.
        /// </summary>
        public static object CancelarConfig()
        {
            var objeto = new
            {
                inline_keyboard = new[]
                        {
                            new[]
                            {
                                new { text = "⚙️ Configuraciones", callback_data = "#CONFIGURACIONES" },
                                new { text = "❌ Cancelar instruccion", callback_data = "#CANCELAR" }
                            },
 
                        }
            };

            return objeto;
        }

        /// <summary>
        /// Extrae el comando y su valor asociado desde un texto formateado con el prefijo #.
        /// Divide la cadena usando el carácter guion bajo (_) como separador entre el comando y su parámetro.
        /// Si no existe un valor adicional, devuelve el comando y el mismo texto como valor.
        /// </summary>
        /// <summary>
        /// Envía un archivo al chat de Telegram usando la API sendDocument.
        /// Admite cualquier tipo de archivo (código, imágenes, datos, etc.).
        /// </summary>
        public static async Task<bool> EnviarArchivoAsync(
            string token, long chatId, string rutaArchivo, string caption = "")
        {
            try
            {
                if (!File.Exists(rutaArchivo)) return false;

                var url = $"https://api.telegram.org/bot{token}/sendDocument";
                using var client = new HttpClient();
                using var form   = new MultipartFormDataContent();

                form.Add(new StringContent(chatId.ToString(), System.Text.Encoding.UTF8), "chat_id");

                if (!string.IsNullOrWhiteSpace(caption))
                    form.Add(new StringContent(caption, System.Text.Encoding.UTF8, "text/plain"), "caption");

                var fileBytes   = await File.ReadAllBytesAsync(rutaArchivo);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "document", Path.GetFileName(rutaArchivo));

                var response = await client.PostAsync(url, form).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enviando archivo Telegram: " + ex.Message);
                return false;
            }
        }

        public static (string comando, string valor) ExtraerComandoYValor(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (string.Empty, string.Empty);

            if (!input.StartsWith("#"))
                return (string.Empty, string.Empty);

            var sinNumeral = input.Substring(1);

            var partes = sinNumeral.Split(new[] { '_' }, 2);

            if (partes.Length == 1)
            {
                // No tiene "_"
                return ("#"+partes[0], partes[0]);
            }

            return ("#"+partes[0], partes[1]);
        }
    }
}
