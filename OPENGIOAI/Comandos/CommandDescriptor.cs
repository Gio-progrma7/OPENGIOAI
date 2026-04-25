// ============================================================
//  CommandDescriptor.cs
//
//  Metadata pura de un comando — sin lógica ejecutable. Se usa
//  para (1) el registry, (2) `#ayuda`, (3) sugerencias por typo.
//
//  Inspirado en los slash-commands de Slack/Discord y los
//  /commands de Claude Code: cada comando se define por un
//  nombre canónico + alias + uso + ejemplos.
// ============================================================

using System.Collections.Generic;

namespace OPENGIOAI.Comandos
{
    public sealed record CommandDescriptor
    {
        /// <summary>Nombre canónico en minúsculas, sin `#`. Ej: "agente".</summary>
        public string Nombre { get; init; } = "";

        /// <summary>Alias adicionales (sin `#`). Ej: ["ag"] para agente.</summary>
        public IReadOnlyList<string> Alias { get; init; } = System.Array.Empty<string>();

        /// <summary>Descripción corta para la línea de ayuda.</summary>
        public string Descripcion { get; init; } = "";

        /// <summary>Patrón de uso — ej: "#agente &lt;nombre&gt;".</summary>
        public string Uso { get; init; } = "";

        /// <summary>Ejemplos concretos — ej: ["#agente claude"].</summary>
        public IReadOnlyList<string> Ejemplos { get; init; } = System.Array.Empty<string>();

        public CommandCategoria Categoria { get; init; } = CommandCategoria.Configuracion;

        /// <summary>
        /// Si es true, el comando NO aparece en `#ayuda` pero sigue siendo
        /// ejecutable. Útil para callback_data legacy como #ACTIVATELEGRAM.
        /// </summary>
        public bool Oculto { get; init; } = false;

        /// <summary>
        /// Devuelve todos los nombres reconocidos (canónico + alias), en
        /// minúsculas. Útil para lookups en el registry.
        /// </summary>
        public IEnumerable<string> NombresReconocidos()
        {
            yield return Nombre.ToLowerInvariant();
            foreach (var a in Alias)
                yield return a.ToLowerInvariant();
        }
    }
}
