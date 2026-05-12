using System;
using System.Collections.Generic;
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
                // 3. Fallback: Si no hay un plug específico, podemos intentar un manejo "Genérico"
                // Por ejemplo, peticiones directas al motor ARIA de OPENGIOAI.
                if (source.ToLower() == "generico" || source.ToLower() == "test")
                {
                    return await ProcesarPeticionGenericaAsync(peticion);
                }

                OnLog?.Invoke($"[ARNES ROUTER] No se encontró un Plug registrado para '{source}'.");
                return GenerarError($"Aplicación origen '{source}' no soportada o no registrada en el servidor ARNES.");
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
                    // Aquí iría el puente con el OrquestadorARIA de la capa superior.
                    // Para mantener la separación de responsabilidades, esta clase
                    // podría disparar un evento para que FrmMandos u otro coordinador lo ejecute.
                    return new ArnesPayload
                    {
                        SourceApp = "opengioai_arnes",
                        Status = "success",
                        Message = "Funcionalidad 'execute_aria' reservada para implementación en OPENGIOAI."
                    };

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
