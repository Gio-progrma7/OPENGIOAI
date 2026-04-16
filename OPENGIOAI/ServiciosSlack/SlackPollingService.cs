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
                    // Puedes loggear aquí si quieres
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

            var json = JsonSerializer.Serialize(payload);

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
        /// Envía texto plano con soporte mrkdwn (sin Blocks Kit).
        /// Usar para código / triple backtick que Block Kit no renderiza bien.
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
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await _http.PostAsync("https://slack.com/api/chat.postMessage", content);
            }
            catch { /* ignorar errores secundarios */ }
        }

        /// <summary>
        /// Envía un mensaje temporal de "pensando..." en el canal y devuelve su timestamp (ts).
        /// Usa el ts devuelto para eliminar el mensaje con <see cref="EliminarMensajeAsync"/> cuando
        /// el procesamiento termine.
        /// Devuelve null si el envío falla.
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
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8, "application/json");

                var resp = await _http.PostAsync(
                    "https://slack.com/api/chat.postMessage", content);

                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean() &&
                    doc.RootElement.TryGetProperty("ts", out var ts))
                    return ts.GetString();
            }
            catch { /* ignorar — indicador secundario */ }
            return null;
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
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8, "application/json");

                await _http.PostAsync(
                    "https://slack.com/api/chat.delete", content);
            }
            catch { /* ignorar */ }
        }
    }
}