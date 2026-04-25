using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class ApisCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "apis",
            Descripcion = "Muestra las APIs disponibles. Usa `#apis recargar` para releer desde disco.",
            Uso         = "#apis [recargar]",
            Ejemplos    = new[] { "#apis", "#apis recargar" },
            Categoria   = CommandCategoria.Agente,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            if (ctx.Arg0.Equals("recargar", System.StringComparison.OrdinalIgnoreCase)
             || ctx.Arg0.Equals("reload",   System.StringComparison.OrdinalIgnoreCase))
            {
                ctx.Servicios.RecargarApis();
            }

            var apis = ctx.Servicios.ListaApis();
            if (apis.Count == 0)
                return Task.FromResult(CommandResult.Advertencia("No hay APIs configuradas."));

            var detalles = apis.Select(a => a.Nombre).ToList();
            return Task.FromResult(new CommandResult
            {
                Ok       = true,
                Titulo   = $"APIs ({apis.Count})",
                Mensaje  = "Disponibles:",
                Detalles = detalles,
            });
        }
    }
}
