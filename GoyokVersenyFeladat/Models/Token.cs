using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Models
{
    public enum TokenType
    {
        Keyword,     // set, to, if, else, while
        Command,     // step_forward, turn_by, mine_here, print
        Condition,   // isThere
        Identifier,  // változónevek (pl. count, i)
        Number,      // egész számok (0-100)
        Symbol,      // pályaelemek: #, @, .
        Unknown      // ismeretlen / hibás token
    }

    internal class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
