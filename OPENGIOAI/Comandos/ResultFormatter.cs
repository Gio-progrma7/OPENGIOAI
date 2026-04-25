// ============================================================
//  ResultFormatter.cs
//
//  Convierte un CommandResult en una string lista para enviarse
//  por cada canal. Cada canal tiene reglas distintas:
//
//    · Telegram: tolera Markdown ligero (bold con *), no Slack blocks.
//    · Slack:    usa mrkdwn con *bold* — misma convención.
//    · UI:       texto plano, los emojis se renderizan bien.
//
//  Por ahora Telegram y Slack comparten la misma salida de texto
//  (ambos aceptan bullets y emojis). Cuando requiramos bloques
//  ricos de Slack, este es el punto de extensión.
// ============================================================

using System.Text;

namespace OPENGIOAI.Comandos
{
    public enum CanalSalida
    {
        Telegram,
        Slack,
        UI,
    }

    public static class ResultFormatter
    {
        /// <summary>
        /// Devuelve null si el resultado es silencioso o no tiene contenido,
        /// en cuyo caso no se debe enviar nada al canal.
        /// </summary>
        public static string? Formatear(CommandResult r, CanalSalida canal)
        {
            if (r.Silencioso) return null;

            var sb = new StringBuilder();

            string icono = r.Tipo.Icono();

            if (!string.IsNullOrWhiteSpace(r.Titulo))
            {
                sb.Append(icono).Append(' ').AppendLine(r.Titulo.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(r.Mensaje) && r.Tipo != ResultTipo.Info)
            {
                // Si no hay título explícito pero el tipo no es Info, anteponer
                // el icono al mensaje para que el tono del resultado se vea.
                sb.Append(icono).Append(' ');
            }

            if (!string.IsNullOrWhiteSpace(r.Mensaje))
            {
                sb.AppendLine(r.Mensaje.Trim());
            }

            if (r.Detalles != null && r.Detalles.Count > 0)
            {
                foreach (var d in r.Detalles)
                {
                    if (string.IsNullOrWhiteSpace(d)) continue;
                    sb.Append("• ").AppendLine(d.Trim());
                }
            }

            string salida = sb.ToString().TrimEnd();
            return salida.Length == 0 ? null : salida;
        }
    }
}
