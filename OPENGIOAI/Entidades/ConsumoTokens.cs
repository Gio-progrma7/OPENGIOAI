// ============================================================
//  ConsumoTokens.cs
//
//  Registro de una SOLA llamada al LLM. Varios de estos se
//  agregan por ejecución (una "instrucción del usuario") para
//  formar el consumo total visible en FrmConsumoTokens.
//
//  Compatibilidad: los 5 campos originales (PromptTokens,
//  CompletionTokens, TotalTokens, Disponible, Proveedor) se
//  mantienen, los nuevos son opcionales con default.
// ============================================================

using System;

namespace OPENGIOAI.Entidades
{
    public class ConsumoTokens
    {
        // ── Campos legados (retrocompatibles) ────────────────────────
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public bool Disponible { get; set; }
        public string Proveedor { get; set; } = "";

        // ── Campos nuevos (telemetría Fase A) ────────────────────────

        /// <summary>Nombre de la fase del pipeline. Ej: "Analista", "Guardián".</summary>
        public string Fase { get; set; } = "General";

        /// <summary>Identificador del modelo usado. Ej: "gpt-4o-mini", "claude-3-5-sonnet".</summary>
        public string Modelo { get; set; } = "";

        /// <summary>Momento de la llamada.</summary>
        public DateTime Instante { get; set; } = DateTime.Now;

        /// <summary>
        /// Id de la ejecución (una "instrucción del usuario"). Todas las fases
        /// de un mismo pipeline comparten este id para poder agruparlas en UI.
        /// </summary>
        public string InstruccionId { get; set; } = "";

        /// <summary>
        /// Costo estimado en USD, calculado vía PreciosModelos.
        /// 0 si el modelo no tiene tarifa registrada o es local (Ollama).
        /// </summary>
        public decimal CostoEstimadoUsd { get; set; } = 0m;

        // ── Prompt caching (Fase 1) ─────────────────────────────────
        //
        // Tokens ingresados al LLM que fueron servidos desde la caché del
        // proveedor (~90% descuento). Se reporta cuando el proveedor expone
        // la métrica:
        //   · Anthropic → usage.cache_read_input_tokens
        //   · OpenAI    → usage.prompt_tokens_details.cached_tokens
        //   · Deepseek  → usage.prompt_cache_hit_tokens
        //
        // Nota: estos tokens ya están contados dentro de PromptTokens.
        // Se desglosan aquí solo para telemetría y cálculo de ahorros.

        /// <summary>Tokens del prompt servidos desde caché del proveedor (subset de PromptTokens).</summary>
        public int CacheReadTokens { get; set; } = 0;

        /// <summary>
        /// Tokens del prompt que se escribieron en la caché en esta llamada
        /// (solo Anthropic: <c>usage.cache_creation_input_tokens</c>).
        /// Representan el "costo de sembrado": se facturan a ~125% del
        /// precio normal, pero después se leen a ~10%.
        /// </summary>
        public int CacheCreationTokens { get; set; } = 0;
    }
}
