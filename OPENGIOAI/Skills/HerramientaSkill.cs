// ============================================================
//  HerramientaSkill.cs — Envuelve un Skill como IHerramienta
//
//  Permite que MotorHerramientas (bucle agéntico con tool-use)
//  llame directamente a cualquier skill registrado en ListSkills.json.
//
//  Contrato de parámetros (mismo que skill_runner.py):
//    · Los parámetros llegan al script hijo via env SKILL_PARAMS (JSON).
//    · El script escribe su resultado en stdout como JSON o texto.
//    · HerramientaSkill devuelve ese stdout tal cual.
// ============================================================

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OPENGIOAI.Entidades;
using OPENGIOAI.Herramientas;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Skills
{
    /// <summary>
    /// Adaptador que expone un <see cref="Skill"/> como <see cref="IHerramienta"/>.
    /// El LLM puede invocarlo mediante tool-use igual que cualquier herramienta nativa.
    /// </summary>
    public sealed class HerramientaSkill : IHerramienta
    {
        private readonly Skill  _skill;
        private readonly string _rutaBase;

        /// <param name="skill">El skill a envolver.</param>
        /// <param name="rutaBase">Directorio de trabajo donde vive ListSkills.json.</param>
        public HerramientaSkill(Skill skill, string rutaBase = "")
        {
            _skill    = skill    ?? throw new ArgumentNullException(nameof(skill));
            _rutaBase = rutaBase ?? "";
        }

        // ── IHerramienta ─────────────────────────────────────────────────────

        public string Nombre => _skill.IdEfectivo;

        public string Descripcion
        {
            get
            {
                string firma = ConstruirFirmaTexto();
                string baseDesc = $"[{_skill.Categoria}] {_skill.Descripcion}";
                return string.IsNullOrEmpty(firma)
                    ? baseDesc
                    : $"{baseDesc}  ·  firma: {Nombre}({firma})";
            }
        }

        public JObject EsquemaParametros => ConstruirSchema();

        /// <summary>
        /// Ejecuta el script del skill pasando <paramref name="parametros"/>
        /// vía la variable de entorno <c>SKILL_PARAMS</c> (JSON).
        /// Devuelve el stdout del proceso. Nunca lanza excepción al caller.
        /// </summary>
        public async Task<string> EjecutarAsync(
            JObject parametros, CancellationToken ct = default)
        {
            // ── Validación previa: requeridos / tipos / opciones ─────────────
            // Si faltan, devolvemos un mensaje estructurado al LLM en lugar de
            // dejar que el script Python crashee, así el agente puede pedir
            // los datos faltantes en el siguiente turno.
            var errores = _skill.ValidarParametros(parametros);
            if (errores.Count > 0)
            {
                var sbErr = new StringBuilder();
                sbErr.AppendLine($"[Skill '{Nombre}' — parámetros inválidos]");
                foreach (var er in errores) sbErr.AppendLine($"  · {er}");
                sbErr.AppendLine();
                sbErr.AppendLine("Vuelve a llamar al skill incluyendo los parámetros faltantes/correctos.");
                return sbErr.ToString().TrimEnd();
            }

            // Resolver ruta absoluta del script
            string scriptPath = ResolverRutaScript();

            if (!File.Exists(scriptPath))
                return $"Error: el script del skill '{Nombre}' no existe en '{scriptPath}'.";

            string paramsJson = parametros?.ToString(Formatting.None) ?? "{}";

            var psi = new ProcessStartInfo
            {
                FileName               = "python",
                Arguments              = $"\"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8,
            };

            psi.Environment["SKILL_PARAMS"]    = paramsJson;
            psi.Environment["SKILL_RUTA_BASE"] = _rutaBase;

            try
            {
                using var proceso = new Process { StartInfo = psi };
                proceso.Start();

                // Timeout de 120 s para skills
                using var linked = CancellationTokenSource
                    .CreateLinkedTokenSource(ct);
                linked.CancelAfter(TimeSpan.FromSeconds(120));

                string stdout = await proceso.StandardOutput
                    .ReadToEndAsync(linked.Token);
                string stderr = await proceso.StandardError
                    .ReadToEndAsync(linked.Token);
                await proceso.WaitForExitAsync(linked.Token);

                if (proceso.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
                    return $"[Skill {Nombre} — error]\n{stderr.Trim()}";

                return string.IsNullOrWhiteSpace(stdout)
                    ? "(skill completado sin salida)"
                    : stdout.Trim();
            }
            catch (OperationCanceledException)
            {
                return $"Error: skill '{Nombre}' superó el tiempo máximo de 120 s.";
            }
            catch (Exception ex)
            {
                return $"Error ejecutando skill '{Nombre}': {ex.Message}";
            }
        }

        // ── Privado ──────────────────────────────────────────────────────────

        private string ResolverRutaScript()
        {
            if (Path.IsPathRooted(_skill.RutaScript))
                return _skill.RutaScript;

            // Ruta relativa → buscar dentro de skills/ del directorio de trabajo
            if (!string.IsNullOrWhiteSpace(_rutaBase))
                return Path.Combine(_rutaBase, "skills", _skill.RutaScript);

            return _skill.RutaScript;
        }

        private JObject ConstruirSchema()
        {
            if (_skill.Parametros == null || _skill.Parametros.Count == 0)
                return JObject.Parse(
                    """{"type":"object","properties":{},"required":[]}""");

            var properties = new JObject();
            var required   = new JArray();

            foreach (var p in _skill.Parametros)
            {
                var prop = new JObject
                {
                    ["type"]        = NormalizarTipoSchema(p.Tipo),
                    ["description"] = p.Descripcion
                };

                // Enum: si hay opciones cerradas, exponerlas al LLM
                if (p.Opciones != null && p.Opciones.Count > 0)
                {
                    var arr = new JArray();
                    foreach (var op in p.Opciones) arr.Add(op);
                    prop["enum"] = arr;
                }

                // Default: ayuda al LLM a no preguntar de más por opcionales
                if (!string.IsNullOrWhiteSpace(p.ValorPorDefecto))
                    prop["default"] = p.ValorPorDefecto;

                properties[p.Nombre] = prop;

                if (p.Requerido)
                    required.Add(p.Nombre);
            }

            return new JObject
            {
                ["type"]       = "object",
                ["properties"] = properties,
                ["required"]   = required
            };
        }

        private static string NormalizarTipoSchema(string tipo)
        {
            return (tipo ?? "string").Trim().ToLowerInvariant() switch
            {
                "int" or "integer" or "long" => "integer",
                "float" or "double" or "number" or "decimal" => "number",
                "bool" or "boolean" => "boolean",
                "list" or "array" => "array",
                "object" or "dict" or "map" => "object",
                _ => "string"
            };
        }

        /// <summary>
        /// Devuelve una firma legible "ruta: string*, sheet: string=Hoja1"
        /// que se concatena a la descripción de la herramienta.
        /// </summary>
        private string ConstruirFirmaTexto()
        {
            if (_skill.Parametros == null || _skill.Parametros.Count == 0)
                return "";

            var partes = _skill.Parametros.Select(p =>
            {
                string sufijo = p.Requerido ? "*" : "";
                string defecto = !string.IsNullOrWhiteSpace(p.ValorPorDefecto)
                    ? $"={p.ValorPorDefecto}"
                    : "";
                return $"{p.Nombre}: {p.Tipo}{sufijo}{defecto}";
            });
            return string.Join(", ", partes);
        }
    }
}
