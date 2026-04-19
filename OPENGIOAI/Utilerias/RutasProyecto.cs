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

        public static string ObtenerRutaListHabilidades()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(rutaBase, "ListHabilidades.json");
        }

        public static string ObtenerRutaListPreciosModelos()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(rutaBase, "ListPreciosModelos.json");
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

        // ══════════════════ Memoria (Fase 1) ══════════════════
        //
        // La memoria vive dentro de la ruta de trabajo de cada agente,
        // en una carpeta "Memoria" — coherente con el resto del proyecto
        // (archivos legibles, editables a mano, portables).
        //
        //   {ruta}/Memoria/Hechos.md       → verdades durables sobre el usuario
        //   {ruta}/Memoria/Episodios.md    → timeline append-only de ejecuciones

        public static string ObtenerRutaCarpetaMemoria(string rutaBase)
        {
            string carpeta = Path.Combine(rutaBase, "Memoria");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        public static string ObtenerRutaMemoriaHechos(string rutaBase)
        {
            return Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "Hechos.md");
        }

        public static string ObtenerRutaMemoriaEpisodios(string rutaBase)
        {
            return Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "Episodios.md");
        }

        // ══════════════════ Patrones (Fase 3) ══════════════════
        //
        // Lista de firmas de patrones que el usuario decidió ignorar
        // (no quiere ver la propuesta de Skill para ese patrón nunca más).
        // Vive junto a la memoria porque el contexto es del mismo workspace.
        //
        //   {ruta}/Memoria/PatronesIgnorados.json

        public static string ObtenerRutaPatronesIgnorados(string rutaBase)
        {
            return Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "PatronesIgnorados.json");
        }

        // ══════════════════ Embeddings / RAG (Fase C) ══════════════════
        //
        // Vector store local: JSONL con un chunk por línea. Se acompaña
        // de un manifest JSON que registra qué archivos fueron indexados
        // (y con qué hash) para poder hacer indexación incremental.
        //
        //   {ruta}/Memoria/embeddings.jsonl
        //   {ruta}/Memoria/embeddings.manifest.json
        //
        // La configuración global (proveedor, modelo, endpoint, api key)
        // es por instalación, no por workspace:
        //   {AppDir}/EmbeddingsConfig.json

        public static string ObtenerRutaEmbeddings(string rutaBase)
        {
            return Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "embeddings.jsonl");
        }

        public static string ObtenerRutaEmbeddingsManifest(string rutaBase)
        {
            return Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "embeddings.manifest.json");
        }

        public static string ObtenerRutaEmbeddingsConfig()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "EmbeddingsConfig.json");
        }
    }
}
