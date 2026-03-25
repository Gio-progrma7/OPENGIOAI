using Newtonsoft.Json.Linq;
using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosAI
{
    public static class TokenUsageReader
    {
        /// <summary>
        /// Analiza la respuesta JSON de un servicio de IA para extraer el consumo de tokens utilizado en la petición.
        /// Soporta múltiples proveedores de modelos de lenguaje, interpretando las estructuras de uso de tokens según cada servicio.
        /// Retorna un objeto con el detalle de tokens de prompt, completados y totales, además de indicar si la información fue obtenida correctamente.
        /// </summary>
        public static ConsumoTokens LeerConsumo(string jsonRespuesta, Servicios servicio)
        {
            var result = new ConsumoTokens
            {
                Disponible = false,
                Proveedor = servicio.ToString()
            };

            if (string.IsNullOrWhiteSpace(jsonRespuesta))
                return result;

            var root = JObject.Parse(jsonRespuesta);

            switch (servicio)
            {
                // ===============================
                // OPENAI / CHATGPT
                // ===============================
                case Servicios.ChatGpt:
                case Servicios.OpenRouter:
                case Servicios.Deespeek:
                    {
                        var usage = root["usage"];
                        if (usage == null) return result;

                        result.PromptTokens = usage["prompt_tokens"]?.Value<int>() ?? 0;
                        result.CompletionTokens = usage["completion_tokens"]?.Value<int>() ?? 0;
                        result.TotalTokens = usage["total_tokens"]?.Value<int>() ?? 0;
                        result.Disponible = true;
                        break;
                    }

                // ===============================
                // ANTHROPIC / CLAUDE
                // ===============================
                case Servicios.Claude:
                    {
                        var usage = root["usage"];
                        if (usage == null) return result;

                        result.PromptTokens = usage["input_tokens"]?.Value<int>() ?? 0;
                        result.CompletionTokens = usage["output_tokens"]?.Value<int>() ?? 0;
                        result.TotalTokens = result.PromptTokens + result.CompletionTokens;
                        result.Disponible = true;
                        break;
                    }

                // ===============================
                // GEMINI
                // ===============================
                case Servicios.Gemenni:
                    {
                        var usage = root["usageMetadata"];
                        if (usage == null) return result;

                        result.PromptTokens = usage["promptTokenCount"]?.Value<int>() ?? 0;
                        result.CompletionTokens = usage["candidatesTokenCount"]?.Value<int>() ?? 0;
                        result.TotalTokens = usage["totalTokenCount"]?.Value<int>() ?? 0;
                        result.Disponible = true;
                        break;
                    }
            }

            return result;
        }
    }
}
