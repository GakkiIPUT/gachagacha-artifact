using System;
using System.Windows;

namespace Gacha.Views
{
    public partial class OcrRegionDialog : Window
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int W { get; private set; }
        public int H { get; private set; }

        public OcrRegionDialog(int x, int y, int w, int h)
        {
            InitializeComponent();
            TbX.Text = x.ToString();
            TbY.Text = y.ToString();
            TbW.Text = w.ToString();
            TbH.Text = h.ToString();
        }

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TbX.Text, out var x) &&
                int.TryParse(TbY.Text, out var y) &&
                int.TryParse(TbW.Text, out var w) &&
                int.TryParse(TbH.Text, out var h) &&
                w > 0 && h > 0)
            {
                X = x; Y = y; W = w; H = h;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("数値を確認してください。", "OCR領域", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
