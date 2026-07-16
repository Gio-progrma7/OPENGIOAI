using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OPENGIOAI.ServiciosSlack;
using OPENGIOAI.ServiciosTelegram;
using OPENGIOAI.ServiciosTTS;
using OPENGIOAI.Utilerias;
using OPENGIOAI.Vistas;
using OPENGIOAI.Modulos.Arnes;
using OPENGIOAI.Modulos.Arnes.Plugs;
using Serilog;
using Serilog.Events;
using System.IO;

namespace OPENGIOAI
{
    public static class Program
    {

        public static string ComsumoTokens { get; set; }
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetCompatibleTextRenderingDefault(false);

            // Generar ícono de la app si aún no existe
            string rutaIco = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OPENGIOAI.ico");
            GeneradorIcono.GenerarSiNoExiste(rutaIco);

            // También generar en el directorio fuente del proyecto para poder
            // reemplazar logo.ico y que quede embebido en el exe en el próximo build.
            string? dirFuente = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string rutaIcoFuente = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "OPENGIOAI", "logo.ico");
            try
            {
                string fullSource = Path.GetFullPath(rutaIcoFuente);
                if (!File.Exists(fullSource))
                    GeneradorIcono.Generar(fullSource);
            }
            catch { /* no crítico */ }

            ConfigurarSerilog();

            try
            {
                var provider = ConstruirContenedor();
                Log.Information("Aplicación iniciada");

                var mainForm = provider.GetRequiredService<FrmPrincipal>();

                if (File.Exists(rutaIco))
                {
                    try { mainForm.Icon = new System.Drawing.Icon(rutaIco); }
                    catch (Exception ex) { Log.Warning(ex, "No se pudo aplicar el ícono generado"); }
                }

                // Iniciar Servidor ARNES
                var arnesServer = provider.GetRequiredService<ArnesServer>();
                // Forzar resolución del router para que se conecte a los eventos del server
                var arnesRouter = provider.GetRequiredService<ArnesRouter>(); 
                arnesServer.Iniciar();

                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "La aplicación terminó por una excepción no manejada");
                throw;
            }
            finally
            {
                Log.Information("Aplicación finalizada");
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Serilog escribe a consola y a un archivo rolling diario en
        /// {AppDir}/Logs/app-YYYYMMDD.log. Se conserva la última semana.
        /// </summary>
        private static void ConfigurarSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: RutasProyecto.ObtenerRutaPatronLogs(),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        /// <summary>
        /// Registra los servicios de aplicación. Los servicios con estado
        /// (Telegram/Slack/Audio) son singletons — una sola instancia compartida
        /// por todos los forms. Los forms se registran como transient para que
        /// cada apertura obtenga una instancia limpia.
        /// </summary>
        private static IServiceProvider ConstruirContenedor()
        {
            var services = new ServiceCollection();

            services.AddLogging(b => b.AddSerilog(dispose: true));

            services.AddSingleton<TelegramService>();
            services.AddSingleton<SlackChannelService>();
            services.AddSingleton<AudioTTSService>();
            services.AddSingleton<BroadcastService>();

            // Módulo ARNES
            services.AddSingleton<ArnesSecurityManager>();
            services.AddSingleton(sp => 
            {
                var securityManager = sp.GetRequiredService<ArnesSecurityManager>();
                // Puerto 5050
                return new ArnesServer(securityManager, 5050);
            });
            services.AddSingleton(sp => 
            {
                var server = sp.GetRequiredService<ArnesServer>();
                var router = new ArnesRouter(server);
                
                // Registrar los adaptadores iniciales
                router.RegistrarPlug(new AntigravityPlug());
                
                return router;
            });

            services.AddTransient<FrmPrincipal>();

            return services.BuildServiceProvider();
        }
    }
}
