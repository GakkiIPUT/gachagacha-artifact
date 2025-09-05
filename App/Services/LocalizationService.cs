using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Gacha.Services
{
    public static class LocalizationService
    {
        static readonly Dictionary<string, string> _dict = new(); // 現在言語用
        static readonly Dictionary<string, Dictionary<string,string>> _dictByLang = new(); // 全言語キャッシュ
        public static string CurrentLanguage { get; private set; } = "ja";

        public static bool SetLanguage(string langCode)
        {
            try
            {
                EnsureLoaded(langCode);            // キャッシュへ読み込み
                _dict.Clear();                     // 現在言語辞書を差し替え
                foreach (var kv in _dictByLang[langCode]) _dict[kv.Key] = kv.Value;

                CurrentLanguage = langCode;
                return true;
            }
            catch { return false; }
        }
        static void EnsureLoaded(string lang)
        {
            if (_dictByLang.ContainsKey(lang)) return;
            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, "Assets", "i18n", $"{lang}.json");
            var map = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                map = JsonSerializer.Deserialize<Dictionary<string, string>>(json, opts) ?? new();
            }
            _dictByLang[lang] = map;
        }

        static string T(string key) => _dict.TryGetValue(key, out var v) ? v : key;

        static string TL(string lang, string key)
        {
            EnsureLoaded(lang);
            return _dictByLang[lang].TryGetValue(key, out var v) ? v : key;
        }

        public static string TrSet(string setKey) => T($"set.{setKey}");
        public static string TrSlot(string slotKey) => T($"slot.{slotKey}");
        public static string Tr(string anyKey) => T(anyKey);

        
        // 直接言語指定
        public static string TrSetLang(string setKey, string lang) => TL(lang, $"set.{setKey}");
        public static string TrSlotLang(string slotKey, string lang) => TL(lang, $"slot.{slotKey}");
     
    }
}
