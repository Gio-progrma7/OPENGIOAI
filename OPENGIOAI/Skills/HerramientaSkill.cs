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

        public string Descripcion =>
            $"[{_skill.Categoria}] {_skill.Descripcion}";

        public JObject EsquemaParametros => ConstruirSchema();

        /// <summary>
        /// Ejecuta el script del skill pasando <paramref name="parametros"/>
        /// vía la variable de entorno <c>SKILL_PARAMS</c> (JSON).
        /// Devuelve el stdout del proceso. Nunca lanza excepción al caller.
        /// </summary>
        public async Task<string> EjecutarAsync(
            JObject parametros, CancellationToken ct = default)
        {
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
                properties[p.Nombre] = new JObject
                {
                    ["type"]        = p.Tipo,
                    ["description"] = p.Descripcion
                };
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
    }
}
