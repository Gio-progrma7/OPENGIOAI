using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Entidades
{
    public class ConfiguracionClient
    {

        public Api Miapi { get; set; }
        public Modelo Mimodelo{  get; set; }
        public Archivo MiArchivo {  get; set; }
        public PreferenciasMandos Preferencias { get; set; }

        /// <summary>
        /// Si es true, al cerrar la ventana principal la app se minimiza a la
        /// bandeja del sistema en vez de salir. Las automatizaciones programadas
        /// siguen ejecutándose en segundo plano.
        /// </summary>
        public bool EjecutarEnSegundoPlano { get; set; } = false;

        /// <summary>
        /// Si es true, en el próximo cierre se preguntará al usuario si desea
        /// activar el modo segundo plano. Una vez respondido, se desactiva.
        /// </summary>
        public bool PreguntarSegundoPlano { get; set; } = true;
    }
}
