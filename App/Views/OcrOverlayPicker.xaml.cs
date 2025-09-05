using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Gacha.Views
{
    public partial class OcrOverlayPicker : Window
    {
        Point _start;
        bool _drag;
        public int X { get; private set; }
        public int Y { get; private set; }
        public int W { get; private set; }
        public int H { get; private set; }

        public OcrOverlayPicker()
        {
            InitializeComponent();
            Loaded += (_, __) => Keyboard.Focus(this);
        }

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _drag = true;
            _start = e.GetPosition(this);
            Selection.Visibility = Visibility.Visible;
            Canvas.SetLeft(Selection, _start.X);
            Canvas.SetTop(Selection, _start.Y);
            Selection.Width = 0;
            Selection.Height = 0;
        }

        void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_drag)
                return;
            var p = e.GetPosition(this);
            var x = Math.Min(_start.X, p.X);
            var y = Math.Min(_start.Y, p.Y);
            var w = Math.Abs(_start.X - p.X);
            var h = Math.Abs(_start.Y - p.Y);
            Canvas.SetLeft(Selection, x);
            Canvas.SetTop(Selection, y);
            Selection.Width = w;
            Selection.Height = h;
            Info.Text = $"X={x:F0} Y={y:F0} W={w:F0} H={h:F0}";
        }

        void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_drag)
                return;
            _drag = false;
            var p = e.GetPosition(this);
            var xDip = Math.Min(_start.X, p.X);
            var yDip = Math.Min(_start.Y, p.Y);
            var wDip = Math.Abs(_start.X - p.X);
            var hDip = Math.Abs(_start.Y - p.Y);

            // 画面座標(物理解像度)へ
            var tlScreen = PointToScreen(new Point(xDip, yDip)); // 物理px
            X = (int)Math.Round(tlScreen.X);
            Y = (int)Math.Round(tlScreen.Y);

            // W/H はDIP→Device変換
            var src = PresentationSource.FromVisual(this);
            var m = src?.CompositionTarget?.TransformToDevice;
            double sx = m?.M11 ?? 1.0,
                sy = m?.M22 ?? 1.0;
            W = (int)Math.Round(wDip * sx);
            H = (int)Math.Round(hDip * sy);

            DialogResult = (W > 2 && H > 2);
            Close();
        }
    }
}
