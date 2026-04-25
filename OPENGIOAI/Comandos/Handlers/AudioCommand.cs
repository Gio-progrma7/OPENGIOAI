// ============================================================
//  AudioCommand.cs
//
//  Configura el subsistema TTS desde el chat:
//
//    · `#audio`                           → estado actual
//    · `#audio on` / `#audio off`         → toggle de envío de audio
//    · `#audio proveedor <SystemSpeech|OpenAI|Google>`
//    · `#audio voz <nombre>`              → actualiza la voz
//    · `#audio idioma <bcp-47>`           → actualiza idioma (Google)
//    · `#audio apikey <key>`              → API key del proveedor
//    · `#audio activar` / `#audio desactivar` → marca Activo en la config TTS
//
//  Los cambios se persisten invocando Servicios.AudioConfigurar(),
//  que a su vez escribe el JSON de config y recarga el AudioTTSService.
// ============================================================

using OPENGIOAI.Entidades;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class AudioCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "audio",
            Alias       = new[] { "tts", "voz" },
            Descripcion = "Configura el subsistema de voz (TTS).",
            Uso         = "#audio [on|off|proveedor|voz|idioma|apikey|activar|desactivar] [valor]",
            Ejemplos    = new[]
            {
                "#audio",
                "#audio on",
                "#audio proveedor OpenAI",
                "#audio voz nova",
                "#audio apikey sk-...",
                "#audio activar",
            },
            Categoria   = CommandCategoria.Integracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            var sub = ctx.Arg0.ToLowerInvariant();

            // Sin args: mostrar estado actual.
            if (string.IsNullOrEmpty(sub))
                return Task.FromResult(Estado(ctx));

            // On/off sobre el toggle de ENVÍO (no cambia la config TTS).
            var onOff = CommandContext.InterpretarOnOff(sub);
            if (onOff != null)
                return Task.FromResult(CambiarEnvio(ctx, onOff.Value));

            var cfg = ctx.Servicios.AudioConfigActual();
            string valor = ctx.Args.Count > 1
                ? string.Join(' ', ctx.Args.Skip(1))
                : "";

            switch (sub)
            {
                case "proveedor":
                case "provider":
                    if (!Enum.TryParse<ProveedorTTS>(valor, ignoreCase: true, out var prov))
                        return Task.FromResult(CommandResult.Error(
                            "Proveedor inválido. Opciones: SystemSpeech, OpenAI, Google."));
                    cfg.Proveedor = prov;
                    return Task.FromResult(Persistir(ctx, cfg, $"Proveedor TTS: *{prov}*."));

                case "voz":
                case "voice":
                    if (string.IsNullOrWhiteSpace(valor))
                        return Task.FromResult(CommandResult.Error("Indica la voz a usar."));
                    cfg.Voz = valor;
                    return Task.FromResult(Persistir(ctx, cfg, $"Voz establecida: *{valor}*."));

                case "idioma":
                case "lang":
                    if (string.IsNullOrWhiteSpace(valor))
                        return Task.FromResult(CommandResult.Error("Indica el código BCP-47 (p.ej. es-MX)."));
                    cfg.Idioma = valor;
                    return Task.FromResult(Persistir(ctx, cfg, $"Idioma TTS: *{valor}*."));

                case "apikey":
                case "key":
                    if (string.IsNullOrWhiteSpace(valor))
                        return Task.FromResult(CommandResult.Error("Indica la API key."));
                    cfg.ApiKey = valor;
                    return Task.FromResult(Persistir(ctx, cfg,
                        "API key TTS actualizada. (No se muestra por seguridad.)"));

                case "activar":
                    cfg.Activo = true;
                    return Task.FromResult(Persistir(ctx, cfg, "TTS marcado como *activo*."));

                case "desactivar":
                    cfg.Activo = false;
                    return Task.FromResult(Persistir(ctx, cfg, "TTS marcado como *inactivo*."));

                default:
                    return Task.FromResult(CommandResult.Error(
                        $"Subcomando `{sub}` no reconocido. Usa `#ayuda audio` para opciones."));
            }
        }

        private static CommandResult Estado(CommandContext ctx)
        {
            var cfg = ctx.Servicios.AudioConfigActual();
            return new CommandResult
            {
                Ok     = true,
                Titulo = "Audio TTS",
                Mensaje = $"Envío de audio: *{(ctx.Servicios.AudioActivo ? "ON" : "OFF")}*\n" +
                          $"Proveedor: *{cfg.Proveedor}*\n" +
                          $"Voz: `{(string.IsNullOrEmpty(cfg.Voz) ? "(default)" : cfg.Voz)}`\n" +
                          $"Idioma: `{cfg.Idioma}`\n" +
                          $"API key: {(string.IsNullOrEmpty(cfg.ApiKey) ? "—" : "configurada")}\n" +
                          $"TTS configurado: {(cfg.Activo ? "sí ✅" : "no ⛔")}",
            };
        }

        private static CommandResult CambiarEnvio(CommandContext ctx, bool encender)
        {
            if (encender)
            {
                var cfg = ctx.Servicios.AudioConfigActual();
                if (!cfg.Activo)
                    return CommandResult.Advertencia(
                        "Primero configura el TTS con `#audio activar` y proveedor/voz.");
            }

            ctx.Servicios.AudioActivo = encender;
            return CommandResult.Exito(
                encender ? "Envío de audio *activado*." : "Envío de audio *desactivado*.",
                titulo: "Audio");
        }

        private static CommandResult Persistir(
            CommandContext ctx, ConfiguracionTTS cfg, string mensajeOk)
        {
            return ctx.Servicios.AudioConfigurar(cfg)
                ? CommandResult.Exito(mensajeOk, titulo: "Audio")
                : CommandResult.Error("No se pudo guardar la configuración TTS.");
        }
    }
}
