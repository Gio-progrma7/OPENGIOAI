using Newtonsoft.Json.Linq;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;

namespace OPENGIOAI.Herramientas
{
    public sealed record ResultadoPoliticaHerramienta(bool Permitido, string Mensaje = "");

    /// <summary>
    /// Capa central de seguridad para Tool Use. Las herramientas siguen siendo
    /// simples, pero el agente no puede ejecutarlas sin pasar por esta politica.
    /// </summary>
    public static class PoliticaHerramientas
    {
        private static readonly string[] ComandosBloqueados =
        [
            "rm -rf",
            "remove-item",
            "del /s",
            "rd /s",
            "format ",
            "diskpart",
            "shutdown",
            "restart-computer",
            "git reset --hard",
            "git checkout --",
            "reg delete",
            "bcdedit",
            "cipher /w"
        ];

        public static ResultadoPoliticaHerramienta ValidarYNormalizar(
            AgentContext ctx,
            LlamadaHerramienta llamada,
            JObject parametros)
        {
            string workspace = NormalizarBase(ctx.RutaArchivo);
            if (string.IsNullOrWhiteSpace(workspace))
                return new ResultadoPoliticaHerramienta(true);

            string nombre = llamada.Nombre.ToLowerInvariant();

            if (nombre is "leer_archivo")
                return NormalizarRuta(parametros, "ruta", workspace, escritura: false);

            if (nombre is "listar_directorio")
                return NormalizarRuta(parametros, "ruta", workspace, escritura: false);

            if (nombre is "buscar_en_archivos")
                return NormalizarRuta(parametros, "directorio", workspace, escritura: false);

            if (nombre is "escribir_archivo")
                return NormalizarRuta(parametros, "ruta", workspace, escritura: true);

            if (nombre is "ejecutar_comando")
                return ValidarComando(parametros, workspace);

            if (nombre is "hacer_solicitud_http")
                return ValidarHttp(parametros);

            // Skills: sus parametros son propios; quedan sujetos al runner del workspace.
            return new ResultadoPoliticaHerramienta(true);
        }

        private static ResultadoPoliticaHerramienta NormalizarRuta(
            JObject parametros,
            string propiedad,
            string workspace,
            bool escritura)
        {
            string valor = parametros[propiedad]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(valor))
                return new ResultadoPoliticaHerramienta(true);

            string ruta = ResolverRuta(valor, workspace);
            if (!EstaDentroDe(workspace, ruta))
            {
                string accion = escritura ? "escribir fuera del workspace" : "leer fuera del workspace";
                return new ResultadoPoliticaHerramienta(
                    false,
                    $"Politica de herramientas: no se permite {accion}. Ruta: {ruta}");
            }

            parametros[propiedad] = ruta;
            return new ResultadoPoliticaHerramienta(true);
        }

        private static ResultadoPoliticaHerramienta ValidarComando(
            JObject parametros,
            string workspace)
        {
            string comando = parametros["comando"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(comando))
                return new ResultadoPoliticaHerramienta(true);

            foreach (string bloqueado in ComandosBloqueados)
            {
                if (comando.Contains(bloqueado, StringComparison.OrdinalIgnoreCase))
                {
                    return new ResultadoPoliticaHerramienta(
                        false,
                        $"Politica de herramientas: comando bloqueado por patron '{bloqueado}'.");
                }
            }

            string directorio = parametros["directorio_trabajo"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(directorio))
            {
                parametros["directorio_trabajo"] = workspace;
                return new ResultadoPoliticaHerramienta(true);
            }

            string rutaTrabajo = ResolverRuta(directorio, workspace);
            if (!Directory.Exists(rutaTrabajo) || !EstaDentroDe(workspace, rutaTrabajo))
            {
                return new ResultadoPoliticaHerramienta(
                    false,
                    $"Politica de herramientas: el comando debe ejecutarse dentro del workspace. Ruta: {rutaTrabajo}");
            }

            parametros["directorio_trabajo"] = rutaTrabajo;
            return new ResultadoPoliticaHerramienta(true);
        }

        private static ResultadoPoliticaHerramienta ValidarHttp(JObject parametros)
        {
            string url = parametros["url"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(url))
                return new ResultadoPoliticaHerramienta(true);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return new ResultadoPoliticaHerramienta(
                    false,
                    "Politica de herramientas: solo se permiten URLs http/https validas.");
            }

            if (uri.Host.Equals("169.254.169.254", StringComparison.OrdinalIgnoreCase))
            {
                return new ResultadoPoliticaHerramienta(
                    false,
                    "Politica de herramientas: acceso a metadata cloud bloqueado.");
            }

            return new ResultadoPoliticaHerramienta(true);
        }

        private static string NormalizarBase(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return "";
            try { return Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            catch { return ""; }
        }

        private static string ResolverRuta(string ruta, string workspace)
        {
            try
            {
                string candidata = Path.IsPathRooted(ruta)
                    ? ruta
                    : Path.Combine(workspace, ruta);
                return Path.GetFullPath(candidata);
            }
            catch
            {
                return ruta;
            }
        }

        private static bool EstaDentroDe(string raiz, string ruta)
        {
            string root = NormalizarBase(raiz) + Path.DirectorySeparatorChar;
            string full = NormalizarBase(ruta) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }
    }
}
