using GoyokVersenyFeladat.Interfaces;
using GoyokVersenyFeladat.Minescript;
using GoyokVersenyFeladat.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoyokVersenyFeladat.Engine
{
    public partial class Engine : UserControl, IRobotController
    {
        // Irányok: 0 kelet, 1 észak, 2 nyugat, 3 dél (a balra fordulás +1). Cellák: 0 üres, 1 fal, 2 arany.
        private static readonly int[] DRow = { 0, -1, 0, 1 };
        private static readonly int[] DCol = { 1, 0, -1, 0 };
        private static Brush B(string hex) => (Brush)new BrushConverter().ConvertFromString(hex);
        private static readonly Brush Fog = B("#0D0F14"), Floor = B("#2B3142"), Rock = B("#5C6478"), RobotBrush = B("#4A8AF4"), Back = B("#1B1E27");
        private static readonly Pen GridPen = new Pen(B("#1B1F2A"), 1);

        private int[,] start, cells;
        private bool[,] seen;
        private int rows, cols, startRow, startCol, startDir, row, col, dir, mined;

        /// <summary>Kézi vezérlésnél lefut: a MainWindow ezen állítsa le a futó programot és ne naplózzon.</summary>
        public event Action ManualControl;

        // A térkép külön elemre rajzol, amit a gyökér Grid első gyerekeként veszünk fel (a gombsor fölé nem kerül).
        private sealed class Surface : FrameworkElement
        {
            public Action<DrawingContext, double, double> Paint;

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);
                Paint?.Invoke(dc, ActualWidth, ActualHeight);
            }
        }

        private readonly Surface surface = new Surface();

        public Engine()
        {
            InitializeComponent();
            surface.Paint = Draw;
            surface.Margin = new Thickness(0, 0, 0, Bar.Height);
            ((Panel)Content).Children.Insert(0, surface);
        }

        /// <summary>A térkép sorai (csak a térkép, a "3 15" sor és a kód nélkül). Alaphelyzetbe is állít.</summary>
        public void LoadMap(string[] lines)
        {
            rows = lines.Length;
            cols = lines[0].Length;
            start = new int[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    char ch = lines[i][j];
                    start[i, j] = ch == '#' ? 1 : ch == '@' ? 2 : 0;
                    int d = ">^<v".IndexOf(ch);
                    if (d >= 0) { startRow = i; startCol = j; startDir = d; }
                }
            Reset();
        }

        /// <summary>Térkép és robot alaphelyzetbe (minden futtatás elején hívandó).</summary>
        public void Reset()
        {
            cells = (int[,])start.Clone();
            seen = new bool[rows, cols];
            row = startRow; col = startCol; dir = startDir; mined = 0;
            seen[row, col] = true;
            Redraw();
        }

        // ---------- IRobotController (hiba esetén a hibakód a kivétel szövege) ----------

        public void StepForward()
        {
            if (Front() == 1) throw new MineScriptException("E_BEDROCK_HIT");
            row += DRow[dir]; col += DCol[dir];
            seen[row, col] = true;
            Redraw();
        }

        /// <summary>fok = negyedfordulatok száma balra (nem fok!), 0 esetén nem változik az irány.</summary>
        public void TurnBy(int fok)
        {
            if (fok < 0) throw new MineScriptException("E_BAD_ARG");
            dir = (dir + fok % 4) % 4;
            Redraw();
        }

        public void MineHere()
        {
            if (cells[row, col] != 2) throw new MineScriptException("E_NO_GOLD");
            cells[row, col] = 0;
            mined++;
            Redraw();
        }

        /// <summary>objectType: "0" üres, "1" fal, "2" arany. A vizsgált mező felfedezetté válik.</summary>
        public bool IsThere(string objectType)
        {
            if (!int.TryParse(objectType, out int w) || w < 0 || w > 2) throw new MineScriptException("E_BAD_ARG");
            int fr = row + DRow[dir], fc = col + DCol[dir];
            if (fr >= 0 && fr < rows && fc >= 0 && fc < cols) seen[fr, fc] = true;
            Redraw();
            return Front() == w;
        }

        // ---------- kézi vezérlés (a gombok a XAML-ben) ----------

        private void Manual(Action a)
        {
            ManualControl?.Invoke();
            if (cells == null) return;
            try { a(); } catch (MineScriptException) { } // falnak ütközés / nincs arany: hiba nélkül megáll
        }

        private void Turn_Click(object sender, RoutedEventArgs e) => Manual(() => TurnBy(1));
        private void Step_Click(object sender, RoutedEventArgs e) => Manual(StepForward);
        private void Mine_Click(object sender, RoutedEventArgs e) => Manual(MineHere);

        // ---------- megjelenítés ----------

        private int At(int r, int c) => r < 0 || r >= rows || c < 0 || c >= cols ? 1 : cells[r, c];

        private int Front() => At(row + DRow[dir], col + DCol[dir]); // a térképen kívül fal

        private void Redraw()
        {
            Status.Text = $"Arany: {mined}   Szenzor: {new[] { "üres", "fal", "arany" }[Front()]}";
            surface.InvalidateVisual();
        }

        private void Draw(DrawingContext dc, double w, double h)
        {
            dc.DrawRectangle(Back, null, new Rect(0, 0, w, h));
            if (cells == null) return;

            double s = Math.Min(w / cols, h / rows);
            if (s <= 0) return;
            double ox = (w - s * cols) / 2, oy = (h - s * rows) / 2;

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    var rect = new Rect(ox + j * s, oy + i * s, s, s);
                    Brush b = !seen[i, j] ? Fog : cells[i, j] == 1 ? Rock : Floor;
                    dc.DrawRectangle(b, GridPen, rect);
                    if (seen[i, j] && cells[i, j] == 2)
                        dc.DrawEllipse(Brushes.Gold, null, new Point(rect.X + s / 2, rect.Y + s / 2), s * 0.25, s * 0.25);
                }

            // robot: háromszög a nézési irányba + szenzorcsík (zöld üres, piros fal, arany)
            double cx = ox + (col + 0.5) * s, cy = oy + (row + 0.5) * s;
            double dx = DCol[dir], dy = DRow[dir], px = -dy, py = dx;
            var tip = new Point(cx + dx * s * 0.35, cy + dy * s * 0.35);
            var left = new Point(cx - dx * s * 0.2 + px * s * 0.25, cy - dy * s * 0.2 + py * s * 0.25);
            var right = new Point(cx - dx * s * 0.2 - px * s * 0.25, cy - dy * s * 0.2 - py * s * 0.25);

            int f = Front();
            var beam = new Pen(f == 1 ? Brushes.Red : f == 2 ? Brushes.Gold : Brushes.LimeGreen, 3);
            dc.DrawLine(beam, tip, new Point(cx + dx * s * 0.95, cy + dy * s * 0.95));

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(tip, true, true);
                ctx.LineTo(left, true, false);
                ctx.LineTo(right, true, false);
            }
            dc.DrawGeometry(RobotBrush, null, geo);
        }
    }
}