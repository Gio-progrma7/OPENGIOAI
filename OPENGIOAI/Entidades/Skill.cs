// ============================================================
//  Skill.cs — Entidad de skill ejecutable
//
//  Compatible hacia atrás: entradas viejas en ListSkills.json
//  (solo RutaScript + Descripcion) deserializan sin errores
//  porque todos los campos nuevos tienen valores por defecto.
// ============================================================

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

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

        // ── Campos Hub (skills instalados desde URL remota) ──────────────────

        /// <summary>
        /// URL desde la que se instaló este skill.
        /// Vacío = skill local (creado manualmente).
        /// Presente = skill del Hub; permite re-descargarlo para actualizar.
        /// </summary>
        public string SourceUrl { get; set; } = "";

        /// <summary>Autor del skill. Ej: "opengio", "username".</summary>
        public string Autor { get; set; } = "";

        /// <summary>Versión semántica. Ej: "1.0.0", "2.3.1".</summary>
        public string Version { get; set; } = "";

        // ── Propiedad computada de origen ────────────────────────────────────

        /// <summary>True si este skill fue instalado desde el Hub (tiene SourceUrl).</summary>
        public bool EsDeHub => !string.IsNullOrWhiteSpace(SourceUrl);

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

        // ── Helpers de parámetros ────────────────────────────────────────────

        /// <summary>
        /// Valida un objeto JSON con los valores de parámetros contra <see cref="Parametros"/>.
        /// Devuelve la lista de errores legibles (vacía si todo es válido).
        /// Reusable por la UI (botón Probar) y el agente (HerramientaSkill).
        /// </summary>
        public List<string> ValidarParametros(JObject? valores)
        {
            var errores = new List<string>();
            if (Parametros == null || Parametros.Count == 0) return errores;

            valores ??= new JObject();

            foreach (var p in Parametros)
            {
                bool presente = valores.TryGetValue(p.Nombre, out var token)
                                && token != null
                                && token.Type != JTokenType.Null;

                bool vacio = presente
                             && token!.Type == JTokenType.String
                             && string.IsNullOrWhiteSpace(token.ToString());

                if (p.Requerido && (!presente || vacio))
                {
                    string desc = string.IsNullOrWhiteSpace(p.Descripcion)
                        ? ""
                        : $" — {p.Descripcion}";
                    errores.Add($"Falta parámetro requerido: '{p.Nombre}' ({p.Tipo}){desc}");
                    continue;
                }

                if (!presente) continue;

                // Validar opciones (enum)
                if (p.Opciones != null && p.Opciones.Count > 0)
                {
                    string actual = token!.ToString();
                    bool ok = p.Opciones.Any(o =>
                        o.Equals(actual, System.StringComparison.OrdinalIgnoreCase));
                    if (!ok)
                        errores.Add(
                            $"Parámetro '{p.Nombre}' debe ser uno de: " +
                            $"{string.Join(", ", p.Opciones)} (recibido: '{actual}')");
                }

                // Validar tipo numérico
                if (p.Tipo.Equals("number", System.StringComparison.OrdinalIgnoreCase) ||
                    p.Tipo.Equals("integer", System.StringComparison.OrdinalIgnoreCase))
                {
                    string actual = token!.ToString();
                    if (!double.TryParse(actual,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out _))
                        errores.Add(
                            $"Parámetro '{p.Nombre}' debe ser numérico (recibido: '{actual}')");
                }

                // Validar tipo booleano
                if (p.Tipo.Equals("boolean", System.StringComparison.OrdinalIgnoreCase) ||
                    p.Tipo.Equals("bool", System.StringComparison.OrdinalIgnoreCase))
                {
                    string actual = token!.ToString().Trim().ToLowerInvariant();
                    if (actual != "true" && actual != "false")
                        errores.Add(
                            $"Parámetro '{p.Nombre}' debe ser true/false (recibido: '{actual}')");
                }
            }

            return errores;
        }
    }

    /// <summary>Parámetro de un skill — define nombre, tipo y si es obligatorio.</summary>
    public class SkillParametro
    {
        /// <summary>Nombre del parámetro tal como se usa en la llamada Python.</summary>
        public string Nombre { get; set; } = "";

        /// <summary>Tipo JSON Schema: "string" · "number" · "integer" · "boolean" · "array"</summary>
        public string Tipo { get; set; } = "string";

        /// <summary>Descripción breve del parámetro.</summary>
        public string Descripcion { get; set; } = "";

        /// <summary>Indica si el parámetro es obligatorio. Por defecto true.</summary>
        public bool Requerido { get; set; } = true;

        /// <summary>
        /// Valor por defecto (string). Se usa para prellenar el diálogo de prueba
        /// y para sugerir al LLM. Vacío = sin default.
        /// </summary>
        public string ValorPorDefecto { get; set; } = "";

        /// <summary>
        /// Lista cerrada de valores aceptados (enum). Vacía = libre.
        /// Si tiene elementos, el diálogo lo mostrará como ComboBox.
        /// </summary>
        public List<string> Opciones { get; set; } = new();
    }
}
