namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Registro centralizado de todas las herramientas disponibles para el agente.
    /// Se crea una vez y se consulta en cada iteración del bucle agéntico.
    /// </summary>
    public class RegistroHerramientas
    {
        private readonly Dictionary<string, IHerramienta> _herramientas;

        public RegistroHerramientas(IEnumerable<IHerramienta> herramientas)
        {
            _herramientas = herramientas.ToDictionary(h => h.Nombre, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Crea el registro estándar con todas las herramientas del sistema.
        /// </summary>
        public static RegistroHerramientas CrearPorDefecto() => new(
        [
            new HerramientaLeerArchivo(),
            new HerramientaEscribirArchivo(),
            new HerramientaListarDirectorio(),
            new HerramientaHacerSolicitudHttp(),
            new HerramientaBuscarEnArchivos(),
            new HerramientaEjecutarComando(),
        ]);

        /// <summary>
        /// Busca una herramienta por nombre. Devuelve null si no existe.
        /// </summary>
        public IHerramienta? Obtener(string nombre) =>
            _herramientas.TryGetValue(nombre, out var h) ? h : null;

        /// <summary>
        /// Devuelve todas las herramientas registradas.
        /// </summary>
        public IReadOnlyCollection<IHerramienta> ObtenerTodas() =>
            _herramientas.Values;

        /// <summary>
        /// Número de herramientas registradas.
        /// </summary>
        public int Count => _herramientas.Count;
    }
}
