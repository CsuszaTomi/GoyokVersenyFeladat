using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Models
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

        private Token ClassifyToken(string word)
        {
            // Kulcsszavak
            if (word == "set" || word == "to" || word == "if" || word == "else" || word == "while")
            {
                return new Token(TokenType.Keyword, word);
            }
            // Robot parancsok
            if (word == "step_forward" || word == "turn_by" || word == "mine_here" || word == "print")
            {
                return new Token(TokenType.Command, word);
            }
            // Feltétel lekérdezés
            if (word == "isThere")
            {
                return new Token(TokenType.Condition, word);
            }
            // Pályaelemek / Szimbólumok
            if (word == "#" || word == "@" || word == ".")
            {
                return new Token(TokenType.Symbol, word);
            }
            // Számok ellenőrzése
            if (int.TryParse(word, out _))
            {
                return new Token(TokenType.Number, word);
            }
            // Ha egyik sem, akkor változónév (Identifier)
            return new Token(TokenType.Identifier, word);
        }
    }
}
