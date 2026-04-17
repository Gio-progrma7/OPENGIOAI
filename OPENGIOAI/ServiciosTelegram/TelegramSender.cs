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

                var parameters = new Dictionary<string, string>
                {
                    { "chat_id", chatId.ToString() },
                    { "text", mensaje ?? string.Empty },
                    {"parse_mode", "HTML" }
                };

                if (inlineKeyboard != null)
                {
                    var replyMarkupJson = JsonSerializer.Serialize(inlineKeyboard);
                    parameters.Add("reply_markup", replyMarkupJson);
                }

                using var client = new HttpClient();
                using var content = new FormUrlEncodedContent(parameters);

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

        /// <summary>
        /// Genera un teclado inline dinámico de Telegram a partir de una lista de agentes.
        /// Organiza los botones en filas de dos elementos y agrega una fila adicional con opciones de configuración.
        /// Cada botón contiene el nombre del agente visible y el callback asociado para su identificación en el sistema.
        /// </summary>
        public static object CrearKeyboardDesdeListaAgentes(IEnumerable<Modelo> listaAgentes)
        {
            var botones = new List<object[]>();
            var filaTemporal = new List<object>();

            foreach (var item in listaAgentes)
            {
                var nombreServicio = item.Agente.ToString();
                filaTemporal.Add(new
                {

                    text = nombreServicio,
                    callback_data = $"#AGENTE_{item.Agente}"
                });

                if (filaTemporal.Count == 2)
                {
                    botones.Add(filaTemporal.ToArray());
                    filaTemporal.Clear();
                }
            }

            if (filaTemporal.Any())
                botones.Add(filaTemporal.ToArray());

            botones.Add(new[]
            {
                    new
                    {
                        text = "⚙️ Configuraciones",
                        callback_data = "#CONFIGURACIONES"
                    }
                });

            return new
            {
                inline_keyboard = botones
            };
        }

        /// <summary>
        /// Genera un teclado inline dinámico de Telegram a partir de una lista de modelos de IA disponibles.
        /// Organiza los botones en filas de dos elementos y agrega una fila adicional con la opción de configuraciones.
        /// Cada botón contiene el nombre del modelo visible al usuario y el callback asociado para su procesamiento interno.
        /// </summary>
        public static object CrearKeyboardDesdeListaModelos(IEnumerable<ModeloAgente> listaAgentes)
        {

            var botones = new List<object[]>();
            var filaTemporal = new List<object>();

            foreach (var item in listaAgentes)
            {
                filaTemporal.Add(new
                {
                    text = item.Nombre,
                    callback_data = $"#MODELO_{item.Nombre}"
                });

                if (filaTemporal.Count == 2)
                {
                    botones.Add(filaTemporal.ToArray());
                    filaTemporal.Clear();
                }
            }

            if (filaTemporal.Any())
                botones.Add(filaTemporal.ToArray());

            botones.Add(new[]
            {
                    new
                    {
                        text = "⚙️ Configuraciones",
                        callback_data = "#CONFIGURACIONES"
                    }
                });

            return new
            {
                inline_keyboard = botones
            };
        }

        /// <summary>
        /// Genera un teclado inline dinámico de Telegram basado en una lista de rutas de archivos.
        /// Organiza los botones en filas de dos elementos y agrega una fila adicional para acceder a las configuraciones.
        /// Cada botón muestra la ruta del archivo y contiene un callback asociado para su procesamiento posterior.
        /// </summary>
        public static object CrearKeyboardDesdeListaRutas(IEnumerable<Archivo> listaAgentes)
        {

            var botones = new List<object[]>();
            var filaTemporal = new List<object>();

            foreach (var item in listaAgentes)
            {
                filaTemporal.Add(new
                {
                    text = item.Ruta,
                    callback_data = $"#RUTA_{item.Ruta}"
                });

                if (filaTemporal.Count == 2)
                {
                    botones.Add(filaTemporal.ToArray());
                    filaTemporal.Clear();
                }
            }

            if (filaTemporal.Any())
                botones.Add(filaTemporal.ToArray());

            botones.Add(new[]
            {
                    new
                    {
                        text = "⚙️ Configuraciones",
                        callback_data = "#CONFIGURACIONES"
                    }
                });

            return new
            {
                inline_keyboard = botones
            };
        }


        /// <summary>
        /// Genera el menú de configuraciones del bot de Telegram en formato de teclado inline.
        /// Incluye opciones para activar o desactivar Telegram, controlar el modo recordar tema, limitar el chat, gestionar APIs y cambiar parámetros del sistema como agente, modelo o ruta de trabajo.
        /// Retorna la estructura compatible con reply_markup para su envío mediante el bot.
        /// </summary>
        public static object Configuraciones_Menu()
        {
            var objeto = new
            {
                inline_keyboard = new[]
                        {
                            new[]
                            {
                                new { text = "✅ Activar Telegram", callback_data = "#ACTIVATELEGRAM" },
                                new { text = "❌ Desactivar Telegram", callback_data = "#DESACTIVATELEGRAM" }
                            },
                            new[]
                            {
                                new { text = "🧠 Recordar Tema", callback_data = "#RECORDAR" },
                                new { text = "💬 Solo Chat", callback_data = "#SOLOCHAT" }
                            },
                          
                            new[]
                            {
                                new { text = "🔌 APIs", callback_data = "#APIS" }
                            },
                             new[]
                            {
                                new { text = "Cambiar Agente", callback_data = "#CAMBIARAGENTE" },
                                new { text = "Cambiar Modelo", callback_data = "#CAMBIARMODELO" },
                                new { text = "Cambiar Ruta trabajo", callback_data = "#CAMBIARRUTA" }
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

                form.Add(new StringContent(chatId.ToString()), "chat_id");

                if (!string.IsNullOrWhiteSpace(caption))
                    form.Add(new StringContent(caption), "caption");

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
