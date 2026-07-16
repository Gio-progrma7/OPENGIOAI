using System.Text.RegularExpressions;

namespace OPENGIOAI.Agentes
{
    public enum RutaAgentica
    {
        ConstructorPython,
        Herramientas
    }

    public sealed record DecisionRutaAgentica(
        RutaAgentica Ruta,
        double Confianza,
        string Razon,
        int MaxIteracionesHerramientas = 8);

    /// <summary>
    /// Router barato de primera capa: evita gastar una llamada LLM solo para
    /// decidir si una tarea obvia debe usar herramientas o el Constructor.
    /// </summary>
    public static class RouterAgentico
    {
        private static readonly Regex RegexHerramientas = new(
            @"\b(lee|leer|lista|listar|busca|buscar|encuentra|archivo|archivos|carpeta|directorio|ruta|http|api|url|webhook|comando|powershell|cmd|terminal|proceso|procesos|skill|herramienta|repo|proyecto|codigo|c[oó]digo)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex RegexConstructor = new(
            @"\b(genera|generar|crea|crear|construye|procesa|procesar|transforma|convertir|convierte|grafica|gr[aá]fica|reporte|automatiza|script|python|excel|csv|pdf|imagen|comprime)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static DecisionRutaAgentica Decidir(string instruccion)
        {
            if (string.IsNullOrWhiteSpace(instruccion))
            {
                return new DecisionRutaAgentica(
                    RutaAgentica.ConstructorPython,
                    0.20,
                    "Instruccion vacia o ambigua.");
            }

            bool quiereHerramientas = RegexHerramientas.IsMatch(instruccion);
            bool quiereConstructor = RegexConstructor.IsMatch(instruccion);

            if (quiereHerramientas && !quiereConstructor)
            {
                return new DecisionRutaAgentica(
                    RutaAgentica.Herramientas,
                    0.82,
                    "La instruccion pide inspeccion, busqueda, lectura o accion directa con herramientas.",
                    MaxIteracionesHerramientas: 6);
            }

            if (quiereHerramientas && quiereConstructor)
            {
                return new DecisionRutaAgentica(
                    RutaAgentica.Herramientas,
                    0.65,
                    "La tarea mezcla contexto externo con ejecucion; se intenta primero Tool Use y se cae al Constructor si no concluye.",
                    MaxIteracionesHerramientas: 8);
            }

            return new DecisionRutaAgentica(
                RutaAgentica.ConstructorPython,
                0.70,
                "La instruccion parece requerir generacion/ejecucion completa o respuesta general.");
        }
    }
}
