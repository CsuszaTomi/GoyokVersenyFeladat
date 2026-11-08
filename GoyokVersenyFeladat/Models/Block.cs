using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Models
{
    class Block
    {
        public Block()
        {
            Keyword = string.Empty;
            Params = new();
            Children = new();
            ElseChildren = new();
        }

        public Block(string keyword, List<string> @params, List<Block> children)
        {
            Keyword = keyword;
            Params = @params;
            Children = children;
            ElseChildren = new();
        }

        public string Keyword { get; set; }
        public List<string> Params { get; set; }
        public List<Block> Children { get; set; }
        public List<Block> ElseChildren { get; set; }
        public int Depth { get; set; }

    }
}