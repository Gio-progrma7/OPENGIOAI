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
    }
}