using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OPENGIOAI.ServiciosSlack;
using OPENGIOAI.ServiciosTelegram;
using OPENGIOAI.ServiciosTTS;
using OPENGIOAI.Utilerias;
using OPENGIOAI.Vistas;
using Serilog;
using Serilog.Events;

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

            ConfigurarSerilog();

            try
            {
                var provider = ConstruirContenedor();
                Log.Information("Aplicación iniciada");
                Application.Run(provider.GetRequiredService<FrmPrincipal>());
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

            services.AddTransient<FrmPrincipal>();

            return services.BuildServiceProvider();
        }
    }
}
