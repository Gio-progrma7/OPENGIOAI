namespace OPENGIOAI.Modulos.Arnes
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Contrato que deben implementar todos los adaptadores (Plugs)
    /// para interactuar con el módulo ARNES.
    /// </summary>
    public interface IArnesPlug
    {
        /// <summary>
        /// El nombre único del plug (ej. "Antigravity", "CodeX").
        /// </summary>
        string Nombre { get; }

        /// <summary>
        /// Inicializa la conexión con la aplicación externa si es necesario.
        /// </summary>
        Task InicializarAsync(CancellationToken ct);

        /// <summary>
        /// Procesa una petición entrante dirigida a este plug.
        /// </summary>
        /// <param name="payload">Los datos de la petición.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>La respuesta estandarizada que se enviará de vuelta.</returns>
        Task<ArnesPayload> ProcesarPeticionAsync(ArnesPayload payload, CancellationToken ct);

        /// <summary>
        /// (Opcional) Método para que OPENGIOAI inicie activamente una tarea en la aplicación externa.
        /// </summary>
        Task<ArnesPayload> EnviarTareaExternaAsync(ArnesPayload tarea, CancellationToken ct);

        /// <summary>
        /// Libera recursos y cierra conexiones.
        /// </summary>
        void Desconectar();
    }
}
