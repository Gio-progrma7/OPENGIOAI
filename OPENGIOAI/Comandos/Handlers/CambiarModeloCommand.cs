using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class CambiarModeloCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "cambiarmodelo",
            Alias       = new[] { "modelos" },
            Descripcion = "Muestra botones para elegir modelo (Telegram).",
            Uso         = "#cambiarmodelo",
            Ejemplos    = new[] { "#cambiarmodelo" },
            Categoria   = CommandCategoria.Agente,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx) =>
            Task.FromResult(new CommandResult
            {
                Ok              = true,
                Mensaje         = "Elije un modelo:",
                TecladoTelegram = ctx.Servicios.KeyboardCambiarModelo(),
            });
    }
}
