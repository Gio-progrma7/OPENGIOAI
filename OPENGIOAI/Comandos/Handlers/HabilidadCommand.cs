// ============================================================
//  HabilidadCommand.cs
//
//  Gestiona las habilidades cognitivas del agente (HabilidadesRegistry)
//  desde cualquier canal.
//
//    · `#habilidad`                → lista todas con su estado.
//    · `#habilidad <clave>`        → muestra detalle de una.
//    · `#habilidad <clave> on/off` → cambia el estado y persiste.
// ============================================================

using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos.Handlers
{
    public sealed class HabilidadCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "habilidad",
            Alias       = new[] { "habilidades", "skills" },
            Descripcion = "Lista o cambia las habilidades cognitivas del agente.",
            Uso         = "#habilidad [clave] [on|off]",
            Ejemplos    = new[]
            {
                "#habilidad",
                "#habilidad memoria",
                "#habilidad memoria on",
                "#habilidad patrones off",
            },
            Categoria   = CommandCategoria.Habilidades,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx)
        {
            var reg = HabilidadesRegistry.Instancia;
            var todas = reg.Todas();

            if (string.IsNullOrWhiteSpace(ctx.Arg0))
                return Task.FromResult(ListarTodas(todas));

            var hab = todas.FirstOrDefault(h =>
                h.Clave.Equals(ctx.Arg0, StringComparison.OrdinalIgnoreCase));
            if (hab == null)
            {
                var claves = string.Join(", ", todas.Select(h => $"`{h.Clave}`"));
                return Task.FromResult(CommandResult.Error(
                    $"Habilidad `{ctx.Arg0}` desconocida. Disponibles: {claves}"));
            }

            // Sólo consulta de detalle
            if (string.IsNullOrWhiteSpace(ctx.Arg1))
                return Task.FromResult(Detalle(hab));

            // Cambio de estado
            bool? pedido = CommandContext.InterpretarOnOff(ctx.Arg1);
            bool nuevo = pedido ?? !hab.Activa;
            reg.Establecer(hab.Clave, nuevo);

            return Task.FromResult(CommandResult.Exito(
                $"{hab.Icono} Habilidad *{hab.Nombre}* → {(nuevo ? "activada ✅" : "desactivada ⛔")}.",
                titulo: "Habilidad"));
        }

        private static CommandResult ListarTodas(IReadOnlyList<Entidades.Habilidad> habs)
        {
            if (habs.Count == 0)
                return CommandResult.Advertencia("No hay habilidades registradas.");

            var detalles = habs
                .Select(h => $"{h.Icono} `{h.Clave}` — {(h.Activa ? "ON " : "OFF")} · {h.Nombre}")
                .ToList();

            return new CommandResult
            {
                Ok       = true,
                Titulo   = $"Habilidades ({habs.Count})",
                Mensaje  = "Usa `#habilidad <clave> on/off` para cambiar el estado.",
                Detalles = detalles,
            };
        }

        private static CommandResult Detalle(Entidades.Habilidad h) => new()
        {
            Ok      = true,
            Titulo  = $"{h.Icono} {h.Nombre}",
            Mensaje = $"Estado: {(h.Activa ? "ON ✅" : "OFF ⛔")}\n" +
                      $"Clave: `{h.Clave}`\n" +
                      $"Impacto: {h.ImpactoTokens}\n\n" +
                      h.Descripcion,
        };
    }
}
