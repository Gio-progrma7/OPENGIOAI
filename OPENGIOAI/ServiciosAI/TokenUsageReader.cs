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

                // ===============================
                // OLLAMA (local)
                //
                // Ollama no devuelve "usage" sino prompt_eval_count y eval_count
                // en el JSON final de streaming ({"done":true, ...}).
                // Estos valores son tokens reales del tokenizer del modelo.
                // ===============================
                case Servicios.Ollama:
                    {
                        // En streaming viene línea-por-línea; el caller debe pasar
                        // el objeto final (done:true) o el response ya agregado.
                        var pTok = root["prompt_eval_count"]?.Value<int>();
                        var cTok = root["eval_count"]?.Value<int>();

                        if (pTok == null && cTok == null) return result;

                        result.PromptTokens = pTok ?? 0;
                        result.CompletionTokens = cTok ?? 0;
                        result.TotalTokens = result.PromptTokens + result.CompletionTokens;
                        result.Disponible = true;
                        break;
                    }
            }

            return result;
        }

        // ============================================================
        //  OLLAMA helper: el /api/generate devuelve múltiples líneas JSON
        //  (streaming). La que trae los counters es la última (done:true).
        //  Este helper extrae ese último JSON para que LeerConsumo lo parsee.
        // ============================================================
        public static string ExtraerUltimaLineaJson(string rawStream)
        {
            if (string.IsNullOrWhiteSpace(rawStream)) return "";

            string? ultima = null;
            foreach (var linea in rawStream.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string t = linea.Trim();
                if (t.StartsWith("{") && t.EndsWith("}")) ultima = t;
            }
            return ultima ?? "";
        }
    }
}
