using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Gacha.ViewModels;
using Gacha.Services;

namespace Gacha.Views
{
    public partial class SetFilterDialog : Window
    {
        readonly ObservableCollection<SetOption> _items;
        public IReadOnlyList<string> ResultKeys { get; private set; } = new List<string>();

        public SetFilterDialog(IEnumerable<SetOption> options)
        {
            InitializeComponent();
            _items = new ObservableCollection<SetOption>(options);
            DataContext = _items;
            List.ItemsSource = _items; // 初期表示
            // ローカライズ（タイトル／ボタン／検索ツールチップ）
            this.Title = LocalizationService.Tr("ui.set_filter");
            SearchBox.ToolTip = LocalizationService.Tr("ui.search");
            BtnSelectAll.Content = LocalizationService.Tr("ui.select_all");
            BtnClearAll.Content  = LocalizationService.Tr("ui.clear_all");
            BtnOk.Content        = LocalizationService.Tr("ui.ok");
            BtnCancel.Content    = LocalizationService.Tr("ui.cancel");
            // 簡易検索
            SearchBox.TextChanged += (_, __) =>
            {
                var q = SearchBox.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(q))
                {
                    List.ItemsSource = _items;
                    return;
                }
                var ql = q.ToLowerInvariant();
                List.ItemsSource = _items.Where(x =>
                    (
                        !string.IsNullOrEmpty(x.Name)
                        && x.Name.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0
                    ) || (!string.IsNullOrEmpty(x.Tokens) && x.Tokens.Contains(ql))
                );
            };
        }

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResultKeys = _items.Where(x => x.IsChecked).Select(x => x.Key).ToArray();
            DialogResult = true;
            Close();
        }

        void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var it in _items)
                it.IsChecked = true;
            List.Items.Refresh();
        }

        void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var it in _items)
                it.IsChecked = false;
            List.Items.Refresh();
        }
    }
}
