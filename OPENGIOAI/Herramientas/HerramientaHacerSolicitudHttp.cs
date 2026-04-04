using Newtonsoft.Json.Linq;
using System.Text;

namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Herramienta: hacer_solicitud_http
    /// Permite al agente consumir APIs REST externas directamente.
    /// </summary>
    public class HerramientaHacerSolicitudHttp : IHerramienta
    {
        // Singleton para evitar socket exhaustion
        private static readonly HttpClient _cliente = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public string Nombre => "hacer_solicitud_http";

        public string Descripcion =>
            "Hace una solicitud HTTP (GET, POST, PUT, DELETE, PATCH) a cualquier URL. " +
            "Úsala para consultar APIs REST, obtener datos de internet, " +
            "enviar formularios, llamar webhooks, etc. " +
            "Devuelve el código de estado HTTP y el cuerpo de la respuesta.";

        public JObject EsquemaParametros => JObject.Parse("""
            {
              "type": "object",
              "properties": {
                "url": {
                  "type": "string",
                  "description": "URL completa de la solicitud, incluyendo query params si aplica."
                },
                "metodo": {
                  "type": "string",
                  "enum": ["GET", "POST", "PUT", "DELETE", "PATCH"],
                  "description": "Método HTTP. Por defecto GET."
                },
                "cuerpo": {
                  "type": "string",
                  "description": "Cuerpo de la solicitud en texto plano o JSON (para POST/PUT/PATCH)."
                },
                "content_type": {
                  "type": "string",
                  "description": "Content-Type del cuerpo. Por defecto 'application/json'."
                },
                "encabezados": {
                  "type": "object",
                  "description": "Cabeceras HTTP adicionales como pares clave-valor.",
                  "additionalProperties": { "type": "string" }
                }
              },
              "required": ["url"]
            }
            """);

        public async Task<string> EjecutarAsync(JObject parametros, CancellationToken ct = default)
        {
            string url = parametros["url"]?.ToString() ?? "";
            string metodo = (parametros["metodo"]?.ToString() ?? "GET").ToUpperInvariant();
            string cuerpo = parametros["cuerpo"]?.ToString() ?? "";
            string contentType = parametros["content_type"]?.ToString() ?? "application/json";
            var encabezados = parametros["encabezados"] as JObject;

            if (string.IsNullOrWhiteSpace(url))
                return "Error: Se requiere el parámetro 'url'.";

            try
            {
                var httpMethod = metodo switch
                {
                    "POST" => HttpMethod.Post,
                    "PUT" => HttpMethod.Put,
                    "DELETE" => HttpMethod.Delete,
                    "PATCH" => HttpMethod.Patch,
                    _ => HttpMethod.Get
                };

                using var req = new HttpRequestMessage(httpMethod, url);

                if (encabezados != null)
                    foreach (var h in encabezados)
                        req.Headers.TryAddWithoutValidation(h.Key, h.Value?.ToString() ?? "");

                if (!string.IsNullOrWhiteSpace(cuerpo))
                    req.Content = new StringContent(cuerpo, Encoding.UTF8, contentType);

                using var resp = await _cliente.SendAsync(req, ct);
                string contenidoResp = await resp.Content.ReadAsStringAsync(ct);

                // Intentar formatear JSON si la respuesta lo es
                string respuestaFormateada = contenidoResp;
                try
                {
                    var jsonToken = JToken.Parse(contenidoResp);
                    respuestaFormateada = jsonToken.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                catch { /* No es JSON, usar como texto */ }

                return $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n" +
                       $"Content-Type: {resp.Content.Headers.ContentType}\n" +
                       $"---\n{respuestaFormateada}";
            }
            catch (TaskCanceledException)
            {
                return $"Error: La solicitud a '{url}' superó el tiempo límite (30s).";
            }
            catch (Exception ex)
            {
                return $"Error en solicitud HTTP a '{url}': {ex.Message}";
            }
        }
    }
}
