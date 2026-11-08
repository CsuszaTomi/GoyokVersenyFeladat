using GoyokVersenyFeladat.Interfaces;
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
            string[] scriptLines = new string[]
            {
    "set count to 0",
    "while count < 2",
    "    step_forward",
    "    turn_by 90",
    "    set count to count + 1",
    "mine_here"
            };

            Tokenizer tokenizer = new Tokenizer();
            List<ProgramLine> program = tokenizer.Tokenize(scriptLines);

            VariableManager vm = new VariableManager();
            IRobotController dummyRobot = new TesztRobot();
            Interpreter interpreter = new Interpreter(vm, dummyRobot);

            interpreter.Execute(program);
        }
    }
}