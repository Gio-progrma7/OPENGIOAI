// ============================================================
//  PreciosModelos.cs
//
//  Registry de tarifas de LLMs para estimar costo por llamada.
//  Mismo patrón que HabilidadesRegistry: defaults desde código,
//  overrides desde JSON editable.
//
//  USO:
//    decimal usd = PreciosModelos.Instancia.Estimar(
//        modelo: "gpt-4o-mini", in: 1200, out: 85);
//
//  PRECIOS:
//    Los defaults reflejan precios públicos conocidos al momento
//    de la Fase A (telemetría). Pueden cambiar — el usuario edita
//    {AppDir}/ListPreciosModelos.json cuando lo necesite.
//    Si un modelo no está registrado, Estimar() devuelve 0
//    (modelo local como Ollama o modelo nuevo) sin reventar nada.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using OPENGIOAI.Entidades;

namespace OPENGIOAI.Utilerias
{
    public sealed class PreciosModelos
    {
        private static readonly PreciosModelos _instancia = new();
        public static PreciosModelos Instancia => _instancia;

        private readonly object _lock = new();
        private Dictionary<string, PrecioModelo> _cache =
            new(StringComparer.OrdinalIgnoreCase);
        private bool _cargado;

        private PreciosModelos() { }

        // ══════════════════ Defaults ══════════════════

        /// <summary>
        /// Precios públicos conocidos (USD por 1M tokens).
        /// Mantener ordenado por proveedor para facilitar lectura.
        /// </summary>
        private static IEnumerable<PrecioModelo> Defaults() => new[]
        {
            // ── OpenAI ────────────────────────────────────────────
            // Prompt caching automático (50% descuento sobre input).
            new PrecioModelo { Modelo = "gpt-4o",         Proveedor = "ChatGpt",    PrecioInputPorMillon = 2.50m,  PrecioOutputPorMillon = 10.00m, PrecioCacheReadPorMillon = 1.25m  },
            new PrecioModelo { Modelo = "gpt-4o-mini",    Proveedor = "ChatGpt",    PrecioInputPorMillon = 0.15m,  PrecioOutputPorMillon = 0.60m,  PrecioCacheReadPorMillon = 0.075m },
            new PrecioModelo { Modelo = "gpt-4-turbo",    Proveedor = "ChatGpt",    PrecioInputPorMillon = 10.00m, PrecioOutputPorMillon = 30.00m },
            new PrecioModelo { Modelo = "gpt-3.5-turbo",  Proveedor = "ChatGpt",    PrecioInputPorMillon = 0.50m,  PrecioOutputPorMillon = 1.50m  },

            // ── Anthropic ─────────────────────────────────────────
            // Prompt caching explícito: read = 0.10×input, creation = 1.25×input.
            new PrecioModelo { Modelo = "claude-3-5-sonnet-20241022", Proveedor = "Claude", PrecioInputPorMillon = 3.00m,  PrecioOutputPorMillon = 15.00m, PrecioCacheReadPorMillon = 0.30m, PrecioCacheCreationPorMillon = 3.75m  },
            new PrecioModelo { Modelo = "claude-3-5-sonnet-latest",   Proveedor = "Claude", PrecioInputPorMillon = 3.00m,  PrecioOutputPorMillon = 15.00m, PrecioCacheReadPorMillon = 0.30m, PrecioCacheCreationPorMillon = 3.75m  },
            new PrecioModelo { Modelo = "claude-3-5-haiku-latest",    Proveedor = "Claude", PrecioInputPorMillon = 0.80m,  PrecioOutputPorMillon = 4.00m,  PrecioCacheReadPorMillon = 0.08m, PrecioCacheCreationPorMillon = 1.00m  },
            new PrecioModelo { Modelo = "claude-3-opus-latest",       Proveedor = "Claude", PrecioInputPorMillon = 15.00m, PrecioOutputPorMillon = 75.00m, PrecioCacheReadPorMillon = 1.50m, PrecioCacheCreationPorMillon = 18.75m },
            new PrecioModelo { Modelo = "claude-sonnet-4-5",          Proveedor = "Claude", PrecioInputPorMillon = 3.00m,  PrecioOutputPorMillon = 15.00m, PrecioCacheReadPorMillon = 0.30m, PrecioCacheCreationPorMillon = 3.75m  },
            new PrecioModelo { Modelo = "claude-opus-4-5",            Proveedor = "Claude", PrecioInputPorMillon = 15.00m, PrecioOutputPorMillon = 75.00m, PrecioCacheReadPorMillon = 1.50m, PrecioCacheCreationPorMillon = 18.75m },

            // ── Google Gemini ─────────────────────────────────────
            new PrecioModelo { Modelo = "gemini-1.5-pro",    Proveedor = "Gemenni", PrecioInputPorMillon = 1.25m,  PrecioOutputPorMillon = 5.00m  },
            new PrecioModelo { Modelo = "gemini-1.5-flash",  Proveedor = "Gemenni", PrecioInputPorMillon = 0.075m, PrecioOutputPorMillon = 0.30m  },
            new PrecioModelo { Modelo = "gemini-2.0-flash",  Proveedor = "Gemenni", PrecioInputPorMillon = 0.10m,  PrecioOutputPorMillon = 0.40m  },

            // ── DeepSeek ──────────────────────────────────────────
            // Prompt caching automático (10% sobre input).
            new PrecioModelo { Modelo = "deepseek-chat",     Proveedor = "Deespeek", PrecioInputPorMillon = 0.27m, PrecioOutputPorMillon = 1.10m, PrecioCacheReadPorMillon = 0.027m },
            new PrecioModelo { Modelo = "deepseek-reasoner", Proveedor = "Deespeek", PrecioInputPorMillon = 0.55m, PrecioOutputPorMillon = 2.19m, PrecioCacheReadPorMillon = 0.14m  },

            // ── Embeddings (Fase C) ───────────────────────────────
            // Solo cobran el input — no hay output generativo.
            new PrecioModelo { Modelo = "text-embedding-3-small", Proveedor = "ChatGpt", PrecioInputPorMillon = 0.02m, PrecioOutputPorMillon = 0m },
            new PrecioModelo { Modelo = "text-embedding-3-large", Proveedor = "ChatGpt", PrecioInputPorMillon = 0.13m, PrecioOutputPorMillon = 0m },
            new PrecioModelo { Modelo = "text-embedding-ada-002", Proveedor = "ChatGpt", PrecioInputPorMillon = 0.10m, PrecioOutputPorMillon = 0m },

            // Ollama y otros locales: sin precio (costo 0).
            // nomic-embed-text, mxbai-embed-large, etc. → gratis.
        };

        // ══════════════════ API pública ══════════════════

        /// <summary>
        /// Estima el costo en USD para un modelo dado y un consumo (in/out).
        /// Devuelve 0 si el modelo no está registrado (sin error).
        /// Sobrecarga legacy — usa 0 tokens cacheados.
        /// </summary>
        public decimal Estimar(string modelo, int tokensEntrada, int tokensSalida)
        {
            return Estimar(modelo, tokensEntrada, tokensSalida,
                           cacheReadTokens: 0, cacheCreationTokens: 0);
        }

        /// <summary>
        /// Estima el costo en USD contemplando tarifas diferenciadas de
        /// prompt caching. Los tokens de caché se asumen DESGLOSADOS del
        /// total de entrada (es decir, PromptTokens incluye los cacheados).
        ///
        /// Cálculo:
        ///   · input "fresco"   = PromptTokens − cacheRead − cacheCreation
        ///   · cache read       = tarifa PrecioCacheReadPorMillon (fallback input)
        ///   · cache creation   = tarifa PrecioCacheCreationPorMillon (fallback input)
        ///   · salida           = tarifa normal de output
        /// </summary>
        public decimal Estimar(
            string modelo,
            int tokensEntrada,
            int tokensSalida,
            int cacheReadTokens,
            int cacheCreationTokens)
        {
            if (string.IsNullOrWhiteSpace(modelo)) return 0m;
            AsegurarCargado();

            lock (_lock)
            {
                if (!_cache.TryGetValue(modelo, out var p)) return 0m;

                // Tokens de entrada "frescos" = total − cacheados.
                // Clamp para evitar negativos si algún proveedor reporta raro.
                int frescos = tokensEntrada - cacheReadTokens - cacheCreationTokens;
                if (frescos < 0) frescos = 0;

                decimal precioRead = p.PrecioCacheReadPorMillon > 0m
                    ? p.PrecioCacheReadPorMillon
                    : p.PrecioInputPorMillon;

                decimal precioCreation = p.PrecioCacheCreationPorMillon > 0m
                    ? p.PrecioCacheCreationPorMillon
                    : p.PrecioInputPorMillon;

                decimal usdFresco   = (frescos             / 1_000_000m) * p.PrecioInputPorMillon;
                decimal usdRead     = (cacheReadTokens     / 1_000_000m) * precioRead;
                decimal usdCreation = (cacheCreationTokens / 1_000_000m) * precioCreation;
                decimal usdOut      = (tokensSalida        / 1_000_000m) * p.PrecioOutputPorMillon;

                return usdFresco + usdRead + usdCreation + usdOut;
            }
        }

        /// <summary>Devuelve el precio registrado o null si no existe.</summary>
        public PrecioModelo? Obtener(string modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo)) return null;
            AsegurarCargado();
            lock (_lock)
            {
                return _cache.TryGetValue(modelo, out var p) ? p : null;
            }
        }

        /// <summary>Fuerza re-lectura del disco (si se editó el JSON a mano).</summary>
        public void Recargar()
        {
            lock (_lock) { _cargado = false; }
            AsegurarCargado();
        }

        // ══════════════════ Infra privada ══════════════════

        private void AsegurarCargado()
        {
            lock (_lock)
            {
                if (_cargado) return;

                var desdeDisco = new List<PrecioModelo>();
                try
                {
                    desdeDisco = JsonManager.LeerOCrear<PrecioModelo>(
                        RutasProyecto.ObtenerRutaListPreciosModelos());
                }
                catch { desdeDisco = new(); }

                _cache.Clear();

                // Base: defaults del código.
                foreach (var d in Defaults())
                    _cache[d.Modelo] = d;

                // Override: lo que haya en disco pisa los defaults.
                foreach (var p in desdeDisco)
                {
                    if (string.IsNullOrWhiteSpace(p.Modelo)) continue;
                    _cache[p.Modelo] = p;
                }

                _cargado = true;

                // Materializar el archivo si no existía — facilita que el usuario
                // lo edite: ya tiene todos los modelos conocidos como referencia.
                Persistir();
            }
        }

        private void Persistir()
        {
            try
            {
                var lista = _cache.Values.OrderBy(p => p.Proveedor)
                                          .ThenBy(p => p.Modelo)
                                          .ToList();
                JsonManager.Guardar(RutasProyecto.ObtenerRutaListPreciosModelos(), lista);
            }
            catch { /* best-effort */ }
        }
    }
}
