using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Selecciona el modelo activo del agente vigente. El argumento se pasa
    /// tal cual al ComboBox (que tolera match case-insensitive).
    /// </summary>
    public sealed class ModeloCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "modelo",
            Alias       = new[] { "model" },
            Descripcion = "Cambia el modelo del agente activo.",
            Uso         = "#modelo <nombre>",
            Ejemplos    = new[] { "#modelo gpt-4o-mini", "#modelo" },
            Categoria   = CommandCategoria.Agente,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.ArgsRaw))
            {
                return Task.FromResult(CommandResult.OkMsg(
                    mensaje: $"Modelo actual: *{ctx.Servicios.ModeloActual()}*",
                    titulo: "Modelo"));
            }

            // Respetamos ArgsRaw para no perder espacios internos.
            ctx.Servicios.SeleccionarModelo(ctx.ArgsRaw);
            return Task.FromResult(CommandResult.Exito(
                $"Modelo cambiado a *{ctx.ArgsRaw}*.", titulo: "Modelo"));
        }
    }
}
