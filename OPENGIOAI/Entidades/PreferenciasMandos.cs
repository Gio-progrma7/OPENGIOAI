namespace OPENGIOAI.Entidades
{
    public class PreferenciasMandos
    {
        public bool RecordarTema { get; set; } = false;
        public bool SoloChat { get; set; } = false;
        public bool SoloRespuestaRapida { get; set; } = false;

        public bool TelegramActivo { get; set; } = false;
        public bool EnviarConstructorTelegram { get; set; } = false;
        public bool EnviarArchivosTelegram { get; set; } = false;

        public bool SlackActivo { get; set; } = false;
        public bool EnviarConstructorSlack { get; set; } = false;
        public bool EnviarArchivosSlack { get; set; } = false;

        public bool EnviarAudio { get; set; } = false;
    }
}
