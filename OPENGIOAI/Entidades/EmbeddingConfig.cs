// ============================================================
//  EmbeddingConfig.cs  — Fase C
//
//  Configuración GLOBAL del motor de embeddings (no por workspace).
//  Vive en {AppDir}/EmbeddingsConfig.json. Si el archivo no existe
//  se crea con defaults de Ollama (no requiere api key).
//
//  CAMPOS:
//    · Proveedor     — Ollama | OpenAI
//    · Modelo        — nombre del modelo del proveedor
//    · EndpointUrl   — override opcional (p.ej. Ollama remoto)
//    · ApiKey        — requerido para OpenAI; ignorado para Ollama
//    · TopK          — cuántos chunks inyectar al prompt por instrucción
//    · ChunkSize     — tamaño objetivo de cada chunk (caracteres)
//    · ChunkOverlap  — solapamiento entre chunks adyacentes (caracteres)
// ============================================================

namespace OPENGIOAI.Entidades
{
    public sealed class EmbeddingConfig
    {
        public ProveedorEmbedding Proveedor { get; set; } = ProveedorEmbedding.Ollama;

        public string Modelo      { get; set; } = "nomic-embed-text";
        public string EndpointUrl { get; set; } = "http://localhost:11434";
        public string ApiKey      { get; set; } = "";

        public int TopK         { get; set; } = 5;
        public int ChunkSize    { get; set; } = 500;
        public int ChunkOverlap { get; set; } = 80;

        // Dimensión esperada del vector — útil para validar que el índice
        // no se corrompa si el usuario cambia de proveedor/modelo.
        public int DimensionEsperada { get; set; } = 768;

        public EmbeddingConfig Clone()
        {
            return new EmbeddingConfig
            {
                Proveedor        = Proveedor,
                Modelo           = Modelo,
                EndpointUrl      = EndpointUrl,
                ApiKey           = ApiKey,
                TopK             = TopK,
                ChunkSize        = ChunkSize,
                ChunkOverlap     = ChunkOverlap,
                DimensionEsperada = DimensionEsperada,
            };
        }
    }
}
