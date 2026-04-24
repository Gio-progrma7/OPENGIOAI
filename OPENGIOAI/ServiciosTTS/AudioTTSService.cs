using Microsoft.Extensions.Logging;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosTTS
{
    /// <summary>
    /// Capa de aplicación para Text-To-Speech. Encapsula la configuración TTS
    /// y la generación del archivo de audio temporal a partir de texto.
    ///
    /// El consumidor decide cómo distribuir el archivo (Telegram, Slack, etc.);
    /// esta clase no conoce los canales de salida.
    /// </summary>
    public class AudioTTSService
    {
        private readonly ILogger<AudioTTSService> _logger;
        private ConfiguracionTTS _config = new();

        public AudioTTSService(ILogger<AudioTTSService> logger)
        {
            _logger = logger;
        }

        public ConfiguracionTTS Config => _config;
        public bool Activo => _config.Activo;

        /// <summary>Recarga la configuración TTS desde disco.</summary>
        public void RecargarConfig()
        {
            _config = Utils.LeerConfig<ConfiguracionTTS>(
                RutasProyecto.ObtenerRutaConfiguracionTTS());
        }

        /// <summary>
        /// Genera el audio para el texto dado y lo escribe en un archivo
        /// temporal. Devuelve la ruta o null si no se generó audio (TTS
        /// inactivo o respuesta vacía del proveedor).
        /// </summary>
        public async Task<string?> GenerarArchivoTemporalAsync(string mensaje)
        {
            if (!_config.Activo) return null;

            try
            {
                var (audioBytes, ext) = await ServicioTTS.GenerarAudioAsync(mensaje, _config);
                if (audioBytes.Length == 0)
                {
                    _logger.LogWarning("TTS devolvió audio vacío (len texto={Len})", mensaje?.Length ?? 0);
                    return null;
                }

                string tmpFile = Path.Combine(Path.GetTempPath(),
                    $"aria_audio_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}");

                await File.WriteAllBytesAsync(tmpFile, audioBytes);
                return tmpFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando audio TTS");
                throw;
            }
        }
    }
}
