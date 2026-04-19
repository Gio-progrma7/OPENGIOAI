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
    }
}
