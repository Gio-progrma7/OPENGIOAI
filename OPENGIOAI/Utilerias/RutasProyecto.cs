using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class RutasProyecto
    {
        public static string ObtenerRutaListApis()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(rutaBase, "ListApis.json");
        }

        public static string ObtenerRutaConfiguracion()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(rutaBase, "Configuracion.json");
        }


        public static string ObtenerRutaSlack(string rutaBase)
        {
            return Path.Combine(rutaBase, "ListSlack.json");
        }

        public static string ObtenerRutaListSlack()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(rutaBase, "ListSlack.json");
        }

        public static string ObtenerRutaListTelegram(string rutaBase)
        {
            return Path.Combine(rutaBase, "ListTelegram.json");
        }

        public static string ObtenerRutaListTelegram()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(rutaBase, "ListTelegram.json");
        }
        public static string ObtenerRutaListSkills(string rutaBase)
        {
            return Path.Combine(rutaBase, "ListSkills.json");
        }

        public static string ObtenerRutaListAutomatizaciones()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(rutaBase, "ListAutomatizaciones.json");
        }


        public static string ObtenerRutaListModelos()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(rutaBase, "ListModelos.json");
        }

        public static string ObtenerRutaListArchivos()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(rutaBase, "ListArchivos.json");
        }


        public static string ObtenerRutaNombre(string rutaBase)
        {

            return Path.Combine(rutaBase + "nombre.txt");
        }

        public static string ObtenerRutaScripts()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(rutaBase + "Scripts");
        }
        public static string ObtenerRutaPromtAgente()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            string rutaPromts = Path.Combine(rutaBase, "promtAgente.md");

            CrearRuta(rutaPromts);
            return rutaPromts;
        }
        public static string ObtenerRutaPromtMaestro()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            string rutaPromts = Path.Combine(rutaBase, "promtMaestro.md");

            CrearRuta(rutaPromts);
            return rutaPromts;
        }

        public static string ObtenerArquitectura()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;

            string rutaPromts = Path.Combine(rutaBase, "Arquitectura.md");

            CrearRuta(rutaPromts);
            return rutaPromts;
        }

        public static string ObtenerRutaConfiguracionTTS()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(rutaBase, "ConfiguracionTTS.json");
        }

        public static void CrearRuta(string rutaBs)
        {
            if (!File.Exists(rutaBs))
            {
                File.Create(rutaBs);
            }
        }

    }
}
