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
        private static readonly SolidColorBrush ClrInput = new(Color.FromRgb(0x2A, 0x2D, 0x3A));
        private static readonly SolidColorBrush ClrInputBorder = new(Color.FromRgb(0x4A, 0x50, 0x70));
        private static readonly SolidColorBrush ClrLabel = new(Color.FromRgb(0xAA, 0xAA, 0xCC));
        private static readonly SolidColorBrush ClrHint = new(Color.FromRgb(0x44, 0x44, 0x44));
        private static readonly SolidColorBrush ClrDelete = new(Color.FromRgb(0x88, 0x88, 0x88));
        private static readonly SolidColorBrush ClrChildZone = new(Color.FromArgb(0x80, 0x18, 0x19, 0x22));
        private static readonly SolidColorBrush ClrElseZone = new(Color.FromArgb(0x80, 0x22, 0x14, 0x14));

        private readonly List<Block> _roots = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Block_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var border = (Border)sender;
            DragDrop.DoDragDrop(border, (string)border.Tag, DragDropEffects.Copy);
        }

        private void BlockPanel_Drop(object sender, DragEventArgs e)
        {
            string kw = (string)e.Data.GetData(typeof(string));
            _roots.Add(new Block { Keyword = kw, Depth = 0 });
            Render();
        }

        void Render()
        {
            BlockPanel.Children.Clear();
            foreach (var node in _roots)
                BlockPanel.Children.Add(BuildUI(node, _roots));
            RefreshCode();
        }

        Border BuildUI(Block node, List<Block> siblings)
        {
            Brush bg = node.Keyword is "if" or "ifelse" or "while"
                ? (Brush)FindResource("BlockCtrl")
                : node.Keyword is "set" or "isThere"
                    ? (Brush)FindResource("BlockVal")
                    : (Brush)FindResource("BlockCmd");

            var header = new StackPanel { Orientation = Orientation.Horizontal };

            string label = node.Keyword == "ifelse" ? "if-else" : node.Keyword;
            header.Children.Add(new TextBlock
            {
                Text = "● " + label,
                FontFamily = new FontFamily("Inter"),
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });

            AddParamFields(header, node);

            var del = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = ClrDelete,
                FontSize = 13,
                Cursor = Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            del.Click += (_, _) => { siblings.Remove(node); Render(); };
            header.Children.Add(del);

            var stack = new StackPanel();
            stack.Children.Add(header);

            if (node.Keyword is "if" or "ifelse" or "while")
            {
                stack.Children.Add(BuildChildZone(node, node.Children, ClrChildZone));
            }

            if (node.Keyword is "ifelse")
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "else",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x88, 0x44)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(4, 6, 0, 2)
                });
                stack.Children.Add(BuildChildZone(node, node.ElseChildren, ClrElseZone));
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

        Border BuildChildZone(Block parent, List<Block> childList, SolidColorBrush zoneBg)
        {
            var hint = new TextBlock
            {
                Text = "Húzz blokkot ide…",
                Foreground = ClrHint,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Margin = new Thickness(2, 0, 0, 0)
            };

            var childPanel = new StackPanel { AllowDrop = true };

            foreach (var child in childList)
            {
                hint.Visibility = Visibility.Collapsed;
                childPanel.Children.Add(BuildUI(child, childList));
            }

            childPanel.Drop += (_, e2) =>
            {
                if (parent.Depth + 1 >= 3)
                {
                    MessageBox.Show("Maximum 3 szint mélységig ágyazhatsz be.",
                        "Mélység korlát", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string kw = (string)e2.Data.GetData(typeof(string));
                childList.Add(new Block { Keyword = kw, Depth = parent.Depth + 1 });
                e2.Handled = true;
                Render();
            };

            childPanel.Children.Add(hint);

            return new Border
            {
                Background = zoneBg,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(12, 8, 0, 0),
                Padding = new Thickness(8, 6, 8, 6),
                AllowDrop = true,
                Child = childPanel
            };
        }

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
                        var tbVal = MakeTextBox(node.Params.Count > 1 ? node.Params[1] : "0", 64);
                        tbVar.TextChanged += (_, _) => SyncParam(node, 0, tbVar.Text);
                        tbVal.TextChanged += (_, _) => SyncParam(node, 1, tbVal.Text);
                        header.Children.Add(tbVar);
                        header.Children.Add(MakeLabel("="));
                        header.Children.Add(tbVal);
                        break;
                    }
                case "isThere":
                    {
                        var cb = MakeCombo(["wall", "gold", "empty"],
                                           node.Params.Count > 0 ? node.Params[0] : "wall");
                        cb.SelectionChanged += (_, _) =>
                            SyncParam(node, 0, (cb.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "wall");
                        header.Children.Add(cb);
                        break;
                    }
                case "if":
                case "ifelse":
                case "while":
                    {
                        var tbLeft = MakeTextBox(node.Params.Count > 0 ? node.Params[0] : "x", 52);
                        var cbOp = MakeCombo(["==", "!=", "<", "<=", ">", ">="],
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

            if (node.Keyword == "ifelse")
            {
                sb.AppendLine($"{pad}if {SafeParam(node, 0, "x")} {SafeParam(node, 1, "==")} {SafeParam(node, 2, "0")}");
                foreach (var child in node.Children)
                    AppendCode(sb, child, indent + 1);
                sb.AppendLine($"{pad}else");
                foreach (var child in node.ElseChildren)
                    AppendCode(sb, child, indent + 1);
                return;
            }

            string line = node.Keyword switch
            {
                "step_forward" => $"step forward {SafeParam(node, 0, "1")}",
                "turn_by" => $"turn by {SafeParam(node, 0, "1")}",
                "mine_here" => "mine here",
                "print" => $"print {SafeParam(node, 0, "x")}",
                "set" => $"set {SafeParam(node, 0, "x")} to {SafeParam(node, 1, "0")}",
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

        TextBox MakeTextBox(string text, double width) => new()
        {
            Text = text,
            Width = width,
            Background = ClrInput,
            Foreground = Brushes.White,
            BorderBrush = ClrInputBorder,
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
            Foreground = ClrLabel,
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
                Background = ClrInput,
                Foreground = Brushes.Black,
                BorderBrush = ClrInputBorder,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };

            cb.Resources.Add(SystemColors.WindowBrushKey, ClrInput);
            cb.Resources.Add(SystemColors.WindowTextBrushKey, Brushes.Black);
            cb.Resources.Add(SystemColors.HighlightBrushKey, ClrInputBorder);
            cb.Resources.Add(SystemColors.HighlightTextBrushKey, Brushes.White);

            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, Brushes.Black));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, ClrInput));
            cb.ItemContainerStyle = itemStyle;

            foreach (var item in items)
                cb.Items.Add(new ComboBoxItem { Content = item, IsSelected = item == selected });

            return cb;
        }
    }
}