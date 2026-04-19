// ============================================================
//  ChunkMemoria.cs  — Fase C
//
//  Un trozo de texto indexado con su vector de embedding.
//  Se serializa como una línea JSON dentro de embeddings.jsonl.
//
//  CAMPOS:
//    · Id        — hash corto único (para deduplicación incremental)
//    · Fuente    — "Hechos" | "Episodios" | nombre libre
//    · Texto     — el chunk original
//    · Vector    — embedding calculado (float[])
//    · Modelo    — qué modelo generó el vector (validación)
//    · Proveedor — qué proveedor (validación)
//    · Fecha     — cuándo se indexó
//
//  DISEÑO:
//    Deliberadamente sin relaciones ni FKs — cada línea del JSONL
//    es autosuficiente. Si el usuario borra el archivo, el sistema
//    re-indexa todo en el próximo arranque.
// ============================================================

using System;

namespace OPENGIOAI.Entidades
{
    public sealed class ChunkMemoria
    {
        public string   Id        { get; set; } = "";
        public string   Fuente    { get; set; } = "";
        public string   Texto     { get; set; } = "";
        public float[]  Vector    { get; set; } = Array.Empty<float>();
        public string   Modelo    { get; set; } = "";
        public string   Proveedor { get; set; } = "";
        public DateTime Fecha     { get; set; } = DateTime.UtcNow;

        /// <summary>Posición dentro de la fuente (inicio del chunk). Útil para depuración.</summary>
        public int Offset { get; set; }
    }

    /// <summary>
    /// Resultado de una búsqueda: chunk + score de similitud coseno (0..1).
    /// </summary>
    public sealed class ResultadoBusquedaMemoria
    {
        public ChunkMemoria Chunk { get; set; } = new();
        public double       Score { get; set; }
    }

    /// <summary>
    /// Manifest de archivos indexados. Se usa para detectar cambios
    /// (por hash del archivo) y re-indexar solo lo que cambió.
    /// </summary>
    public sealed class ManifestEmbeddings
    {
        public string Proveedor { get; set; } = "";
        public string Modelo    { get; set; } = "";
        public int    Dimension { get; set; }
        public System.Collections.Generic.Dictionary<string, string> HashPorFuente { get; set; } = new();
        public DateTime UltimaIndexacion { get; set; } = DateTime.MinValue;
    }
}
