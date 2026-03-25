using Newtonsoft.Json.Linq;
using SlackAPI;
using SlackAPI.WebSocketMessages;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosSlack
{
    public class SlackListener
    {
        private readonly string _botToken;
        private readonly string _rutaBase;
        private readonly string? _canalPermitido;

        private SlackSocketClient? _client;
        private string? _botUserId;

        public event Action<string, string>? OnMessageReceived;
        public event Func<string, string, string, long, Task>? OnFileReceived;

        public SlackListener(string botToken, string rutaBase, string? canalPermitido = null)
        {
            _botToken = botToken;
            _rutaBase = rutaBase;
            _canalPermitido = canalPermitido;

            if (!Directory.Exists(_rutaBase))
                Directory.CreateDirectory(_rutaBase);
        }

        // ======================================
        // START
        // ======================================
        public void Start()
        {
            _client = new SlackSocketClient(_botToken);

            _client.Connect((login) =>
            {
                _botUserId = login.self.id;
                Console.WriteLine($"✅ Conectado como {_botUserId}");
            },
            () =>
            {
                Console.WriteLine("❌ Desconectado");
            });

            _client.OnMessageReceived += async (message) =>
            {
                await HandleMessage(message);
            };
        }

        public void Stop()
        {
            _client?.CloseSocket();
        }

        // ======================================
        // MANEJO MENSAJES
        // ======================================
        private async Task HandleMessage(NewMessage message)
        {
            if (message.user == null)
                return;

            if (_botUserId != null && message.user == _botUserId)
                return;

            string channel = message.channel;

            if (_canalPermitido != null && channel != _canalPermitido)
                return;

            if (!string.IsNullOrWhiteSpace(message.text))
            {
                OnMessageReceived?.Invoke(channel, message.text);
            }

            if (message.subtype == "file_share")
            {
                await ProcesarUltimoArchivo(channel);
            }
        }

        // ======================================
        // OBTENER ÚLTIMO ARCHIVO VÍA API
        // ======================================
        private async Task ProcesarUltimoArchivo(string channel)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _botToken);

            var response = await http.GetStringAsync("https://slack.com/api/files.list?count=1");

            var json = JObject.Parse(response);

            if (!(bool)json["ok"])
                return;

            var file = json["files"]?[0];
            if (file == null)
                return;

            string url = file["url_private"]?.ToString() ?? "";
            string name = file["name"]?.ToString() ?? "archivo";
            long size = file["size"]?.ToObject<long>() ?? 0;

            if (!string.IsNullOrEmpty(url))
            {
                string ruta = await DescargarArchivo(url, name);

                if (OnFileReceived != null)
                    await OnFileReceived.Invoke(channel, url, ruta, size);
            }
        }

        // ======================================
        // ENVIAR MENSAJE (WEB API)
        // ======================================
        public async Task EnviarMensajeAsync(string channelId, string texto)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _botToken);

            var payload = new
            {
                channel = channelId,
                text = texto
            };

            var content = new StringContent(
                JObject.FromObject(payload).ToString(),
                Encoding.UTF8,
                "application/json");

            await http.PostAsync("https://slack.com/api/chat.postMessage", content);
        }

        // ======================================
        // DESCARGAR ARCHIVO
        // ======================================
        private async Task<string> DescargarArchivo(string url, string nombre)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _botToken);

            var response = await http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Error descargando archivo Slack");

            var contentType = response.Content.Headers.ContentType?.MediaType;

            if (contentType == "text/html")
                throw new Exception("Slack devolvió HTML (verifica files:read)");

            var bytes = await response.Content.ReadAsByteArrayAsync();

            string ruta = Path.Combine(_rutaBase, nombre);

            await System.IO.File.WriteAllBytesAsync(ruta, bytes);

            return ruta;
        }
    }
}
