using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Activa/desactiva el canal Telegram. Acepta on/off o toggle (sin args).
    /// Mantenemos alias legacy `activatelegram`/`desactivatelegram` ocultos para
    /// seguir respondiendo al callback_data de botones inline antiguos.
    /// </summary>
    public sealed class TelegramCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "telegram",
            Alias       = new[] { "tg" },
            Descripcion = "Activa/desactiva el canal Telegram.",
            Uso         = "#telegram [on|off]",
            Ejemplos    = new[] { "#telegram on", "#telegram off", "#telegram" },
            Categoria   = CommandCategoria.Integracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            bool? pedido = CommandContext.InterpretarOnOff(ctx.Arg0);
            bool nuevo = pedido ?? !ctx.Servicios.TelegramActivo;
            ctx.Servicios.TelegramActivo = nuevo;

            return Task.FromResult(CommandResult.Exito(
                nuevo ? "Canal Telegram *activado*." : "Canal Telegram *desactivado*.",
                titulo: "Telegram"));
        }
    }

    /// <summary>Alias legacy oculto para `#activatelegram`.</summary>
    public sealed class ActivaTelegramCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "activatelegram",
            Descripcion = "Activa Telegram (legacy).",
            Uso         = "#activatelegram",
            Categoria   = CommandCategoria.Integracion,
            Oculto      = true,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            ctx.Servicios.TelegramActivo = true;
            return Task.FromResult(CommandResult.Exito("Canal Telegram *activado*."));
        }
    }

    /// <summary>Alias legacy oculto para `#desactivatelegram`.</summary>
    public sealed class DesactivaTelegramCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "desactivatelegram",
            Descripcion = "Desactiva Telegram (legacy).",
            Uso         = "#desactivatelegram",
            Categoria   = CommandCategoria.Integracion,
            Oculto      = true,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            ctx.Servicios.TelegramActivo = false;
            return Task.FromResult(CommandResult.Exito("Canal Telegram *desactivado*."));
        }
    }
}
