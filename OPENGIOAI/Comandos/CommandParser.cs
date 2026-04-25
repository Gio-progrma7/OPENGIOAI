// ============================================================
//  CommandParser.cs
//
//  Parsing profesional de comandos tipo `#cmd arg1 "arg con espacios"`.
//
//  FORMATOS ACEPTADOS:
//    · `#cmd`                     → nombre="cmd", args=[]
//    · `#cmd a b c`               → nombre="cmd", args=["a","b","c"]
//    · `#cmd "frase completa"`    → nombre="cmd", args=["frase completa"]
//    · `#CMD_ARG`     (legacy)    → nombre="cmd", args=["ARG"]
//    · `#CMD_ARG otra cosa`       → nombre="cmd", args=["ARG","otra","cosa"]
//
//  REGLAS:
//    · El nombre (sin `#`) se normaliza a minúsculas.
//    · Los args preservan el case original — crítico para nombres
//      de rutas, voces TTS, claves de API.
//    · La legacy `_` como separador sólo aplica al PRIMER separador;
//      dentro del resto el split es por espacios.
//    · Las comillas dobles preservan espacios dentro de un arg.
//
//  POR QUÉ EL LEGACY `_`:
//    Los botones inline de Telegram pasan `callback_data` como
//    `#AGENTE_ChatGpt`. Si rompemos eso, se rompen todos los menús
//    inline existentes. Por eso lo mantenemos como fallback.
// ============================================================

using System.Collections.Generic;

namespace OPENGIOAI.Comandos
{
    public sealed record ComandoParseado(
        string NombreOriginal,             // "AGENTE" tal como vino (preserva case)
        string Nombre,                     // "agente" (lowercase canónico)
        string ArgsRaw,                    // "ChatGpt extras"
        IReadOnlyList<string> Args);       // ["ChatGpt","extras"]

    public static class CommandParser
    {
        /// <summary>
        /// Devuelve null si el texto no empieza con `#` o está vacío después
        /// de quitar el hash. En cualquier otro caso devuelve un parse válido,
        /// aunque el nombre del comando no exista en el registry.
        /// </summary>
        public static ComandoParseado? Parsear(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;

            string s = texto.Trim();
            if (!s.StartsWith('#')) return null;

            string sinHash = s.Substring(1);
            if (sinHash.Length == 0) return null;

            // Encuentra el primer separador — espacio, tab o underscore legacy.
            int sepIdx = IndiceDelPrimerSeparador(sinHash);

            string nombreOriginal, resto;
            if (sepIdx < 0)
            {
                nombreOriginal = sinHash;
                resto = "";
            }
            else
            {
                nombreOriginal = sinHash.Substring(0, sepIdx);
                resto = sinHash.Substring(sepIdx + 1).Trim();
            }

            var args = TokenizarArgs(resto);

            return new ComandoParseado(
                NombreOriginal: nombreOriginal,
                Nombre:         nombreOriginal.ToLowerInvariant(),
                ArgsRaw:        resto,
                Args:           args);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static int IndiceDelPrimerSeparador(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '_') return i;
            }
            return -1;
        }

        /// <summary>
        /// Split shell-lite: respeta comillas dobles para preservar espacios
        /// dentro de un argumento. Los pares `"..."` se desvanecen; el
        /// contenido queda sin comillas.
        /// </summary>
        internal static List<string> TokenizarArgs(string resto)
        {
            var lista = new List<string>();
            if (string.IsNullOrEmpty(resto)) return lista;

            var buffer = new System.Text.StringBuilder();
            bool dentroComillas = false;

            void Commit()
            {
                if (buffer.Length == 0) return;
                lista.Add(buffer.ToString());
                buffer.Clear();
            }

            foreach (char c in resto)
            {
                if (c == '"')
                {
                    dentroComillas = !dentroComillas;
                    continue;
                }

                if ((c == ' ' || c == '\t') && !dentroComillas)
                {
                    Commit();
                    continue;
                }

                buffer.Append(c);
            }
            Commit();

            return lista;
        }
    }
}
