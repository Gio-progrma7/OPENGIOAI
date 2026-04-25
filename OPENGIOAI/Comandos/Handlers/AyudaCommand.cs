// ============================================================
//  AyudaCommand.cs
//
//  Implementa `#ayuda` — lista los comandos visibles agrupados
//  por categoría, o el detalle de uno específico si se pasa como
//  argumento: `#ayuda audio`.
//
//  Inspirado en `/help` de Claude Code y Slack: la salida es
//  compacta, se lee en el chat sin abrumar.
// ============================================================

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class AyudaCommand : ICommand
    {
        private readonly CommandRegistry _registry;

        public AyudaCommand(CommandRegistry registry) => _registry = registry;

        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "ayuda",
            Alias       = new[] { "help", "h", "?" },
            Descripcion = "Muestra los comandos disponibles. Usa `#ayuda <cmd>` para detalle.",
            Uso         = "#ayuda [comando]",
            Ejemplos    = new[] { "#ayuda", "#ayuda audio" },
            Categoria   = CommandCategoria.Ayuda,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            if (!string.IsNullOrWhiteSpace(ctx.Arg0))
                return Task.FromResult(AyudaDe(ctx.Arg0.TrimStart('#')));

            return Task.FromResult(AyudaGeneral());
        }

        // ── Ayuda general ────────────────────────────────────────────────────
        private CommandResult AyudaGeneral()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Comandos disponibles — escribe `#ayuda <cmd>` para detalles.");
            sb.AppendLine();

            var porCategoria = _registry.TodosVisibles()
                .GroupBy(c => c.Descriptor.Categoria)
                .OrderBy(g => (int)g.Key);

            foreach (var grupo in porCategoria)
            {
                sb.Append(grupo.Key.Icono()).Append(' ')
                  .Append('*').Append(grupo.Key.Titulo()).Append('*')
                  .AppendLine();

                foreach (var cmd in grupo.OrderBy(c => c.Descriptor.Nombre))
                {
                    sb.Append("  `#").Append(cmd.Descriptor.Nombre).Append('`')
                      .Append(" — ").AppendLine(cmd.Descriptor.Descripcion);
                }
                sb.AppendLine();
            }

            return new CommandResult
            {
                Ok      = true,
                Titulo  = "Ayuda",
                Mensaje = sb.ToString().TrimEnd(),
            };
        }

        // ── Ayuda de un comando concreto ─────────────────────────────────────
        private CommandResult AyudaDe(string nombre)
        {
            var cmd = _registry.Resolver(nombre);
            if (cmd == null)
            {
                var sug = _registry.Sugerencias(nombre);
                if (sug.Count == 0)
                    return CommandResult.Error($"No existe el comando `#{nombre}`.");

                var nombres = string.Join(", ", sug.Select(s => $"`#{s.Descriptor.Nombre}`"));
                return CommandResult.Error(
                    $"No existe `#{nombre}`. ¿Quisiste decir {nombres}?");
            }

            var d = cmd.Descriptor;
            var sb = new StringBuilder();
            sb.Append("*").Append(d.Descripcion).AppendLine("*");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(d.Uso))
                sb.Append("Uso: `").Append(d.Uso).AppendLine("`");

            if (d.Alias.Count > 0)
                sb.Append("Alias: ")
                  .AppendLine(string.Join(", ", d.Alias.Select(a => $"`#{a}`")));

            if (d.Ejemplos.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Ejemplos:");
                foreach (var ej in d.Ejemplos)
                    sb.Append("  • `").Append(ej).AppendLine("`");
            }

            return new CommandResult
            {
                Ok      = true,
                Titulo  = $"#{d.Nombre}",
                Mensaje = sb.ToString().TrimEnd(),
            };
        }
    }
}
