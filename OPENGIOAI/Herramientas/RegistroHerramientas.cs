namespace OPENGIOAI.Herramientas
{
    /// <summary>
    /// Registro centralizado de todas las herramientas disponibles para el agente.
    /// Se crea una vez y se consulta en cada iteración del bucle agéntico.
    /// </summary>
    public class RegistroHerramientas
    {
        // No readonly: AgregarSkills() necesita mutar el diccionario post-construcción
        private Dictionary<string, IHerramienta> _herramientas;

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

        /// <summary>
        /// Registra skills cargados de ListSkills.json como herramientas de tool-use.
        /// Cada skill se envuelve en <see cref="OPENGIOAI.Skills.HerramientaSkill"/>.
        /// No sobreescribe herramientas nativas si hay colisión de nombre.
        /// </summary>
        /// <param name="skills">Skills activos del directorio de trabajo.</param>
        /// <param name="rutaBase">Directorio de trabajo para resolver rutas relativas.</param>
        public void AgregarSkills(
            IEnumerable<OPENGIOAI.Entidades.Skill> skills,
            string rutaBase = "")
        {
            foreach (var skill in skills)
            {
                var herramienta = new OPENGIOAI.Skills.HerramientaSkill(skill, rutaBase);
                // Las herramientas nativas tienen prioridad — no sobreescribir
                if (!_herramientas.ContainsKey(herramienta.Nombre))
                    _herramientas[herramienta.Nombre] = herramienta;
            }
        }

        /// <summary>
        /// Crea un registro por defecto con herramientas nativas y añade los skills
        /// del directorio de trabajo indicado en un solo paso.
        /// </summary>
        public static RegistroHerramientas CrearConSkills(
            IEnumerable<OPENGIOAI.Entidades.Skill> skills,
            string rutaBase)
        {
            var registro = CrearPorDefecto();
            registro.AgregarSkills(skills, rutaBase);
            return registro;
        }
    }
}
