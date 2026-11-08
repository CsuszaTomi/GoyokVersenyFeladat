using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;

namespace GoyokVersenyFeladat.Minescript
{
    /// <summary>
    /// A parancssori indítás adatai (az App tölti ki, a főablak olvashatja).
    /// </summary>
    internal static class LaunchOptions
    {
        public static bool Quiet;
        public static string InputPath;   // pl. input_001.txt
        public static string OutputPath;  // pl. output_001.txt
    }

    internal static class MineScriptEngine
    {
        /// <summary>
        /// Csendes (-q) mód: beolvas, lefuttat, naplót ír. Nincs ablak és nincs MessageBox.
        /// A robot itt grafikától független (SimpleRobot); a UI-s robotnak ugyanazt a logikát kell követnie.
        /// </summary>
        public static void RunHeadless(string inputPath, string outputPath)
        {
            var (map, script) = new InputReader().ReadInput(inputPath);
            List<ProgramLine> program = new Tokenizer().Tokenize(script);
            var logger = new FileLogger(outputPath);
            var interpreter = new Interpreter(new VariableManager(), new SimpleRobot(map), logger, Console.WriteLine);
            try
            {
                interpreter.Execute(program);
            }
            catch
            {
                logger.Flush(); // váratlan hibánál is maradjon napló
                throw;
            }
        }
    }
}