namespace Gacha.ViewModels
{
    public sealed class SetOption
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsChecked { get; set; }
        public string Tokens { get; set; } = ""; // ja/en/zh 名称と setKey を含む小文字連結
    }
}
