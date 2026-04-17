using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosTTS
{
    /// <summary>
    /// Genera audio TTS desde tres proveedores: System.Speech (Windows),
    /// OpenAI TTS y Google Cloud Text-to-Speech.
    /// Devuelve los bytes del audio y la extensión del archivo resultante.
    /// </summary>
    public static class ServicioTTS
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };

        // Límite práctico de chars para evitar audios muy largos / costos excesivos
        private const int MaxCharsAudio = 3_000;

        // ── Voces predefinidas por proveedor ────────────────────────────────────

        public static readonly IReadOnlyList<string> VocesOpenAI =
            new[] { "nova", "alloy", "echo", "fable", "onyx", "shimmer" };

        public static readonly IReadOnlyList<string> VocesGoogle =
            new[]
            {
                "es-ES-Neural2-A", "es-ES-Neural2-B", "es-ES-Neural2-C", "es-ES-Neural2-D",
                "es-MX-Neural2-A", "es-MX-Neural2-B", "es-MX-Neural2-C",
                "es-US-Neural2-A", "es-US-Neural2-B",
                "en-US-Neural2-A", "en-US-Neural2-C", "en-US-Neural2-F",
                "en-GB-Neural2-A", "en-GB-Neural2-B"
            };

        // ── Punto de entrada público ────────────────────────────────────────────

        /// <summary>
        /// Genera audio para el texto dado usando la configuración activa.
        /// Retorna (bytes, extensión) — p.ej. (mp3Bytes, "mp3") o (wavBytes, "wav").
        /// En caso de error retorna un array vacío.
        /// </summary>
        public static async Task<(byte[] Bytes, string Extension)> GenerarAudioAsync(
            string texto, ConfiguracionTTS config)
        {
            if (string.IsNullOrWhiteSpace(texto) || !config.Activo)
                return (Array.Empty<byte>(), "mp3");

            // Truncar para respetar límites de las APIs y mantener audios ágiles
            if (texto.Length > MaxCharsAudio)
                texto = texto[..MaxCharsAudio] + "...";

            try
            {
                return config.Proveedor switch
                {
                    ProveedorTTS.OpenAI => await GenerarOpenAIAsync(texto, config),
                    ProveedorTTS.Google => await GenerarGoogleAsync(texto, config),
                    _                  => GenerarSystemSpeech(texto, config)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Error al generar audio: {ex.Message}");
                return (Array.Empty<byte>(), "mp3");
            }
        }

        // ── System.Speech (Windows SAPI) ────────────────────────────────────────

        private static (byte[], string) GenerarSystemSpeech(string texto, ConfiguracionTTS config)
        {
            using var synth = new System.Speech.Synthesis.SpeechSynthesizer();

            if (!string.IsNullOrWhiteSpace(config.Voz))
            {
                try { synth.SelectVoice(config.Voz); }
                catch { /* voz no instalada — usar la predeterminada */ }
            }

            using var ms = new MemoryStream();
            synth.SetOutputToWaveStream(ms);
            synth.Speak(texto);
            synth.SetOutputToNull();   // importante: cierra el stream limpiamente
            return (ms.ToArray(), "wav");
        }

        /// <summary>Devuelve los nombres de todas las voces SAPI5 instaladas en el sistema.</summary>
        public static List<string> ObtenerVocesInstaladas()
        {
            var voces = new List<string>();
            try
            {
                using var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                foreach (var v in synth.GetInstalledVoices())
                    voces.Add(v.VoiceInfo.Name);
            }
            catch { /* SAPI no disponible */ }
            return voces;
        }

        // ── OpenAI TTS ──────────────────────────────────────────────────────────

        private static async Task<(byte[], string)> GenerarOpenAIAsync(
            string texto, ConfiguracionTTS config)
        {
            var voz = string.IsNullOrWhiteSpace(config.Voz) ? "nova" : config.Voz;

            var body = new
            {
                model          = "tts-1",
                input          = texto,
                voice          = voz,
                response_format = "mp3"
            };

            using var req = new HttpRequestMessage(
                HttpMethod.Post, "https://api.openai.com/v1/audio/speech");
            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", config.ApiKey);
            req.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadAsByteArrayAsync(), "mp3");
        }

        // ── Google Cloud TTS ────────────────────────────────────────────────────

        private static async Task<(byte[], string)> GenerarGoogleAsync(
            string texto, ConfiguracionTTS config)
        {
            var voz    = string.IsNullOrWhiteSpace(config.Voz)    ? "es-ES-Neural2-A" : config.Voz;
            var idioma = string.IsNullOrWhiteSpace(config.Idioma) ? "es-ES"           : config.Idioma;

            var body = new
            {
                input       = new { text = texto },
                voice       = new { languageCode = idioma, name = voz },
                audioConfig = new { audioEncoding = "MP3" }
            };

            var url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={config.ApiKey}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var b64 = doc.RootElement.GetProperty("audioContent").GetString()!;
            return (Convert.FromBase64String(b64), "mp3");
        }
    }
}
