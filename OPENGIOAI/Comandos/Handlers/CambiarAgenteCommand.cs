using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Envía al usuario (Telegram) un teclado inline con la lista de agentes
    /// para que elija sin tener que teclear el nombre exacto.
    /// </summary>
    public sealed class CambiarAgenteCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "cambiaragente",
            Alias       = new[] { "agentes" },
            Descripcion = "Muestra botones para elegir agente (Telegram).",
            Uso         = "#cambiaragente",
            Ejemplos    = new[] { "#cambiaragente" },
            Categoria   = CommandCategoria.Agente,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            return Task.FromResult(new CommandResult
            {
                Ok             = true,
                Tipo           = ResultTipo.Info,
                Mensaje        = "Elije un agente:",
                TecladoTelegram = ctx.Servicios.KeyboardCambiarAgente(),
            });
        }
    }
}
