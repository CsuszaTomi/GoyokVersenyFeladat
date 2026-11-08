using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Minescript
{
    internal class Tokenizer
    {
        public List<ProgramLine> Tokenize(string[] scriptlines)
        {
            List<ProgramLine> sorok = new List<ProgramLine>();
            for (int i = 0; i < scriptlines.Length; i++)
            {
                string sor = scriptlines[i];
                if(string.IsNullOrWhiteSpace(sor))
                {
                    continue;
                }
                int indentLevel = CountIndent(sor);
                string vagottsor = sor.Trim();
                string[] bontas = vagottsor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                List<Token> tokens = new List<Token>();
                foreach (var resz in bontas)
                {
                    Token token = ClassifyToken(resz);
                    tokens.Add(token);
                }
                sorok.Add(new ProgramLine
                {
                    LineNumber = i + 1,
                    IndentLevel = indentLevel,
                    Tokens = tokens
                });
            }
            return sorok;
        }

        private int CountIndent(string line)
        {
            int db = 0;
            foreach (char c in line)
            {
                if (c == ' ')
                {
                    db++;
                }
                else if (c == '\t')
                {
                    db += 4; //tab +4
                }
                else
                {
                    break; //nem szokoz
                }
            }
            return db;
        }

        /// <summary>
        /// A nyelv foglalt szavai. Változónév nem lehet ezek egyike, kis- és nagybetű kombinációtól függetlenül
        /// (a blokkszerkesztő ezzel ellenőrizze a beírt változónevet).
        /// </summary>
        public static readonly HashSet<string> ReservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and", "by", "else", "false", "forward", "here", "if", "isThere", "mine",
            "not", "or", "print", "set", "step", "to", "true", "turn", "while"
        };


        public static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name) || ReservedWords.Contains(name) || !IsAsciiLetter(name[0]))
            {
                return false;
            }
            return name.All(c => IsAsciiLetter(c) || (c >= '0' && c <= '9'));
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private Token ClassifyToken(string word)
        {
            switch (word)
            {
                case "set":
                case "to":
                case "if":
                case "else":
                case "while":
                case "step":
                case "forward":
                case "turn":
                case "by":
                case "mine":
                case "here":
                case "print":
                    return new Token(TokenType.Keyword, word);
                case "isThere":
                    return new Token(TokenType.Condition, word);
                case "+":
                case "-":
                case "*":
                case "=":
                case "<>":
                case "<":
                case "<=":
                case ">":
                case ">=":
                case "and":
                case "or":
                case "not":
                    return new Token(TokenType.Operator, word);
                case "true":
                case "false":
                    return new Token(TokenType.Boolean, word);
            }
            if (int.TryParse(word, out _))
            {
                return new Token(TokenType.Number, word);
            }
            return new Token(TokenType.Identifier, word);
        }
    }

}
