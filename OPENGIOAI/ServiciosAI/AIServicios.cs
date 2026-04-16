using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json.Linq;
using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosAI
{
    public static class AIServicios
    {

        public static async Task<List<string>> ObtenerModelosOllamaApiAsync()
        {

            bool estado = await OllamaEstaActivoAsync();

            if(!estado) return new List<string>();

            using (var http = new HttpClient())
            {
                var response = await http.GetStringAsync("http://localhost:11434/api/tags");

                // Parsear usando Newtonsoft.Json
                var json = JObject.Parse(response);
                var modelos = new List<string>();

                foreach (var model in json["models"])
                {
                    modelos.Add(model["name"].ToString());
                }

                return modelos;
            }
        }

        public static async Task<List<string>> ObtenerModelosOpenAIAsync(string apiKey)
        {

            bool estado = await ApiKeyOpenAIValidaAsync(apiKey);

            if (!estado) return new List<string>();


            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await http.GetStringAsync("https://api.openai.com/v1/models");

                var json = JObject.Parse(response);
                var modelos = new List<string>();

                foreach (var model in json["data"])
                {
                    modelos.Add(model["id"].ToString());
                }

                return modelos;
            }
        }

        public static async Task<List<string>> ObtenerModelosClaudeAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return new List<string>();

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("x-api-key", apiKey);
                http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var response = await http.GetAsync("https://api.anthropic.com/v1/models");

                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                var modelos = new List<string>();

                foreach (var model in json["data"])
                {
                    modelos.Add(model["id"]!.ToString());
                }

                return modelos;
            }
        }

        public static async Task<List<string>> ObtenerModelosDeepSeekAsync(string apiKey)
        {
            bool estado = await ApiKeyDeepSeekValidaAsync(apiKey);
            if (!estado) return new List<string>();

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await http.GetStringAsync("https://api.deepseek.com/v1/models");

                var json = JObject.Parse(response);
                var modelos = new List<string>();

                foreach (var model in json["data"])
                {
                    modelos.Add(model["id"]!.ToString());
                }

                return modelos;
            }
        }


        public static async Task<List<string>> ObtenerModelosOpenRouterAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return new List<string>();

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();

                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                http.DefaultRequestHeaders.Add("HTTP-Referer", "https://tuapp.local");
                http.DefaultRequestHeaders.Add("X-Title", "OPENGIOAI");

                var response = await http.GetAsync("https://openrouter.ai/api/v1/models");

                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var jsonText = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonText);

                var modelos = new List<string>();

                foreach (var model in json["data"])
                {
                    modelos.Add(model["id"]?.ToString() ?? "");
                }

                return modelos;
            }
        }

        public static async Task<List<string>> ObtenerModelosGeminiAsync(string apiKey)
        {
            bool estado = await ApiKeyGeminiValidaAsync(apiKey);

            if (!estado) return new List<string>();

            using (var http = new HttpClient())
            {
                var response = await http.GetStringAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}"
                );

                var json = JObject.Parse(response);
                var modelos = new List<string>();

                foreach (var model in json["models"])
                {
                    string modelo = model["name"].ToString();

                    modelo = modelo.Replace("models/", "");


                    modelos.Add(modelo);
                }

                return modelos;
            }
        }

        public static async Task<List<ModeloAgente>> ObtenerVacios( List<ModeloAgente> ls)
        {
            return ls;
        }

        public static async Task<bool> ApiKeyDeepSeekValidaAsync(string apiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                    return false;

                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                    var response = await http.GetAsync("https://api.deepseek.com/v1/models");

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> ApiKeyOpenAIValidaAsync(string apiKey)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                    var response = await http.GetAsync("https://api.openai.com/v1/models");

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> ApiKeyOpenRouterValidaAsync(string apiKey)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Clear();

                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                    http.DefaultRequestHeaders.Add("HTTP-Referer", "https://tuapp.local");
                    http.DefaultRequestHeaders.Add("X-Title", "OPENGIOAI");

                    var response = await http.GetAsync("https://openrouter.ai/api/v1/models");

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }


        public static async Task<bool> ApiKeyGeminiValidaAsync(string apiKey)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetAsync(
                        $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}"
                    );

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> OllamaEstaActivoAsync()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(3);

                    var response = await http.GetAsync("http://localhost:11434/api/tags");

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> ApiKeyClaudeValidaAsync(string apiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                    return false;

                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Add("x-api-key", apiKey);
                    http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                    var response = await http.GetAsync("https://api.anthropic.com/v1/models");

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }


        // ── ANTIGRAVITY (Google Vertex AI — gcloud ADC) ───────────────────────

        /// <summary>
        /// Devuelve los modelos disponibles en Vertex AI para el proyecto GCP dado.
        /// projectId = ID del proyecto GCP (ej. "my-project-123").
        /// Si la consulta falla, devuelve una lista curada con los modelos más comunes.
        /// </summary>
        public static async Task<List<string>> ObtenerModelosAntigravityAsync(string projectId)
        {
            // Lista curada de modelos Vertex AI — visible aunque la API falle
            var fallback = new List<string>
            {
                "gemini-2.5-pro-preview-03-25",
                "gemini-2.0-flash-001",
                "gemini-2.0-flash-lite-001",
                "gemini-2.0-flash-exp",
                "gemini-1.5-pro-002",
                "gemini-1.5-flash-002",
                "gemini-1.5-flash-8b-001",
                "gemini-1.0-pro-002",
            };

            // Resolver Project ID: parámetro → auto-detección
            if (string.IsNullOrWhiteSpace(projectId))
                projectId = await ObtenerProyectoGcloudAsync();

            // Sin proyecto → sólo fallback (no hay URL que construir)
            if (string.IsNullOrWhiteSpace(projectId))
                return fallback;

            try
            {
                string token = await ObtenerTokenGcloudAsync();
                if (string.IsNullOrWhiteSpace(token))
                    return fallback;

                // ── Endpoint correcto para listar publisher models de Google ──────
                // v1beta1/publishers/google/models  (sin project en la ruta)
                // El proyecto se envía como cabecera de cuota para billing
                string region = "us-central1";
                string url = $"https://{region}-aiplatform.googleapis.com" +
                             $"/v1beta1/publishers/google/models";

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                // Cabecera requerida para imputar cuota y facturación al proyecto
                http.DefaultRequestHeaders.Add("x-goog-user-project", projectId);

                var resp = await http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                    return fallback;

                var jsonText = await resp.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonText);
                var modelos = new List<string>();

                // ── La respuesta usa "publisherModels", NO "models" ──────────────
                foreach (var m in json["publisherModels"] ?? new JArray())
                {
                    string name = m["name"]?.ToString() ?? "";
                    // "publishers/google/models/gemini-1.5-pro" → "gemini-1.5-pro"
                    int lastSlash = name.LastIndexOf('/');
                    if (lastSlash >= 0) name = name[(lastSlash + 1)..];

                    // Incluir sólo modelos con soporte de generación de contenido
                    bool puedeGenerar =
                        m["supportedActions"]?["generateContent"] != null ||
                        m["supportedActions"]?["serverStreamingGenerateContent"] != null;

                    if (!string.IsNullOrWhiteSpace(name) && puedeGenerar)
                        modelos.Add(name);
                }

                return modelos.Count > 0 ? modelos : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Comprueba si Antigravity (Vertex AI) está disponible.
        ///
        /// Si el projectId pasado no luce como un Project ID de GCP válido
        /// (p.ej. es una API key de otro servicio), intenta auto-detectarlo
        /// desde "gcloud config get-value project".
        ///
        /// La validación pasa si:
        ///   a) El token gcloud es válido   AND
        ///   b) Hay un Project ID utilizable (pasado o auto-detectado)
        /// </summary>
        public static async Task<bool> AntigravityEstaActivoAsync(string projectId)
        {
            try
            {
                // 1. Obtener token — si no hay token, el usuario no está autenticado
                string token = await ObtenerTokenGcloudAsync();
                if (string.IsNullOrWhiteSpace(token))
                    return false;

                // 2. Resolver Project ID: usar el pasado si parece válido,
                //    si no auto-detectar desde gcloud config
                string pid = projectId?.Trim() ?? "";
                if (!EsProjectIdGcpValido(pid))
                    pid = await ObtenerProyectoGcloudAsync();

                if (string.IsNullOrWhiteSpace(pid))
                    return false;   // autenticado pero sin proyecto configurado

                // 3. Probar conectividad con Vertex AI
                string region = "us-central1";
                string url = $"https://{region}-aiplatform.googleapis.com/v1/projects/{pid}" +
                             $"/locations/{region}/publishers/google/models/gemini-1.5-flash-002";

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var resp = await http.GetAsync(url);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Comprueba si un string tiene la forma básica de un GCP Project ID.
        /// Formato: letras minúsculas, dígitos y guiones; entre 6 y 30 caracteres;
        /// empieza y termina con letra o dígito.
        /// </summary>
        private static bool EsProjectIdGcpValido(string s) =>
            !string.IsNullOrWhiteSpace(s) &&
            s.Length >= 6 && s.Length <= 30 &&
            System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-z][a-z0-9\-]{4,28}[a-z0-9]$");

        /// <summary>
        /// Obtiene un token de acceso GCP usando Application Default Credentials (ADC).
        ///
        /// Estrategia (en orden de preferencia):
        ///  1. Google.Apis.Auth  — lee el archivo ADC directamente; NO requiere gcloud CLI.
        ///     Funciona si existe %APPDATA%\gcloud\application_default_credentials.json
        ///     (creado al ejecutar una sola vez: gcloud auth application-default login).
        ///  2. gcloud CLI        — fallback para entornos donde Google.Apis.Auth no puede
        ///     leer el archivo ADC pero sí está disponible el CLI.
        /// </summary>
        internal static async Task<string> ObtenerTokenGcloudAsync()
        {
            // ── Opción 1: Google.Apis.Auth (no requiere gcloud en runtime) ────────
            try
            {
                var credential = await GoogleCredential.GetApplicationDefaultAsync();
                // Scope requerido para Vertex AI / Cloud Platform
                credential = credential.CreateScoped(
                    "https://www.googleapis.com/auth/cloud-platform");
                string adcToken = await ((ITokenAccess)credential)
                    .GetAccessTokenForRequestAsync();
                if (!string.IsNullOrWhiteSpace(adcToken))
                    return adcToken;
            }
            catch { /* continuar con fallback */ }

            // ── Opción 2: gcloud CLI (fallback) ───────────────────────────────────
            try
            {
                string gcloudExe = EncontrarGcloudExe();
                string fileName  = "cmd.exe";
                string arguments = string.IsNullOrEmpty(gcloudExe)
                    ? "/c gcloud auth application-default print-access-token"
                    : $"/c \"{gcloudExe}\" auth application-default print-access-token";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = fileName,
                    Arguments              = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) return "";

                string token = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();
                return token.Trim();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Obtiene el GCP Project ID activo.
        ///
        /// Estrategia (en orden de preferencia):
        ///  1. Variables de entorno estándar de GCP (GOOGLE_CLOUD_PROJECT, etc.).
        ///  2. Archivo ADC (%APPDATA%\gcloud\application_default_credentials.json):
        ///       • "quota_project_id"  → credenciales de usuario (authorized_user)
        ///       • "project_id"        → cuenta de servicio (service_account)
        ///  3. gcloud CLI             → fallback si los anteriores no aplican.
        /// </summary>
        internal static async Task<string> ObtenerProyectoGcloudAsync()
        {
            // ── Opción 1: Variables de entorno ────────────────────────────────────
            foreach (string var in new[] { "GOOGLE_CLOUD_PROJECT", "GCLOUD_PROJECT", "GCP_PROJECT" })
            {
                string? v = Environment.GetEnvironmentVariable(var);
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            }

            // ── Opción 2: Leer directamente el archivo ADC ────────────────────────
            try
            {
                string adcPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "gcloud", "application_default_credentials.json");

                if (File.Exists(adcPath))
                {
                    var adcJson = JObject.Parse(await File.ReadAllTextAsync(adcPath));
                    // authorized_user → quota_project_id
                    string? qp = adcJson["quota_project_id"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(qp)) return qp.Trim();
                    // service_account → project_id
                    string? sp = adcJson["project_id"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(sp)) return sp.Trim();
                }
            }
            catch { /* continuar con fallback */ }

            // ── Opción 3: gcloud CLI ──────────────────────────────────────────────
            try
            {
                string gcloudExe = EncontrarGcloudExe();
                string fileName  = "cmd.exe";
                string arguments = string.IsNullOrEmpty(gcloudExe)
                    ? "/c gcloud config get-value project"
                    : $"/c \"{gcloudExe}\" config get-value project";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = fileName,
                    Arguments              = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) return "";

                string output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                string projectId = output.Trim();
                // gcloud puede devolver "(unset)" si no hay proyecto configurado
                if (projectId == "(unset)" || projectId.StartsWith("Your active configuration"))
                    return "";

                return projectId;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Busca el ejecutable gcloud.cmd en los directorios de instalación típicos
        /// de Google Cloud SDK en Windows.
        /// Retorna la ruta completa si la encuentra, o string vacío si no.
        /// </summary>
        internal static string EncontrarGcloudExe()
        {
            // ── Rutas de instalación típicas de Google Cloud SDK en Windows ──────
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string progFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string progFiles   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var candidatos = new[]
            {
                // Instalación por usuario (más común con el instalador oficial)
                Path.Combine(localApp,    "Google", "Cloud SDK", "google-cloud-sdk", "bin", "gcloud.cmd"),
                Path.Combine(localApp,    "google-cloud-sdk", "bin", "gcloud.cmd"),
                // Instalación global
                Path.Combine(progFiles86, "Google", "Cloud SDK", "google-cloud-sdk", "bin", "gcloud.cmd"),
                Path.Combine(progFiles,   "Google", "Cloud SDK", "google-cloud-sdk", "bin", "gcloud.cmd"),
                // Ruta alternativa con carpetas sin espacio
                Path.Combine(userProfile, "google-cloud-sdk", "bin", "gcloud.cmd"),
                Path.Combine(@"C:\tools", "google-cloud-sdk", "bin", "gcloud.cmd"),
            };

            foreach (var ruta in candidatos)
            {
                if (File.Exists(ruta))
                    return ruta;
            }

            // Último recurso: preguntar al propio cmd.exe dónde está gcloud
            try
            {
                var where = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = "cmd.exe",
                    Arguments              = "/c where gcloud",
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };
                using var proc = System.Diagnostics.Process.Start(where);
                if (proc != null)
                {
                    string resultado = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    // "where" puede devolver varias líneas; tomar la primera
                    string primera = resultado.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                              .FirstOrDefault()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(primera) && File.Exists(primera))
                        return primera;
                }
            }
            catch { /* ignorar */ }

            return string.Empty; // no encontrado
        }

        /// <summary>
        /// Diagnóstico rápido de la conexión Antigravity (Vertex AI).
        /// Devuelve una tupla que describe exactamente qué paso falla,
        /// para mostrar mensajes precisos en la UI sin swallow silencioso.
        /// </summary>
        /// <returns>
        ///   tokenOk    — true si se obtuvo un token ADC válido.
        ///   projectOk  — true si se detectó un GCP Project ID.
        ///   projectId  — el Project ID detectado (vacío si projectOk=false).
        ///   mensaje    — texto descriptivo del estado para mostrar en UI.
        /// </returns>
        public static async Task<(bool tokenOk, bool projectOk, string projectId, string mensaje)>
            DiagnosticarAntigravityAsync()
        {
            // 1. ¿Hay credenciales ADC?
            string token = await ObtenerTokenGcloudAsync();
            if (string.IsNullOrWhiteSpace(token))
                return (false, false, "",
                    "Sin credenciales — ejecuta: gcloud auth application-default login");

            // 2. ¿Hay un Project ID configurado?
            string projectId = await ObtenerProyectoGcloudAsync();
            if (string.IsNullOrWhiteSpace(projectId))
                return (true, false, "",
                    "Token OK · Sin proyecto — ejecuta: gcloud config set project TU-PROYECTO");

            return (true, true, projectId, $"Conectado · {projectId}");
        }

        public static async Task MostrarConsumoTokens(Servicios servicio, string result)
        {

            var consumo = TokenUsageReader.LeerConsumo(result, servicio);

            if (consumo.Disponible)
            {
                Program.ComsumoTokens = $"Consumo provedor : [{consumo.Proveedor}] Tokens usados: {consumo.TotalTokens}";
            }
            else
            {
                Program.ComsumoTokens = $"[{consumo.Proveedor}] No reporta tokens.";
            }
        }
    }
}
