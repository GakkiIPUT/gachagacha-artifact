using System.Collections.Generic;
using System.Windows;

namespace Gacha.Views
{
    public partial class OcrSlotDialog : Window
    {
        public string? SlotKey { get; private set; } // "flower","plume","sands","goblet","circlet"

        public OcrSlotDialog(string uiLang, string? lastSlotKey = null)
        {
            InitializeComponent();
            var items = BuildItems(uiLang);
            Cmb.ItemsSource = items;
            // 直近値があれば選択
            if (!string.IsNullOrEmpty(lastSlotKey))
            {
                for (int i = 0; i < items.Count; i++)
                    if (items[i].Key == lastSlotKey) { Cmb.SelectedIndex = i; break; }
            }
            if (Cmb.SelectedIndex < 0) Cmb.SelectedIndex = 0;
        }

        record Item(string Key, string Label);

        static List<Item> BuildItems(string lang) => lang switch
        {
            "ja" => new()
            {
                new("flower","花（生の花）"),
                new("plume","羽（死の羽）"),
                new("sands","砂（時の砂）"),
                new("goblet","杯（空の杯）"),
                new("circlet","冠（理の冠）"),
            },
            "zh" => new()
            {
                new("flower","生之花"),
                new("plume","死之羽"),
                new("sands","时之沙"),
                new("goblet","空之杯"),
                new("circlet","理之冠"),
            },
            _ => new() // en
            {
                new("flower","Flower of Life"),
                new("plume","Plume of Death"),
                new("sands","Sands of Eon"),
                new("goblet","Goblet of Eonothem"),
                new("circlet","Circlet of Logos"),
            }
        };

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (Cmb.SelectedItem is Item it)
            {
                SlotKey = it.Key;
                DialogResult = true;
                Close();
            }
        }
    }
}
