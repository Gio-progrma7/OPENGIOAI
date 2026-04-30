// ============================================================
//  SkillResultRenderer.cs — Render JSON-aware del output de skills
//
//  Los skills siguen el contrato:
//      print(json.dumps({"status": "ok"|"error", ...}))
//
//  Este helper toma el stdout completo del proceso, intenta
//  extraer el último JSON válido, y lo renderiza en el RichTextBox
//  con:
//    · Badge de status (✅/❌) coloreado
//    · Campos canónicos destacados (timestamp, duracion, resumen,
//      detalle, nivel, sugerencia)
//    · Resto de campos formateados como JSON indentado
//
//  Si no encuentra JSON válido, no hace nada (deja el output crudo
//  que ya se imprimió por streaming).
// ============================================================

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    /// <summary>
    /// Render estructurado del output de un skill que cumple el contrato
    /// <c>print(json.dumps({...}))</c>.
    /// </summary>
    public static class SkillResultRenderer
    {
        // ── Paleta (alineada con Skills.cs) ──────────────────────────────────
        private static readonly Color ColorVerde            = Color.FromArgb(52,  211, 153);
        private static readonly Color ColorRojo             = Color.FromArgb(248, 113, 113);
        private static readonly Color ColorAmbar            = Color.FromArgb(251, 191,  36);
        private static readonly Color ColorTextoPrincipal   = Color.FromArgb(241, 245, 249);
        private static readonly Color ColorTextoSecundario  = Color.FromArgb(148, 163, 184);
        private static readonly Color ColorAcento           = Color.FromArgb(96,  165, 250);
        private static readonly Color ColorMagenta          = Color.FromArgb(216, 180, 254);
        private static readonly Color ColorTenue            = Color.FromArgb( 71,  85, 105);

        /// <summary>
        /// Intenta detectar y renderizar el JSON del skill al final del
        /// <paramref name="rtb"/>. Devuelve true si lo logró.
        /// </summary>
        public static bool RenderizarAlFinal(RichTextBox rtb, string stdoutCompleto)
        {
            if (rtb == null || string.IsNullOrWhiteSpace(stdoutCompleto)) return false;

            var jo = ExtraerUltimoJson(stdoutCompleto);
            if (jo == null) return false;

            // Prefijo separador
            Append(rtb, "\n", ColorTextoSecundario);
            Append(rtb, new string('─', 60) + "\n", ColorTenue);

            // ── Badge de status ──────────────────────────────────────────────
            string status = jo.Value<string>("status") ?? "";
            string nivel  = jo.Value<string>("nivel")  ?? "";

            (string icono, Color color) = status.ToLowerInvariant() switch
            {
                "ok"      or "success" => ("✅", ColorVerde),
                "error"   or "fail"    => ("❌", ColorRojo),
                "warning" or "warn"    => ("⚠️ ", ColorAmbar),
                ""                     => ("ℹ️ ", ColorAcento),
                _                      => ("•",  ColorTextoSecundario),
            };

            Append(rtb, $"{icono}  RESULTADO", color, bold: true);
            if (!string.IsNullOrWhiteSpace(status))
                Append(rtb, $"  · status: ", ColorTextoSecundario, bold: false,
                    suffix: $"{status}\n", suffixColor: color, suffixBold: true);
            else
                Append(rtb, "\n", ColorTextoSecundario);

            // Campos canónicos en orden fijo, si están presentes
            RenderizarCampoCanonico(rtb, jo, "duracion",   "duración",   ColorAcento);
            RenderizarCampoCanonico(rtb, jo, "duracion_segundos", "duración (s)", ColorAcento);
            RenderizarCampoCanonico(rtb, jo, "timestamp",  "timestamp",  ColorTenue);
            if (!string.IsNullOrWhiteSpace(nivel))
            {
                Color nivelColor = nivel.ToUpperInvariant() switch
                {
                    "ALTO" or "CRITICO" or "CRÍTICO" => ColorRojo,
                    "MEDIO"                          => ColorAmbar,
                    "BAJO"                           => ColorTextoSecundario,
                    _                                => ColorMagenta,
                };
                Append(rtb, "  nivel:        ", ColorTextoSecundario);
                Append(rtb, $"{nivel}\n", nivelColor, bold: true);
            }
            RenderizarCampoCanonico(rtb, jo, "resumen",    "resumen",    ColorTextoPrincipal, destacar: true);
            RenderizarCampoCanonico(rtb, jo, "accion",     "acción",     ColorTextoPrincipal);
            RenderizarCampoCanonico(rtb, jo, "detalle",    "detalle",
                status.Equals("error", StringComparison.OrdinalIgnoreCase) ? ColorRojo : ColorTextoPrincipal);
            RenderizarCampoCanonico(rtb, jo, "sugerencia", "sugerencia", ColorAmbar);

            // ── Campos extra (todo lo que no es canónico) ────────────────────
            var canonicos = new[]
            {
                "status", "nivel", "duracion", "duracion_segundos", "timestamp",
                "resumen", "accion", "detalle", "sugerencia"
            };
            var extras = jo.Properties()
                .Where(p => !canonicos.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (extras.Count > 0)
            {
                Append(rtb, "\n  extras:\n", ColorTextoSecundario, bold: true);
                foreach (var prop in extras)
                {
                    Append(rtb, $"    {prop.Name}: ", ColorMagenta);
                    RenderizarValor(rtb, prop.Value, indent: 4);
                }
            }

            Append(rtb, new string('─', 60) + "\n", ColorTenue);

            rtb.SelectionStart = rtb.TextLength;
            rtb.ScrollToCaret();
            return true;
        }

        // ── Privado: extracción del JSON ─────────────────────────────────────

        /// <summary>
        /// Extrae el último objeto JSON balanceado del texto.
        /// Tolera líneas de log antes de la salida final.
        /// </summary>
        private static JObject? ExtraerUltimoJson(string texto)
        {
            // Caso óptimo: parse directo (skill bien comportado)
            string trimmed = texto.Trim();
            if (TryParseObj(trimmed, out var directo)) return directo;

            // Buscar el último '{' balanceado
            int ultimoCierre = trimmed.LastIndexOf('}');
            if (ultimoCierre < 0) return null;

            int profundidad = 0;
            for (int i = ultimoCierre; i >= 0; i--)
            {
                char c = trimmed[i];
                if (c == '}') profundidad++;
                else if (c == '{')
                {
                    profundidad--;
                    if (profundidad == 0)
                    {
                        string candidato = trimmed[i..(ultimoCierre + 1)];
                        if (TryParseObj(candidato, out var jo)) return jo;
                    }
                }
            }
            return null;
        }

        private static bool TryParseObj(string s, out JObject jo)
        {
            try
            {
                jo = JObject.Parse(s);
                return true;
            }
            catch { jo = null!; return false; }
        }

        // ── Privado: render de campos ────────────────────────────────────────

        private static void RenderizarCampoCanonico(
            RichTextBox rtb, JObject jo, string nombre, string label,
            Color colorValor, bool destacar = false)
        {
            if (!jo.TryGetValue(nombre, StringComparison.OrdinalIgnoreCase, out var token))
                return;
            if (token == null || token.Type == JTokenType.Null) return;

            string valor = token.Type == JTokenType.String
                ? token.Value<string>() ?? ""
                : token.ToString(Formatting.None);

            if (string.IsNullOrWhiteSpace(valor)) return;

            string padLabel = (label + ":").PadRight(13);
            Append(rtb, $"  {padLabel} ", ColorTextoSecundario);
            Append(rtb, $"{valor}\n", colorValor, bold: destacar);
        }

        private static void RenderizarValor(RichTextBox rtb, JToken token, int indent)
        {
            string sangria = new(' ', indent);
            switch (token.Type)
            {
                case JTokenType.Object:
                    Append(rtb, "\n", ColorTextoSecundario);
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        Append(rtb, $"{sangria}  {prop.Name}: ", ColorMagenta);
                        RenderizarValor(rtb, prop.Value, indent + 2);
                    }
                    break;
                case JTokenType.Array:
                    var arr = (JArray)token;
                    if (arr.Count == 0)
                    {
                        Append(rtb, "[]\n", ColorTextoSecundario);
                        return;
                    }
                    Append(rtb, $"[{arr.Count}]\n", ColorTextoSecundario);
                    int max = Math.Min(arr.Count, 8);
                    for (int i = 0; i < max; i++)
                    {
                        Append(rtb, $"{sangria}  - ", ColorTenue);
                        RenderizarValor(rtb, arr[i], indent + 2);
                    }
                    if (arr.Count > max)
                        Append(rtb, $"{sangria}  ... (+{arr.Count - max} más)\n", ColorTenue);
                    break;
                case JTokenType.String:
                    string s = token.Value<string>() ?? "";
                    Append(rtb, s.Length > 200 ? s[..200] + "…" : s,
                        ColorTextoPrincipal);
                    Append(rtb, "\n", ColorTextoPrincipal);
                    break;
                case JTokenType.Boolean:
                    Append(rtb, $"{token}\n",
                        token.Value<bool>() ? ColorVerde : ColorRojo);
                    break;
                case JTokenType.Integer:
                case JTokenType.Float:
                    Append(rtb, $"{token}\n", ColorAcento);
                    break;
                case JTokenType.Null:
                    Append(rtb, "null\n", ColorTenue);
                    break;
                default:
                    Append(rtb, $"{token}\n", ColorTextoPrincipal);
                    break;
            }
        }

        // ── Privado: append coloreado ────────────────────────────────────────

        private static void Append(
            RichTextBox rtb, string texto, Color color,
            bool bold = false,
            string? suffix = null, Color? suffixColor = null, bool suffixBold = false)
        {
            if (rtb.InvokeRequired)
            {
                rtb.Invoke(() => Append(rtb, texto, color, bold, suffix, suffixColor, suffixBold));
                return;
            }
            rtb.SelectionStart  = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor  = color;
            var fontBase = rtb.Font;
            rtb.SelectionFont = bold
                ? new Font(fontBase, FontStyle.Bold)
                : new Font(fontBase, FontStyle.Regular);
            rtb.AppendText(texto);

            if (suffix != null)
            {
                rtb.SelectionStart  = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor  = suffixColor ?? color;
                rtb.SelectionFont   = suffixBold
                    ? new Font(fontBase, FontStyle.Bold)
                    : new Font(fontBase, FontStyle.Regular);
                rtb.AppendText(suffix);
            }
        }
    }
}
