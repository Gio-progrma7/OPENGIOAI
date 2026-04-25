using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Ajusta los reintentos del Guardián. Sin argumentos muestra el valor actual.
    /// Rango 0..5; 0 significa sin reintentos automáticos.
    /// </summary>
    public sealed class ReintentosCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "reintentos",
            Alias       = new[] { "retries", "retry" },
            Descripcion = "Muestra o cambia los reintentos del Guardián (0–5).",
            Uso         = "#reintentos [N]",
            Ejemplos    = new[] { "#reintentos", "#reintentos 3" },
            Categoria   = CommandCategoria.Configuracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arg0))
            {
                return Task.FromResult(CommandResult.OkMsg(
                    $"Reintentos actuales: *{ctx.Servicios.Reintentos}* (rango 0–5).",
                    titulo: "Reintentos"));
            }

            if (!int.TryParse(ctx.Arg0, out int n))
                return Task.FromResult(CommandResult.Error(
                    $"`{ctx.Arg0}` no es un número. Usa `#reintentos 0..5`."));

            if (n < 0 || n > 5)
                return Task.FromResult(CommandResult.Error("El valor debe estar entre 0 y 5."));

            ctx.Servicios.Reintentos = n;
            return Task.FromResult(CommandResult.Exito(
                $"Reintentos establecidos en *{n}*.", titulo: "Reintentos"));
        }
    }
}
