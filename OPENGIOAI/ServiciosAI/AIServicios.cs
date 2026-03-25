using Newtonsoft.Json.Linq;
using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
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
