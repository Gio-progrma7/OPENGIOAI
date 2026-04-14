// ============================================================
//  SkillRunnerHelper.cs — Genera skill_runner.py y extrae los
//  scripts Python de los archivos .md de skills.
//
//  El Constructor puede importar este helper en su código:
//    from skill_runner import skill_run, skill_run_json
//    result = skill_run("obtener_ram")
//
//  Contrato entre skill_runner.py y cada script de skill:
//    · El script lee sus parámetros con:
//        params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
//    · El script escribe su resultado en stdout como JSON.
// ============================================================

using OPENGIOAI.Entidades;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Skills
{
    /// <summary>
    /// Escribe <c>skill_runner.py</c> en el directorio de trabajo
    /// y extrae/genera los .py individuales de los skills .md.
    /// </summary>
    public static class SkillRunnerHelper
    {
        private const string NombreRunner  = "skill_runner.py";
        private const string CarpetaSkills = "skills";

        /// <summary>
        /// Genera (o actualiza) skill_runner.py y extrae los .py de cada skill .md.
        /// Fire-and-forget seguro — los errores se silencian para no interrumpir ARIA.
        /// </summary>
        public static async Task GenerarAsync(
            string rutaBase,
            IReadOnlyList<Skill> skills,
            CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // Extraer scripts .py de cada skill que proviene de .md
                await ExtraerScriptsMdAsync(rutaBase, skills, ct);

                // Generar el skill_runner.py orquestador
                string ruta      = Path.Combine(rutaBase, NombreRunner);
                string contenido = GenerarContenidoRunner(rutaBase, skills);
                await File.WriteAllTextAsync(ruta, contenido, Encoding.UTF8, ct);
            }
            catch { /* Silenciar — nunca interrumpir el pipeline */ }
        }

        /// <summary>
        /// Extrae el bloque Python de cada skill .md y lo escribe como .py.
        /// Solo actúa si el skill tiene ContenidoMd (viene de archivo .md).
        /// </summary>
        public static async Task ExtraerScriptsMdAsync(
            string rutaBase,
            IReadOnlyList<Skill> skills,
            CancellationToken ct = default)
        {
            string carpeta = Path.Combine(rutaBase, CarpetaSkills);
            Directory.CreateDirectory(carpeta);

            foreach (var skill in skills)
            {
                ct.ThrowIfCancellationRequested();

                // Solo procesar skills que vienen de .md
                if (string.IsNullOrWhiteSpace(skill.ContenidoMd)) continue;

                string? codigo = SkillMdParser.ExtraerCodigo(skill.ContenidoMd);
                if (string.IsNullOrWhiteSpace(codigo)) continue;

                // Nombre del .py = id del skill
                string nombrePy = $"{skill.IdEfectivo}.py";
                string rutaPy   = Path.Combine(carpeta, nombrePy);

                try
                {
                    await File.WriteAllTextAsync(rutaPy, codigo, Encoding.UTF8, ct);
                    // Actualizar RutaScript en memoria para que el runner lo encuentre
                    skill.RutaScript = nombrePy;
                }
                catch { /* Continuar con el siguiente */ }
            }
        }

        // ── Privado ──────────────────────────────────────────────────────────

        private static string GenerarContenidoRunner(
            string rutaBase, IReadOnlyList<Skill> skills)
        {
            // Construir el diccionario id → ruta absoluta del script
            var entradas = skills
                .Where(s => !string.IsNullOrWhiteSpace(s.RutaScript))
                .Select(s =>
                {
                    string scriptPath = Path.IsPathRooted(s.RutaScript)
                        ? s.RutaScript
                        : Path.Combine(rutaBase, CarpetaSkills, s.RutaScript);

                    string escapada = scriptPath.Replace("\\", "\\\\");
                    return $"    \"{s.IdEfectivo}\": r\"{escapada}\"";
                });

            string registry     = string.Join(",\n", entradas);
            string rutaEscapada = rutaBase.Replace("\\", "\\\\");

            return $@"# skill_runner.py — AUTO-GENERADO por OPENGIOAI. No editar manualmente.
# Importar en codigo generado: from skill_runner import skill_run, skill_run_json
#
# Contrato: el script hijo lee sus parametros con:
#   params = json.loads(os.environ.get(""SKILL_PARAMS"", ""{{}}""))
#
import sys, os, json, subprocess

_RUTA_BASE = r""{rutaEscapada}""

# Registro: id -> ruta absoluta del script
_SKILLS = {{
{registry}
}}

def skill_run(skill_id: str, **kwargs) -> str:
    """"""Ejecuta un skill por ID. Devuelve el stdout del script como string.""""""
    script = _SKILLS.get(skill_id)
    if not script:
        disponibles = "", "".join(_SKILLS.keys())
        raise ValueError(
            f""Skill '{{skill_id}}' no registrado. Disponibles: {{disponibles}}"")

    env = os.environ.copy()
    env[""SKILL_PARAMS""]    = json.dumps(kwargs, ensure_ascii=False)
    env[""SKILL_RUTA_BASE""] = _RUTA_BASE

    result = subprocess.run(
        [sys.executable, script],
        capture_output=True, text=True, encoding=""utf-8"",
        env=env, timeout=120
    )

    if result.returncode != 0 and result.stderr.strip():
        return f""[Error en skill {{skill_id}}]\n{{result.stderr.strip()}}""

    return result.stdout.strip()


def skill_run_json(skill_id: str, **kwargs) -> dict:
    """"""Como skill_run pero parsea el stdout como JSON y devuelve dict.""""""
    raw = skill_run(skill_id, **kwargs)
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return {{""resultado"": raw}}


if __name__ == ""__main__"":
    print(f""skill_runner.py - {{len(_SKILLS)}} skills registrados:"")
    for sid, path in _SKILLS.items():
        existe = ""OK"" if os.path.exists(path) else ""NO ENCONTRADO""
        print(f""  {{sid:30}} {{existe}}"")
";
        }
    }
}
