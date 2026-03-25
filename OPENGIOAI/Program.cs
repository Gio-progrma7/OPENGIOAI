using OPENGIOAI.Vistas;

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

            Application.Run(new FrmPrincipal());
        }
    }
}