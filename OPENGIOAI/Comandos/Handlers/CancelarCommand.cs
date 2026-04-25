using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>Cancela la petición activa al modelo de IA.</summary>
    public sealed class CancelarCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "cancelar",
            Alias       = new[] { "stop", "cancel" },
            Descripcion = "Cancela la petición en curso al modelo.",
            Uso         = "#cancelar",
            Ejemplos    = new[] { "#cancelar" },
            Categoria   = CommandCategoria.Estado,
        };

        public async Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            await ctx.Servicios.CancelarInstruccionAsync();
            return CommandResult.Advertencia("Cancelación solicitada.", titulo: "Cancelar");
        }
    }
}
