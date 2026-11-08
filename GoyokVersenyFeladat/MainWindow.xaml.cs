// lang: C#, file: MainWindow.xaml.cs
// target: Windows 11, .NET 8, WPF

using GoyokVersenyFeladat.Interfaces;
using GoyokVersenyFeladat.Minescript;
using GoyokVersenyFeladat.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoyokVersenyFeladat
{
    public partial class MainWindow : Window
    {
        private readonly List<Block> _roots = new();

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

        // ════════════════════════════════════════════
        // DRAG – palettáról
        // ════════════════════════════════════════════
        private void Block_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var border = (Border)sender;
            DragDrop.DoDragDrop(border, (string)border.Tag, DragDropEffects.Copy);
        }

        // ════════════════════════════════════════════
        // DROP – gyökér canvas
        // ════════════════════════════════════════════
        private void BlockPanel_Drop(object sender, DragEventArgs e)
        {
            string kw = (string)e.Data.GetData(typeof(string));
            var node = new Block { Keyword = kw, Depth = 0 };
            _roots.Add(node);
            Render();
        }

        // ════════════════════════════════════════════
        // RENDER
        // ════════════════════════════════════════════
        void Render()
        {
            BlockPanel.Children.Clear();
            foreach (var node in _roots)
                BlockPanel.Children.Add(BuildUI(node, _roots));
            RefreshCode();
        }

        Border BuildUI(Block node, List<Block> siblings)
        {
            Brush bg = node.Keyword is "if" or "while"
                ? (Brush)FindResource("BlockCtrl")
                : node.Keyword is "set" or "isThere"
                    ? (Brush)FindResource("BlockVal")
                    : (Brush)FindResource("BlockCmd");

            var header = new StackPanel { Orientation = Orientation.Horizontal };

            var label = new TextBlock
            {
                Text = "● " + node.Keyword,
                FontFamily = new FontFamily("Inter"),
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            header.Children.Add(label);

            AddParamFields(header, node);

            var del = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontSize = 13,
                Cursor = Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            del.Click += (_, _) =>
            {
                siblings.Remove(node);
                Render();
            };
            header.Children.Add(del);

            var stack = new StackPanel();
            stack.Children.Add(header);

            if (node.Keyword is "if" or "while")
            {
                var hint = new TextBlock
                {
                    Text = "Húzz blokkot ide…",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 10,
                    Margin = new Thickness(2, 0, 0, 0)
                };

                var childPanel = new StackPanel { AllowDrop = true };

                foreach (var child in node.Children)
                {
                    hint.Visibility = Visibility.Collapsed;
                    childPanel.Children.Add(BuildUI(child, node.Children));
                }

                childPanel.Drop += (_, e2) =>
                {
                    if (node.Depth + 1 >= 3)
                    {
                        MessageBox.Show("Maximum 3 szint mélységig ágyazhatsz be.",
                            "Mélység korlát", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    string kw = (string)e2.Data.GetData(typeof(string));
                    node.Children.Add(new Block { Keyword = kw, Depth = node.Depth + 1 });
                    e2.Handled = true;
                    Render();
                };

                childPanel.Children.Add(hint);

                var childZone = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x80, 0x18, 0x19, 0x22)),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(12, 8, 0, 0),
                    Padding = new Thickness(8, 6, 8, 6),
                    AllowDrop = true,
                    Child = childPanel
                };

                stack.Children.Add(childZone);
            }

            var outer = new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4),
                AllowDrop = true,
                Child = stack
            };

            outer.MouseMove += (_, e2) =>
            {
                if (e2.LeftButton != MouseButtonState.Pressed) return;
                var pos = e2.GetPosition(BlockPanel);
                if (pos.X < 0 || pos.Y < 0 ||
                    pos.X > BlockPanel.ActualWidth ||
                    pos.Y > BlockPanel.ActualHeight)
                {
                    siblings.Remove(node);
                    Render();
                }
            };

            return outer;
        }

        // ════════════════════════════════════════════
        // INLINE PARAMÉTER MEZŐK
        // ════════════════════════════════════════════
        void AddParamFields(StackPanel header, Block node)
        {
            switch (node.Keyword)
            {
                case "step_forward":
                case "turn_by":
                    {
                        var tb = MakeTextBox(node.Params.Count > 0 ? node.Params[0] : "1", 40);
                        tb.TextChanged += (_, _) => SyncParam(node, 0, tb.Text);
                        header.Children.Add(tb);
                        break;
                    }
                case "print":
                    {
                        var tb = MakeTextBox(node.Params.Count > 0 ? node.Params[0] : "x", 72);
                        tb.TextChanged += (_, _) => SyncParam(node, 0, tb.Text);
                        header.Children.Add(tb);
                        break;
                    }
                case "set":
                    {
                        var tbVar = MakeTextBox(node.Params.Count > 0 ? node.Params[0] : "x", 52);
                        var eq = MakeLabel("=");
                        var tbVal = MakeTextBox(node.Params.Count > 1 ? node.Params[1] : "0", 64);
                        tbVar.TextChanged += (_, _) => SyncParam(node, 0, tbVar.Text);
                        tbVal.TextChanged += (_, _) => SyncParam(node, 1, tbVal.Text);
                        header.Children.Add(tbVar);
                        header.Children.Add(eq);
                        header.Children.Add(tbVal);
                        break;
                    }
                case "isThere":
                    {
                        var cb = MakeCombo(new[] { "wall", "gold", "empty" },
                                           node.Params.Count > 0 ? node.Params[0] : "wall");
                        cb.SelectionChanged += (_, _) =>
                            SyncParam(node, 0, (cb.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "wall");
                        header.Children.Add(cb);
                        break;
                    }
                case "if":
                case "while":
                    {
                        var tbLeft = MakeTextBox(node.Params.Count > 0 ? node.Params[0] : "x", 52);
                        var cbOp = MakeCombo(new[] { "==", "!=", "<", "<=", ">", ">=" },
                                                node.Params.Count > 1 ? node.Params[1] : "==", 52);
                        var tbRight = MakeTextBox(node.Params.Count > 2 ? node.Params[2] : "0", 52);

                        tbLeft.TextChanged += (_, _) => SyncParam(node, 0, tbLeft.Text);
                        cbOp.SelectionChanged += (_, _) =>
                            SyncParam(node, 1, (cbOp.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "==");
                        tbRight.TextChanged += (_, _) => SyncParam(node, 2, tbRight.Text);

                        header.Children.Add(tbLeft);
                        header.Children.Add(cbOp);
                        header.Children.Add(tbRight);
                        break;
                    }
            }
        }

        void SyncParam(Block node, int idx, string val)
        {
            while (node.Params.Count <= idx) node.Params.Add("");
            node.Params[idx] = val;
            RefreshCode();
        }

        // ════════════════════════════════════════════
        // KÓD GENERÁLÁS
        // ════════════════════════════════════════════
        void RefreshCode()
        {
            var sb = new StringBuilder();
            foreach (var node in _roots)
                AppendCode(sb, node, 0);
            CodeTextBlock.Text = sb.ToString();
        }

        void AppendCode(StringBuilder sb, Block node, int indent)
        {
            string pad = new string(' ', indent * 4);

            string line = node.Keyword switch
            {
                "set" => $"set {SafeParam(node, 0)} {SafeParam(node, 1)}",
                "step_forward" => $"step_forward {SafeParam(node, 0, "1")}",
                "turn_by" => $"turn_by {SafeParam(node, 0, "1")}",
                "print" => $"print {SafeParam(node, 0, "x")}",
                "mine_here" => "mine_here",
                "isThere" => $"isThere {SafeParam(node, 0, "wall")}",
                "if" => $"if {SafeParam(node, 0, "x")} {SafeParam(node, 1, "==")} {SafeParam(node, 2, "0")}",
                "while" => $"while {SafeParam(node, 0, "x")} {SafeParam(node, 1, "==")} {SafeParam(node, 2, "0")}",
                _ => node.Keyword
            };

            sb.AppendLine(pad + line);

            foreach (var child in node.Children)
                AppendCode(sb, child, indent + 1);
        }

        string SafeParam(Block node, int idx, string fallback = "") =>
            idx < node.Params.Count && !string.IsNullOrEmpty(node.Params[idx])
                ? node.Params[idx]
                : fallback;

        // ════════════════════════════════════════════
        // HELPER FACTORY-K
        // ════════════════════════════════════════════
        TextBox MakeTextBox(string text, double width) => new()
        {
            Text = text,
            Width = width,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2D, 0x3A)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x50, 0x70)),
            BorderThickness = new Thickness(1),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Padding = new Thickness(4, 2, 4, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        };

        TextBlock MakeLabel(string text) => new()
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xCC)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        };

        ComboBox MakeCombo(string[] items, string selected, double width = 72)
        {
            var cb = new ComboBox
            {
                Width = width,
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2D, 0x3A)),
                Foreground = Brushes.Black,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x50, 0x70)),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            cb.Resources.Add(SystemColors.WindowBrushKey,
                new SolidColorBrush(Color.FromRgb(0x2A, 0x2D, 0x3A)));
            cb.Resources.Add(SystemColors.WindowTextBrushKey, Brushes.Black);
            cb.Resources.Add(SystemColors.HighlightBrushKey,
                new SolidColorBrush(Color.FromRgb(0x4A, 0x50, 0x70)));
            cb.Resources.Add(SystemColors.HighlightTextBrushKey, Brushes.White);

            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, Brushes.Black));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(0x2A, 0x2D, 0x3A))));
            cb.ItemContainerStyle = itemStyle;

            foreach (var item in items)
            {
                var cbi = new ComboBoxItem { Content = item };
                if (item == selected) cbi.IsSelected = true;
                cb.Items.Add(cbi);
            }
            return cb;
        }
    }
}