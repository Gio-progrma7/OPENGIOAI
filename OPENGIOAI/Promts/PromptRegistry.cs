// ============================================================
//  PromptRegistry.cs
//  Registro central de prompts — lectura dinámica y edición.
//
//  RESPONSABILIDADES:
//    · Exponer todos los prompts del sistema (PromptCatalogo.Todos()).
//    · Cargar overrides de {AppDir}/PromtsUsuario/{clave}.md si existen.
//    · Dar la plantilla efectiva (override > default) a quien la pida.
//    · Renderizar con {{variables}}.
//    · Persistir / restaurar ediciones del usuario.
//
//  DISEÑO:
//    · Singleton thread-safe (Lazy<T>).
//    · Cache en memoria del texto efectivo; se invalida tras guardar/restaurar.
//    · Si no hay override → devuelve default. Si hay override vacío → default.
//    · Los defaults NUNCA se modifican: viven solo en código.
// ============================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OPENGIOAI.Entidades;

namespace OPENGIOAI.Promts
{
    public sealed class PromptRegistry
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        private static readonly Lazy<PromptRegistry> _instancia =
            new(() => new PromptRegistry());
        public static PromptRegistry Instancia => _instancia.Value;

        // ── Estado ────────────────────────────────────────────────────────────
        private readonly Dictionary<string, PromptDefinition> _definiciones;
        private readonly ConcurrentDictionary<string, string> _cacheEfectivo = new();
        private readonly string _carpetaOverrides;

        // Regex para sanear nombres de archivo a partir de la clave (seguridad en disco)
        private static readonly Regex RegexNombreSeguro =
            new(@"[^A-Za-z0-9._\-]", RegexOptions.Compiled);

        private PromptRegistry()
        {
            _definiciones = PromptCatalogo.Todos().ToDictionary(p => p.Clave, p => p);

            _carpetaOverrides = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "PromtsUsuario");

            try
            {
                if (!Directory.Exists(_carpetaOverrides))
                    Directory.CreateDirectory(_carpetaOverrides);
            }
            catch { /* silenciar — si no hay permisos, se usa solo default */ }
        }

        // ══════════════════ API pública ══════════════════

        /// <summary>Todas las definiciones registradas (solo lectura).</summary>
        public IReadOnlyCollection<PromptDefinition> TodasLasDefiniciones() =>
            _definiciones.Values;

        /// <summary>Obtiene la definición por clave (null si no existe).</summary>
        public PromptDefinition? Definicion(string clave) =>
            _definiciones.TryGetValue(clave, out var d) ? d : null;

        /// <summary>
        /// Plantilla efectiva: override del usuario si existe y no es vacío,
        /// en caso contrario el default del catálogo.
        /// Sin sustitución de variables.
        /// </summary>
        public string PlantillaEfectiva(string clave)
        {
            if (!_definiciones.TryGetValue(clave, out var def))
                return "";

            if (_cacheEfectivo.TryGetValue(clave, out var cached))
                return cached;

            string efectiva = LeerOverrideDesdeDisco(clave);
            if (string.IsNullOrWhiteSpace(efectiva))
                efectiva = def.TemplatePorDefecto;

            _cacheEfectivo[clave] = efectiva;
            return efectiva;
        }

        /// <summary>
        /// Plantilla efectiva con {{variables}} sustituidas.
        /// Sobrecarga con dictionary literal.
        /// </summary>
        public string Obtener(string clave, IReadOnlyDictionary<string, string>? vars = null)
        {
            var plantilla = PlantillaEfectiva(clave);
            return PromptTemplate.Render(plantilla, vars);
        }

        /// <summary>
        /// ¿Hay un override personalizado en disco para esta clave?
        /// (Distingue "usuario editó" vs "usando default").
        /// Para prompts con archivo externo, "override" = el archivo tiene
        /// contenido distinto al default.
        /// </summary>
        public bool TieneOverride(string clave)
        {
            try
            {
                if (!_definiciones.TryGetValue(clave, out var def)) return false;

                var path = RutaOverride(clave);
                if (!File.Exists(path)) return false;

                var txt = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(txt)) return false;

                // Para archivo externo: considerar "override" si difiere del default.
                if (def.ObtenerRutaArchivoExterno != null)
                {
                    var baseTxt = (def.TemplatePorDefecto ?? "")
                        .Replace("\r\n", "\n").TrimEnd();
                    var actual = txt.Replace("\r\n", "\n").TrimEnd();
                    return !string.Equals(actual, baseTxt, StringComparison.Ordinal);
                }

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Guarda un override editado por el usuario.
        /// · Prompts internos: si el texto es igual al default (o vacío) borra el
        ///   override y vuelve al default.
        /// · Prompts con archivo externo: siempre se persiste lo que escriba el
        ///   usuario (incluido vacío) porque ese archivo es el canónico del sistema.
        /// </summary>
        public async Task GuardarOverrideAsync(string clave, string nuevoTexto)
        {
            if (!_definiciones.TryGetValue(clave, out var def)) return;

            string actualizado = nuevoTexto ?? "";
            bool esArchivoExterno = def.ObtenerRutaArchivoExterno != null;

            // Solo los prompts internos hacen auto-cleanup al igualar el default.
            if (!esArchivoExterno)
            {
                string baseTxt = def.TemplatePorDefecto ?? "";
                bool igualADefault = string.Equals(
                    actualizado.Replace("\r\n", "\n").TrimEnd(),
                    baseTxt.Replace("\r\n", "\n").TrimEnd(),
                    StringComparison.Ordinal);

                if (string.IsNullOrWhiteSpace(actualizado) || igualADefault)
                {
                    Restaurar(clave);
                    return;
                }
            }

            try
            {
                var rutaDestino = RutaOverride(clave);
                var directorio  = Path.GetDirectoryName(rutaDestino);
                if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
                    Directory.CreateDirectory(directorio);

                await File.WriteAllTextAsync(rutaDestino, actualizado, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"No se pudo guardar el prompt '{clave}': {ex.Message}", ex);
            }

            _cacheEfectivo[clave] = actualizado;
        }

        /// <summary>
        /// Elimina el override y restaura el default del catálogo.
        /// Para prompts con archivo externo canónico no se borra el archivo
        /// (otros componentes lo leen directamente); en su lugar se reescribe
        /// con el texto por defecto.
        /// </summary>
        public void Restaurar(string clave)
        {
            try
            {
                _definiciones.TryGetValue(clave, out var def);
                var path = RutaOverride(clave);

                if (def?.ObtenerRutaArchivoExterno != null)
                {
                    // Archivo canónico del sistema: escribir el default en lugar de borrarlo
                    File.WriteAllText(path, def.TemplatePorDefecto ?? "", Encoding.UTF8);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { /* silenciar — la cache se limpia de todos modos */ }

            _cacheEfectivo.TryRemove(clave, out _);
        }

        /// <summary>
        /// Fuerza recarga desde disco: útil si el archivo se editó externamente.
        /// </summary>
        public void InvalidarCache()
        {
            _cacheEfectivo.Clear();
        }

        // ══════════════════ Helpers privados ══════════════════

        private string RutaOverride(string clave)
        {
            // Si la definición apunta a un archivo externo canónico, usarlo tal cual.
            if (_definiciones.TryGetValue(clave, out var def) &&
                def.ObtenerRutaArchivoExterno != null)
            {
                try { return def.ObtenerRutaArchivoExterno(); }
                catch { /* fallback a ruta interna */ }
            }

            // Ruta interna por defecto, saneando el nombre para evitar traversals.
            var seguro = RegexNombreSeguro.Replace(clave, "_");
            if (string.IsNullOrEmpty(seguro)) seguro = "prompt";
            return Path.Combine(_carpetaOverrides, seguro + ".md");
        }

        private string LeerOverrideDesdeDisco(string clave)
        {
            try
            {
                var path = RutaOverride(clave);
                if (!File.Exists(path)) return "";
                return File.ReadAllText(path);
            }
            catch { return ""; }
        }
    }
}
