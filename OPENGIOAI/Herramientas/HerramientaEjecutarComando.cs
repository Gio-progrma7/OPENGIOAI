using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Herramienta: ejecutar_comando
    /// Permite al agente ejecutar comandos en PowerShell, Python o cmd con restricciones de seguridad.
    /// </summary>
    public class HerramientaEjecutarComando : IHerramienta
    {
        public string Nombre => "ejecutar_comando";

        public string Descripcion =>
            "Ejecuta un comando en PowerShell, Python o cmd y devuelve la salida. " +
            "Úsala para operaciones del sistema, procesamiento de datos, " +
            "instalar paquetes (pip install), consultas de red, etc. " +
            "Tiene límite de tiempo configurable y lista negra de comandos destructivos.";

        public JObject EsquemaParametros => JObject.Parse("""
            {
              "type": "object",
              "properties": {
                "comando": {
                  "type": "string",
                  "description": "El comando a ejecutar como string."
                },
                "tipo": {
                  "type": "string",
                  "enum": ["powershell", "python", "cmd"],
                  "description": "Intérprete a usar. Por defecto 'powershell'."
                },
                "directorio_trabajo": {
                  "type": "string",
                  "description": "Directorio de trabajo donde ejecutar el comando."
                },
                "timeout_segundos": {
                  "type": "integer",
                  "description": "Tiempo máximo de ejecución en segundos. Por defecto 30, máximo 300."
                }
              },
              "required": ["comando"]
            }
            """);

        // Lista negra — operaciones que nunca se permiten
        private static readonly string[] _bloqueados =
        [
            "rm -rf", "Remove-Item -Recurse -Force",
            "del /s /q", "rd /s /q",
            "format ", "diskpart",
            "shutdown", "restart-computer",
            "net user", "net localgroup",
            "reg delete", "reg add",
            "bcdedit", "bootrec",
            "cipher /w", "sfc /scannow"
        ];

        public async Task<string> EjecutarAsync(JObject parametros, CancellationToken ct = default)
        {
            string comando = parametros["comando"]?.ToString() ?? "";
            string tipo = (parametros["tipo"]?.ToString() ?? "powershell").ToLowerInvariant();
            string directorio = parametros["directorio_trabajo"]?.ToString() ?? "";
            int timeout = Math.Min(parametros["timeout_segundos"]?.Value<int>() ?? 30, 300);

            if (string.IsNullOrWhiteSpace(comando))
                return "Error: Se requiere el parámetro 'comando'.";

            // Verificar lista negra
            foreach (var bloqueado in _bloqueados)
                if (comando.Contains(bloqueado, StringComparison.OrdinalIgnoreCase))
                    return $"Seguridad: Operación bloqueada. El comando contiene '{bloqueado}'.";

            var (fileName, args) = tipo switch
            {
                "python" => ("python", $"-c \"{EscaparPython(comando)}\""),
                "cmd" => ("cmd.exe", $"/c {comando}"),
                _ => ("powershell.exe",
                      $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{EscaparPS(comando)}\"")
            };

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            if (!string.IsNullOrWhiteSpace(directorio) && Directory.Exists(directorio))
                psi.WorkingDirectory = directorio;

            try
            {
                using var proceso = new Process { StartInfo = psi };
                proceso.Start();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                string output = await proceso.StandardOutput.ReadToEndAsync(cts.Token);
                string error = await proceso.StandardError.ReadToEndAsync(cts.Token);
                await proceso.WaitForExitAsync(cts.Token);

                var sb = new StringBuilder();
                sb.AppendLine($"Código de salida: {proceso.ExitCode}");

                if (!string.IsNullOrWhiteSpace(output))
                    sb.AppendLine($"Salida:\n{output.Trim()}");

                if (!string.IsNullOrWhiteSpace(error))
                    sb.AppendLine($"Stderr:\n{error.Trim()}");

                return sb.ToString().Trim();
            }
            catch (OperationCanceledException)
            {
                return $"Error: El comando superó el tiempo límite de {timeout} segundos.";
            }
            catch (Exception ex)
            {
                return $"Error al ejecutar el comando: {ex.Message}";
            }
        }

        private static string EscaparPS(string cmd) =>
            cmd.Replace("\"", "`\"").Replace("\n", "; ");

        private static string EscaparPython(string cmd) =>
            cmd.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
