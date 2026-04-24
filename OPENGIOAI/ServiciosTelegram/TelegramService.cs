using Microsoft.Extensions.Logging;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosTelegram
{
    /// <summary>
    /// Capa de transporte para Telegram. Encapsula el listener, la configuración
    /// del chat (token + chatId), el semáforo de exclusión mutua para procesar
    /// mensajes entrantes y todas las operaciones de envío.
    ///
    /// Los consumidores se suscriben a los eventos para reaccionar a mensajes,
    /// callbacks o archivos entrantes; los errores se notifican por OnError
    /// para que la UI pueda mostrarlos sin que esta clase dependa de WinForms.
    /// </summary>
    public class TelegramService
    {
        private readonly ILogger<TelegramService> _logger;
        private TelegramListener? _listener;
        private TelegramChat _chat = new() { ChatId = 0, Apikey = "" };
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public TelegramService(ILogger<TelegramService> logger)
        {
            _logger = logger;
        }

        public TelegramChat Chat => _chat;
        public bool IsConfigured => _chat.ChatId != 0 && !string.IsNullOrEmpty(_chat.Apikey);
        public SemaphoreSlim Semaphore => _semaphore;

        public event Action<long, string>? OnMessageReceived;
        public event Action<long, string>? OnCallbackReceived;
        public event Action<long, string, string, long, string>? OnFileReceived;
        public event Action<string>? OnError;

        /// <summary>
        /// Lee la configuración de Telegram (token + chatId) asociada al directorio
        /// de trabajo dado. Si el archivo no existe deja la configuración vacía.
        /// </summary>
        public void CargarConfiguracion(string rutaTrabajo)
        {
            if (string.IsNullOrEmpty(rutaTrabajo) || !Directory.Exists(rutaTrabajo))
                return;

            var config = JsonManager.Leer<TelegramChat>(
                RutasProyecto.ObtenerRutaListTelegram(rutaTrabajo));

            _chat = config.Count > 0
                ? config[0]
                : new TelegramChat { ChatId = 0, Apikey = "" };
        }

        /// <summary>
        /// Inicia (o reinicia) el listener con la configuración actual. Devuelve
        /// false si la configuración no es válida.
        /// </summary>
        public bool Iniciar(string rutaTrabajo)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("Iniciar() abortado: Telegram no está configurado");
                return false;
            }

            _listener?.Stop();
            _listener = new TelegramListener(_chat.Apikey, rutaTrabajo, _chat.ChatId);

            _listener.OnMessageReceived  += (id, txt)  => OnMessageReceived?.Invoke(id, txt);
            _listener.OnCallbackReceived += (id, data) => OnCallbackReceived?.Invoke(id, data);
            _listener.OnFileReceived     += (id, fid, fname, fsize, ftype) =>
            {
                OnFileReceived?.Invoke(id, fid, fname, fsize, ftype);
                return Task.CompletedTask;
            };

            _listener.Start();
            _logger.LogInformation("Telegram listener iniciado (chatId={ChatId})", _chat.ChatId);
            return true;
        }

        public void Detener()
        {
            _listener?.Stop();
            _listener = null;
            _logger.LogInformation("Telegram listener detenido");
        }

        /// <summary>Envía un mensaje al chat configurado.</summary>
        public Task EnviarMensajeAsync(string mensaje, object? btns = null) =>
            EnviarMensajeAsync(_chat.ChatId, mensaje, btns);

        /// <summary>
        /// Envía un mensaje a un chatId específico (útil para callbacks que
        /// pueden venir de chats distintos al configurado).
        /// </summary>
        public async Task EnviarMensajeAsync(long chatId, string mensaje, object? btns = null)
        {
            if (string.IsNullOrEmpty(_chat.Apikey)) return;

            try
            {
                await TelegramSender.EnviarMensajeAsync(_chat.Apikey, chatId, mensaje, btns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando mensaje a Telegram (chatId={ChatId})", chatId);
                OnError?.Invoke($"Error enviando mensaje a Telegram: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía una respuesta larga particionándola según el límite de Telegram.
        /// La última parte incluye el teclado inline si la respuesta lo requiere.
        /// </summary>
        public async Task EnviarRespuestaParticionadaAsync(string respuesta)
        {
            if (!IsConfigured) return;

            try
            {
                var keyboard = Utils.CrearKeyboardDesdeTexto(respuesta);
                string codificada = WebUtility.HtmlEncode(respuesta);
                var partes = Utils.DividirMensajeTelegram(codificada);

                for (int i = 0; i < partes.Count; i++)
                {
                    bool esUltima = i == partes.Count - 1;
                    await EnviarMensajeAsync(partes[i], esUltima ? keyboard : null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando respuesta particionada a Telegram");
                OnError?.Invoke(ex.ToString());
            }
        }

        public Task EnviarArchivoAsync(string rutaArchivo, string caption = "")
        {
            if (!IsConfigured) return Task.CompletedTask;
            return TelegramSender.EnviarArchivoAsync(_chat.Apikey, _chat.ChatId, rutaArchivo, caption);
        }

        public Task EnviarTypingAsync()
        {
            if (!IsConfigured) return Task.CompletedTask;
            return TelegramSender.EnviarAccionEscribiendoAsync(_chat.Apikey, _chat.ChatId);
        }
    }
}
