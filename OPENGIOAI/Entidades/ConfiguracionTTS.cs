namespace OPENGIOAI.Entidades
{
    public class ConfiguracionTTS
    {
        /// <summary>Proveedor TTS activo.</summary>
        public ProveedorTTS Proveedor { get; set; } = ProveedorTTS.SystemSpeech;

        /// <summary>API Key para OpenAI o Google Cloud TTS. Vacío para System.Speech.</summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Nombre de la voz.
        /// · System.Speech: nombre exacto de la voz SAPI instalada (ej. "Microsoft Helena Desktop").
        /// · OpenAI: alloy | echo | fable | nova | onyx | shimmer.
        /// · Google: nombre de voz Neural2 (ej. "es-ES-Neural2-A").
        /// </summary>
        public string Voz { get; set; } = "";

        /// <summary>Código de idioma BCP-47 usado solo por Google TTS (ej. "es-ES", "es-MX").</summary>
        public string Idioma { get; set; } = "es-ES";

        /// <summary>
        /// Indica si el TTS está configurado y activo.
        /// FrmMandos solo genera audio si este flag es true.
        /// </summary>
        public bool Activo { get; set; } = false;
    }
}
