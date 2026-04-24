using Microsoft.Extensions.Logging;
using OPENGIOAI.ServiciosSlack;
using OPENGIOAI.ServiciosTelegram;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    /// <summary>
    /// Coordinador de envío simultáneo a Telegram y Slack. Encapsula el patrón
    /// Task.WhenAll para no repetirlo en cada call-site del enrutador de comandos.
    /// </summary>
    public class BroadcastService
    {
        private readonly TelegramService _telegram;
        private readonly SlackChannelService _slack;
        private readonly ILogger<BroadcastService> _logger;

        public BroadcastService(
            TelegramService telegram,
            SlackChannelService slack,
            ILogger<BroadcastService> logger)
        {
            _telegram = telegram;
            _slack = slack;
            _logger = logger;
        }

        /// <summary>
        /// Envía el mismo mensaje a los canales activos en paralelo. Para
        /// Telegram usa el chatId provisto (puede diferir del configurado si
        /// viene de un callback); para Slack siempre envía al canal configurado.
        /// </summary>
        public async Task EnviarAsync(
            long chatIdTelegram, string mensaje, object? btns,
            bool usarTelegram, bool usarSlack)
        {
            var tareas = new List<Task>();

            if (usarTelegram)
                tareas.Add(_telegram.EnviarMensajeAsync(chatIdTelegram, mensaje, btns));

            if (usarSlack && _slack.IsConfigured)
                tareas.Add(_slack.EnviarMensajeAsync(mensaje));

            if (tareas.Count > 0)
            {
                _logger.LogDebug("Broadcast: {Canales} canal(es), len={Len}",
                    tareas.Count, mensaje?.Length ?? 0);
                await Task.WhenAll(tareas);
            }
        }
    }
}
