using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    /// <summary>
    /// Menú principal de configuraciones para Telegram — expone un teclado
    /// inline con los toggles más usados.
    /// </summary>
    public sealed class ConfiguracionesCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "configuraciones",
            Alias       = new[] { "configs", "config", "ajustes" },
            Descripcion = "Abre el menú de configuraciones (Telegram).",
            Uso         = "#configuraciones",
            Ejemplos    = new[] { "#configuraciones" },
            Categoria   = CommandCategoria.Configuracion,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx) =>
            Task.FromResult(new CommandResult
            {
                Ok              = true,
                Mensaje         = "Elije una opción:",
                TecladoTelegram = ctx.Servicios.KeyboardConfiguraciones(),
            });
    }
}
