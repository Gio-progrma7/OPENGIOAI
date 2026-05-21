namespace OPENGIOAI.Data
{
    /// <summary>
    /// Resultado tipado de una instruccion generada por el LLM y ejecutada como script.
    /// Mantiene respuesta.txt como salida de compatibilidad, pero evita que los
    /// orquestadores lo usen como bus interno entre fases.
    /// </summary>
    public sealed class ResultadoEjecucionIA
    {
        public string CodigoGenerado { get; init; } = "";
        public string Stdout { get; init; } = "";
        public string Stderr { get; init; } = "";
        public string RespuestaTxt { get; init; } = "";
        public string RutaScript { get; init; } = "";
        public string WorkspaceEjecucion { get; init; } = "";
        public int? ExitCode { get; init; }
        public bool Ejecutado { get; init; }

        public string SalidaPreferida =>
            !string.IsNullOrWhiteSpace(RespuestaTxt) ? RespuestaTxt :
            !string.IsNullOrWhiteSpace(Stdout) ? Stdout :
            CodigoGenerado;

        public string SalidaTecnica
        {
            get
            {
                if (!Ejecutado) return CodigoGenerado;

                string salida = CombinarSalidaProceso();
                if (ExitCode.GetValueOrDefault() != 0)
                    return CodigoGenerado + "\n\nError al ejecutar el script:\n" + salida;

                return string.IsNullOrWhiteSpace(salida)
                    ? CodigoGenerado
                    : CodigoGenerado + "\n\nSalida del script:\n" + salida;
            }
        }

        private string CombinarSalidaProceso()
        {
            if (string.IsNullOrWhiteSpace(Stderr)) return Stdout.Trim();
            if (string.IsNullOrWhiteSpace(Stdout)) return Stderr.Trim();
            return (Stdout.TrimEnd() + "\n" + Stderr.Trim()).Trim();
        }
    }
}
