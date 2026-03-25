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
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            // Leer prompts base del disco (única lectura por ejecución)
            string promptMaestro = await MarkdownFileManager
                .LeerAsync(RutasProyecto.ObtenerRutaPromtMaestro());

            ct.ThrowIfCancellationRequested();

            string promptErr = await MarkdownFileManager
                .LeerAsync(RutasProyecto.ObtenerRutaPromtAgente());

            var ctx = new AgentContext
            {
                PromptMaestro = promptMaestro,
                PromptRespuestaErr = promptErr,
                RutaArchivo = rutaArchivo,
                Modelo = modelo,
                ApiKey = apiKey,
                Servicio = servicio,
                SoloChat = soloChat,
                ClavesDisponibles = clavesDisponibles,
            };

            // Ensamblar prompt efectivo una sola vez
            ctx.PromptEfectivo = ctx.ConstruirPromptEfectivo();

            return ctx;
        }

        /// <summary>
        /// Ensambla el prompt maestro + secciones dinámicas.
        /// CLAVE: solo incluye secciones relevantes para reducir tokens.
        /// Una tarea sin Telegram no necesita las reglas de Telegram.
        /// </summary>
        private string ConstruirPromptEfectivo()
        {
            // Si no hay claves configuradas, el prompt maestro es suficiente.
            if (string.IsNullOrWhiteSpace(ClavesDisponibles))
                return PromptMaestro;

            var sb = new System.Text.StringBuilder();
            sb.Append(PromptMaestro);

            // ── Sección: credenciales (siempre si hay claves) ────
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

            // ── Sección: salida y rutas base ─────────────────────
            sb.Append($@"
================= RUTA DE TRABAJO =================
Ruta activa: {RutaArchivo}
Archivo de respuesta: {RutaArchivo}\respuesta.txt
Historial: {RutaArchivo}\Historial.md
");

            // ── Sección: SOLO_CHAT (solo si aplica) ──────────────
            if (SoloChat)
            {
                sb.Append($@"
================= MODO SOLO CHAT =================
SOLO_CHAT = TRUE
Toda salida visible EXCLUSIVAMENTE en: {RutaArchivo}\respuesta.txt
Tu respuesta SIEMPRE debe ser código Python válido. Sin texto plano.
");
            }

            // ── Sección: skills ──────────────────────────────────
            sb.Append($@"
================= SISTEMA DE SKILLS =================
Scripts en: {RutaArchivo}\skills\
Lista en:   {RutaArchivo}\ListSkills.json
");

            // ── Sección: automatizaciones ────────────────────────
            sb.Append($@"
================= AUTOMATIZACIONES =================
Carpeta: {RutaArchivo}\Automatizaciones\
Lista:   {RutaArchivo}\ListAutomatizacion.json
");

            // ── Sección: historial ───────────────────────────────
            sb.Append($@"
================= HISTORIAL =================
Registra cada ejecución en {RutaArchivo}\Historial.md
Formato: #[FECHA] Descripción breve: [TIPO_ACCION]
");
            /*
            // ── Sección: arquitectura ────────────────────────────
            sb.Append($@"
================= ARQUITECTURA =================
Si el usuario pregunta cómo estás hecho, lee: {RutasProyecto.ObtenerArquitectura()}
Escribe la respuesta en respuesta.txt.
");
*/
            // ── Sección: prompts maestros ────────────────────────
            sb.Append($@"
================= CONFIGURACIÓN DE PROMPTS =================
Prompt maestro:         {RutasProyecto.ObtenerRutaPromtMaestro()}
Prompt respuesta error: {RutasProyecto.ObtenerRutaPromtAgente()}
");

            // ── Sección: usuario ─────────────────────────────────
            sb.Append(@"
================= USUARIO =================
Nombre: Giovanni Sanchez
");

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
                PromptMaestro = PromptMaestro,
                PromptRespuestaErr = PromptRespuestaErr,
                RutaArchivo = RutaArchivo,
                Modelo = Modelo,
                ApiKey = ApiKey,
                Servicio = Servicio,
                SoloChat = SoloChat,
                ClavesDisponibles = ClavesDisponibles,
                // El agente 2 usa el prompt de error/respuesta como base
                PromptEfectivo = PromptRespuestaErr,
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
                PromptEfectivo = PromtsBase.PromtInicioUsuario(RutaArchivo),
            };
        }
    }
}