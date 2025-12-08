using RFToolkit;

namespace TestRFTookkit
{
    internal static class Program
    {
        static bool gui = false;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (gui)
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            else
            {
                Console.ReadKey();
            }
        }
    }
}