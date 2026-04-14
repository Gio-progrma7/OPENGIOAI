// ============================================================
//  Skill.cs — Entidad de skill ejecutable
//
//  Compatible hacia atrás: entradas viejas en ListSkills.json
//  (solo RutaScript + Descripcion) deserializan sin errores
//  porque todos los campos nuevos tienen valores por defecto.
// ============================================================

using System.Collections.Generic;

namespace OPENGIOAI.Entidades
{
    /// <summary>
    /// Describe un skill ejecutable (script Python, PowerShell, etc.).
    /// Las propiedades computadas IdEfectivo y NombreEfectivo aseguran
    /// retrocompatibilidad con entradas que solo tienen RutaScript + Descripcion.
    /// </summary>
    public class Skill
    {
        // ── Campos heredados (retrocompatibles) ──────────────────────────────
        public string RutaScript  { get; set; } = "";
        public string Descripcion { get; set; } = "";

        // ── Campos enriquecidos (nuevos) ─────────────────────────────────────

        /// <summary>Identificador único snake_case. Ej: "obtener_ram"</summary>
        public string Id { get; set; } = "";

        /// <summary>Nombre legible. Ej: "Obtener RAM del sistema"</summary>
        public string Nombre { get; set; } = "";

        /// <summary>
        /// Categoría para agrupar en el manifiesto.
        /// Valores sugeridos: sistema · archivos · ia · web · datos · general
        /// </summary>
        public string Categoria { get; set; } = "general";

        /// <summary>Parámetros del skill expuestos en el manifiesto y en el schema de IHerramienta.</summary>
        public List<SkillParametro> Parametros { get; set; } = new();

        /// <summary>Cuando false, el skill se oculta del manifiesto y del registro de herramientas.</summary>
        public bool Activa { get; set; } = true;

        /// <summary>Ejemplo de uso mostrado en el manifiesto. Ej: skill_run("obtener_ram")</summary>
        public string Ejemplo { get; set; } = "";

        // ── Campos .md (solo en skills creados desde archivos markdown) ────────

        /// <summary>Ruta completa al archivo .md fuente. Vacío si viene de ListSkills.json legado.</summary>
        public string RutaMd { get; set; } = "";

        /// <summary>Cuerpo del .md (sin el frontmatter). Contiene ## Código, ## Parámetros, etc.</summary>
        public string ContenidoMd { get; set; } = "";

        // ── Propiedades computadas (no serializadas) ─────────────────────────

        /// <summary>
        /// Devuelve Id si está definido; si no, deriva un snake_case del nombre del script.
        /// Garantiza que siempre haya un identificador válido aunque el JSON sea el legado.
        /// </summary>
        public string IdEfectivo =>
            !string.IsNullOrWhiteSpace(Id)
                ? Id
                : System.IO.Path.GetFileNameWithoutExtension(RutaScript)
                        .ToLowerInvariant()
                        .Replace(" ", "_")
                        .Replace("-", "_");

        /// <summary>
        /// Devuelve Nombre si está definido; si no, convierte IdEfectivo a Title Case.
        /// </summary>
        public string NombreEfectivo =>
            !string.IsNullOrWhiteSpace(Nombre)
                ? Nombre
                : System.Globalization.CultureInfo.InvariantCulture
                        .TextInfo.ToTitleCase(IdEfectivo.Replace("_", " "));
    }

    /// <summary>Parámetro de un skill — define nombre, tipo y si es obligatorio.</summary>
    public class SkillParametro
    {
        /// <summary>Nombre del parámetro tal como se usa en la llamada Python.</summary>
        public string Nombre { get; set; } = "";

        /// <summary>Tipo JSON Schema: "string" · "number" · "boolean" · "array"</summary>
        public string Tipo { get; set; } = "string";

        /// <summary>Descripción breve del parámetro.</summary>
        public string Descripcion { get; set; } = "";

        /// <summary>Indica si el parámetro es obligatorio. Por defecto true.</summary>
        public bool Requerido { get; set; } = true;
    }
}
