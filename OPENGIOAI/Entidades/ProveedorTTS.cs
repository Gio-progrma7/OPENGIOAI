namespace OPENGIOAI.Entidades
{
    public enum ProveedorTTS
    {
        /// <summary>Motor de voz integrado en Windows. Sin API Key. Calidad básica.</summary>
        SystemSpeech = 0,

        /// <summary>OpenAI TTS API — requiere API Key de OpenAI. Calidad alta.</summary>
        OpenAI = 1,

        /// <summary>Google Cloud Text-to-Speech — requiere API Key de Google Cloud. Calidad alta.</summary>
        Google = 2
    }
}
