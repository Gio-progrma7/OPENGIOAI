// ============================================================
//  CommandContext.cs
//
//  Lo que recibe un handler cuando se ejecuta. Inmutable: toda
//  la mutación del estado de la app pasa por `Servicios`, un
//  objeto bien tipado (ver `IServiciosComandos.cs`).
// ============================================================

using System.Collections.Generic;
using System.Threading;

namespace OPENGIOAI.Comandos
{
    public sealed record CommandContext
    {
        /// <summary>Nombre tal como lo escribió el usuario (sin `#`, case preservado).</summary>
        public string NombreOriginal { get; init; } = "";

        /// <summary>Todos los argumentos en crudo, sin el comando. Ej: "claude 3.5".</summary>
        public string ArgsRaw { get; init; } = "";

        /// <summary>Argumentos ya tokenizados preservando el case original.</summary>
        public IReadOnlyList<string> Args { get; init; } = System.Array.Empty<string>();

        /// <summary>Chat de Telegram origen, si el comando vino de ahí.</summary>
        public long ChatIdTelegram { get; init; }

        public bool UsarTelegram { get; init; }
        public bool UsarSlack    { get; init; }

        /// <summary>Fachada tipada con todo lo que un handler puede tocar.</summary>
        public IServiciosComandos Servicios { get; init; } = null!;

        /// <summary>Token de cancelación — útil para handlers que esperan I/O.</summary>
        public CancellationToken Cancelacion { get; init; } = default;

        // ── Helpers de args ──────────────────────────────────────────────────

        /// <summary>Primer argumento o "" si no hay ninguno.</summary>
        public string Arg0 => Args.Count > 0 ? Args[0] : "";

        /// <summary>Segundo argumento o "" si no hay.</summary>
        public string Arg1 => Args.Count > 1 ? Args[1] : "";

        /// <summary>
        /// Interpreta un argumento como on/off/toggle.
        /// "on", "true", "1", "si", "sí", "activo"   → true
        /// "off", "false", "0", "no", "inactivo"      → false
        /// Cualquier otra cosa (incluido vacío)       → null (toggle)
        /// </summary>
        public static bool? InterpretarOnOff(string valor)
        {
            var v = (valor ?? "").Trim().ToLowerInvariant();
            return v switch
            {
                "on" or "true" or "1" or "si" or "sí" or "activo" or "activa"   => true,
                "off" or "false" or "0" or "no" or "inactivo" or "inactiva"     => false,
                _ => null,
            };
        }
    }
}
