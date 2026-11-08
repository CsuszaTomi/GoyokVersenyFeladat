using GoyokVersenyFeladat.Interfaces;
using GoyokVersenyFeladat.Models;
using System;

namespace GoyokVersenyFeladat.Minescript
{
    /// <summary>
    /// Egyszerű robot csak teszteléshez (a valódi UI-robot helyett).
    /// </summary>
    internal class SimpleRobot : IRobotController
    {
        private static readonly int[] DR = { 0, -1, 0, 1 }; // kelet, észak, nyugat, dél
        private static readonly int[] DC = { 1, 0, -1, 0 };
        private readonly char[][] grid;
        private int r, c, d;

        public SimpleRobot(MapData map)
        {
            grid = new char[map.Height][];
            for (int i = 0; i < map.Height; i++)
            {
                grid[i] = map.Rows[i].ToCharArray();
                for (int j = 0; j < grid[i].Length; j++)
                {
                    int dir = ">^<v".IndexOf(grid[i][j]);
                    if (dir >= 0) { r = i; c = j; d = dir; grid[i][j] = '.'; }
                }
            }
        }

        private int Front()
        {
            int nr = r + DR[d], nc = c + DC[d];
            if (nr < 0 || nc < 0 || nr >= grid.Length || nc >= grid[0].Length) return 1;
            return grid[nr][nc] == '#' ? 1 : grid[nr][nc] == '@' ? 2 : 0;
        }

        public void StepForward()
        {
            if (Front() == 1) throw new MineScriptException("E_BEDROCK_HIT");
            r += DR[d]; c += DC[d];
        }

        public void TurnBy(int n) { d = (d + n) % 4; }

        public void MineHere()
        {
            if (grid[r][c] != '@') throw new MineScriptException("E_NO_GOLD");
            grid[r][c] = '.';
        }

        public bool IsThere(string what) { return Front() == int.Parse(what); }
    }

    internal static class SelfTest
    {
        /// <summary>
        /// Pl. a Main elején: SelfTest.Run("input_001.txt", "output_001.txt");
        /// </summary>
        public static void Run(string inputPath, string outputPath)
        {
            var (map, script) = new InputReader().ReadInput(inputPath);
            var program = new Tokenizer().Tokenize(script);
            var interpreter = new Interpreter(new VariableManager(), new SimpleRobot(map), new FileLogger(outputPath), Console.WriteLine);
            interpreter.Execute(program);
        }
    }
}