using System.Linq;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Selecciona el agente activo por nombre (ChatGpt, Claude, Ollama, etc.)
    /// Sin argumentos, muestra el agente actual.
    /// </summary>
    public sealed class AgenteCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "agente",
            Alias       = new[] { "ag" },
            Descripcion = "Cambia el agente activo (ChatGpt, Claude, Gemini, ...).",
            Uso         = "#agente <nombre>",
            Ejemplos    = new[] { "#agente Claude", "#agente" },
            Categoria   = CommandCategoria.Agente,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arg0))
            {
                var actual = ctx.Servicios.AgenteActual();
                var disponibles = string.Join(", ",
                    ctx.Servicios.ListaAgentes().Select(a => a.Agente.ToString()));
                return Task.FromResult(CommandResult.OkMsg(
                    mensaje: $"Agente actual: *{actual}*\nDisponibles: {disponibles}",
                    titulo: "Agente"));
            }

            bool ok = ctx.Servicios.SeleccionarAgente(ctx.Arg0);
            if (!ok)
            {
                return Task.FromResult(CommandResult.Error(
                    $"No encontré el agente `{ctx.Arg0}`. Usa `#agente` sin args para ver los disponibles."));
            }

            return Task.FromResult(CommandResult.Exito(
                $"Agente cambiado a *{ctx.Arg0}*.", titulo: "Agente"));
        }
    }
}
