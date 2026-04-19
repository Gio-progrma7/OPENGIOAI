// ============================================================
//  EmbeddingsService.cs  — Fase C
//
//  Cliente HTTP para calcular embeddings. Dos proveedores:
//
//    · Ollama (local)
//        POST http://localhost:11434/api/embeddings
//        { "model": "nomic-embed-text", "prompt": "<text>" }
//        → { "embedding": [..768 floats..] }
//
//    · OpenAI
//        POST https://api.openai.com/v1/embeddings
//        { "model": "text-embedding-3-small", "input": ["<text>", ...] }
//        → { "data": [{"embedding": [..1536 floats..]}, ...], "usage": {...} }
//
//  DISEÑO:
//    · HttpClient singleton por proveedor (como AIModelConector).
//    · Método único EmbedAsync(texto) → float[]
//    · Método batch EmbedManyAsync(textos) → List<float[]> (más eficiente,
//      solo OpenAI lo soporta nativamente; Ollama hace loop interno).
//    · Telemetría: integra con ConsumoTokensTracker si la config tiene
//      un modelo reconocido por PreciosModelos.
//    · Errores encapsulados — ninguna excepción se propaga al caller
//      hot-path; devuelve vector vacío y loguea.
// ============================================================

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OPENGIOAI.Entidades;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPENGIOAI.ServiciosAI
{
    public static class EmbeddingsService
    {
        // ──────────── HttpClient singletons por proveedor ────────────

        private static readonly HttpClient _clienteOllama = new()
        {
            Timeout = TimeSpan.FromMinutes(2),
        };

        private static readonly HttpClient _clienteOpenAI = new()
        {
            Timeout = TimeSpan.FromMinutes(2),
        };

        // ──────────── Config singleton ────────────
        //
        // La config se lee/escribe desde FrmEmbeddings y se mantiene en memoria
        // para que cada EmbedAsync no toque disco. ConsumoTokensTracker ya tiene
        // este mismo patrón.

        private static EmbeddingConfig? _config;
        private static readonly object _lockCfg = new();

        public static EmbeddingConfig CargarConfig()
        {
            lock (_lockCfg)
            {
                if (_config != null) return _config.Clone();
                _config = LeerConfigDeDisco();
                return _config.Clone();
            }
        }

        public static void GuardarConfig(EmbeddingConfig cfg)
        {
            lock (_lockCfg)
            {
                _config = cfg.Clone();
                try
                {
                    string path = RutasProyecto.ObtenerRutaEmbeddingsConfig();
                    string json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
                    System.IO.File.WriteAllText(path, json);
                }
                catch
                {
                    // Si falla el guardado, el valor en memoria sigue válido
                    // para esta sesión.
                }
            }
        }

        private static EmbeddingConfig LeerConfigDeDisco()
        {
            try
            {
                string path = RutasProyecto.ObtenerRutaEmbeddingsConfig();
                if (!System.IO.File.Exists(path))
                {
                    // Defaults: Ollama local. Gratis, sin api key.
                    var def = new EmbeddingConfig();
                    return def;
                }
                string json = System.IO.File.ReadAllText(path);
                var cfg = JsonConvert.DeserializeObject<EmbeddingConfig>(json);
                return cfg ?? new EmbeddingConfig();
            }
            catch
            {
                return new EmbeddingConfig();
            }
        }

        // ──────────── API pública ────────────

        /// <summary>
        /// Calcula el embedding de un texto. Devuelve float[] vacío si falla.
        /// </summary>
        public static async Task<float[]> EmbedAsync(string texto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Array.Empty<float>();

            var cfg = CargarConfig();

            try
            {
                return cfg.Proveedor switch
                {
                    ProveedorEmbedding.Ollama => await EmbedOllamaAsync(texto, cfg, ct),
                    ProveedorEmbedding.OpenAI => (await EmbedOpenAIBatchAsync(new[] { texto }, cfg, ct)) switch
                    {
                        { Count: > 0 } list => list[0],
                        _                   => Array.Empty<float>(),
                    },
                    _ => Array.Empty<float>(),
                };
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        /// <summary>
        /// Batch — más eficiente en OpenAI (una sola llamada HTTP).
        /// En Ollama hace loop secuencial internamente.
        /// </summary>
        public static async Task<List<float[]>> EmbedManyAsync(
            IEnumerable<string> textos, CancellationToken ct = default)
        {
            var cfg = CargarConfig();
            var lista = textos is IList<string> il ? il : new List<string>(textos);
            var resultado = new List<float[]>(lista.Count);

            if (cfg.Proveedor == ProveedorEmbedding.OpenAI)
            {
                // OpenAI permite hasta 2048 strings por request. Chunk de 128
                // para no disparar timeouts ni payloads gigantes.
                const int tamLote = 128;
                for (int i = 0; i < lista.Count; i += tamLote)
                {
                    ct.ThrowIfCancellationRequested();
                    var slice = new List<string>();
                    for (int j = i; j < Math.Min(i + tamLote, lista.Count); j++)
                        slice.Add(lista[j]);
                    try
                    {
                        var vectores = await EmbedOpenAIBatchAsync(slice, cfg, ct);
                        resultado.AddRange(vectores);
                    }
                    catch
                    {
                        // Si falla el lote, rellenar con vacíos para mantener el orden.
                        for (int k = 0; k < slice.Count; k++)
                            resultado.Add(Array.Empty<float>());
                    }
                }
            }
            else
            {
                // Ollama: secuencial. Las llamadas son rápidas en local
                // y paralelizar sobrecarga la CPU del usuario.
                foreach (var t in lista)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        resultado.Add(await EmbedOllamaAsync(t, cfg, ct));
                    }
                    catch
                    {
                        resultado.Add(Array.Empty<float>());
                    }
                }
            }

            return resultado;
        }

        /// <summary>
        /// Ping para verificar que el proveedor configurado responde.
        /// Devuelve (ok, mensaje, dimensionReal).
        /// </summary>
        public static async Task<(bool ok, string mensaje, int dim)> ProbarConexionAsync(
            EmbeddingConfig cfg, CancellationToken ct = default)
        {
            try
            {
                float[] v = cfg.Proveedor == ProveedorEmbedding.Ollama
                    ? await EmbedOllamaAsync("hola", cfg, ct)
                    : (await EmbedOpenAIBatchAsync(new[] { "hola" }, cfg, ct)) switch
                    {
                        { Count: > 0 } l => l[0],
                        _                => Array.Empty<float>(),
                    };

                if (v.Length == 0)
                    return (false, "El proveedor respondió pero no devolvió vector.", 0);
                return (true, $"OK. Dimensión: {v.Length}", v.Length);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0);
            }
        }

        // ──────────── Implementaciones por proveedor ────────────

        private static async Task<float[]> EmbedOllamaAsync(
            string texto, EmbeddingConfig cfg, CancellationToken ct)
        {
            string baseUrl = string.IsNullOrWhiteSpace(cfg.EndpointUrl)
                ? "http://localhost:11434"
                : cfg.EndpointUrl.TrimEnd('/');

            var body = new
            {
                model  = string.IsNullOrWhiteSpace(cfg.Modelo) ? "nomic-embed-text" : cfg.Modelo,
                prompt = texto,
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/embeddings")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"),
            };

            using var resp = await _clienteOllama.SendAsync(req, ct);
            string raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return Array.Empty<float>();

            var root = JObject.Parse(raw);
            var arr = root["embedding"] as JArray;
            if (arr == null) return Array.Empty<float>();

            var v = new float[arr.Count];
            for (int i = 0; i < arr.Count; i++)
                v[i] = arr[i].Value<float>();
            return v;
        }

        private static async Task<List<float[]>> EmbedOpenAIBatchAsync(
            IList<string> textos, EmbeddingConfig cfg, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cfg.ApiKey))
                throw new InvalidOperationException(
                    "OpenAI requiere API key. Configúrala en el panel de Embeddings.");

            string baseUrl = string.IsNullOrWhiteSpace(cfg.EndpointUrl) ||
                             cfg.EndpointUrl.StartsWith("http://localhost")
                ? "https://api.openai.com"
                : cfg.EndpointUrl.TrimEnd('/');

            var body = new
            {
                model = string.IsNullOrWhiteSpace(cfg.Modelo) ? "text-embedding-3-small" : cfg.Modelo,
                input = textos,
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/embeddings")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);

            using var resp = await _clienteOpenAI.SendAsync(req, ct);
            string raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"OpenAI embeddings devolvió {(int)resp.StatusCode}: {Truncar(raw, 300)}");

            var root = JObject.Parse(raw);
            var data = root["data"] as JArray;
            if (data == null) return new List<float[]>();

            // Telemetría: OpenAI devuelve usage con tokens totales.
            try
            {
                int promptTok = root["usage"]?["prompt_tokens"]?.Value<int>() ?? 0;
                int totalTok  = root["usage"]?["total_tokens"]?.Value<int>()  ?? 0;
                if (totalTok > 0)
                {
                    var consumo = new ConsumoTokens
                    {
                        Proveedor       = "OpenAI",
                        Fase            = "Embedding",
                        Modelo          = cfg.Modelo,
                        PromptTokens    = promptTok,
                        CompletionTokens= 0,
                        TotalTokens     = totalTok,
                        Disponible      = true,
                    };
                    ConsumoTokensTracker.Instancia.Registrar(consumo);
                }
            }
            catch { }

            var salida = new List<float[]>(data.Count);
            foreach (var item in data)
            {
                var arr = item["embedding"] as JArray;
                if (arr == null) { salida.Add(Array.Empty<float>()); continue; }

                var v = new float[arr.Count];
                for (int i = 0; i < arr.Count; i++)
                    v[i] = arr[i].Value<float>();
                salida.Add(v);
            }
            return salida;
        }

        private static string Truncar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
