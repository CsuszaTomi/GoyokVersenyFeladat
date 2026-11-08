using GoyokVersenyFeladat.Minescript;
using GoyokVersenyFeladat.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoyokVersenyFeladat
{
    /// <summary>
    /// A blokkszerkesztő (MainWindow), az Interpreter és a robot szimulátor (Engine) összekötése:
    /// betöltés, futtatás (lassítva), nyomkövetés, leállítás, kézi vezérlés, konzol, változók, napló.
    /// Külön partial fájlban van, hogy ne ütközzön a szerkesztő kódjával.
    /// </summary>
    public partial class MainWindow
    {
        private static readonly SolidColorBrush ClrCurrentLineBg = new(Color.FromRgb(0x5A, 0x4A, 0x10));
        private static readonly SolidColorBrush ClrCurrentLineFg = new(Color.FromRgb(0xFF, 0xE0, 0x6A));
        private static readonly SolidColorBrush ClrStatusError = new(Color.FromRgb(0xFF, 0x8A, 0x80));
        private static readonly SolidColorBrush ClrStatusOk = new(Color.FromRgb(0x8A, 0xD8, 0x9A));

        // A robot szimulátor (Engine.xaml, változatlanul) külön ablakban nyílik meg; az ablakot kódból hozzuk létre.
        private Window _robotWindow;
        private Engine.Engine _robot;
        private Engine.Engine Robot => _robot;
        private bool _exiting; // true: a főablak záródik, a szimulátor ablak is tényleg bezárulhat

        private Interpreter _interpreter;
        private FileLogger _logger;
        private DispatcherTimer _timer;
        private string _logPath = "output.txt";
        private string[] _sessionLines = Array.Empty<string>(); // a futó program sorai (a kiemeléshez)
        private bool _sessionActive;                            // fut vagy nyomkövetés alatt áll egy program
        private bool _mapLoaded;

        // ---------- inicializálás ----------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateRobotWindow();
            Closing += (_, _) => _exiting = true; // a főablakkal együtt zár be

            _logPath = LaunchOptions.OutputPath ?? "output.txt";
            _logger = new FileLogger(_logPath);
            _interpreter = new Interpreter(new VariableManager(), Robot, _logger, PrintToConsole);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(IntervalMs()) };
            _timer.Tick += (_, _) => StepAndUpdate();

            Robot.ManualControl += OnManualControl;

            LoadInput();
            UpdateButtons();
        }

        /// <summary>Az indításkor megadott input fájl: térkép a szimulátorba, program a szerkesztőbe.</summary>
        private void LoadInput()
        {
            string path = LaunchOptions.InputPath;
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Nincs bemeneti fájl megadva", true);
                return;
            }
            try
            {
                var (map, script) = new InputReader().ReadInput(path);
                Robot.LoadMap(map.Rows);
                _mapLoaded = true;
                LoadProgramIntoEditor(script);
                SetStatus("Betöltve: " + Path.GetFileName(path), false);
            }
            catch (Exception ex)
            {
                SetStatus("Hiba a betöltéskor", true);
                MessageBox.Show("A bemeneti fájl nem olvasható:\n" + ex.Message, "Betöltési hiba");
            }
        }

        /// <summary>
        /// A betöltött programot megmutatja a szöveges nézetben, és ezt futtatja a Futtatás/Debug.
        /// TODO (blokkszerkesztő): ide kell a szöveg -> blokkfa átalakítás is, hogy a blokk nézet is kitöltődjön
        /// (_roots feltöltése, majd Render()). Az első blokkszerkesztés után a szöveget már a blokkok generálják.
        /// </summary>
        private void LoadProgramIntoEditor(string[] script)
        {
            _roots.Clear();
            CodeTextBlock.Text = string.Join("\n", script);
        }

        // ---------- szimulátor ablak ----------

        /// <summary>
        /// Létrehozza és megnyitja a szimulátor ablakot: az Engine vezérlő egy kódból készített ablak tartalma.
        /// Bezárás helyett elrejtőzik, így a robot állapota megmarad, és a Szimulátor gombbal újra megnyitható.
        /// </summary>
        private void CreateRobotWindow()
        {
            _robot = new Engine.Engine();
            _robotWindow = new Window
            {
                Title = "Robot szimulátor",
                Content = _robot,
                Width = 560,
                Height = 600,
                MinWidth = 360,
                MinHeight = 300,
                Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x1E, 0x27)),
                Owner = this
            };
            _robotWindow.Closing += (_, e) =>
            {
                if (_exiting) return;
                e.Cancel = true;
                _robotWindow.Hide();
            };
            PositionRobotWindow();
            _robotWindow.Show();
        }

        /// <summary>A szimulátor a főablak mellé kerül (ha elfér a képernyőn), különben a jobb szélre.</summary>
        private void PositionRobotWindow()
        {
            var area = SystemParameters.WorkArea;
            _robotWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            _robotWindow.Left = Math.Max(area.Left, Math.Min(Left + Width, area.Right - _robotWindow.Width));
            _robotWindow.Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - _robotWindow.Height));
        }

        /// <summary>Megnyitja / előtérbe hozza a szimulátor ablakot (ha bezárták vagy takarásban van).</summary>
        private void ShowRobotWindow()
        {
            if (!_robotWindow.IsVisible) _robotWindow.Show();
            if (_robotWindow.WindowState == WindowState.Minimized) _robotWindow.WindowState = WindowState.Normal;
            _robotWindow.Activate();
        }

        private void Robot_Click(object sender, RoutedEventArgs e) => ShowRobotWindow();

        // ---------- gombok ----------

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionActive) _timer.Start();   // szüneteltetett nyomkövetés folytatása
            else StartSession(paused: false);
            UpdateButtons();
        }

        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionActive) StartSession(paused: true);
            UpdateButtons();
        }

        /// <summary>Egy sor végrehajtása. Futás közben szünetelteti az automatikus futást.</summary>
        private void Step_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionActive) return;
            _timer.Stop();
            StepAndUpdate();
            UpdateButtons();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionActive) return;
            _interpreter.Abort();
            EndSession();
            SetStatus("Leállítva", false);
        }

        private void Log_Click(object sender, RoutedEventArgs e)
        {
            string full = Path.GetFullPath(_logPath);
            if (!File.Exists(full))
            {
                MessageBox.Show("Még nincs naplófájl: " + full, "Napló");
                return;
            }
            Process.Start(new ProcessStartInfo(full) { UseShellExecute = true });
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_timer != null) _timer.Interval = TimeSpan.FromMilliseconds(IntervalMs());
        }

        /// <summary>Jobbra húzva gyorsabb: a csúszka 20..1000 értékéből 1000..20 ms késleltetés lesz.</summary>
        private double IntervalMs() => 1020 - SpeedSlider.Value;

        /// <summary>A szimulátor kézi vezérlése megszakítja a futó programot, és nem naplóz.</summary>
        private void OnManualControl()
        {
            if (!_sessionActive) return;
            _interpreter.Abort();
            EndSession();
            SetStatus("Kézi irányítás: a program futása megszakadt", false);
        }

        // ---------- futás / nyomkövetés ----------

        private void StartSession(bool paused)
        {
            if (!_mapLoaded)
            {
                MessageBox.Show("Nincs betöltött térkép, így nem lehet futtatni.", "Futtatás");
                return;
            }

            string[] lines = ProgramLinesFromView();
            List<ProgramLine> program = new Tokenizer().Tokenize(lines);

            _sessionLines = lines;
            ConsoleBox.Clear();
            Robot.Reset();                 // a térkép és a robot alaphelyzetbe kerül
            ShowRobotWindow();             // futtatáskor a szimulátornak látszania kell
            BlockCanvas.IsEnabled = false; // futás közben nem szerkeszthető a program
            _sessionActive = true;

            try
            {
                _interpreter.Reset(program); // törli a naplót és a változókat
            }
            catch (Exception ex)
            {
                EndSession();
                SetStatus("Belső hiba: " + ex.Message, true);
                return;
            }

            UpdateVariables();
            if (_interpreter.IsFinished)
            {
                FinishSession(-1); // üres program
                return;
            }

            ShowCode(_sessionLines, _interpreter.CurrentLineNumber);
            SetStatus(paused ? "Nyomkövetés: nyomd a Lépés gombot" : "Fut...", false);
            if (!paused) _timer.Start();
        }

        private void StepAndUpdate()
        {
            if (!_sessionActive)
            {
                _timer.Stop();
                return;
            }

            int line = _interpreter.CurrentLineNumber;
            bool running;
            try
            {
                running = _interpreter.StepOnce();
            }
            catch (Exception ex) // pl. hiányos blokk miatt csonka programsor
            {
                _interpreter.Abort();
                EndSession();
                ReportStatus("Belső hiba (hiányos blokk?): " + ex.Message, true);
                return;
            }

            UpdateVariables();
            if (running)
            {
                ShowCode(_sessionLines, _interpreter.CurrentLineNumber);
                SetStatus($"Lépés: {_interpreter.InstructionCount}  |  következő sor: {_interpreter.CurrentLineNumber}", false);
            }
            else
            {
                FinishSession(line);
            }
        }

        /// <summary>A futás véget ért (EOR, hiba vagy limit): eredmény kiírása, a felület visszaáll.</summary>
        private void FinishSession(int lastLine)
        {
            string code = _interpreter.ErrorCode;
            if (code == null)
            {
                ReportStatus("Kész: a program lefutott (EOR)", false);
            }
            else if (code == "INSTRUCTION_COUNT_LIMIT_EXCEEDED")
            {
                ReportStatus("Leállt: INSTRUCTION_COUNT_LIMIT_EXCEEDED (az utasításszám-limit túllépve)", true);
            }
            else
            {
                ReportStatus($"Hiba: {code} ({lastLine}. sor)", true);
            }
            EndSession(keepStatus: true);
        }

        private void EndSession(bool keepStatus = false)
        {
            _timer.Stop();
            _sessionActive = false;
            BlockCanvas.IsEnabled = true;
            CodeTextBlock.Text = string.Join("\n", _sessionLines); // kiemelés törlése
            UpdateButtons();
        }

        // ---------- megjelenítés ----------

        private string[] ProgramLinesFromView()
        {
            string text = CodeTextBlock.Text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
            return text.Split('\n');
        }

        /// <summary>A szöveges nézet kirajzolása, a megadott sorszámú sor kiemelésével (0 vagy -1: nincs kiemelés).</summary>
        private void ShowCode(string[] lines, int highlightLine)
        {
            CodeTextBlock.Inlines.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                var run = new Run(lines[i]);
                if (i + 1 == highlightLine)
                {
                    run.Background = ClrCurrentLineBg;
                    run.Foreground = ClrCurrentLineFg;
                }
                CodeTextBlock.Inlines.Add(run);
                if (i < lines.Length - 1) CodeTextBlock.Inlines.Add(new LineBreak());
            }
        }

        private void UpdateVariables()
        {
            VarList.ItemsSource = _interpreter.Variables.Select(kv => $"{kv.Key} = {kv.Value}").ToList();
        }

        private void PrintToConsole(string text)
        {
            ConsoleBox.AppendText(text + "\n");
            ConsoleBox.ScrollToEnd();
        }

        /// <summary>Állapotsor + konzolsor (hiba / végeredmény).</summary>
        private void ReportStatus(string text, bool error)
        {
            SetStatus(text, error);
            ConsoleBox.AppendText("» " + text + "\n");
            ConsoleBox.ScrollToEnd();
        }

        private void SetStatus(string text, bool error)
        {
            StatusBar.Text = text;
            StatusBar.Foreground = error ? ClrStatusError : ClrStatusOk;
        }

        private void UpdateButtons()
        {
            bool running = _timer != null && _timer.IsEnabled;
            RunButton.IsEnabled = !running;
            DebugButton.IsEnabled = !_sessionActive;
            StepButton.IsEnabled = _sessionActive;
            StopButton.IsEnabled = _sessionActive;
        }
    }
}