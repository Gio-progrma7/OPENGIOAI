// ============================================================
//  SkillManifestBuilder.cs — Construye el manifiesto de skills
//
//  Produce un bloque compacto para inyectar en el prompt del
//  Constructor. Objetivo: máximo ~20 líneas sin importar cuántos
//  skills haya — eficiencia de tokens primero.
// ============================================================

using OPENGIOAI.Entidades;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPENGIOAI.Skills
{
    /// <summary>
    /// Construye la sección de skills para el prompt del LLM.
    /// Formato ultra-compacto: una línea por skill, agrupadas por categoría.
    /// </summary>
    public static class SkillManifestBuilder
    {
        /// <summary>
        /// Construye el bloque de manifiesto listo para inyectar en el prompt.
        /// Devuelve string.Empty si la lista está vacía (sin gasto de tokens).
        ///
        /// Ejemplo de salida (3 skills):
        ///
        ///   SKILLS — skill_run("id", param=valor)
        ///   [sistema  ]  obtener_ram          Consulta memoria RAM instalada
        ///   [datos    ]  leer_excel           Lee un Excel y retorna filas   | params: ruta
        ///   [web      ]  http_request         Hace una petición HTTP GET/POST | params: url, metodo
        ///
        /// </summary>
        public static string Construir(IReadOnlyList<Skill> skills)
        {
            if (skills == null || skills.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"SKILLS DISPONIBLES ({skills.Count}) — Llamar con: skill_run(\"id\", param=valor)");
            sb.AppendLine("Firma de params: nombre: tipo* (* = requerido, =X = default, ∈[a|b] = opciones)");

            // Agrupar por categoría para legibilidad
            var grupos = skills
                .GroupBy(s => (s.Categoria ?? "general").ToLowerInvariant())
                .OrderBy(g => g.Key);

            foreach (var grupo in grupos)
            {
                foreach (var skill in grupo.OrderBy(s => s.IdEfectivo))
                {
                    // Descripción truncada a 55 chars para mantener la línea corta
                    string desc = skill.Descripcion ?? "";
                    if (desc.Length > 55) desc = desc[..52] + "...";

                    string paramsHint = ConstruirHintParams(skill);

                    sb.AppendLine(
                        $"  [{grupo.Key,-8}]  {skill.IdEfectivo,-22}  {desc}{paramsHint}");
                }
            }

            // Adjuntar un ejemplo concreto si algún skill lo tiene definido
            var conEjemplo = skills.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Ejemplo));
            if (conEjemplo != null)
                sb.Append($"  Ejemplo: {conEjemplo.Ejemplo}");

            return sb.ToString().TrimEnd();
        }

        // ── Privado ──────────────────────────────────────────────────────────

        private static string ConstruirHintParams(Skill skill)
        {
            if (skill.Parametros == null || skill.Parametros.Count == 0)
                return string.Empty;

            // Firma completa: nombre: tipo* (=default | enum)
            // El * marca requeridos. Es compacto pero permite al LLM saber
            // exactamente qué pasarle al skill sin necesidad de leer el .md.
            var partes = skill.Parametros.Select(p =>
            {
                string sufijo = p.Requerido ? "*" : "";
                string extra  = "";
                if (p.Opciones != null && p.Opciones.Count > 0)
                    extra = $"∈[{string.Join("|", p.Opciones)}]";
                else if (!string.IsNullOrWhiteSpace(p.ValorPorDefecto))
                    extra = $"={p.ValorPorDefecto}";
                return $"{p.Nombre}: {p.Tipo}{sufijo}{extra}";
            });

            return $"  | ({string.Join(", ", partes)})";
        }
    }
}
