using Newtonsoft.Json.Linq;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OPENGIOAI.ServiciosTelegram
{
    /// <summary>
    /// Clase encargada de gestionar la comunicación con Telegram mediante un bot.
    /// Proporciona funcionalidades para iniciar y detener la escucha de mensajes, manejar eventos de texto, botones interactivos y archivos multimedia.
    /// Implementa el procesamiento de actualizaciones de Telegram, validando el chat autorizado, descargando archivos recibidos y propagando los eventos al sistema consumidor.
    /// También gestiona la captura de errores para mantener la estabilidad del servicio.
    /// </summary>
    public class TelegramListener
    {
        private readonly TelegramBotClient _bot;
        private readonly long _chatIdPermitido;
        private CancellationTokenSource? _cts;
        private string _rutaBase = "";
        private string _token = "";

        public TelegramListener(string token, string Ruta, long chatIdPermitido)
        {
            _bot = new TelegramBotClient(token);
            _chatIdPermitido = chatIdPermitido;
            _token = token;
            _rutaBase = Ruta;
        }

        // Texto
        public event Action<long, string>? OnMessageReceived;

        // Botones
        public event Action<long, string>? OnCallbackReceived;

        // Archivos
        public event Func<long, string, string, long, string, Task>? OnFileReceived;
        // chatId, fileId, fileName, fileSize, fileType

        /// <summary>
        /// Inicia el proceso de recepción de mensajes y actualizaciones desde Telegram.
        /// Crea un nuevo token de cancelación, configura las opciones de recepción permitiendo todos los tipos de actualizaciones,
        /// y activa el mecanismo de escucha del bot para comenzar a procesar eventos entrantes de forma asíncrona.
        /// </summary>
        public void Start()
        {
            _cts = new CancellationTokenSource();

            var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: _cts.Token
            );
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Maneja las actualizaciones recibidas desde Telegram, incluyendo mensajes de texto, archivos multimedia y eventos de callback.
        /// Valida el chat autorizado antes de procesar el contenido.
        /// Si recibe texto, dispara el evento correspondiente para su procesamiento.
        /// Si recibe archivos (documentos, audio, voz, fotos o video), extrae la información del archivo y notifica mediante el evento de archivos.
        /// También gestiona las interacciones de botones inline mediante CallbackQuery confirmando previamente la acción en Telegram.
        /// </summary>
        private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
        {
            if (update == null)
                return;

            // =========================
            // MENSAJES
            // =========================
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                var message = update.Message;
                long chatId = message.Chat.Id;

                if (chatId != _chatIdPermitido)
                    return;

                // =========================
                // TEXTO NORMAL
                // =========================
                if (!string.IsNullOrEmpty(message.Text))
                {
                    OnMessageReceived?.Invoke(chatId, message.Text);
                    return;
                }

                // Variable común
                string? fileId = null;
                string fileName = "";
                long fileSize = 0;
                string fileType = "";
                string? caption = message.Caption;

                // =========================
                // DOCUMENTO
                // =========================
                if (message.Document != null)
                {
                    fileId = message.Document.FileId;
                    fileName = message.Document.FileName ?? "document";
                    fileSize = message.Document.FileSize ?? 0;
                    fileType = "document";
                }
                // =========================
                // VOICE
                // =========================
                else if (message.Voice != null)
                {
                    fileId = message.Voice.FileId;
                    fileName = "voice.ogg";
                    fileSize = message.Voice.FileSize ?? 0;
                    fileType = "voice";
                }
                // =========================
                // AUDIO
                // =========================
                else if (message.Audio != null)
                {
                    fileId = message.Audio.FileId;
                    fileName = message.Audio.FileName ?? "audio.mp3";
                    fileSize = message.Audio.FileSize ?? 0;
                    fileType = "audio";
                }
                // =========================
                // FOTO
                // =========================
                else if (message.Photo != null && message.Photo.Any())
                {
                    var photo = message.Photo.Last(); // mejor resolución
                    fileId = photo.FileId;
                    fileName = "photo.jpg";
                    fileSize = photo.FileSize ?? 0;
                    fileType = "photo";
                }
                // =========================
                // VIDEO
                // =========================
                else if (message.Video != null)
                {
                    fileId = message.Video.FileId;
                    fileName = message.Video.FileName ?? "video.mp4";
                    fileSize = message.Video.FileSize ?? 0;
                    fileType = "video";
                }

                // =========================
                //  SI ES ARCHIVO
                // =========================
                if (!string.IsNullOrEmpty(fileId))
                {
                    if (OnFileReceived != null)
                        await OnFileReceived.Invoke(
                            chatId,
                            fileId,
                            fileName,
                            fileSize,
                            fileType
                        );

                    // Si trae descripción, también la enviamos
                    if (!string.IsNullOrEmpty(caption))
                    {
                        string ruta = await Utils.DescargarArchivoTelegram(_token ,fileId, fileName, fileType, _rutaBase, fileSize);
                        OnMessageReceived?.Invoke(chatId, caption +  " Ruta : " + ruta);
                    }
                }
            }

            // =========================
            // CALLBACK QUERY
            // =========================
            else if (update.Type == UpdateType.CallbackQuery &&
                     update.CallbackQuery?.Data != null)
            {
                long chatId = update.CallbackQuery.Message.Chat.Id;

                if (chatId != _chatIdPermitido)
                    return;

                await bot.AnswerCallbackQuery(
                    update.CallbackQuery.Id,
                    cancellationToken: ct
                );

                OnCallbackReceived?.Invoke(chatId, update.CallbackQuery.Data);
            }
        }

        /// <summary>
        /// Maneja las excepciones generadas durante la ejecución del bot de Telegram.
        /// Registra el mensaje de error en la consola para facilitar el diagnóstico y permite que el flujo del sistema continúe sin interrupciones.
        /// </summary>
        private Task HandleErrorAsync(
            ITelegramBotClient bot,
            Exception exception,
            CancellationToken ct)
        {
            Console.WriteLine($"Error Telegram: {exception.Message}");
            return Task.CompletedTask;
        }
    }

}
