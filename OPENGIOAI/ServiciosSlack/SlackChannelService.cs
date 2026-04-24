using Microsoft.Extensions.Logging;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosSlack
{
    /// <summary>
    /// Capa de transporte para Slack. Encapsula el SlackPollingService, la
    /// configuración del canal (token + IDcanal + lista de usuarios autorizados)
    /// y todas las operaciones de envío.
    ///
    /// Los consumidores se suscriben a OnMessageReceived para reaccionar a
    /// mensajes entrantes; OnAviso reporta condiciones de configuración faltante
    /// para que la UI pueda mostrarlas sin que esta clase dependa de WinForms.
    /// </summary>
    public class SlackChannelService
    {
        private readonly ILogger<SlackChannelService> _logger;
        private SlackPollingService? _polling;
        private SlackChat _config = new() { Tokem = "", IDcanal = "" };

        public SlackChannelService(ILogger<SlackChannelService> logger)
        {
            _logger = logger;
        }

        public SlackChat Chat => _config;
        public bool IsConfigured => !string.IsNullOrEmpty(_config?.Tokem);

        public event Action<SlackMessage>? OnMessageReceived;
        public event Action<string>? OnAviso;

        /// <summary>
        /// Lee la configuración de Slack asociada al directorio de trabajo dado.
        /// Si no existe, deja la configuración vacía y dispara OnAviso para que
        /// la UI lo notifique al usuario.
        /// </summary>
        public void CargarConfiguracion(string rutaTrabajo)
        {
            if (string.IsNullOrEmpty(rutaTrabajo) || !Directory.Exists(rutaTrabajo))
                return;

            var config = JsonManager.Leer<SlackChat>(
                RutasProyecto.ObtenerRutaSlack(rutaTrabajo));

            if (config.Count > 0)
            {
                _config = config[0];
            }
            else
            {
                _config = new SlackChat { Tokem = "", IDcanal = "" };
                OnAviso?.Invoke(
                    "No tienes configuración para Slack, configúrala para poder comunicarte.");
            }
        }

        /// <summary>
        /// Inicia (o reinicia) el polling con la configuración actual. Devuelve
        /// false si la configuración no es válida.
        /// </summary>
        public bool Iniciar()
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("Iniciar() abortado: Slack no está configurado");
                return false;
            }

            _polling?.Stop();
            _polling = new SlackPollingService(_config.Tokem, _config.IDcanal);
            _polling.OnMessageReceived += msg => OnMessageReceived?.Invoke(msg);
            _polling.Start();
            _logger.LogInformation("Slack polling iniciado (canal={Canal})", _config.IDcanal);
            return true;
        }

        public void Detener()
        {
            _polling?.Stop();
            _polling = null;
            _logger.LogInformation("Slack polling detenido");
        }

        public Task EnviarMensajeAsync(string text) =>
            _polling?.SendMessage(text) ?? Task.CompletedTask;

        public Task EnviarCodigoAsync(string text) =>
            _polling?.EnviarCodigoAsync(text) ?? Task.CompletedTask;

        public Task<string?> EnviarPensandoAsync() =>
            _polling?.EnviarPensandoAsync() ?? Task.FromResult<string?>(null);

        public Task EnviarArchivoAsync(string rutaArchivo, string title = "") =>
            _polling?.EnviarArchivoAsync(rutaArchivo, title) ?? Task.CompletedTask;

        public Task EliminarMensajeAsync(string ts) =>
            _polling?.EliminarMensajeAsync(ts) ?? Task.CompletedTask;
    }
}
