using System.Collections.Generic;
using System.Threading.Tasks;
using OPENGIOAI.Agentes;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System.Threading.Tasks;

namespace OPENGIOAI.Modulos.Arnes
{
    /// <summary>
    /// Enrutador principal del módulo ARNES.
    /// Conecta el Servidor Local con los Plugs (adaptadores) registrados.
    /// </summary>
    public class ArnesRouter
    {
        private readonly ArnesServer _server;
        private readonly Dictionary<string, IArnesPlug> _plugsActivos = new(StringComparer.OrdinalIgnoreCase);

        public event Action<string>? OnLog;

        public ArnesRouter(ArnesServer server)
        {
            _server = server;
            _server.OnPeticionRecibida += RutearPeticionAsync;
            _server.OnLog += (msg) => OnLog?.Invoke(msg);
        }

        /// <summary>
        /// Registra un nuevo adaptador (Plug) en el enrutador.
        /// </summary>
        public void RegistrarPlug(IArnesPlug plug)
        {
            if (!_plugsActivos.ContainsKey(plug.Nombre))
            {
                _plugsActivos.Add(plug.Nombre, plug);
                OnLog?.Invoke($"[ARNES ROUTER] Plug registrado: {plug.Nombre}");
            }
        }

        /// <summary>
        /// Método principal que despacha la petición entrante al Plug correspondiente.
        /// </summary>
        private async Task<ArnesPayload> RutearPeticionAsync(ArnesPayload peticion)
        {
            // 1. Validar el origen de la petición (SourceApp)
            string source = peticion.SourceApp;
            
            if (string.IsNullOrWhiteSpace(source))
            {
                return GenerarError("Petición sin 'SourceApp' definido.");
            }

            // 2. Buscar si hay un Plug registrado para esa aplicación
            if (_plugsActivos.TryGetValue(source, out var plug))
            {
                try
                {
                    OnLog?.Invoke($"[ARNES ROUTER] Despachando acción '{peticion.Action}' al plug '{plug.Nombre}'...");
                    
                    // Se delega el procesamiento al Plug específico
                    // Usamos CancellationToken.None por ahora, pero en el futuro se puede atar a un timeout.
                    var respuesta = await plug.ProcesarPeticionAsync(peticion, System.Threading.CancellationToken.None);
                    return respuesta;
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[ARNES ROUTER] Error en Plug '{plug.Nombre}': {ex.Message}");
                    return GenerarError($"Error interno en el Plug {plug.Nombre}: {ex.Message}");
                }
            }
            else
            {
                // 3. Fallback: Si no hay un plug específico, despachar a la lógica genérica
                // Esto permite que aplicaciones nuevas (como extensiones) puedan usar
                // execute_aria sin tener un plug registrado en el código C#.
                OnLog?.Invoke($"[ARNES ROUTER] Plug no registrado para '{source}'. Usando manejador genérico.");
                return await ProcesarPeticionGenericaAsync(peticion);
            }
        }

        /// <summary>
        /// Procesa peticiones que no van a un Plug específico, sino directamente
        /// a funcionalidades core de OPENGIOAI (Ej: ejecutar una instrucción).
        /// </summary>
        private async Task<ArnesPayload> ProcesarPeticionGenericaAsync(ArnesPayload peticion)
        {
            // Ejemplo de manejo genérico de acciones
            switch (peticion.Action.ToLower())
            {
                case "ping":
                    return new ArnesPayload
                    {
                        SourceApp = "opengioai_arnes",
                        Status = "success",
                        Message = "Pong! El servidor ARNES está funcionando."
                    };

                case "execute_aria":
                    // Despachar al OrquestadorARIA en modo Headless
                    string instruction = peticion.Parameters.ContainsKey("instruction") 
                        ? peticion.Parameters["instruction"].ToString() ?? ""
                        : "";

                    if (string.IsNullOrWhiteSpace(instruction))
                    {
                        return GenerarError("La acción 'execute_aria' requiere el parámetro 'instruction'.");
                    }

                    try
                    {
                        var config = Utils.LeerConfig<ConfiguracionClient>(RutasProyecto.ObtenerRutaConfiguracion()) ?? new ConfiguracionClient();
                        var apis = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis()) ?? new List<Api>();

                        var aria = new OrquestadorARIA(
                            config.Mimodelo?.Modelos ?? "",
                            config.MiArchivo?.Ruta ?? RutasProyecto.ObtenerRutaScripts(),
                            config.Mimodelo?.ApiKey ?? "",
                            Utils.ObtenerNombresApis(apis),
                            soloChat: true, 
                            config.Mimodelo?.Agente ?? Servicios.Gemenni,
                            maxReintentos: 3);

                        // La ejecución puede tardar un poco, el cliente debe soportar la latencia
                        string result = await aria.EjecutarAsync(instruction, System.Threading.CancellationToken.None);

                        return new ArnesPayload
                        {
                            SourceApp = "opengioai_arnes",
                            Status = "success",
                            Message = result
                        };
                    }
                    catch (Exception ex)
                    {
                        return GenerarError($"Error ejecutando ARIA de forma genérica: {ex.Message}");
                    }

                default:
                    return GenerarError($"Acción genérica no soportada: {peticion.Action}");
            }
        }

        private ArnesPayload GenerarError(string mensaje)
        {
            return new ArnesPayload
            {
                SourceApp = "opengioai_arnes",
                Status = "error",
                Message = mensaje
            };
        }
    }
}
