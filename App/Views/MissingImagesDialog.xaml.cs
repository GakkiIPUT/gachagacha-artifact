using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using Gacha.ViewModels;
using Gacha.Services;

namespace Gacha.Views
{
    public partial class MissingImagesDialog : Window
    {
        readonly ObservableCollection<ArtifactVM> _items;
        public MissingImagesDialog(ObservableCollection<ArtifactVM> items)
        {
            InitializeComponent();
            _items = items;
            DataContext = _items;
        }

        void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            ImageProvider.OpenImagesFolder();
        }

        void Browse_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not ArtifactVM a) return;

            var dlg = new OpenFileDialog
            {
                Title = "画像ファイルを選択",
                Filter = "画像 (*.png;*.jpg;*.jpeg;*.webp;*.gif)|*.png;*.jpg;*.jpeg;*.webp;*.gif|すべてのファイル (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() == true)
            {
                var paths = ImageProvider.SaveImage(a.SetKey, a.SlotKey, dlg.FileName);
                a.ImagePath = paths.Path;
                a.HasImage = true;
                // プレースホルダは保持（使われなくなるだけ）
                Grid.Items.Refresh();
            }
        }
    }
}
