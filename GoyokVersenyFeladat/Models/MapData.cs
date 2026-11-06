using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyokVersenyFeladat.Models
{
    internal class MapData
    {
        public MapData(int height, int width, string[] rows)
        {
            Height = height;
            Width = width;
            Rows = rows;
        }

        public MapData()
        {
        }

        public int Height { get; set; }
        public int Width { get; set; }
        public string[] Rows { get; set; }
    }
}
