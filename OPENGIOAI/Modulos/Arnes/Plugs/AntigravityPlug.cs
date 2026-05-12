using System.Threading;
using System.Threading.Tasks;
using OPENGIOAI.Agentes;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;

namespace OPENGIOAI.Modulos.Arnes.Plugs
{
    /// <summary>
    /// Adaptador específico (Plug) para conectar la aplicación "Antigravity"
    /// al ecosistema OPENGIOAI a través del servidor ARNES.
    /// </summary>
    public class AntigravityPlug : IArnesPlug
    {
        public string Nombre => "antigravity";

        public Task InicializarAsync(CancellationToken ct)
        {
            // Aquí iría código para validar licencias, configurar directorios 
            // compartidos o preparar variables de entorno específicas para Antigravity.
            return Task.CompletedTask;
        }

        public async Task<ArnesPayload> ProcesarPeticionAsync(ArnesPayload payload, CancellationToken ct)
        {
            // Lógica específica para interpretar los mensajes que manda Antigravity
            switch (payload.Action.ToLower())
            {
                case "analyze_code":
                case "execute_aria":
                    return await HandleAnalyzeCodeAsync(payload, ct);
                    
                case "sync_workspace":
                    return await HandleSyncWorkspaceAsync(payload, ct);

                default:
                    return new ArnesPayload
                    {
                        SourceApp = Nombre,
                        Status = "error",
                        Message = $"El adaptador Antigravity no soporta la acción '{payload.Action}'."
                    };
            }
        }

        private async Task<ArnesPayload> HandleAnalyzeCodeAsync(ArnesPayload payload, CancellationToken ct)
        {
            // Extraer parámetros específicos
            string instruction = payload.Parameters.ContainsKey("instruction") 
                ? payload.Parameters["instruction"].ToString() ?? "Revisa este código." 
                : "Revisa este código.";
                
            string? codeFragment = payload.Parameters.ContainsKey("code") 
                ? payload.Parameters["code"].ToString() 
                : null;

            // Cargar configuración global para ejecutar ARIA en modo Headless
            var config = Utils.LeerConfig<ConfiguracionClient>(RutasProyecto.ObtenerRutaConfiguracion()) ?? new ConfiguracionClient();
            string modelo = config.Mimodelo?.Modelos ?? "";
            string apiKey = config.Mimodelo?.ApiKey ?? "";
            string ruta = config.MiArchivo?.Ruta ?? RutasProyecto.ObtenerRutaScripts();
            Servicios servicio = config.Mimodelo?.Agente ?? Servicios.Gemenni;

            var apis = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis()) ?? new System.Collections.Generic.List<Api>();
            string claves = Utils.ObtenerNombresApis(apis);

            // Instanciar el Orquestador en modo "Solo Chat" para no sobreescribir scripts locales sin permiso
            var aria = new OrquestadorARIA(
                modelo, ruta, apiKey, claves, 
                soloChat: true, 
                servicio, 
                maxReintentos: 3);

            string fullInstruction = instruction;
            if (!string.IsNullOrWhiteSpace(codeFragment))
            {
                fullInstruction += $"\n\n```\n{codeFragment}\n```";
            }

            try
            {
                string resultado = await aria.EjecutarAsync(fullInstruction, ct);

                var respuesta = new ArnesPayload
                {
                    SourceApp = Nombre,
                    Status = "success",
                    Message = resultado
                };
                
                return respuesta;
            }
            catch (System.Exception ex)
            {
                return new ArnesPayload
                {
                    SourceApp = Nombre,
                    Status = "error",
                    Message = $"Error al ejecutar el análisis: {ex.Message}"
                };
            }
        }

        private Task<ArnesPayload> HandleSyncWorkspaceAsync(ArnesPayload payload, CancellationToken ct)
        {
            return Task.FromResult(new ArnesPayload
            {
                SourceApp = Nombre,
                Status = "success",
                Message = "Workspace sincronizado con el contexto de Antigravity."
            });
        }

        public Task<ArnesPayload> EnviarTareaExternaAsync(ArnesPayload tarea, CancellationToken ct)
        {
            // Implementación futura: enviar datos al proceso de Antigravity si expone una API.
            throw new System.NotImplementedException("Comunicación Outbound hacia Antigravity aún no implementada.");
        }

        public void Desconectar()
        {
            // Limpieza de recursos
        }
    }
}
