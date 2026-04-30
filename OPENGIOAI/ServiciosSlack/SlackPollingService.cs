using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosSlack
{
    public class SlackPollingService
    {
        // Opciones de JSON con emojis literales (no \uXXXX), garantiza que los
        // emojis salgan en el body sin ser escapados � Slack los renderiza bien.
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly string _botToken;
        private readonly string _channelId;
        private readonly HttpClient _http;
        private string _lastTimestamp;
        private CancellationTokenSource _cts;

        public event Action<SlackMessage> OnMessageReceived;

        public SlackPollingService(string botToken, string channelId)
        {
            _botToken = botToken;
            _channelId = channelId;

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _botToken);
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => PollLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string url = $"https://slack.com/api/conversations.history?channel={_channelId}&limit=10";

                    var response = await _http.GetStringAsync(url);

                    using var doc = JsonDocument.Parse(response);

                    if (!doc.RootElement.GetProperty("ok").GetBoolean())
                        continue;

                    var messages = doc.RootElement.GetProperty("messages");

                    foreach (var msg in messages.EnumerateArray())
                    {
                        if (!msg.TryGetProperty("ts", out var tsProp))
                            continue;

                        string ts = tsProp.GetString();

                        // Solo procesar mensajes nuevos
                        if (_lastTimestamp != null &&
                            String.Compare(ts, _lastTimestamp, StringComparison.Ordinal) <= 0)
                            continue;

                        _lastTimestamp = ts;

                        // Obtener texto
                        string text = msg.TryGetProperty("text", out var textProp)
                            ? textProp.GetString()
                            : null;

                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        // Obtener user
                        string user = msg.TryGetProperty("user", out var userProp)
                            ? userProp.GetString()
                            : null;

                        // Obtener bot_id (si existe)
                        string botId = msg.TryGetProperty("bot_id", out var botProp)
                            ? botProp.GetString()
                            : null;

                        var slackMessage = new SlackMessage
                        {
                            Text = text,
                            User = user,
                            BotId = botId,
                            Timestamp = ts
                        };

                        OnMessageReceived?.Invoke(slackMessage);
                    }
                }
                catch
                {
                    // Puedes loggear aqu� si quieres
                }

                await Task.Delay(2000, token);
            }
        }

        public async Task SendMessage(string text)
        {

            var payload = new
            {
                channel = _channelId,
                text = text, // fallback
                blocks = Utils.CrearBlocksDesdeTexto(text)
            };

            var json = JsonSerializer.Serialize(payload, JsonOpts);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            await _http.PostAsync(
                "https://slack.com/api/chat.postMessage",
                content
            );
        }

        /// <summary>
        /// Env�a texto plano con soporte mrkdwn (sin Blocks Kit).
        /// Usar para c�digo / triple backtick que Block Kit no renderiza bien.
        /// </summary>
        public async Task EnviarCodigoAsync(string text)
        {
            try
            {
                var payload = new
                {
                    channel = _channelId,
                    text    = text,
                    mrkdwn  = true
                };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");
                await _http.PostAsync("https://slack.com/api/chat.postMessage", content);
            }
            catch { /* ignorar errores secundarios */ }
        }

        /// <summary>
        /// Env�a un mensaje temporal de "pensando..." en el canal y devuelve su timestamp (ts).
        /// Usa el ts devuelto para eliminar el mensaje con <see cref="EliminarMensajeAsync"/> cuando
        /// el procesamiento termine.
        /// Devuelve null si el env�o falla.
        /// </summary>
        public async Task<string?> EnviarPensandoAsync()
        {
            try
            {
                var payload = new
                {
                    channel = _channelId,
                    text    = "⏳ Procesando tu solicitud..."
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload, JsonOpts),
                    Encoding.UTF8, "application/json");

                var resp = await _http.PostAsync(
                    "https://slack.com/api/chat.postMessage", content);

                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean() &&
                    doc.RootElement.TryGetProperty("ts", out var ts))
                    return ts.GetString();
            }
            catch { /* ignorar � indicador secundario */ }
            return null;
        }

        /// <summary>
        /// Sube un archivo al canal de Slack usando la API files.upload.
        /// El archivo aparece como adjunto en el canal configurado.
        /// </summary>
        public async Task EnviarArchivoAsync(string rutaArchivo, string title = "")
        {
            try
            {
                if (!System.IO.File.Exists(rutaArchivo)) return;

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(_channelId), "channels");
                form.Add(new StringContent(
                    string.IsNullOrWhiteSpace(title)
                        ? System.IO.Path.GetFileName(rutaArchivo)
                        : title),
                    "title");
                form.Add(new StringContent(System.IO.Path.GetFileName(rutaArchivo)), "filename");

                var fileBytes   = await System.IO.File.ReadAllBytesAsync(rutaArchivo);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "file", System.IO.Path.GetFileName(rutaArchivo));

                await _http.PostAsync("https://slack.com/api/files.upload", form);
            }
            catch { /* ignorar errores secundarios */ }
        }

        /// <summary>
        /// Elimina un mensaje previo usando su timestamp (ts).
        /// Requiere que el bot tenga el scope <c>chat:write</c> y <c>chat:write.public</c>.
        /// </summary>
        public async Task EliminarMensajeAsync(string ts)
        {
            try
            {
                var payload = new
                {
                    channel = _channelId,
                    ts      = ts
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload, JsonOpts),
                    Encoding.UTF8, "application/json");

                await _http.PostAsync(
                    "https://slack.com/api/chat.delete", content);
            }
            catch { /* ignorar */ }
        }
    }
}