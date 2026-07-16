using Serilog;

namespace OPENGIOAI.Data
{
    /// <summary>
    /// Resultado tipado de una instruccion generada por el LLM y ejecutada como script.
    /// Stdout es la fuente primaria de salida (streaming confiable).
    /// RespuestaTxt se conserva como compatibilidad con scripts externos,
    /// pero NO se usa como bus interno entre fases del pipeline.
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

        /// <summary>
        /// Salida preferida para consumo interno del pipeline:
        /// 1. Stdout (streaming confiable)
        /// 2. RespuestaTxt (compatibilidad con scripts legacy)
        /// 3. CodigoGenerado (fallback)
        /// </summary>
        public string SalidaPreferida =>
            !string.IsNullOrWhiteSpace(Stdout) ? Stdout :
            !string.IsNullOrWhiteSpace(RespuestaTxt) ? RespuestaTxt :
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
