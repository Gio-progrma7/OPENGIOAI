// ============================================================
//  SkillLoader.cs — Carga skills desde archivos .md y/o ListSkills.json
//
//  Prioridad:
//    1. Escanea skills/*.md  (sistema nuevo Claude-style)
//    2. Si no hay .md, cae a ListSkills.json (retrocompatible)
//
//  Nunca escribe en disco. Solo enriquece en memoria.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OPENGIOAI.Skills
{
    /// <summary>
    /// Carga el listado de skills desde el disco para un directorio de trabajo dado.
    /// Escanea skills/*.md primero; si no hay, usa ListSkills.json como fallback.
    /// </summary>
    public static class SkillLoader
    {
        private const string CarpetaSkills = "skills";

        /// <summary>
        /// Devuelve solo los skills activos.
        /// Es el método que usa el pipeline ARIA para construir el manifiesto.
        /// </summary>
        public static List<Skill> CargarActivas(string rutaBase)
        {
            return CargarTodas(rutaBase)
                .Where(s => s.Activa)
                .ToList();
        }

        /// <summary>
        /// Devuelve todos los skills (activos e inactivos).
        /// Usado por UIs de administración como FrmSkills.
        /// </summary>
        public static List<Skill> CargarTodas(string rutaBase)
        {
            // ── Intentar primero con archivos .md ────────────────────────────
            var desdeMarkdown = CargarDesdeMd(rutaBase);
            if (desdeMarkdown.Count > 0)
                return desdeMarkdown;

            // ── Fallback: ListSkills.json (retrocompatible) ──────────────────
            return CargarDesdeJson(rutaBase);
        }

        /// <summary>
        /// Carga todos los skills escaneando skills/*.md en el directorio de trabajo.
        /// </summary>
        public static List<Skill> CargarDesdeMd(string rutaBase)
        {
            var result = new List<Skill>();

            string carpeta = Path.Combine(rutaBase, CarpetaSkills);
            if (!Directory.Exists(carpeta))
                return result;

            foreach (string rutaMd in Directory.GetFiles(carpeta, "*.md",
                SearchOption.TopDirectoryOnly).OrderBy(f => f))
            {
                var skill = SkillMdParser.Parsear(rutaMd);
                if (skill != null)
                    result.Add(skill);
            }

            return result;
        }

        /// <summary>
        /// Guarda un skill como archivo .md en el directorio skills/.
        /// Si ya existe, lo sobreescribe.
        /// </summary>
        public static void GuardarMd(string rutaBase, Skill skill, string contenidoMd)
        {
            string carpeta = Path.Combine(rutaBase, CarpetaSkills);
            Directory.CreateDirectory(carpeta);

            string nombreArchivo = $"{skill.IdEfectivo}.md";
            string rutaCompleta  = Path.Combine(carpeta, nombreArchivo);

            File.WriteAllText(rutaCompleta, contenidoMd, System.Text.Encoding.UTF8);

            // Actualizar la ruta en el skill
            skill.RutaMd     = rutaCompleta;
            skill.RutaScript = Path.ChangeExtension(nombreArchivo, ".py");
        }

        /// <summary>
        /// Elimina el archivo .md de un skill del disco.
        /// </summary>
        public static bool EliminarMd(string rutaMd)
        {
            if (!File.Exists(rutaMd)) return false;
            File.Delete(rutaMd);
            return true;
        }

        // ── Privado ──────────────────────────────────────────────────────────

        private static List<Skill> CargarDesdeJson(string rutaBase)
        {
            string ruta = RutasProyecto.ObtenerRutaListSkills(rutaBase);
            return JsonManager.Leer<Skill>(ruta);
        }
    }
}
