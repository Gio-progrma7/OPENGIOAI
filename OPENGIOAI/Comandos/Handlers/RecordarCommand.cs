using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class RecordarCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "recordar",
            Alias       = new[] { "recuerda" },
            Descripcion = "Activa/desactiva la memoria conversacional (recordar tema).",
            Uso         = "#recordar [on|off]",
            Ejemplos    = new[] { "#recordar on", "#recordar" },
            Categoria   = CommandCategoria.Configuracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            bool? pedido = CommandContext.InterpretarOnOff(ctx.Arg0);
            bool nuevo = pedido ?? !ctx.Servicios.RecordarTema;
            ctx.Servicios.RecordarTema = nuevo;

            return Task.FromResult(CommandResult.Exito(
                nuevo ? "Recordar tema *activado*." : "Recordar tema *desactivado*.",
                titulo: "Recordar"));
        }
    }
}
