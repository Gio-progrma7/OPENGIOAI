using SlackAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosSlack
{
    public class SlackService
    {
        private readonly SlackSocketClient _client;
        private readonly string _channelObjetivo;

        public event Action<string, string> OnMessageReceived;

        public SlackService(string botToken, string channelObjetivo)
        {
            _client = new SlackSocketClient(botToken);
            _channelObjetivo = channelObjetivo;
        }

        public void Connect()
        {
            // 🔥 SUSCRIBIRSE ANTES DE CONECTAR
            _client.OnMessageReceived += (message) =>
            {
                if (message == null) return;
                if (string.IsNullOrEmpty(message.channel)) return;
                if (string.IsNullOrEmpty(message.text)) return;

                // Ignorar mensajes del propio bot o sistema
                if (message.subtype == "bot_message") return;
                if (message.user == null) return;

                // 🔥 FILTRAR SOLO EL CANAL OBJETIVO
                if (message.channel != _channelObjetivo) return;

                OnMessageReceived?.Invoke(message.channel, message.text);
            };

            _client.Connect(
                (connected) =>
                {
                    Console.WriteLine("✅ Conectado a Slack");
                },
                () =>
                {
                    Console.WriteLine("❌ Error al conectar");
                }
            );
        }

        public void SendMessage(string text)
        {
            if (string.IsNullOrEmpty(_channelObjetivo)) return;

            _client.PostMessage(
                response =>
                {
                    if (response != null && !response.ok)
                    {
                        Console.WriteLine("❌ Error enviando mensaje");
                    }
                },
                _channelObjetivo,
                text
            );
        }

        public void Disconnect()
        {
            _client?.CloseSocket();
        }
    }
}
