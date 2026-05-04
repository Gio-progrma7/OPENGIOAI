using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosAI
{
    /// <summary>
    /// Gestiona autenticación para Antigravity (Google Vertex AI).
    ///
    /// Tres modos en orden de prioridad:
    ///   1. Service Account JSON  — sin interacción del usuario, ideal para producción
    ///   2. OAuth 2.0 PKCE        — login con cuenta Google en el browser, sin gcloud CLI
    ///   3. gcloud ADC            — fallback: requiere Google Cloud SDK instalado
    /// </summary>
    public static class AntigravityOAuthService
    {
        // ── Rutas en disco ────────────────────────────────────────────────────────
        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "antigravity_oauth.json");

        private static readonly string TokenStorePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "oauth_tokens");

        // ── Estado en memoria ─────────────────────────────────────────────────────
        private static UserCredential?    _oauthCred;
        private static GoogleCredential?  _svcCred;
        private static AntigravityOAuthConfig? _config;

        // ── Config pública ────────────────────────────────────────────────────────
        public static AntigravityOAuthConfig Config => _config ??= LoadConfig();

        public static bool TieneOAuth =>
            !string.IsNullOrWhiteSpace(Config.ClientId) &&
            !string.IsNullOrWhiteSpace(Config.ClientSecret);

        public static bool TieneServiceAccount =>
            !string.IsNullOrWhiteSpace(Config.ServiceAccountPath) &&
            File.Exists(Config.ServiceAccountPath);

        // ── Token principal ───────────────────────────────────────────────────────

        /// <summary>Devuelve un token de acceso usando el método configurado.</summary>
        public static async Task<string> ObtenerTokenAsync()
        {
            if (TieneServiceAccount) return await TokenSvcAccount();
            if (TieneOAuth)          return await TokenOAuth(openBrowserIfNeeded: false);
            return await AIServicios.ObtenerTokenGcloudAsync();
        }

        // ── Conexión OAuth 2.0 ───────────────────────────────────────────────────

        /// <summary>
        /// Inicia el flujo Authorization Code Flow con PKCE.
        /// Abre el browser del sistema; espera el callback en localhost.
        /// Persiste el refresh_token en disco para sesiones futuras.
        /// </summary>
        public static async Task<(bool ok, string msg)> ConectarOAuthAsync(
            string clientId, string clientSecret, CancellationToken ct = default)
        {
            try
            {
                var secrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret };
                var store   = new FileDataStore(TokenStorePath, fullPath: true);

                // GoogleWebAuthorizationBroker maneja todo el flujo:
                //   abre browser → captura callback → obtiene tokens → persiste refresh_token
                _oauthCred = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    new[] { "https://www.googleapis.com/auth/cloud-platform" },
                    "user", ct, store);

                string token = await _oauthCred.GetAccessTokenForRequestAsync();
                if (string.IsNullOrWhiteSpace(token))
                    return (false, "No se obtuvo token tras autenticación");

                Config.ClientId     = clientId;
                Config.ClientSecret = clientSecret;
                Config.Modo         = "oauth";
                SaveConfig(Config);

                return (true, "Autenticado con OAuth 2.0 correctamente");
            }
            catch (OperationCanceledException) { return (false, "Cancelado por el usuario"); }
            catch (Exception ex)               { return (false, $"Error OAuth: {ex.Message}"); }
        }

        // ── Service Account ───────────────────────────────────────────────────────

        /// <summary>
        /// Carga credenciales desde un archivo Service Account JSON de GCP.
        /// No requiere intervención del usuario ni gcloud CLI.
        /// </summary>
        public static async Task<(bool ok, string msg)> ConectarServiceAccountAsync(string jsonPath)
        {
            try
            {
                if (!File.Exists(jsonPath))
                    return (false, "Archivo no encontrado");

                var cred = GoogleCredential
                    .FromFile(jsonPath)
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

                string token = await ((ITokenAccess)cred)
                    .GetAccessTokenForRequestAsync();

                if (string.IsNullOrWhiteSpace(token))
                    return (false, "El Service Account no devolvió un token válido");

                _svcCred                    = cred;
                Config.ServiceAccountPath   = jsonPath;
                Config.Modo                 = "service_account";
                SaveConfig(Config);

                return (true, $"Service Account autenticado: {Path.GetFileName(jsonPath)}");
            }
            catch (Exception ex) { return (false, $"Error Service Account: {ex.Message}"); }
        }

        // ── Revocar / desconectar ─────────────────────────────────────────────────

        public static async Task RevocarAsync()
        {
            try
            {
                if (_oauthCred != null)
                    await _oauthCred.RevokeTokenAsync(CancellationToken.None);
                _oauthCred = null;
                _svcCred   = null;

                if (Directory.Exists(TokenStorePath))
                    Directory.Delete(TokenStorePath, recursive: true);
            }
            catch { }

            Config.Modo = "gcloud";
            SaveConfig(Config);
        }

        // ── Diagnóstico ───────────────────────────────────────────────────────────

        public static async Task<(bool tokenOk, bool projectOk, string projectId, string msg, string modo)>
            DiagnosticarAsync()
        {
            string modo = TieneServiceAccount ? "service_account"
                        : TieneOAuth          ? "oauth"
                        :                       "gcloud";

            string token = await ObtenerTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                string hint = modo switch
                {
                    "oauth"           => "Haz clic en 'Conectar con Google'",
                    "service_account" => "Archivo Service Account inválido o sin permisos",
                    _                 => "Ejecuta: gcloud auth application-default login"
                };
                return (false, false, "", $"Sin credenciales · {hint}", modo);
            }

            string pid = await AIServicios.ObtenerProyectoGcloudAsync();
            if (string.IsNullOrWhiteSpace(pid))
                return (true, false, "", $"Token OK [{modo}] · Sin proyecto GCP configurado", modo);

            return (true, true, pid, $"Conectado [{modo}] · {pid}", modo);
        }

        // ── Internos ──────────────────────────────────────────────────────────────

        private static async Task<string> TokenOAuth(bool openBrowserIfNeeded = false)
        {
            // Intentar refrescar la credencial en memoria
            if (_oauthCred != null)
            {
                try { return await _oauthCred.GetAccessTokenForRequestAsync(); }
                catch { _oauthCred = null; }
            }

            // Intentar cargar tokens guardados en disco (no abre browser si existen)
            if (!string.IsNullOrWhiteSpace(Config.ClientId))
            {
                try
                {
                    var secrets = new ClientSecrets
                        { ClientId = Config.ClientId, ClientSecret = Config.ClientSecret };
                    var store = new FileDataStore(TokenStorePath, fullPath: true);

                    // Si ya hay refresh_token almacenado, AuthorizeAsync lo usa sin browser
                    _oauthCred = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { "https://www.googleapis.com/auth/cloud-platform" },
                        "user", CancellationToken.None, store);

                    return await _oauthCred.GetAccessTokenForRequestAsync();
                }
                catch { return ""; }
            }

            return "";
        }

        private static async Task<string> TokenSvcAccount()
        {
            if (_svcCred == null && TieneServiceAccount)
            {
                try
                {
                    _svcCred = GoogleCredential
                        .FromFile(Config.ServiceAccountPath)
                        .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                }
                catch { return ""; }
            }

            if (_svcCred == null) return "";

            try
            {
                return await ((ITokenAccess)_svcCred)
                    .GetAccessTokenForRequestAsync();
            }
            catch { return ""; }
        }

        // ── Persistencia de configuración ─────────────────────────────────────────

        private static AntigravityOAuthConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    return JsonConvert.DeserializeObject<AntigravityOAuthConfig>(
                               File.ReadAllText(ConfigPath))
                           ?? new AntigravityOAuthConfig();
            }
            catch { }
            return new AntigravityOAuthConfig();
        }

        public static void SaveConfig(AntigravityOAuthConfig cfg)
        {
            try
            {
                _config = cfg;
                File.WriteAllText(ConfigPath,
                    JsonConvert.SerializeObject(cfg, Formatting.Indented));
            }
            catch { }
        }
    }

    // ── Entidad de configuración ──────────────────────────────────────────────────

    public class AntigravityOAuthConfig
    {
        /// <summary>OAuth 2.0 Client ID (de Google Cloud Console → Credenciales).</summary>
        public string ClientId { get; set; } = "";

        /// <summary>OAuth 2.0 Client Secret correspondiente al Client ID.</summary>
        public string ClientSecret { get; set; } = "";

        /// <summary>Ruta al archivo JSON de Service Account de GCP.</summary>
        public string ServiceAccountPath { get; set; } = "";

        /// <summary>Modo activo: "oauth" | "service_account" | "gcloud".</summary>
        public string Modo { get; set; } = "gcloud";
    }
}
