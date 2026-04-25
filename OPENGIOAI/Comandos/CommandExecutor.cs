// ============================================================
//  CommandExecutor.cs
//
//  Orquesta el ciclo completo de un `#comando`:
//
//    texto → PARSE → LOOKUP → EXEC → FORMAT → BROADCAST
//
//  RESPONSABILIDADES:
//    · Detectar si el texto es un comando o texto libre.
//    · Resolver alias / nombre canónico.
//    · Si no existe pero empieza con `#` → sugerir por typo.
//    · Capturar excepciones del handler → CommandResult.Error.
//    · Formatear y enviar por el canal correcto.
//
//  NO HACE:
//    · Ejecutar el motor LLM. Si el texto no es un comando,
//      devuelve `ResultadoEjecucion.NoEsComando` y el caller
//      decide (p. ej. enruta al motor IA).
// ============================================================

using Microsoft.Extensions.Logging;
using OPENGIOAI.Utilerias;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos
{
    /// <summary>
    /// Resultado del despacho a nivel de executor — incluye el caso
    /// "no era un comando" para que el caller pueda delegar al LLM.
    /// </summary>
    public sealed record ResultadoEjecucion(
        bool EsComando,
        bool ComandoReconocido,
        CommandResult? Resultado);

    public sealed class CommandExecutor
    {
        private readonly CommandRegistry _registry;
        private readonly IServiciosComandos _servicios;
        private readonly ILogger<CommandExecutor>? _logger;

        public CommandExecutor(
            CommandRegistry registry,
            IServiciosComandos servicios,
            ILogger<CommandExecutor>? logger = null)
        {
            _registry  = registry  ?? throw new ArgumentNullException(nameof(registry));
            _servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _logger    = logger;
        }

        /// <summary>
        /// Referencia al registry para que los handlers (ej. Ayuda) puedan
        /// inspeccionar la lista completa de comandos disponibles.
        /// </summary>
        public CommandRegistry Registry => _registry;

        /// <summary>
        /// Intenta despachar el texto como comando. Si lo logra (reconocido o
        /// no) envía el resultado por el canal. Si el texto no empieza con `#`,
        /// devuelve EsComando=false para que el caller decida.
        /// </summary>
        public async Task<ResultadoEjecucion> DespacharAsync(
            string texto,
            long chatIdTelegram,
            bool usarTelegram,
            bool usarSlack,
            CancellationToken ct = default)
        {
            var parseado = CommandParser.Parsear(texto);
            if (parseado == null)
                return new ResultadoEjecucion(EsComando: false, ComandoReconocido: false, Resultado: null);

            var cmd = _registry.Resolver(parseado.Nombre);

            if (cmd == null)
            {
                var sugerencias = _registry.Sugerencias(parseado.Nombre);
                var resultado = ConstruirResultadoDesconocido(parseado.Nombre, sugerencias);
                await EnviarAsync(resultado, chatIdTelegram, usarTelegram, usarSlack);
                return new ResultadoEjecucion(EsComando: true, ComandoReconocido: false, Resultado: resultado);
            }

            var ctx = new CommandContext
            {
                NombreOriginal = parseado.NombreOriginal,
                ArgsRaw        = parseado.ArgsRaw,
                Args           = parseado.Args,
                ChatIdTelegram = chatIdTelegram,
                UsarTelegram   = usarTelegram,
                UsarSlack      = usarSlack,
                Servicios      = _servicios,
                Cancelacion    = ct,
            };

            CommandResult r;
            try
            {
                r = await cmd.EjecutarAsync(ctx) ?? CommandResult.Silencio();
            }
            catch (OperationCanceledException)
            {
                r = CommandResult.Advertencia("Comando cancelado.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error ejecutando #{Cmd}", cmd.Descriptor.Nombre);
                r = CommandResult.Error(
                    $"Error ejecutando `#{cmd.Descriptor.Nombre}`: {ex.Message}");
            }

            await EnviarAsync(r, chatIdTelegram, usarTelegram, usarSlack);
            return new ResultadoEjecucion(EsComando: true, ComandoReconocido: true, Resultado: r);
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private async Task EnviarAsync(
            CommandResult r, long chatId, bool usarTelegram, bool usarSlack)
        {
            if (r.Silencioso) return;

            // Canal primario: el que originó (Telegram/Slack). Si ninguno,
            // mostramos en la UI local.
            if (usarTelegram || usarSlack)
            {
                var canal = usarTelegram ? CanalSalida.Telegram : CanalSalida.Slack;
                string? texto = ResultFormatter.Formatear(r, canal);
                if (!string.IsNullOrEmpty(texto))
                {
                    await _servicios.Broadcast.EnviarAsync(
                        chatId, texto, r.TecladoTelegram, usarTelegram, usarSlack);
                }
            }
            else
            {
                string? texto = ResultFormatter.Formatear(r, CanalSalida.UI);
                if (!string.IsNullOrEmpty(texto))
                    _servicios.MostrarEnUI(texto);
            }
        }

        private static CommandResult ConstruirResultadoDesconocido(
            string nombre, System.Collections.Generic.IReadOnlyList<ICommand> sugerencias)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("No existe el comando `#").Append(nombre).Append("`.");

            if (sugerencias.Count > 0)
            {
                sb.Append(" ¿Quisiste decir ");
                for (int i = 0; i < sugerencias.Count; i++)
                {
                    if (i > 0) sb.Append(i == sugerencias.Count - 1 ? " o " : ", ");
                    sb.Append('`').Append('#').Append(sugerencias[i].Descriptor.Nombre).Append('`');
                }
                sb.Append('?');
            }
            else
            {
                sb.Append(" Usa `#ayuda` para ver los disponibles.");
            }

            return CommandResult.Error(sb.ToString(), titulo: "Comando no reconocido");
        }
    }
}
