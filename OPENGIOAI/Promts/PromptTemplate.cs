// ============================================================
//  PromptTemplate.cs
//  Motor de sustitución {{variable}} para los prompts.
//
//  Diseño:
//    · Formato único: {{clave}} — sin anidamiento, sin lógica.
//    · Sustitución literal (no interpreta los valores).
//    · Placeholders no resueltos se dejan tal cual → visible que falta algo,
//      en lugar de producir strings silenciosamente rotos.
//    · Thread-safe: el motor es stateless.
// ============================================================

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OPENGIOAI.Promts
{
    public static class PromptTemplate
    {
        // Regex precompilada: {{identificador}} con espacios opcionales.
        // Solo letras, dígitos, guión y guión_bajo. No se permite anidar {{{{...}}}}.
        private static readonly Regex RegexPlaceholder = new(
            @"\{\{\s*([A-Za-z_][A-Za-z0-9_\-]*)\s*\}\}",
            RegexOptions.Compiled);

        /// <summary>
        /// Sustituye {{placeholders}} en el template con los valores del diccionario.
        /// Claves no presentes se dejan tal cual (fail-visible).
        /// </summary>
        public static string Render(string template, IReadOnlyDictionary<string, string>? vars)
        {
            if (string.IsNullOrEmpty(template)) return template ?? "";
            if (vars == null || vars.Count == 0) return template;

            return RegexPlaceholder.Replace(template, m =>
            {
                var clave = m.Groups[1].Value;
                return vars.TryGetValue(clave, out var v) ? (v ?? "") : m.Value;
            });
        }

        /// <summary>
        /// Extrae los placeholders únicos presentes en un template.
        /// Útil para validación en la UI de edición.
        /// </summary>
        public static IReadOnlyList<string> ExtraerPlaceholders(string template)
        {
            var set = new HashSet<string>();
            if (string.IsNullOrEmpty(template)) return new List<string>();

            foreach (Match m in RegexPlaceholder.Matches(template))
                set.Add(m.Groups[1].Value);

            return new List<string>(set);
        }
    }
}
