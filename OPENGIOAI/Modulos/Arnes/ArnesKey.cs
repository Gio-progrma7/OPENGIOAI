using System;

namespace OPENGIOAI.Modulos.Arnes
{
    /// <summary>
    /// Representa una llave de seguridad (Token) generada para una 
    /// aplicación externa específica que desea conectarse al Hub ARNES.
    /// </summary>
    public class ArnesKey
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Nombre descriptivo de la aplicación (Ej: "Visual Copilot", "Antigravity")
        /// </summary>
        public string AppName { get; set; } = string.Empty;
        
        /// <summary>
        /// El token real que debe enviar la aplicación en el header Authorization (Bearer ...)
        /// </summary>
        public string Key { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
        
        /// <summary>
        /// Si es false, el servidor rechazará automáticamente peticiones con esta llave.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
