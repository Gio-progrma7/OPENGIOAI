using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Selecciona la ruta de trabajo. Sin argumentos muestra la ruta actual.
    /// </summary>
    public sealed class RutaCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "ruta",
            Alias       = new[] { "path", "proyecto" },
            Descripcion = "Cambia la ruta de trabajo del agente.",
            Uso         = "#ruta <nombre>",
            Ejemplos    = new[] { "#ruta \"Proyecto A\"", "#ruta" },
            Categoria   = CommandCategoria.Agente,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.ArgsRaw))
            {
                return Task.FromResult(CommandResult.OkMsg(
                    mensaje: $"Ruta actual: `{ctx.Servicios.RutaActual()}`",
                    titulo: "Ruta"));
            }

            ctx.Servicios.SeleccionarRuta(ctx.ArgsRaw);
            return Task.FromResult(CommandResult.Exito(
                $"Ruta cambiada a `{ctx.ArgsRaw}`.", titulo: "Ruta"));
        }
    }
}
