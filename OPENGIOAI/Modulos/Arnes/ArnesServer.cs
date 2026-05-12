using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.Modulos.Arnes
{
    /// <summary>
    /// Servidor local ultra-ligero embebido en OPENGIOAI para escuchar
    /// peticiones entrantes de aplicaciones externas (Arnés).
    /// </summary>
    public class ArnesServer
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly int _puerto;
        private readonly string _tokenSeguridad;
        private bool _isRunning = false;

        public event Action<string>? OnLog;
        public event Func<ArnesPayload, Task<ArnesPayload>>? OnPeticionRecibida;

        public ArnesServer(int puerto = 5050, string tokenSeguridad = "openga_arnes_local")
        {
            _puerto = puerto;
            _tokenSeguridad = tokenSeguridad;
        }

        public void Iniciar()
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                // Permitir conexiones desde localhost
                _listener.Prefixes.Add($"http://localhost:{_puerto}/arnes/");
                _listener.Start();

                _cts = new CancellationTokenSource();
                _isRunning = true;

                OnLog?.Invoke($"[ARNES] Servidor iniciado en http://localhost:{_puerto}/arnes/");

                // Loop de escucha en hilo secundario
                _ = Task.Run(() => ListenLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[ARNES] Error al iniciar el servidor: {ex.Message}");
                _isRunning = false;
            }
        }

        public void Detener()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            _isRunning = false;

            OnLog?.Invoke("[ARNES] Servidor detenido.");
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcesarContextoAsync(context, ct));
                }
                catch (HttpListenerException)
                {
                    // Se lanza al detener el listener, es normal.
                    break;
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[ARNES] Error en ListenLoop: {ex.Message}");
                }
            }
        }

        private async Task ProcesarContextoAsync(HttpListenerContext context, CancellationToken ct)
        {
            var request = context.Request;
            var response = context.Response;

            // Configurar cabeceras CORS básicas (para web extensions)
            response.AppendHeader("Access-Control-Allow-Origin", "*");
            response.AppendHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.AppendHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            try
            {
                // Validación básica de autenticación
                var authHeader = request.Headers["Authorization"];
                if (authHeader != $"Bearer {_tokenSeguridad}")
                {
                    await EnviarRespuestaErrorAsync(response, 401, "No autorizado. Token inválido.");
                    return;
                }

                if (request.HttpMethod != "POST")
                {
                    await EnviarRespuestaErrorAsync(response, 405, "Método no permitido. Use POST.");
                    return;
                }

                // Leer cuerpo JSON
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
                string jsonBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(jsonBody))
                {
                    await EnviarRespuestaErrorAsync(response, 400, "El cuerpo de la petición está vacío.");
                    return;
                }

                ArnesPayload? payloadReq;
                try
                {
                    payloadReq = JsonConvert.DeserializeObject<ArnesPayload>(jsonBody);
                    if (payloadReq == null) throw new Exception("Payload nulo");
                }
                catch
                {
                    await EnviarRespuestaErrorAsync(response, 400, "JSON malformado o no coincide con ArnesPayload.");
                    return;
                }

                OnLog?.Invoke($"[ARNES] Petición recibida de: {payloadReq.SourceApp} - Acción: {payloadReq.Action}");

                // Despachar al Router / Manejador
                ArnesPayload payloadRes;
                if (OnPeticionRecibida != null)
                {
                    payloadRes = await OnPeticionRecibida.Invoke(payloadReq);
                }
                else
                {
                    payloadRes = new ArnesPayload
                    {
                        SourceApp = "opengioai_arnes",
                        Action = payloadReq.Action,
                        Status = "error",
                        Message = "El servidor está activo, pero no hay un enrutador (Router) configurado para manejar la petición."
                    };
                }

                // Enviar respuesta
                await EnviarRespuestaJsonAsync(response, 200, payloadRes);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[ARNES] Error procesando petición: {ex.Message}");
                await EnviarRespuestaErrorAsync(response, 500, "Error interno del servidor Arnes.");
            }
            finally
            {
                response.Close();
            }
        }

        private async Task EnviarRespuestaErrorAsync(HttpListenerResponse response, int statusCode, string mensaje)
        {
            var errorPayload = new ArnesPayload
            {
                SourceApp = "opengioai_arnes",
                Status = "error",
                Message = mensaje
            };
            await EnviarRespuestaJsonAsync(response, statusCode, errorPayload);
        }

        private async Task EnviarRespuestaJsonAsync(HttpListenerResponse response, int statusCode, ArnesPayload data)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
