using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class CambiarRutaCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "cambiarruta",
            Alias       = new[] { "rutas" },
            Descripcion = "Muestra botones para elegir ruta (Telegram).",
            Uso         = "#cambiarruta",
            Ejemplos    = new[] { "#cambiarruta" },
            Categoria   = CommandCategoria.Agente,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx) =>
            Task.FromResult(new CommandResult
            {
                Ok              = true,
                Mensaje         = "Elije una ruta:",
                TecladoTelegram = ctx.Servicios.KeyboardCambiarRuta(),
            });
    }
}
