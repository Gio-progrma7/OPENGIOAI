using OPENGIOAI.ServiciosTelegram;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    /// <summary>
    /// Contexto inmutable que recibe cada handler de comando.
    /// </summary>
    public record CommandContext(
        string Valor,
        long ChatIdTelegram,
        bool UsarTelegram,
        bool UsarSlack);

    /// <summary>
    /// Dispatcher de comandos tipo "#COMANDO valor". Acepta handlers registrados
    /// por nombre y un fallback opcional para texto libre (cuando no es comando
    /// o el comando no está registrado).
    ///
    /// El router no conoce los servicios de transporte: cada handler es una
    /// closure que captura lo que necesita (form state, BroadcastService, etc.).
    /// </summary>
    public class CommandRouter
    {
        private readonly Dictionary<string, Func<CommandContext, Task>> _handlers =
            new(StringComparer.OrdinalIgnoreCase);

        private Func<string, CommandContext, Task>? _fallback;

        public void Registrar(string comando, Func<CommandContext, Task> handler)
        {
            _handlers[comando] = handler;
        }

        /// <summary>
        /// Handler para texto libre o comandos no registrados. Recibe el texto
        /// original y el contexto completo (incluye usarTelegram / usarSlack).
        /// </summary>
        public void RegistrarFallback(Func<string, CommandContext, Task> handler)
        {
            _fallback = handler;
        }

        /// <summary>
        /// Despacha un texto entrante. Si comienza con '#' y el comando está
        /// registrado, ejecuta el handler; si no, delega al fallback (si existe).
        /// </summary>
        public async Task DespacharAsync(
            string textoCmd, string textoOriginal,
            long chatIdTelegram,
            bool usarTelegram, bool usarSlack)
        {
            textoCmd = textoCmd.Trim().ToUpperInvariant();

            string valor = "";
            if (textoCmd.StartsWith('#'))
            {
                var (comando, val) = TelegramSender.ExtraerComandoYValor(textoCmd);
                textoCmd = comando;
                valor = val;
            }

            var contexto = new CommandContext(valor, chatIdTelegram, usarTelegram, usarSlack);

            if (_handlers.TryGetValue(textoCmd, out var handler))
            {
                await handler(contexto);
                return;
            }

            if (_fallback != null && !string.IsNullOrEmpty(textoOriginal))
                await _fallback(textoOriginal, contexto);
        }
    }
}
