using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Ajusta el timeout del pipeline ARIA en segundos (10..1800).
    /// </summary>
    public sealed class TimeoutCommand : ICommand
    {
        private const int Min = 10;
        private const int Max = 1800;

        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "timeout",
            Alias       = new[] { "tiempo" },
            Descripcion = "Muestra o cambia el timeout del pipeline (segundos, 10–1800).",
            Uso         = "#timeout [segundos]",
            Ejemplos    = new[] { "#timeout", "#timeout 180", "#timeout 60" },
            Categoria   = CommandCategoria.Configuracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arg0))
            {
                return Task.FromResult(CommandResult.OkMsg(
                    $"Timeout actual: *{ctx.Servicios.TimeoutSegundos}s* (rango {Min}–{Max}).",
                    titulo: "Timeout"));
            }

            if (!int.TryParse(ctx.Arg0, out int seg))
                return Task.FromResult(CommandResult.Error(
                    $"`{ctx.Arg0}` no es un número. Usa `#timeout <segundos>`."));

            if (seg < Min || seg > Max)
                return Task.FromResult(CommandResult.Error(
                    $"El timeout debe estar entre {Min} y {Max} segundos."));

            ctx.Servicios.TimeoutSegundos = seg;
            return Task.FromResult(CommandResult.Exito(
                $"Timeout establecido en *{seg}s*.", titulo: "Timeout"));
        }
    }
}
