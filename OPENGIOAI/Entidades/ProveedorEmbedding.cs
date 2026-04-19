// ============================================================
//  ProveedorEmbedding.cs  — Fase C
//
//  Qué motor usa el sistema para calcular vectores.
//  Cada proveedor tiene su endpoint, su dimensión de vector
//  y su mapeo de pago. La lógica vive en EmbeddingsService.
// ============================================================

namespace OPENGIOAI.Entidades
{
    public enum ProveedorEmbedding
    {
        /// <summary>
        /// Ollama local. Default: nomic-embed-text (768d).
        /// Gratis, privado, requiere que el usuario tenga Ollama
        /// corriendo en http://localhost:11434.
        /// </summary>
        Ollama,

        /// <summary>
        /// OpenAI. Default: text-embedding-3-small (1536d).
        /// Rápido, alta calidad, muy barato ($0.02 / 1M tokens).
        /// Requiere API key en EmbeddingsConfig.
        /// </summary>
        OpenAI,
    }
}
