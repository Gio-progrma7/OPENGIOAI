// ============================================================
//  DetectorPatrones.cs
//
//  Detección LOCAL (sin LLM) de patrones recurrentes en
//  Episodios.md. Es la primera pasada del pipeline de Fase 3:
//  dado el append-only de episodios, agrupa líneas por "firma"
//  y reporta los clusters con ≥ UmbralOcurrencias entradas.
//
//  FIRMA:
//    Se obtiene normalizando la descripción del episodio:
//      - Se quita la fecha/hora del prefijo
//      - Se baja a minúsculas y se quitan acentos
//      - Se descartan números, fechas sueltas y stopwords
//      - Se conservan sólo tokens significativos (len ≥ 4)
//      - Se toman los TOP N tokens más largos y se ordenan
//    Esto hace que "genera reporte de ventas de enero" y
//    "genera reporte de ventas de marzo" caigan en el mismo cluster
//    porque sus tokens significativos (generar, reporte, ventas)
//    coinciden tras la normalización.
//
//  LIMITACIONES CONOCIDAS:
//    - Es heurístico, no semántico. Errores de tipeo grandes
//      pueden romper el cluster. El LLM (fase siguiente) limpia esto
//      al generar el nombre/descripcion del skill propuesto.
//    - No distingue intención. Dos tareas distintas con el mismo
//      vocabulario se agrupan. El LLM también filtra esto.
// ============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OPENGIOAI.Entidades;

namespace OPENGIOAI.Utilerias
{
    public static class DetectorPatrones
    {
        public const int UmbralOcurrencias = 3;
        private const int TopTokens = 5;

        // Stopwords ES básicas — lo mínimo para no contaminar la firma.
        private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "para","con","por","que","como","los","las","una","unos","unas",
            "del","este","esta","estos","estas","esto","eso","ese","esa",
            "esos","esas","ello","ella","ellos","ellas","pero","desde",
            "hasta","sobre","cuando","donde","entre","segun","hacia",
            "favor","porfavor","ahora","hoy","ayer","manana","siempre",
            "tambien","asi","cada","todo","toda","todos","todas",
            "sin","algun","alguna","algunos","algunas","nada","nadie",
            "quiero","puedes","hazme","haz","dame","dime","pon"
        };

        /// <summary>
        /// Lee Episodios.md y devuelve los patrones recurrentes encontrados,
        /// excluyendo los ya ignorados por el usuario.
        /// </summary>
        public static async Task<List<PatronDetectado>> DetectarAsync(
            string rutaTrabajo,
            HashSet<string> firmasIgnoradas)
        {
            var resultado = new List<PatronDetectado>();
            if (string.IsNullOrWhiteSpace(rutaTrabajo)) return resultado;

            string rutaEpisodios = RutasProyecto.ObtenerRutaMemoriaEpisodios(rutaTrabajo);
            if (!File.Exists(rutaEpisodios)) return resultado;

            string contenido = await File.ReadAllTextAsync(rutaEpisodios);
            var descripciones = ExtraerDescripciones(contenido);

            // Agrupar por firma
            var clusters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var desc in descripciones)
            {
                string firma = CalcularFirma(desc);
                if (string.IsNullOrEmpty(firma)) continue;
                if (firmasIgnoradas != null && firmasIgnoradas.Contains(firma)) continue;

                if (!clusters.TryGetValue(firma, out var lista))
                {
                    lista = new List<string>();
                    clusters[firma] = lista;
                }
                lista.Add(desc);
            }

            // Filtrar por umbral y armar patrones
            foreach (var kv in clusters.OrderByDescending(c => c.Value.Count))
            {
                if (kv.Value.Count < UmbralOcurrencias) continue;

                resultado.Add(new PatronDetectado
                {
                    Firma       = kv.Key,
                    Ocurrencias = kv.Value.Count,
                    Ejemplos    = kv.Value.Take(3).ToList(),
                    DetectadoEn = DateTime.Now,
                });
            }

            return resultado;
        }

        // ── Parsing de Episodios.md ──────────────────────────────────

        // Formato típico: "- 2026-04-17 14:32 — descripción del episodio"
        private static readonly Regex RxLinea = new(
            @"^\s*-\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*(?:—|-|:)\s*(.+)$",
            RegexOptions.Compiled);

        private static List<string> ExtraerDescripciones(string contenido)
        {
            var salida = new List<string>();
            if (string.IsNullOrEmpty(contenido)) return salida;

            foreach (var rawLinea in contenido.Replace("\r\n", "\n").Split('\n'))
            {
                var m = RxLinea.Match(rawLinea);
                if (!m.Success) continue;
                string desc = m.Groups[1].Value.Trim();
                if (desc.Length < 6) continue;
                salida.Add(desc);
            }
            return salida;
        }

        // ── Firma ────────────────────────────────────────────────────

        private static string CalcularFirma(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion)) return "";

            string normal = QuitarAcentos(descripcion.ToLowerInvariant());

            // Dejar sólo letras y espacios — eliminar números, signos, fechas
            var sb = new StringBuilder(normal.Length);
            foreach (var c in normal)
                sb.Append(char.IsLetter(c) ? c : ' ');

            var tokens = sb.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 4 && !Stopwords.Contains(t))
                .Distinct()
                .OrderByDescending(t => t.Length)
                .Take(TopTokens)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tokens.Count < 2) return ""; // firma demasiado débil, no agrupar
            return string.Join("_", tokens);
        }

        private static string QuitarAcentos(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (var c in norm)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // ── Persistencia de la lista de ignorados ────────────────────

        public static async Task<HashSet<string>> LeerIgnoradasAsync(string rutaTrabajo)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(rutaTrabajo)) return set;

            try
            {
                string ruta = RutasProyecto.ObtenerRutaPatronesIgnorados(rutaTrabajo);
                if (!File.Exists(ruta)) return set;
                string json = await File.ReadAllTextAsync(ruta);
                var lista = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json)
                            ?? new List<string>();
                foreach (var f in lista) set.Add(f);
            }
            catch { /* best-effort */ }

            return set;
        }

        public static async Task AgregarIgnoradaAsync(string rutaTrabajo, string firma)
        {
            if (string.IsNullOrWhiteSpace(rutaTrabajo)) return;
            if (string.IsNullOrWhiteSpace(firma)) return;

            var actuales = await LeerIgnoradasAsync(rutaTrabajo);
            actuales.Add(firma);

            try
            {
                string ruta = RutasProyecto.ObtenerRutaPatronesIgnorados(rutaTrabajo);
                string json = System.Text.Json.JsonSerializer.Serialize(
                    actuales.ToList(),
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(ruta, json);
            }
            catch { /* best-effort */ }
        }
    }
}
