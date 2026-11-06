using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoyokVersenyFeladat.Minescript
{
    internal class InputReader
    {
        public (MapData mapdata, string[] script) ReadInput(string inputFilePath)
        {
            string[] sorok = File.ReadAllLines(inputFilePath).ToArray();
            int height = Convert.ToInt32(sorok[0].Split(" ")[0]);
            int width = Convert.ToInt32(sorok[0].Split(" ")[1]);
            string[] map = new string[height];
            for (int i = 0; i < height; i++)
            {
                map[i] = sorok[i + 1];
            }
            int minescriptStartIndex = height + 1;
            int minescriptLength = sorok.Length - minescriptStartIndex;
            string[] minescript = new string[minescriptLength];
            for (int i = 0; i < minescriptLength; i++)
            {
                minescript[i] = sorok[minescriptStartIndex + i];
            }
            //egy string az eddigi összes adatoról
            string allData = $"{height} {width}\n{string.Join("\n", map)}\n{string.Join("\n", minescript)}";
            MessageBox.Show(allData);
            MapData mapData = new MapData
            {
                Height = height,
                Width = width,
                Rows = map
            };
            return (mapData, minescript);
        }   
    }
}
