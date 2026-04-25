using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class SlackCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "slack",
            Descripcion = "Activa/desactiva el canal Slack.",
            Uso         = "#slack [on|off]",
            Ejemplos    = new[] { "#slack on", "#slack" },
            Categoria   = CommandCategoria.Integracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            bool? pedido = CommandContext.InterpretarOnOff(ctx.Arg0);
            bool nuevo = pedido ?? !ctx.Servicios.SlackActivo;
            ctx.Servicios.SlackActivo = nuevo;

            return Task.FromResult(CommandResult.Exito(
                nuevo ? "Canal Slack *activado*." : "Canal Slack *desactivado*.",
                titulo: "Slack"));
        }
    }
}
