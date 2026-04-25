// ============================================================
//  EstadoCommand.cs
//
//  Snapshot de TODO el estado relevante para el usuario. Se usa
//  cuando alguien dice "¿cómo estoy?" desde Telegram y quiere
//  ver, en un solo mensaje, qué agente, modelo, ruta y toggles
//  están activos. Útil también para debug.
// ============================================================

using OPENGIOAI.Utilerias;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class EstadoCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "estado",
            Alias       = new[] { "status", "info" },
            Descripcion = "Muestra un resumen del estado actual.",
            Uso         = "#estado",
            Ejemplos    = new[] { "#estado" },
            Categoria   = CommandCategoria.Estado,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            var s = ctx.Servicios;

            // Conteo de habilidades activas para un overview rápido.
            int habsActivas = 0;
            int habsTotal = 0;
            try
            {
                var todas = HabilidadesRegistry.Instancia.Todas();
                habsTotal = todas.Count;
                foreach (var h in todas) if (h.Activa) habsActivas++;
            }
            catch
            {
                // Si falla la carga, seguimos — no queremos romper `#estado`.
            }

            var detalles = new List<string>
            {
                $"🤖 Agente: *{s.AgenteActual()}*  ·  modelo: `{s.ModeloActual()}`",
                $"📂 Ruta: `{s.RutaActual()}`",
                $"📡 Telegram: {OnOff(s.TelegramActivo)}  ·  Slack: {OnOff(s.SlackActivo)}",
                $"🔊 Audio: {OnOff(s.AudioActivo)}",
                $"🧠 Memoria (recordar): {OnOff(s.RecordarTema)}  ·  Sólo chat: {OnOff(s.SoloChat)}",
                $"🔁 Reintentos: *{s.Reintentos}*  ·  ⏱ Timeout: *{s.TimeoutSegundos}s*",
                $"🧩 Habilidades activas: *{habsActivas}/{habsTotal}*",
            };

            return Task.FromResult(new CommandResult
            {
                Ok       = true,
                Titulo   = "Estado",
                Detalles = detalles,
            });
        }

        private static string OnOff(bool v) => v ? "ON ✅" : "OFF ⛔";
    }
}
