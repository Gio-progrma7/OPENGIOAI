using OPENGIOAI.Data;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OPENGIOAI.Modulos.Arnes
{
    /// <summary>
    /// Maneja la persistencia y validación de los tokens de seguridad del servidor ARNES.
    /// </summary>
    public class ArnesSecurityManager
    {
        private List<ArnesKey> _keys = new List<ArnesKey>();
        private readonly string _rutaArchivo;

        public ArnesSecurityManager()
        {
            _rutaArchivo = RutasProyecto.ObtenerRutaArnesKeys();
            CargarLlaves();
            
            // Proveer una llave por defecto si está completamente vacío (retrocompatibilidad)
            if (_keys.Count == 0)
            {
                GenerarLlave("Default Admin Key", "openga_arnes_local");
            }
        }

        private void CargarLlaves()
        {
            var keysGuardadas = JsonManager.Leer<ArnesKey>(_rutaArchivo);
            if (keysGuardadas != null && keysGuardadas.Count > 0)
            {
                _keys = keysGuardadas;
            }
        }

        private void GuardarLlaves()
        {
            JsonManager.Guardar(_rutaArchivo, _keys);
        }

        /// <summary>
        /// Valida si el token proveído existe y está activo. 
        /// Actualiza su fecha de último uso.
        /// </summary>
        public bool ValidarToken(string token)
        {
            var keyObj = _keys.FirstOrDefault(k => k.Key == token && k.IsActive);
            if (keyObj != null)
            {
                keyObj.LastUsedAt = DateTime.UtcNow;
                GuardarLlaves(); // Opcionalmente podríamos no guardar en cada request por performance, pero por seguridad es mejor.
                return true;
            }
            return false;
        }

        /// <summary>
        /// Obtiene todas las llaves generadas (incluso las revocadas).
        /// </summary>
        public List<ArnesKey> ObtenerTodas()
        {
            return _keys.ToList();
        }

        /// <summary>
        /// Crea una nueva llave para una aplicación.
        /// </summary>
        public ArnesKey GenerarLlave(string appName, string tokenEspecifico = null)
        {
            // Si no nos dan un token explícito, generamos uno seguro
            string token = string.IsNullOrWhiteSpace(tokenEspecifico) 
                ? $"sk-arnes-{Guid.NewGuid().ToString("N")}" 
                : tokenEspecifico;

            var nuevaLlave = new ArnesKey
            {
                AppName = appName,
                Key = token
            };

            _keys.Add(nuevaLlave);
            GuardarLlaves();
            
            return nuevaLlave;
        }

        /// <summary>
        /// Revoca o reactiva una llave por su ID.
        /// </summary>
        public void CambiarEstado(string id, bool isActive)
        {
            var keyObj = _keys.FirstOrDefault(k => k.Id == id);
            if (keyObj != null)
            {
                keyObj.IsActive = isActive;
                GuardarLlaves();
            }
        }

        /// <summary>
        /// Elimina permanentemente una llave.
        /// </summary>
        public void EliminarLlave(string id)
        {
            _keys.RemoveAll(k => k.Id == id);
            GuardarLlaves();
        }
    }
}
