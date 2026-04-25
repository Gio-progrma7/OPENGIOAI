// ============================================================
//  PrecioModelo.cs
//
//  Tarifa de un modelo LLM, expresada en USD por 1 millón de
//  tokens. Se usa para estimar el costo de cada llamada en el
//  panel de consumo.
//
//  FUENTE DE VERDAD:
//    Defaults en PreciosModelos.cs (precios públicos conocidos).
//    Override del usuario en {AppDir}/ListPreciosModelos.json.
//    Si un modelo no aparece, se reporta costo = 0 (sin error).
// ============================================================

namespace OPENGIOAI.Entidades
{
    public class PrecioModelo
    {
        /// <summary>Identificador exacto del modelo. Ej: "gpt-4o-mini".</summary>
        public string Modelo { get; set; } = "";

        /// <summary>Proveedor al que pertenece. Ej: "ChatGpt", "Claude".</summary>
        public string Proveedor { get; set; } = "";

        /// <summary>Precio por 1M tokens de entrada (prompt). USD.</summary>
        public decimal PrecioInputPorMillon { get; set; } = 0m;

        /// <summary>Precio por 1M tokens de salida (completion). USD.</summary>
        public decimal PrecioOutputPorMillon { get; set; } = 0m;

        // ── Prompt caching (Fase 1) ─────────────────────────────────
        //
        // Cuando una llamada reusa un prefijo cacheado, el proveedor
        // factura esos tokens a una tarifa reducida. Valor 0 = "no
        // aplicable o desconocido"; Estimar() cae al precio de input
        // en ese caso.

        /// <summary>
        /// Precio por 1M tokens leídos de caché (cache hit). USD.
        /// Típico: 0.10 × input en Anthropic/Deepseek, 0.50 × input en OpenAI.
        /// </summary>
        public decimal PrecioCacheReadPorMillon { get; set; } = 0m;

        /// <summary>
        /// Precio por 1M tokens escritos a caché (solo Anthropic). USD.
        /// Típico: 1.25 × input (25% más caro de sembrar, luego barato de leer).
        /// </summary>
        public decimal PrecioCacheCreationPorMillon { get; set; } = 0m;
    }
}
