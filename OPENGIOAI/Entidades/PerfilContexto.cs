// ============================================================
//  PerfilContexto.cs  — Fase B: slicing de contexto por fase
//
//  Cada fase del pipeline ARIA tiene necesidades muy distintas:
//    · Constructor   — necesita TODO (credenciales, skills, memoria...)
//    · Analista      — solo interpreta; nada de credenciales/skills
//    · Analizador    — solo verifica JSON; contexto mínimo
//    · Guardián      — idem
//    · Comunicador   — formatea; identidad sí, credenciales no
//    · Memorista     — extrae hechos; no necesita leer su propia memoria
//    · AnalizadorPatrones — parseo puro de patrones
//
//  Antes de Fase B todos recibían el mismo PromptEfectivo pesado, aunque
//  muchos de ellos lo reemplazaban por "" via ConPromptPersonalizado.
//  Eso seguía cargando I/O de disco (memoria, skills, prompt maestro)
//  y ocupando memoria por gusto.
//
//  Con PerfilContexto cada fase declara qué necesita, y BuildAsync:
//    · Evita leer archivos innecesarios (disco)
//    · No incluye secciones irrelevantes en PromptEfectivo (tokens)
// ============================================================

namespace OPENGIOAI.Entidades
{
    /// <summary>
    /// Especifica qué partes del contexto debe cargar/incluir
    /// <see cref="OPENGIOAI.Data.AgentContext"/> al construirse.
    /// Usar los presets estáticos (Completo, SoloIdentidad, …) en vez de
    /// construir manualmente — evita olvidar campos.
    /// </summary>
    public sealed class PerfilContexto
    {
        // ── I/O de disco: ¿qué archivos leer? ─────────────────────
        public bool LeerPromptMaestroDeDisco { get; init; } = true;
        public bool LeerSkillsDeDisco        { get; init; } = true;
        public bool LeerMemoriaDeDisco       { get; init; } = true;

        // ── Secciones del PromptEfectivo ─────────────────────────
        public bool IncluirPromptMaestro     { get; init; } = true;
        public bool IncluirCredenciales      { get; init; } = true;
        public bool IncluirRutaTrabajo       { get; init; } = true;
        public bool IncluirSoloChat          { get; init; } = true;
        public bool IncluirSkills            { get; init; } = true;
        public bool IncluirAutomatizaciones  { get; init; } = true;
        public bool IncluirHistorial         { get; init; } = true;
        public bool IncluirMemoria           { get; init; } = true;
        public bool IncluirPromptsMaestros   { get; init; } = true;
        public bool IncluirUsuario           { get; init; } = true;

        /// <summary>Nombre descriptivo del perfil (para logs/telemetría).</summary>
        public string Nombre { get; init; } = "Completo";

        // ═══════════════════ Presets ═══════════════════

        /// <summary>
        /// Perfil clásico — carga todo. Usado por el Constructor, el
        /// Agente1 y cualquier fase que genere código/acciones reales.
        /// </summary>
        public static PerfilContexto Completo => new()
        {
            Nombre = "Completo",
        };

        /// <summary>
        /// Solo el prompt maestro + sección "Usuario". Sin credenciales,
        /// skills, memoria, rutas. Útil para fases que necesitan saber
        /// "quién es el agente" pero no "qué herramientas tiene".
        /// Ej: Comunicador.
        /// </summary>
        public static PerfilContexto SoloIdentidad => new()
        {
            Nombre                  = "SoloIdentidad",
            LeerSkillsDeDisco       = false,
            LeerMemoriaDeDisco      = false,
            IncluirCredenciales     = false,
            IncluirRutaTrabajo      = false,
            IncluirSoloChat         = false,
            IncluirSkills           = false,
            IncluirAutomatizaciones = false,
            IncluirHistorial        = false,
            IncluirMemoria          = false,
            IncluirPromptsMaestros  = false,
        };

        /// <summary>
        /// Cero I/O de disco y cero secciones. Para fases que ignoran
        /// totalmente el PromptEfectivo y pasan su propio prompt con
        /// <c>ConPromptPersonalizado(...)</c>.
        /// Ej: Analista, Analizador, Guardián, AnalizadorPatrones.
        /// </summary>
        public static PerfilContexto Minimo => new()
        {
            Nombre                     = "Minimo",
            LeerPromptMaestroDeDisco   = false,
            LeerSkillsDeDisco          = false,
            LeerMemoriaDeDisco         = false,
            IncluirPromptMaestro       = false,
            IncluirCredenciales        = false,
            IncluirRutaTrabajo         = false,
            IncluirSoloChat            = false,
            IncluirSkills              = false,
            IncluirAutomatizaciones    = false,
            IncluirHistorial           = false,
            IncluirMemoria             = false,
            IncluirPromptsMaestros     = false,
            IncluirUsuario             = false,
        };

        /// <summary>
        /// Para el Memorista: NO necesita leer memoria (la está escribiendo)
        /// ni skills ni credenciales. Solo tiene su propio prompt con el
        /// par (instrucción, respuesta) que acaba de ocurrir.
        /// </summary>
        public static PerfilContexto Memorista => Minimo;

        /// <summary>
        /// Para el Comunicador: mantiene identidad y usuario (estilo),
        /// quita credenciales/skills/memoria/rutas. Prompt más liviano
        /// → streaming más rápido y menos tokens de prompt repetidos.
        /// </summary>
        public static PerfilContexto Comunicador => new()
        {
            Nombre                  = "Comunicador",
            LeerSkillsDeDisco       = false,
            // La memoria sí puede aportar contexto al Comunicador
            // (ej. "me llamo Gio" para personalizar), pero solo si
            // el usuario tiene la habilidad de memoria activa.
            LeerMemoriaDeDisco      = true,
            IncluirCredenciales     = false,
            IncluirRutaTrabajo      = false,
            IncluirSoloChat         = false,
            IncluirSkills           = false,
            IncluirAutomatizaciones = false,
            IncluirHistorial        = false,
            IncluirPromptsMaestros  = false,
        };
    }
}
