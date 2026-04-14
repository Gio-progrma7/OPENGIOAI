// ============================================================
//  SkillMdParser.cs — Parsea archivos .md de skills
//
//  Formato Claude-style:
//  ─────────────────────────────────────────
//  ---
//  id: obtener_ram
//  nombre: Obtener RAM del Sistema
//  categoria: sistema
//  descripcion: Consulta la memoria RAM
//  activa: true
//  ejemplo: skill_run("obtener_ram")
//  ---
//
//  ## Descripción
//  Texto libre de documentación...
//
//  ## Código
//  ```python
//  # Código Python ejecutable
//  import psutil
//  ...
//  ```
//
//  ## Parámetros (opcional)
//  - nombre: ruta | tipo: string | requerido: true
//  ─────────────────────────────────────────
// ============================================================

using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OPENGIOAI.Skills
{
    /// <summary>
    /// Parsea un archivo .md de skill y devuelve la entidad Skill con todos sus datos.
    /// </summary>
    public static class SkillMdParser
    {
        // ── Punto de entrada público ──────────────────────────────────────────

        /// <summary>
        /// Lee un archivo .md y construye la entidad <see cref="Skill"/> correspondiente.
        /// Devuelve null si el archivo no existe o no tiene frontmatter válido.
        /// </summary>
        public static Skill? Parsear(string rutaMd)
        {
            if (!File.Exists(rutaMd)) return null;

            try
            {
                string contenido = File.ReadAllText(rutaMd, Encoding.UTF8);
                var (metadata, cuerpo) = SepararFrontmatter(contenido);

                if (metadata == null) return null;

                var skill = new Skill
                {
                    RutaMd       = rutaMd,
                    ContenidoMd  = cuerpo,
                    Id           = ObtenerValor(metadata, "id"),
                    Nombre       = ObtenerValor(metadata, "nombre"),
                    Descripcion  = ObtenerValor(metadata, "descripcion"),
                    Categoria    = ObtenerValor(metadata, "categoria", "general"),
                    Ejemplo      = ObtenerValor(metadata, "ejemplo"),
                    Activa       = ObtenerBool(metadata, "activa", true),
                    // RutaScript apunta al .py generado (mismo nombre que el .md)
                    RutaScript   = Path.ChangeExtension(Path.GetFileName(rutaMd), ".py"),
                    Parametros   = ParsearParametros(cuerpo),
                };

                // Si no tiene Id, derivarlo del nombre del archivo
                if (string.IsNullOrWhiteSpace(skill.Id))
                    skill.Id = Path.GetFileNameWithoutExtension(rutaMd)
                        .ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

                return skill;
            }
            catch { return null; }
        }

        /// <summary>
        /// Extrae el bloque de código Python del cuerpo de un .md de skill.
        /// Busca el primer bloque ``` python después de la sección ## Código.
        /// </summary>
        public static string? ExtraerCodigo(string contenidoMd)
        {
            if (string.IsNullOrWhiteSpace(contenidoMd)) return null;

            // Buscar sección ## Código (case-insensitive, acepta variaciones)
            var matchSeccion = Regex.Match(contenidoMd,
                @"##\s*(?:Código|Codigo|Code|Script)\s*\n",
                RegexOptions.IgnoreCase);

            string buscarEn = matchSeccion.Success
                ? contenidoMd[matchSeccion.Index..]
                : contenidoMd;

            // Extraer primer bloque ```python ... ```
            var matchBloque = Regex.Match(buscarEn,
                @"```(?:python|py)\s*\n([\s\S]*?)```",
                RegexOptions.IgnoreCase);

            return matchBloque.Success
                ? matchBloque.Groups[1].Value.TrimEnd()
                : null;
        }

        // ── Parseo de frontmatter ─────────────────────────────────────────────

        private static (Dictionary<string, string>? metadata, string cuerpo)
            SepararFrontmatter(string texto)
        {
            var lineas = texto.Split('\n');

            // El frontmatter debe comenzar con '---' en la primera línea no vacía
            int inicio = -1;
            for (int i = 0; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                if (lineas[i].Trim() == "---") { inicio = i; break; }
                else return (null, texto); // No hay frontmatter
            }

            if (inicio < 0) return (null, texto);

            // Buscar el cierre '---'
            int fin = -1;
            for (int i = inicio + 1; i < lineas.Length; i++)
            {
                if (lineas[i].Trim() == "---") { fin = i; break; }
            }

            if (fin < 0) return (null, texto);

            // Parsear clave:valor del frontmatter
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = inicio + 1; i < fin; i++)
            {
                var linea = lineas[i];
                int sep = linea.IndexOf(':');
                if (sep < 0) continue;

                string clave = linea[..sep].Trim().ToLowerInvariant();
                string valor = linea[(sep + 1)..].Trim()
                    .Trim('"').Trim('\''); // Quitar comillas opcionales

                if (!string.IsNullOrWhiteSpace(clave))
                    meta[clave] = valor;
            }

            // El cuerpo es todo lo que viene después del segundo '---'
            string cuerpo = string.Join("\n",
                lineas.Skip(fin + 1)).TrimStart('\n', '\r');

            return (meta, cuerpo);
        }

        // ── Parseo de parámetros del cuerpo ──────────────────────────────────

        private static List<SkillParametro> ParsearParametros(string cuerpo)
        {
            var result = new List<SkillParametro>();
            if (string.IsNullOrWhiteSpace(cuerpo)) return result;

            // Buscar sección ## Parámetros
            var matchSeccion = Regex.Match(cuerpo,
                @"##\s*(?:Parámetros|Parametros|Parameters)\s*\n",
                RegexOptions.IgnoreCase);

            if (!matchSeccion.Success) return result;

            // Parsear líneas en formato: - nombre: x | tipo: string | requerido: true
            string seccion = cuerpo[matchSeccion.Index..];
            var lineas = seccion.Split('\n').Skip(1); // Saltar el encabezado

            foreach (var linea in lineas)
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                if (linea.TrimStart().StartsWith('#')) break; // Nueva sección = fin

                if (!linea.TrimStart().StartsWith('-')) continue;

                var partes = linea.TrimStart('-').Split('|')
                    .Select(p => p.Trim())
                    .Where(p => p.Contains(':'))
                    .ToDictionary(
                        p => p[..p.IndexOf(':')].Trim().ToLowerInvariant(),
                        p => p[(p.IndexOf(':') + 1)..].Trim(),
                        StringComparer.OrdinalIgnoreCase);

                if (!partes.ContainsKey("nombre")) continue;

                result.Add(new SkillParametro
                {
                    Nombre      = partes.GetValueOrDefault("nombre", ""),
                    Tipo        = partes.GetValueOrDefault("tipo", "string"),
                    Descripcion = partes.GetValueOrDefault("descripcion", ""),
                    Requerido   = partes.GetValueOrDefault("requerido", "true")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase),
                });
            }

            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string ObtenerValor(
            Dictionary<string, string> meta, string clave, string defecto = "")
            => meta.TryGetValue(clave, out var v) && !string.IsNullOrWhiteSpace(v) ? v : defecto;

        private static bool ObtenerBool(
            Dictionary<string, string> meta, string clave, bool defecto = true)
        {
            if (!meta.TryGetValue(clave, out var v)) return defecto;
            return v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
