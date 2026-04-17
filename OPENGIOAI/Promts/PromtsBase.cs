using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Promts
{
    // ═══════════════════════════════════════════════════════════════════════
    //  PromtsBase — fachada de compatibilidad.
    //  Las cadenas crudas se movieron a PromptCatalogo (defaults) y se leen
    //  dinámicamente vía PromptRegistry, que respeta los overrides que el
    //  usuario edite desde el módulo FrmPromts.
    //  Este tipo se mantiene para no romper llamadores existentes.
    // ═══════════════════════════════════════════════════════════════════════
    internal class PromtsBase
    {
        public static string PromtAgente { get; set; } = @"";

        public static string PromtAgenteRes = @"";

        /// <summary>
        /// Prompt usado cuando el pipeline termina en error y hay que hablarle
        /// al usuario en lenguaje humano. Se lee del registry para permitir edición.
        /// </summary>
        public static string PromtAgenteResError =>
            PromptRegistry.Instancia.Obtener(PromptCatalogo.K_RESPUESTA_ERROR);

        /// <summary>
        /// Bienvenida del agente. Sustituye {{nombre_archivo}} por el contenido
        /// del archivo nombre.txt de la ruta activa, como antes.
        /// </summary>
        public static string PromtInicioUsuario(string ruta)
        {
            string nombreArchivo = Utils.LeerArchivoTxt(
                RutasProyecto.ObtenerRutaNombre(ruta)) ?? "";

            var vars = new Dictionary<string, string>
            {
                ["nombre_archivo"] = nombreArchivo,
            };
            return PromptRegistry.Instancia.Obtener(PromptCatalogo.K_INICIO, vars);
        }
    }
}
