using GoyokVersenyFeladat.Minescript;
using GoyokVersenyFeladat.Models;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoyokVersenyFeladat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static InputReader inpReader = new();
        public MainWindow()
        {
            InitializeComponent();
            var (mapData, scriptLines) = inpReader.ReadInput("pelda.txt");
            Tokenizer tokenizer = new Tokenizer();
            List<ProgramLine> parsedLines = tokenizer.Tokenize(scriptLines);
            if (parsedLines.Count > 0)
            {
                var firstLine = parsedLines[0];
                MessageBox.Show($"1. sor tokenjei ({firstLine.Tokens.Count} db, behúzás: {firstLine.IndentLevel}):\n" +
                                string.Join("\n", firstLine.Tokens.Select(t => $"{t.Type}: {t.Value}")));
            }
        }
    }
}