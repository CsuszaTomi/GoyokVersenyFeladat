using GoyokVersenyFeladat.Minescript;
using System;
using System.Windows;

namespace GoyokVersenyFeladat
{
    public partial class App : Application
    {
        /// <summary>
        /// Indítás:
        ///   robot.exe input.txt output.txt -> grafikus IDE
        ///   robot.exe -q input.txt output.txt -> csendes mód: nincs ablak, napló írása, kilépés
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string[] args = e.Args;
            bool quiet = args.Length > 0 && args[0] == "-q";
            int first = quiet ? 1 : 0;
            LaunchOptions.Quiet = quiet;
            LaunchOptions.InputPath = args.Length > first ? args[first] : null;
            LaunchOptions.OutputPath = args.Length > first + 1 ? args[first + 1] : "output.txt";
            if (quiet)
            {
                int exitCode = 0;
                try
                {
                    if (LaunchOptions.InputPath == null)
                    {
                        throw new ArgumentException("Hiányzó input fájl.");
                    }
                    MineScriptEngine.RunHeadless(LaunchOptions.InputPath, LaunchOptions.OutputPath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    exitCode = 1;
                }
                Shutdown(exitCode);
                return;
            }
            new MainWindow().Show();
        }
    }
}