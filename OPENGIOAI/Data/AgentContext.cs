// ============================================================
//  AgentContext.cs
//  PASO 1 — Elimina el estado global mutable de PromtsBase.
//
//  PROBLEMA ANTERIOR:
//    PromtsBase.PromtAgente era un static string que se mutaba
//    y restauraba alrededor de awaits. Con dos llamadas concurrentes
//    (Telegram + UI, doble click) ambas pisaban el mismo string
//    y la restauración dejaba el estado incorrecto — bug silencioso.
//
//  SOLUCIÓN:
//    AgentContext encapsula todo lo que varía por ejecución.
//    Se construye una vez y se pasa como parámetro.
//    Nunca se comparte entre ejecuciones distintas.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.Promts;
using OPENGIOAI.Skills;
using OPENGIOAI.Utilerias;

namespace OPENGIOAI.Data
{
    /// <summary>
    /// Contexto inmutable de una ejecución del agente.
    /// Reemplaza las mutaciones de PromtsBase.PromtAgente.
    /// Se construye con AgentContext.BuildAsync() y se pasa
    /// como parámetro a todos los métodos del pipeline.
    /// </summary>
    public sealed class AgentContext
    {
        // ── Identidad del agente ──────────────────────────────
        public string PromptMaestro { get; init; } = "";
        public string PromptRespuestaErr { get; init; } = "";

        // ── Configuración de ejecución ────────────────────────
        public string RutaArchivo { get; init; } = "";
        public string Modelo { get; init; } = "";
        public string ApiKey { get; init; } = "";
        public Servicios Servicio { get; init; }
        public bool SoloChat { get; init; }

        // ── Credenciales disponibles ──────────────────────────
        public string ClavesDisponibles { get; init; } = "";

        // ── Prompt final listo para enviar al LLM ─────────────
        // Se construye una sola vez en BuildAsync(), nunca se muta.
        public string PromptEfectivo { get; private set; } = "";

        // ── Skills cargados para este contexto ────────────────
        public IReadOnlyList<Skill> Skills { get; init; } =
            System.Array.Empty<Skill>();
        public string ManifiestoSkills { get; init; } = "";

        // ── Memoria (Fase 1) ──────────────────────────────────
        // Sección ya formateada y recortada al presupuesto de tokens.
        // Se lee una sola vez en BuildAsync. Si la memoria está vacía,
        // queda string vacío y la sección se omite.
        public string MemoriaFormateada { get; init; } = "";

        // ── Telemetría (Fase A) ───────────────────────────────
        // Etiqueta de fase del pipeline. Se propaga a cada llamada al LLM
        // para que ConsumoTokensTracker pueda agrupar el consumo por fase.
        // Default "General" para llamadas fuera del pipeline.
        public string NombreFase { get; init; } = "General";

        // ── Perfil de contexto (Fase B) ──────────────────────────
        // Declara qué secciones debe incluir PromptEfectivo. Default
        // "Completo" para preservar el comportamiento histórico
        // (Constructor, soloChat, Agente1). Otras fases usan perfiles
        // más livianos para ahorrar tokens y I/O.
        public PerfilContexto Perfil { get; init; } = PerfilContexto.Completo;

        // ── Constructor privado — usar BuildAsync() ───────────
        private AgentContext() { }

        /// <summary>
        /// Construye el contexto leyendo los archivos de prompt del disco
        /// y ensamblando el prompt efectivo con las secciones necesarias.
        /// Es el único lugar donde se leen MarkdownFileManager.
        /// </summary>
        public static async Task<AgentContext> BuildAsync(
            string rutaArchivo,
            string modelo,
            string apiKey,
            Servicios servicio,
            bool soloChat,
            string clavesDisponibles,
            CancellationToken ct = default,
            PerfilContexto? perfil = null,
            string instruccionUsuario = "")
        {
            ct.ThrowIfCancellationRequested();

            // El default sigue siendo Completo para no romper llamadas antiguas.
            perfil ??= PerfilContexto.Completo;

            // Leer prompts base del disco (única lectura por ejecución)
            // — pero SOLO si el perfil los necesita. El Analista/Guardián
            //   pasan "" como PromptEfectivo, así que no tiene sentido leer
            //   el archivo maestro para descartarlo inmediatamente.
            string promptMaestro = "";
            string promptErr     = "";
            if (perfil.LeerPromptMaestroDeDisco)
            {
                promptMaestro = await MarkdownFileManager
                    .LeerAsync(RutasProyecto.ObtenerRutaPromtMaestro());

                ct.ThrowIfCancellationRequested();

                promptErr = await MarkdownFileManager
                    .LeerAsync(RutasProyecto.ObtenerRutaPromtAgente());
            }

            // ── Cargar skills del directorio de trabajo ──────────────────────
            // El manifiesto suele ser la sección más gorda del prompt (crece
            // con cada Skill). Si el perfil no necesita skills, ni siquiera
            // los cargamos ni generamos el skill_runner.py.
            IReadOnlyList<Skill> skills = System.Array.Empty<Skill>();
            string manifiesto = "";
            if (perfil.LeerSkillsDeDisco)
            {
                skills = SkillLoader.CargarActivas(rutaArchivo);
                manifiesto = SkillManifestBuilder.Construir(skills);

                // Generar skill_runner.py en background — no bloquea el pipeline
                _ = SkillRunnerHelper.GenerarAsync(rutaArchivo, skills, ct);
            }

            // ── Cargar memoria durable de la ruta de trabajo (Fase 1) ───────
            // Doble gate: habilidad "Memoria" ACTIVA + perfil lo pide.
            // Así el Memorista (que escribe memoria) no lee su propia memoria,
            // y el Analizador/Guardián (que solo verifican JSON) no la leen.
            //
            // Fase C (RAG): si además la habilidad "memoria_semantica" está
            // activa Y tenemos una instrucción del usuario, usamos búsqueda
            // semántica (top-K) en lugar del dump completo. Ahorro típico:
            // −500 a −4000 tokens por ejecución cuando la memoria crece.
            string memoriaFormateada = "";
            if (perfil.LeerMemoriaDeDisco &&
                HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_MEMORIA))
            {
                bool ragActivo =
                    HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_MEMORIA_SEMANTICA)
                    && !string.IsNullOrWhiteSpace(instruccionUsuario);

                if (ragActivo)
                {
                    try
                    {
                        memoriaFormateada = await MemoriaSemantica.ObtenerContextoRelevanteAsync(
                            rutaArchivo, instruccionUsuario, ct);
                    }
                    catch
                    {
                        memoriaFormateada = "";
                    }

                    // Fallback: si el RAG no devolvió nada (índice vacío, error
                    // del proveedor, etc.) caemos al dump completo clásico para
                    // no dejar al agente sin memoria.
                    if (string.IsNullOrWhiteSpace(memoriaFormateada))
                    {
                        try
                        {
                            memoriaFormateada = await MemoriaManager.FormatearParaPromptAsync(rutaArchivo);
                        }
                        catch { memoriaFormateada = ""; }
                    }
                }
                else
                {
                    try
                    {
                        memoriaFormateada = await MemoriaManager.FormatearParaPromptAsync(rutaArchivo);
                    }
                    catch
                    {
                        // La memoria es best-effort: si falla la lectura, el pipeline
                        // debe seguir funcionando sin ella.
                        memoriaFormateada = "";
                    }
                }
            }

            var ctx = new AgentContext
            {
                PromptMaestro      = promptMaestro,
                PromptRespuestaErr = promptErr,
                RutaArchivo        = rutaArchivo,
                Modelo             = modelo,
                ApiKey             = apiKey,
                Servicio           = servicio,
                SoloChat           = soloChat,
                ClavesDisponibles  = clavesDisponibles,
                Skills             = skills,
                ManifiestoSkills   = manifiesto,
                MemoriaFormateada  = memoriaFormateada,
                Perfil             = perfil,
            };

            // Ensamblar prompt efectivo una sola vez
            ctx.PromptEfectivo = ctx.ConstruirPromptEfectivo();

            return ctx;
        }

        /// <summary>
        /// Ensambla el prompt maestro + secciones dinámicas.
        /// Cada sección está gobernada por el <see cref="Perfil"/>:
        /// fases pesadas (Constructor) reciben todo; fases ligeras
        /// (Analista, Memorista…) reciben solo lo imprescindible.
        /// </summary>
        private string ConstruirPromptEfectivo()
        {
            // Si el perfil es Mínimo (todo apagado) devolvemos "" sin
            // construir nada — típico del Analista/Guardián que pasan
            // su propio prompt vía ConPromptPersonalizado.
            if (!Perfil.IncluirPromptMaestro &&
                !Perfil.IncluirCredenciales &&
                !Perfil.IncluirRutaTrabajo &&
                !Perfil.IncluirSkills &&
                !Perfil.IncluirAutomatizaciones &&
                !Perfil.IncluirHistorial &&
                !Perfil.IncluirMemoria &&
                !Perfil.IncluirPromptsMaestros &&
                !Perfil.IncluirUsuario)
            {
                return "";
            }

            // Si no hay claves configuradas, el prompt maestro es suficiente.
            if (string.IsNullOrWhiteSpace(ClavesDisponibles) &&
                Perfil.IncluirPromptMaestro)
                return PromptMaestro;

            var sb = new System.Text.StringBuilder();
            if (Perfil.IncluirPromptMaestro)
                sb.Append(PromptMaestro);

            // ── Sección: credenciales (solo si el perfil lo pide) ────
            if (Perfil.IncluirCredenciales && !string.IsNullOrWhiteSpace(ClavesDisponibles))
            {
                sb.Append($@"
================= SISTEMA DE CREDENCIALES =================

RUTA DEL JSON: {RutasProyecto.ObtenerRutaListApis()}

NOMBRES DISPONIBLES:
{ClavesDisponibles}

REGLAS:
* Solo usa una credencial si es estrictamente necesaria.
* Busca por nombre; extrae siempre el campo ""key"".
* Nunca inventes claves. Si no hay coincidencia → informa.
* Si un token está vencido, usa el token Refresh para renovarlo.
");
            }

            // ── Sección: salida y rutas base ─────────────────────
            if (Perfil.IncluirRutaTrabajo)
            {
                sb.Append($@"
================= RUTA DE TRABAJO =================
Ruta activa: {RutaArchivo}
Archivo de respuesta: {RutaArchivo}\respuesta.txt
Historial: {RutaArchivo}\Historial.md
");
            }

            // ── Sección: SOLO_CHAT (solo si aplica) ──────────────
            if (Perfil.IncluirSoloChat && SoloChat)
            {
                sb.Append($@"
================= MODO SOLO CHAT =================
SOLO_CHAT = TRUE
Toda salida visible EXCLUSIVAMENTE en: {RutaArchivo}\respuesta.txt
Tu respuesta SIEMPRE debe ser código Python válido. Sin texto plano.
");
            }

            // ── Sección: skills disponibles (manifiesto dinámico) ──
            if (Perfil.IncluirSkills && !string.IsNullOrWhiteSpace(ManifiestoSkills))
            {
                sb.Append($@"
================= SKILLS DISPONIBLES =================
{ManifiestoSkills}
Importar en Python: from skill_runner import skill_run, skill_run_json
");
            }

            // ── Sección: automatizaciones ────────────────────────
            if (Perfil.IncluirAutomatizaciones)
            {
                sb.Append($@"
================= AUTOMATIZACIONES =================
Carpeta: {RutaArchivo}\Automatizaciones\
Lista:   {RutaArchivo}\ListAutomatizacion.json
");
            }

            // ── Sección: historial ───────────────────────────────
            if (Perfil.IncluirHistorial)
            {
                sb.Append($@"
================= HISTORIAL =================
Registra cada ejecución en {RutaArchivo}\Historial.md
Formato: #[FECHA] Descripción breve: [TIPO_ACCION]
");
            }

            // ── Sección: prompts maestros ────────────────────────
            if (Perfil.IncluirPromptsMaestros)
            {
                sb.Append($@"
================= CONFIGURACIÓN DE PROMPTS =================
Prompt maestro:         {RutasProyecto.ObtenerRutaPromtMaestro()}
Prompt respuesta error: {RutasProyecto.ObtenerRutaPromtAgente()}
");
            }

            // ── Sección: memoria durable (Fase 1) ────────────────
            if (Perfil.IncluirMemoria && !string.IsNullOrWhiteSpace(MemoriaFormateada))
            {
                sb.Append(MemoriaFormateada);
            }

            // ── Sección: usuario ─────────────────────────────────
            if (Perfil.IncluirUsuario)
            {
                sb.Append(@"
================= USUARIO =================
Nombre: Giovanni Sanchez
");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Devuelve una copia del contexto usando el prompt de respuesta/error
        /// en lugar del prompt maestro. Usado por el Agente 2 (validador).
        /// No muta nada — crea una nueva instancia.
        /// </summary>
        public AgentContext ComoAgente2()
        {
            return new AgentContext
            {
                PromptMaestro      = PromptMaestro,
                PromptRespuestaErr = PromptRespuestaErr,
                RutaArchivo        = RutaArchivo,
                Modelo             = Modelo,
                ApiKey             = ApiKey,
                Servicio           = Servicio,
                SoloChat           = SoloChat,
                ClavesDisponibles  = ClavesDisponibles,
                Skills             = Skills,
                ManifiestoSkills   = ManifiestoSkills,
                MemoriaFormateada  = MemoriaFormateada,
                NombreFase         = NombreFase,
                Perfil             = Perfil,
                PromptEfectivo     = PromptRespuestaErr,
            };
        }

        /// <summary>
        /// Devuelve una copia del contexto con un prompt personalizado.
        /// Usado por los agentes del pipeline (Planificador, Ejecutor, Verificador, Formateador)
        /// y por el AgentePlanificador (ReAct/CoT) para inyectar sus propios system prompts
        /// sin leer archivos del disco ni mutar estado global.
        /// </summary>
        public AgentContext ConPromptPersonalizado(string promptEfectivo)
        {
            return new AgentContext
            {
                PromptMaestro      = PromptMaestro,
                PromptRespuestaErr = PromptRespuestaErr,
                RutaArchivo        = RutaArchivo,
                Modelo             = Modelo,
                ApiKey             = ApiKey,
                Servicio           = Servicio,
                SoloChat           = SoloChat,
                ClavesDisponibles  = ClavesDisponibles,
                Skills             = Skills,
                ManifiestoSkills   = ManifiestoSkills,
                MemoriaFormateada  = MemoriaFormateada,
                NombreFase         = NombreFase,
                Perfil             = Perfil,
                PromptEfectivo     = promptEfectivo,
            };
        }

        /// <summary>
        /// Devuelve una copia para el prompt de bienvenida/inicio de sesión.
        /// </summary>
        public AgentContext ComoInicio()
        {
            return new AgentContext
            {
                PromptMaestro = PromptMaestro,
                PromptRespuestaErr = PromptRespuestaErr,
                RutaArchivo = RutaArchivo,
                Modelo = Modelo,
                ApiKey = ApiKey,
                Servicio = Servicio,
                SoloChat = SoloChat,
                ClavesDisponibles = ClavesDisponibles,
                MemoriaFormateada = MemoriaFormateada,
                NombreFase        = NombreFase,
                Perfil            = Perfil,
                PromptEfectivo = PromtsBase.PromtInicioUsuario(RutaArchivo),
            };
        }

        /// <summary>
        /// Devuelve una copia del contexto etiquetada con el nombre de una fase
        /// del pipeline. Usado por la telemetría de tokens para saber qué fase
        /// generó cada consumo.
        ///
        /// No muta nada — mismo patrón que ComoAgente2/ConPromptPersonalizado.
        /// </summary>
        public AgentContext ComoFase(string nombreFase)
        {
            return new AgentContext
            {
                PromptMaestro      = PromptMaestro,
                PromptRespuestaErr = PromptRespuestaErr,
                RutaArchivo        = RutaArchivo,
                Modelo             = Modelo,
                ApiKey             = ApiKey,
                Servicio           = Servicio,
                SoloChat           = SoloChat,
                ClavesDisponibles  = ClavesDisponibles,
                Skills             = Skills,
                ManifiestoSkills   = ManifiestoSkills,
                MemoriaFormateada  = MemoriaFormateada,
                NombreFase         = string.IsNullOrWhiteSpace(nombreFase) ? "General" : nombreFase,
                Perfil             = Perfil,
                PromptEfectivo     = PromptEfectivo,
            };
        }
    }
}