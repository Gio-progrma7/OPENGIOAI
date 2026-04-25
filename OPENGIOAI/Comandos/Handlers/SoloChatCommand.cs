using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class SoloChatCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "solochat",
            Alias       = new[] { "chat" },
            Descripcion = "Activa/desactiva el modo sólo chat (sin pipeline agentes).",
            Uso         = "#solochat [on|off]",
            Ejemplos    = new[] { "#solochat on", "#solochat" },
            Categoria   = CommandCategoria.Configuracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            bool? pedido = CommandContext.InterpretarOnOff(ctx.Arg0);
            bool nuevo = pedido ?? !ctx.Servicios.SoloChat;
            ctx.Servicios.SoloChat = nuevo;

            return Task.FromResult(CommandResult.Exito(
                nuevo ? "Modo sólo chat *activado*." : "Modo sólo chat *desactivado*.",
                titulo: "Sólo Chat"));
        }
    }
}
