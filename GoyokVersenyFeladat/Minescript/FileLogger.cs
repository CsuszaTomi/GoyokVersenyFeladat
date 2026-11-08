using GoyokVersenyFeladat.Interfaces;
using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Minescript
{


    internal class FileLogger : ILogger
    {
        private readonly List<string> lines = new List<string>();
        private readonly string path;

        public FileLogger(string path)
        {
            this.path = path;
        }

        public IReadOnlyList<string> Lines => lines;

        public void Begin() => lines.Clear();

        public void BeginLine(int lineNumber) => lines.Add(lineNumber.ToString());

        public void Append(string text) => lines[lines.Count - 1] += " " + text;

        public void Error(string code) => lines[lines.Count - 1] += " " + code;

        public void End(bool limitExceeded)
        {
            lines.Add(limitExceeded ? "INSTRUCTION_COUNT_LIMIT_EXCEEDED" : "EOR");
        }

        public void Flush()
        {
            File.WriteAllLines(path, lines);
        }
    }

    /// <summary>
    /// Kézi irányításhoz, nem naplóz semmit.
    /// </summary>
    internal class NullLogger : ILogger
    {
        public void Begin() { }
        public void BeginLine(int lineNumber) { }
        public void Append(string text) { }
        public void Error(string code) { }
        public void End(bool limitExceeded) { }
        public void Flush() { }
    }
}