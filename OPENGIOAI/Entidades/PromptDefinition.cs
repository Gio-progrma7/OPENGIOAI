// ============================================================
//  PromptDefinition.cs
//  Define un prompt del sistema como dato (no código).
//  Cada prompt tiene:
//    · Clave única (estable, no cambia aunque cambie el texto)
//    · Nombre y descripción visibles para el usuario
//    · Categoría para agruparlos en la UI
//    · Template por defecto (el "fallback" si no hay override)
//    · Placeholders declarados → la UI los muestra como ayuda
//  Los overrides del usuario se guardan en disco y se cargan
//  dinámicamente cada vez que se pide un prompt.
// ============================================================

using System.Collections.Generic;

namespace OPENGIOAI.Entidades
{
    /// <summary>
    /// Descriptor inmutable de un prompt administrable por el usuario.
    /// La clave identifica al prompt en el registry; el template por defecto
    /// vive en código para garantizar que el sistema arranque siempre
    /// aunque se borren los archivos de override.
    /// </summary>
    public sealed class PromptDefinition
    {
        /// <summary>Clave estable (ej. "aria.analista"). Se usa como nombre de archivo.</summary>
        public string Clave { get; init; } = "";

        /// <summary>Nombre legible para mostrar en la UI.</summary>
        public string NombreVisible { get; init; } = "";

        /// <summary>Categoría visible (ej. "Pipeline ARIA", "Sistema", "Error").</summary>
        public string Categoria { get; init; } = "General";

        /// <summary>Descripción de 1-2 líneas: qué hace este prompt en el sistema.</summary>
        public string Descripcion { get; init; } = "";

        /// <summary>Icono/emoji opcional para la UI.</summary>
        public string Icono { get; init; } = "📝";

        /// <summary>
        /// Placeholders soportados (ej. "instruccion", "codigo"). La UI los
        /// muestra como referencia. El motor los sustituye al estilo {{clave}}.
        /// </summary>
        public IReadOnlyList<string> Placeholders { get; init; } = new List<string>();

        /// <summary>
        /// Plantilla por defecto. Es la fuente de verdad si no hay override.
        /// NUNCA se modifica en tiempo de ejecución — inmutable.
        /// </summary>
        public string TemplatePorDefecto { get; init; } = "";

        /// <summary>
        /// Si es true, la UI muestra el prompt como editable.
        /// Si es false, solo es visible (sensible).
        /// </summary>
        public bool Editable { get; init; } = true;

        /// <summary>
        /// Proveedor opcional de ruta absoluta a un archivo .md externo que
        /// actúa como almacenamiento del prompt. Si está definido, la registry
        /// lee y escribe aquí en lugar de usar {AppDir}/PromtsUsuario/{clave}.md.
        ///
        /// Útil para prompts cuyo archivo ya es canónico en el sistema
        /// (ej. promtMaestro.md — consumido directamente por AgentContext).
        /// </summary>
        public Func<string>? ObtenerRutaArchivoExterno { get; init; }
    }
}
