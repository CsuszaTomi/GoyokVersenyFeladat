using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Models
{
    internal class ProgramLine
    {
        public ProgramLine(int lineNumber, int indentLevel, List<Token> tokens)
        {
            LineNumber = lineNumber;
            IndentLevel = indentLevel;
            Tokens = tokens;
        }

        public ProgramLine()
        {
        }

        public int LineNumber { get; set; }
        public int IndentLevel { get; set; }
        public List<Token> Tokens { get; set; } = new List<Token>();
    }
}
