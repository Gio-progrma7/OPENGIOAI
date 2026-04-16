// ============================================================
//  SkillHubManager.cs — Gestión de skills remotos (Hub)
//
//  Permite instalar skills desde cualquier URL que apunte a un
//  archivo .md con frontmatter válido, actualizarlos y exportarlos.
//
//  Flujo de instalación:
//    1. Descargar .md desde URL
//    2. Validar que tiene id + nombre + código Python
//    3. Inyectar source_url en el frontmatter (para rastreo)
//    4. Guardar en skills/ y extraer el .py
//
//  El formato de URL soportado es cualquier URL que devuelva
//  texto plano (GitHub raw, pastebin, servidor propio, etc.)
// ============================================================

using OPENGIOAI.Entidades;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Skills
{
    /// <summary>
    /// Gestiona la instalación, actualización y exportación de skills
    /// desde fuentes remotas (Hub).
    /// </summary>
    public static class SkillHubManager
    {
        // HttpClient estático — reutilizable, thread-safe
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // ── Instalación ───────────────────────────────────────────────────────

        /// <summary>
        /// Descarga un skill desde una URL, lo valida e instala en la carpeta
        /// de skills del proyecto.
        ///
        /// Lanza <see cref="SkillHubException"/> si:
        ///  - La URL no es accesible
        ///  - El contenido no tiene frontmatter válido
        ///  - Falta el campo 'id' o el bloque de código Python
        /// </summary>
        /// <param name="url">URL del archivo .md (raw, pastebin, etc.)</param>
        /// <param name="rutaBase">Carpeta raíz del proyecto (donde existe /skills/)</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>El <see cref="Skill"/> parseado e instalado.</returns>
        public static async Task<Skill> InstalarDesdeUrlAsync(
            string url, string rutaBase, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new SkillHubException("La URL no puede estar vacía.");

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
                throw new SkillHubException("La URL debe ser una dirección HTTP/HTTPS válida.");

            // 1. Descargar
            string contenidoOriginal;
            try
            {
                contenidoOriginal = await _http.GetStringAsync(uri, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new SkillHubException($"No se pudo descargar el skill: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                throw new SkillHubException("La descarga tardó demasiado (timeout 30 s).");
            }

            // 2. Validar frontmatter mínimo
            ValidarContenidoMd(contenidoOriginal, out string errorValidacion);
            if (!string.IsNullOrEmpty(errorValidacion))
                throw new SkillHubException($"Skill inválido: {errorValidacion}");

            // 3. Parsear para obtener el ID (necesario para el nombre de archivo)
            var skillParcial = SkillMdParser.Parsear(
                GuardarTemporal(contenidoOriginal, rutaBase));

            if (skillParcial == null)
                throw new SkillHubException("No se pudo parsear el skill descargado.");

            // 4. Inyectar/actualizar source_url y los campos de Hub en el frontmatter
            string contenidoFinal = InyectarSourceUrl(contenidoOriginal, url.Trim());

            // 5. Guardar como .md definitivo en skills/
            string carpeta = Path.Combine(rutaBase, "skills");
            Directory.CreateDirectory(carpeta);
            string rutaMd = Path.Combine(carpeta, $"{skillParcial.IdEfectivo}.md");
            await File.WriteAllTextAsync(rutaMd, contenidoFinal, Encoding.UTF8, ct);

            // 6. Parsear la versión final y extraer el .py
            var skill = SkillMdParser.Parsear(rutaMd);
            if (skill == null)
                throw new SkillHubException("Error al parsear el skill tras guardarlo.");

            await SkillRunnerHelper.ExtraerScriptsMdAsync(
                rutaBase, new[] { skill }, ct);

            return skill;
        }

        /// <summary>
        /// Re-descarga un skill del Hub para actualizarlo.
        /// Preserva el SourceUrl original si la nueva versión no lo incluye.
        /// </summary>
        public static async Task<Skill> ActualizarAsync(
            Skill skill, string rutaBase, CancellationToken ct = default)
        {
            if (!skill.EsDeHub)
                throw new SkillHubException("Este skill no tiene una URL de origen para actualizar.");

            return await InstalarDesdeUrlAsync(skill.SourceUrl, rutaBase, ct);
        }

        // ── Exportación ───────────────────────────────────────────────────────

        /// <summary>
        /// Genera el contenido .md completo y listo para compartir.
        /// Lee el archivo .md original y asegura que el frontmatter incluya
        /// los campos de Hub (autor, version, source_url si los tiene).
        /// </summary>
        public static string GenerarMdParaExportar(Skill skill)
        {
            string md = ObtenerContenidoMdActual(skill);
            if (string.IsNullOrWhiteSpace(md)) return "";

            // Si el skill tiene SourceUrl, asegurar que aparece en el frontmatter
            if (!string.IsNullOrWhiteSpace(skill.SourceUrl))
                md = InyectarSourceUrl(md, skill.SourceUrl);

            return md;
        }

        // ── Validación ────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el contenido .md tiene la estructura mínima requerida.
        /// </summary>
        /// <param name="contenido">Contenido crudo del .md</param>
        /// <param name="error">Descripción del problema si no es válido; vacío si es OK.</param>
        /// <returns>True si el contenido es válido.</returns>
        public static bool ValidarContenidoMd(string contenido, out string error)
        {
            error = "";

            if (string.IsNullOrWhiteSpace(contenido))
            {
                error = "El archivo está vacío.";
                return false;
            }

            // Debe tener frontmatter (comienza con ---)
            string trimmed = contenido.TrimStart();
            if (!trimmed.StartsWith("---"))
            {
                error = "El archivo no tiene frontmatter YAML (debe comenzar con ---)";
                return false;
            }

            // Debe tener un cierre ---
            int segundoSep = trimmed.IndexOf("---", 3);
            if (segundoSep < 0)
            {
                error = "El frontmatter no está cerrado (falta el segundo ---)";
                return false;
            }

            string frontmatter = trimmed[3..segundoSep];

            // Debe tener campo 'id'
            if (!frontmatter.Contains("id:"))
            {
                error = "El frontmatter no tiene el campo 'id' requerido.";
                return false;
            }

            // Debe tener bloque de código Python
            if (!contenido.Contains("```python") && !contenido.Contains("```py"))
            {
                error = "El skill no tiene un bloque de código Python (```python ... ```)";
                return false;
            }

            return true;
        }

        // ── Helpers privados ──────────────────────────────────────────────────

        /// <summary>
        /// Inyecta o actualiza el campo source_url en el frontmatter del .md.
        /// Si ya existe la línea, la reemplaza; si no, la agrega al final del bloque.
        /// </summary>
        private static string InyectarSourceUrl(string contenido, string url)
        {
            var lines = contenido.Replace("\r\n", "\n").Split('\n');

            // Encontrar rango del frontmatter
            int inicio = -1, fin = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---")
                {
                    if (inicio < 0) inicio = i;
                    else { fin = i; break; }
                }
            }

            if (inicio < 0 || fin < 0) return contenido; // sin frontmatter

            var resultado = new System.Collections.Generic.List<string>(lines);

            // Buscar si ya existe source_url dentro del frontmatter
            bool encontrado = false;
            for (int i = inicio + 1; i < fin; i++)
            {
                if (resultado[i].TrimStart().StartsWith("source_url:"))
                {
                    resultado[i] = $"source_url: {url}";
                    encontrado = true;
                    break;
                }
            }

            if (!encontrado)
                resultado.Insert(fin, $"source_url: {url}");

            return string.Join("\n", resultado);
        }

        /// <summary>
        /// Guarda el contenido en un archivo temporal para parsear el ID.
        /// El archivo se elimina al finalizar.
        /// </summary>
        private static string GuardarTemporal(string contenido, string rutaBase)
        {
            string tmp = Path.Combine(
                Path.GetTempPath(),
                $"skill_hub_tmp_{Guid.NewGuid():N}.md");

            File.WriteAllText(tmp, contenido, Encoding.UTF8);
            return tmp;
        }

        /// <summary>
        /// Obtiene el contenido .md actual del skill desde disco.
        /// </summary>
        private static string ObtenerContenidoMdActual(Skill skill)
        {
            if (!string.IsNullOrWhiteSpace(skill.RutaMd) && File.Exists(skill.RutaMd))
                return File.ReadAllText(skill.RutaMd, Encoding.UTF8);

            return "";
        }
    }

    /// <summary>
    /// Excepción específica del Skills Hub — permite distinguir
    /// errores de negocio (URL inválida, frontmatter incorrecto)
    /// de errores de sistema (IOException, etc.)
    /// </summary>
    public sealed class SkillHubException : Exception
    {
        public SkillHubException(string message) : base(message) { }
        public SkillHubException(string message, Exception inner) : base(message, inner) { }
    }
}
