using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OPENGIOAI.Utilerias
{
    /// <summary>
    /// Centraliza todas las rutas de archivos y carpetas del proyecto.
    ///
    /// El proyecto distingue dos tipos de rutas:
    ///
    ///  • GLOBALES (vivas en {AppDir}, una sola por instalación):
    ///    - Configuracion.json        → config raíz que apunta a la ruta de trabajo activa
    ///    - EmbeddingsConfig.json     → proveedor + endpoint + key del embedder
    ///    - Traces/                   → tracing global (todas las sesiones)
    ///    - Logs/                     → logs global de Serilog
    ///
    ///  • POR WORKSPACE (vivas en la ruta de trabajo del usuario):
    ///    - ListApis.json, ListAutomatizaciones.json, ListSkills.json,
    ///      ListHabilidades.json, ListModelos.json, ListArchivos.json,
    ///      ListSlack.json, ListTelegram.json, ListPreciosModelos.json
    ///    - promtAgente.md, promtMaestro.md, Arquitectura.md
    ///    - ConfiguracionTTS.json, nombre.txt, Scripts/
    ///    - Memoria/* (Hechos, Episodios, PatronesIgnorados, embeddings*)
    ///
    /// La ruta de trabajo se obtiene desde <see cref="RutaTrabajoActual"/>,
    /// que se inicializa al arrancar la app desde Configuracion.json y se
    /// actualiza con <see cref="EstablecerRutaTrabajo"/> cuando el usuario
    /// la cambia desde FrmRutas.
    ///
    /// Si la ruta de trabajo está vacía (primera ejecución antes de configurar
    /// nada), los métodos workspace caen al AppDir como fallback — esto
    /// evita que la app se rompa antes de que el usuario seleccione una ruta.
    /// </summary>
    public static class RutasProyecto
    {
        // ═══════════════════════════════════════════════════════════════════
        //  Ruta de trabajo activa
        // ═══════════════════════════════════════════════════════════════════

        private static string _rutaTrabajoActual = string.Empty;

        /// <summary>
        /// Ruta de trabajo activa donde se persisten los archivos del workspace.
        /// Se inicializa desde Configuracion.json al arrancar la app.
        /// Si está vacía o el directorio no existe, los métodos workspace
        /// caerán al AppDir (modo fallback de primera ejecución).
        /// </summary>
        public static string RutaTrabajoActual => _rutaTrabajoActual;

        /// <summary>
        /// Establece la ruta de trabajo activa y, si corresponde, migra los
        /// archivos del workspace que existieran en AppDir copiándolos a la
        /// nueva carpeta. La migración es no-destructiva: si el archivo ya
        /// existe en el destino no se sobreescribe.
        /// </summary>
        public static void EstablecerRutaTrabajo(string nuevaRuta)
        {
            if (string.IsNullOrWhiteSpace(nuevaRuta))
            {
                _rutaTrabajoActual = string.Empty;
                return;
            }

            try
            {
                if (!Directory.Exists(nuevaRuta))
                    Directory.CreateDirectory(nuevaRuta);

                _rutaTrabajoActual = nuevaRuta;

                // Migración no-destructiva: si el AppDir tiene archivos del
                // workspace que no están en la nueva ruta, los copiamos para
                // no perder credenciales / automatizaciones / prompts viejos.
                MigrarSiCorresponde(nuevaRuta);
            }
            catch
            {
                // Fallar de la migración no debe colgar la app
                _rutaTrabajoActual = nuevaRuta;
            }
        }

        /// <summary>
        /// Resuelve la base donde guardar archivos del workspace.
        /// Devuelve <see cref="RutaTrabajoActual"/> si está definida y existe;
        /// caso contrario el AppDir como fallback.
        /// </summary>
        private static string ResolverBaseWorkspace()
        {
            if (!string.IsNullOrWhiteSpace(_rutaTrabajoActual)
                && Directory.Exists(_rutaTrabajoActual))
                return _rutaTrabajoActual;

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Migración AppDir → Workspace
        // ═══════════════════════════════════════════════════════════════════

        // Archivos del workspace que se migran si existen en AppDir y no en
        // la nueva ruta. NO incluye Configuracion.json ni EmbeddingsConfig.json
        // (esos son globales) ni Traces/Logs (también globales).
        private static readonly string[] ArchivosWorkspace =
        {
            "ListApis.json",
            "ListAutomatizaciones.json",
            "ListSkills.json",
            "ListHabilidades.json",
            "ListModelos.json",
            "ListArchivos.json",
            "ListSlack.json",
            "ListTelegram.json",
            "ListPreciosModelos.json",
            "ConfiguracionTTS.json",
            "promtAgente.md",
            "promtMaestro.md",
            "Arquitectura.md",
            "nombre.txt"
        };

        private static readonly string[] CarpetasWorkspace =
        {
            "Scripts",
            "Memoria"
        };

        private static void MigrarSiCorresponde(string destino)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.Equals(
                    Path.GetFullPath(appDir).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(destino).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
                return; // mismo directorio, nada que migrar

            foreach (var nombre in ArchivosWorkspace)
            {
                try
                {
                    string origen  = Path.Combine(appDir, nombre);
                    string destinoArchivo = Path.Combine(destino, nombre);
                    if (File.Exists(origen) && !File.Exists(destinoArchivo))
                        File.Copy(origen, destinoArchivo);
                }
                catch { /* tolerar fallos de copia individual */ }
            }

            foreach (var carpeta in CarpetasWorkspace)
            {
                try
                {
                    string origen  = Path.Combine(appDir, carpeta);
                    string destinoCarp = Path.Combine(destino, carpeta);
                    if (Directory.Exists(origen) && !Directory.Exists(destinoCarp))
                        CopiarDirectorioRecursivo(origen, destinoCarp);
                }
                catch { /* tolerar */ }
            }
        }

        private static void CopiarDirectorioRecursivo(string origen, string destino)
        {
            Directory.CreateDirectory(destino);
            foreach (var f in Directory.GetFiles(origen))
            {
                string dst = Path.Combine(destino, Path.GetFileName(f));
                if (!File.Exists(dst)) File.Copy(f, dst);
            }
            foreach (var d in Directory.GetDirectories(origen))
            {
                string dst = Path.Combine(destino, Path.GetFileName(d));
                CopiarDirectorioRecursivo(d, dst);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  RUTAS GLOBALES (siempre en AppDir)
        // ═══════════════════════════════════════════════════════════════════

        public static string ObtenerRutaConfiguracion()
        {
            string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(rutaBase, "Configuracion.json");
        }

        public static string ObtenerRutaEmbeddingsConfig()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "EmbeddingsConfig.json");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  RUTAS POR WORKSPACE (en RutaTrabajoActual o AppDir como fallback)
        // ═══════════════════════════════════════════════════════════════════

        public static string ObtenerRutaListApis()
            => Path.Combine(ResolverBaseWorkspace(), "ListApis.json");

        public static string ObtenerRutaListAutomatizaciones()
            => Path.Combine(ResolverBaseWorkspace(), "ListAutomatizaciones.json");

        public static string ObtenerRutaListHabilidades()
            => Path.Combine(ResolverBaseWorkspace(), "ListHabilidades.json");

        public static string ObtenerRutaListPreciosModelos()
            => Path.Combine(ResolverBaseWorkspace(), "ListPreciosModelos.json");

        public static string ObtenerRutaListModelos()
            => Path.Combine(ResolverBaseWorkspace(), "ListModelos.json");

        public static string ObtenerRutaListArchivos()
            => Path.Combine(ResolverBaseWorkspace(), "ListArchivos.json");

        public static string ObtenerRutaConfiguracionTTS()
            => Path.Combine(ResolverBaseWorkspace(), "ConfiguracionTTS.json");

        public static string ObtenerRutaListSlack()
            => Path.Combine(ResolverBaseWorkspace(), "ListSlack.json");

        public static string ObtenerRutaListTelegram()
            => Path.Combine(ResolverBaseWorkspace(), "ListTelegram.json");

        // Sobrecargas con rutaBase explícita (usadas desde código que ya
        // recibe la ruta como parámetro — no rompe ese flujo).
        public static string ObtenerRutaSlack(string rutaBase)
            => Path.Combine(rutaBase, "ListSlack.json");

        public static string ObtenerRutaListTelegram(string rutaBase)
            => Path.Combine(rutaBase, "ListTelegram.json");

        public static string ObtenerRutaListSkills(string rutaBase)
            => Path.Combine(rutaBase, "ListSkills.json");

        public static string ObtenerRutaListSkills()
            => Path.Combine(ResolverBaseWorkspace(), "ListSkills.json");

        public static string ObtenerRutaNombre(string rutaBase)
            => Path.Combine(rutaBase, "nombre.txt");

        public static string ObtenerRutaScripts()
            => Path.Combine(ResolverBaseWorkspace(), "Scripts");

        public static string ObtenerRutaPromtAgente()
        {
            string ruta = Path.Combine(ResolverBaseWorkspace(), "promtAgente.md");
            CrearRuta(ruta);
            return ruta;
        }

        public static string ObtenerRutaPromtMaestro()
        {
            string ruta = Path.Combine(ResolverBaseWorkspace(), "promtMaestro.md");
            CrearRuta(ruta);
            return ruta;
        }

        public static string ObtenerArquitectura()
        {
            string ruta = Path.Combine(ResolverBaseWorkspace(), "Arquitectura.md");
            CrearRuta(ruta);
            return ruta;
        }

        public static void CrearRuta(string rutaBs)
        {
            try
            {
                string carpeta = Path.GetDirectoryName(rutaBs);
                if (!string.IsNullOrEmpty(carpeta) && !Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                if (!File.Exists(rutaBs))
                {
                    using (File.Create(rutaBs)) { }
                }
            }
            catch { /* tolerar */ }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Memoria (Fase 1) — siempre por workspace explícito
        // ═══════════════════════════════════════════════════════════════════
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
            => Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "Hechos.md");

        public static string ObtenerRutaMemoriaEpisodios(string rutaBase)
            => Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "Episodios.md");

        // ═══════════════════════════════════════════════════════════════════
        //  Patrones (Fase 3)
        // ═══════════════════════════════════════════════════════════════════
        //
        //   {ruta}/Memoria/PatronesIgnorados.json

        public static string ObtenerRutaPatronesIgnorados(string rutaBase)
            => Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "PatronesIgnorados.json");

        // ═══════════════════════════════════════════════════════════════════
        //  Embeddings / RAG (Fase C) — vector store por workspace
        // ═══════════════════════════════════════════════════════════════════
        //
        //   {ruta}/Memoria/embeddings.jsonl
        //   {ruta}/Memoria/embeddings.manifest.json

        public static string ObtenerRutaEmbeddings(string rutaBase)
            => Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "embeddings.jsonl");

        public static string ObtenerRutaEmbeddingsManifest(string rutaBase)
            => Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "embeddings.manifest.json");

        // ═══════════════════════════════════════════════════════════════════
        //  Contexto Semántico (Fase D)
        // ═══════════════════════════════════════════════════════════════════
        //
        //   {ruta}/Memoria/embeddings_contexto.jsonl
        //   {ruta}/Memoria/embeddings_contexto.manifest.json

        public static string ObtenerRutaEmbeddingsContexto(string rutaBase)
            => Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "embeddings_contexto.jsonl");

        public static string ObtenerRutaEmbeddingsContextoManifest(string rutaBase)
            => Path.Combine(ObtenerRutaCarpetaMemoria(rutaBase), "embeddings_contexto.manifest.json");

        // ═══════════════════════════════════════════════════════════════════
        //  Traces (Fase 1A) — siempre global
        // ═══════════════════════════════════════════════════════════════════

        public static string ObtenerRutaCarpetaTraces()
        {
            string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Traces");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        public static string ObtenerRutaTracesDelDia(DateTime fecha)
            => Path.Combine(ObtenerRutaCarpetaTraces(),
                            fecha.ToString("yyyy-MM-dd") + ".jsonl");

        public static string ObtenerRutaTracesIndexDelDia(DateTime fecha)
            => Path.Combine(ObtenerRutaCarpetaTraces(),
                            fecha.ToString("yyyy-MM-dd") + ".index.json");

        // ═══════════════════════════════════════════════════════════════════
        //  Logs (Fase 3) — siempre global
        // ═══════════════════════════════════════════════════════════════════

        public static string ObtenerRutaCarpetaLogs()
        {
            string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        public static string ObtenerRutaPatronLogs()
            => Path.Combine(ObtenerRutaCarpetaLogs(), "app-.log");
    }
}
