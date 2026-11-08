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
        private static readonly SolidColorBrush ClrInvalidBorder = new(Color.FromRgb(0xE0, 0x40, 0x40));
        private static readonly SolidColorBrush ClrLabel = new(Color.FromRgb(0xAA, 0xAA, 0xCC));
        private static readonly SolidColorBrush ClrHint = new(Color.FromRgb(0x44, 0x44, 0x44));
        private static readonly SolidColorBrush ClrDelete = new(Color.FromRgb(0x88, 0x88, 0x88));
        private static readonly SolidColorBrush ClrChildZone = new(Color.FromArgb(0x80, 0x18, 0x19, 0x22));
        private static readonly SolidColorBrush ClrElseZone = new(Color.FromArgb(0x80, 0x22, 0x14, 0x14));

        // A set blokk értékrészének fajtái (Params[1]) és a hozzájuk tartozó mezők:
        // Params: 0 = ident, 1 = fajta, 2 = első simpleval, 3 = operátor, 4 = második simpleval
        private const string KindSimple = "simple";   // set x to a
        private const string KindBinop = "a op b";    // set x to a op b
        private const string KindNot = "not";         // set x to not a
        private const string KindIsThere = "isThere"; // set x to isThere a
        private static readonly string[] SetKinds = [KindSimple, KindBinop, KindNot, KindIsThere];
        private static readonly string[] BinOperators = ["+", "-", "*", "=", "<>", "<", "<=", ">", ">=", "and", "or"];

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
            string kw = e.Data.GetData(typeof(string)) as string;
            if (!IsAcceptedKeyword(kw)) return;
            _roots.Add(new Block { Keyword = kw, Depth = 0 });
            Render();
        }

        /// <summary>
        /// Az isThere nem önálló blokk (értékblokk, csak a set része), ezért önmagában nem szúrható be.
        /// </summary>
        static bool IsAcceptedKeyword(string kw) => !string.IsNullOrEmpty(kw) && kw != "isThere";

        static bool IsControl(string kw) => kw is "if" or "ifelse" or "while";

        /// <summary>
        /// A blokk mélysége a fában (gyökér = 0), -1 ha nincs a fában.
        /// A mélységet a fából számoljuk, így nem függ a Block.Depth mezőtől.
        /// </summary>
        static int FindDepth(List<Block> list, Block target, int depth)
        {
            foreach (var b in list)
            {
                if (ReferenceEquals(b, target)) return depth;
                int d = FindDepth(b.Children, target, depth + 1);
                if (d >= 0) return d;
                d = FindDepth(b.ElseChildren, target, depth + 1);
                if (d >= 0) return d;
            }
            return -1;
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
            Brush bg = IsControl(node.Keyword)
                ? (Brush)FindResource("BlockCtrl")
                : node.Keyword is "set"
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

            if (IsControl(node.Keyword))
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

            var childPanel = new StackPanel { AllowDrop = true, Background = Brushes.Transparent };

            foreach (var child in childList)
            {
                hint.Visibility = Visibility.Collapsed;
                childPanel.Children.Add(BuildUI(child, childList));
            }

            DragEventHandler onDrop = (_, e2) =>
            {
                e2.Handled = true; // különben a külső panel is megkapná, és gyökérblokkot hozna létre

                string kw = e2.Data.GetData(typeof(string)) as string;
                if (!IsAcceptedKeyword(kw)) return;

                // Csak a vezérlőblokkok egymásba ágyazása korlátozott (max. 3 szint);
                // parancsblokk a legbelső szinten is elhelyezhető.
                int newDepth = FindDepth(_roots, parent, 0) + 1;
                if (IsControl(kw) && newDepth >= 3)
                {
                    MessageBox.Show("Maximum 3 szint mélységig ágyazhatsz be.",
                        "Mélység korlát", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                childList.Add(new Block { Keyword = kw, Depth = newDepth });
                Render();
            };

            childPanel.Drop += onDrop;
            childPanel.Children.Add(hint);

            // A Drop a zóna keretén is figyel: a keret belső margója és a gyerekblokkok fölé ejtés
            // is ide érkezik (felfelé buborékol), nem a gyökér panelre.
            var zone = new Border
            {
                Background = zoneBg,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(12, 8, 0, 0),
                Padding = new Thickness(8, 6, 8, 6),
                MinHeight = 28,
                AllowDrop = true,
                Child = childPanel
            };
            zone.Drop += onDrop;
            return zone;
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
                        AddSetFields(header, node);
                        break;
                    }
                case "if":
                case "ifelse":
                case "while":
                    {
                        // a feltétel egyetlen simpleval (ident | number | boolean-lit)
                        var tb = MakeTextBox(node.Params.Count > 0 ? node.Params[0] : "x", 72);
                        tb.TextChanged += (_, _) => SyncParam(node, 0, tb.Text);
                        header.Children.Add(tb);
                        break;
                    }
            }
        }

        /// <summary>
        /// set ⟨ident⟩ to ⟨calculatedval⟩: az érték fajtáját legördülő választja ki
        /// (simpleval | a op b | not a | isThere a), a hozzá tartozó mezőkkel.
        /// </summary>
        void AddSetFields(StackPanel header, Block node)
        {
            string kind = GetSetKind(node);

            // változónév: csak érvényes azonosító kerülhet a kódba
            var tbVar = MakeTextBox(node.Params.Count > 0 ? node.Params[0] : "x", 64);
            tbVar.TextChanged += (_, _) =>
            {
                bool ok = Tokenizer.IsValidIdentifier(tbVar.Text);
                tbVar.BorderBrush = ok ? ClrInputBorder : ClrInvalidBorder;
                tbVar.ToolTip = ok ? null : "Érvénytelen változónév (betűvel kezdődjön, csak betű/szám lehet, és nem lehet kulcsszó)";
                if (ok) SyncParam(node, 0, tbVar.Text);
            };
            tbVar.LostFocus += (_, _) =>
            {
                if (Tokenizer.IsValidIdentifier(tbVar.Text)) return;
                string rejected = tbVar.Text;
                tbVar.Text = SafeParam(node, 0, "x"); // visszaáll az utolsó érvényes névre
                MessageBox.Show($"A(z) \"{rejected}\" nem használható változónévnek: kulcsszó vagy érvénytelen azonosító.",
                    "Érvénytelen változónév", MessageBoxButton.OK, MessageBoxImage.Warning);
            };
            header.Children.Add(tbVar);
            header.Children.Add(MakeLabel("to"));

            // az érték fajtája
            var cbKind = MakeCombo(SetKinds, kind, 84);
            cbKind.SelectionChanged += (_, _) =>
            {
                string v = (cbKind.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? KindSimple;
                if (v == GetSetKind(node)) return;
                SyncParam(node, 1, v);
                Dispatcher.BeginInvoke(new Action(Render)); // a mezők a fajtától függnek
            };
            header.Children.Add(cbKind);

            // első operandus (mindegyik fajtánál van)
            var tbA = MakeTextBox(SafeParam(node, 2, DefaultFirst(kind)), 52);
            tbA.TextChanged += (_, _) => SyncParam(node, 2, tbA.Text);
            header.Children.Add(tbA);

            if (kind == KindBinop)
            {
                var cbOp = MakeCombo(BinOperators, SafeParam(node, 3, "+"), 56);
                cbOp.SelectionChanged += (_, _) =>
                    SyncParam(node, 3, (cbOp.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "+");
                header.Children.Add(cbOp);

                var tbB = MakeTextBox(SafeParam(node, 4, "0"), 52);
                tbB.TextChanged += (_, _) => SyncParam(node, 4, tbB.Text);
                header.Children.Add(tbB);
            }
        }

        static string DefaultFirst(string kind) => kind switch
        {
            KindNot => "true",
            KindIsThere => "1",
            _ => "0"
        };

        string GetSetKind(Block node)
        {
            string k = SafeParam(node, 1, KindSimple);
            return Array.IndexOf(SetKinds, k) >= 0 ? k : KindSimple;
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
                sb.AppendLine($"{pad}if {SafeParam(node, 0, "x")}");
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
                "set" => SetLine(node),
                "if" => $"if {SafeParam(node, 0, "x")}",
                "while" => $"while {SafeParam(node, 0, "x")}",
                _ => node.Keyword
            };
            sb.AppendLine(pad + line);
            foreach (var child in node.Children)
                AppendCode(sb, child, indent + 1);
        }

        string SetLine(Block node)
        {
            string kind = GetSetKind(node);
            string name = SafeParam(node, 0, "x");
            string a = SafeParam(node, 2, DefaultFirst(kind));
            return kind switch
            {
                KindBinop => $"set {name} to {a} {SafeParam(node, 3, "+")} {SafeParam(node, 4, "0")}",
                KindNot => $"set {name} to not {a}",
                KindIsThere => $"set {name} to isThere {a}",
                _ => $"set {name} to {a}"
            };
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