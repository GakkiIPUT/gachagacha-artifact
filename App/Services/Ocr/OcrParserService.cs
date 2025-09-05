using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gacha.Services.Ocr
{
    public sealed class OcrParsed
    {
        public double CR { get; init; } // 会心率(%)
        public double CD { get; init; } // 会心ダメ(%)
        public double ATKp { get; init; } // 攻撃%(サブ)
        public double HPp { get; init; } // HP%(サブ)
        public double DEFp { get; init; } // 防御%(サブ)
        public double EM { get; init; } // 熟知(数値)
        public double HPf { get; init; } // 例: HP+296
        public double ATKf { get; init; } // 例: 攻撃力+19 / ATK+19
        public double DEFf { get; init; } // 例: 防御力+23 / DEF+23
        public double ER { get; init; } // 例: 元素チャージ効率+12.3%
        public bool Any => CR > 0 || CD > 0 || ATKp > 0 || HPp > 0 || DEFp > 0 || EM > 0;
        public string Debug { get; init; } = "";
    }

    public static class OcrParserService
    {
        // 全角→半角、読点/小数点の正規化
        static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var t = s.Replace('％', '%')
                .Replace('．', '.')
                .Replace('，', '.')
                .Replace('：', ':')
                .Replace('＋', '+')
                .Replace("Elem. Mastery", "Elemental Mastery"); // 英略称吸収
            t = t.Replace('ｰ','ー');
            // 全角数字→半角
            var map = "０１２３４５６７８９";
            for (int i = 0; i < map.Length; i++)
                t = t.Replace(map[i].ToString(), i.ToString());
            return t;
        }

        static double Num(string s)
        {
            s = s.Trim().Replace(",", "."); // 万一のカンマ小数も許容
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return v;
            return 0;
        }

        // 1行からパターン抽出（%必須のものは%を強制）
        static double TryMatch(string line, params string[] patterns)
        {
            foreach (var p in patterns)
            {
                var m = Regex.Match(line, p, RegexOptions.IgnoreCase);
                if (m.Success && m.Groups.Count > 1)
                    return Num(m.Groups[1].Value);
            }
            return 0;
        }

        public static OcrParsed Parse(string raw, string uiLang)
        {
            var text = Normalize(raw);
            var lines = text.Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();

            // 日本語は OCR が内部に空白を挟むことがあるため、空白除去版も用意
            double cr=0, cd=0, atkp=0, hpp=0, defp=0, em=0, hpF=0, atkF=0, defF=0, er=0;
            foreach (var line in lines)
            {
                var l = line;
                var lJP = Regex.Replace(l, @"\s+", ""); // JPマッチ用：内部空白を除去

                // --- ER（読み取りのみ。スコアは不使用） ---
                if (er==0)
                    er = TryMatch(l,  // 英語/中国語は空白ありのまま
                        @"元素チャージ効率\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%",
                        @"ENERGY\s*RECHARGE\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%",
                        @"元素充能效率\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");

                // --- 会心率 ---
                if (cr==0) cr = TryMatch(lJP,  @"会心率\+?([0-9]+(?:\.[0-9]+)?)%");
                if (cr==0) cr = TryMatch(l,    @"CRIT\s*RATE\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"暴击率\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");

                // --- 会心ダメージ ---
                if (cd==0) cd = TryMatch(lJP,  @"会心ダメ(?:ー|ｰ)?(?:ジ|ージ)?\+?([0-9]+(?:\.[0-9]+)?)%");
                if (cd==0) cd = TryMatch(l,    @"CRIT\s*DMG\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"暴击伤害\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");

                // --- 攻撃%/HP%/防御%/熟知 ---
                if (atkp==0) atkp = TryMatch(lJP, @"攻撃力\+?([0-9]+(?:\.[0-9]+)?)%");  if (atkp==0) atkp = TryMatch(l, @"ATK\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"攻击力\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (hpp==0)  hpp  = TryMatch(l,   @"HP\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"生命值\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (defp==0) defp = TryMatch(lJP, @"防御力\+?([0-9]+(?:\.[0-9]+)?)%");  if (defp==0) defp = TryMatch(l, @"DEF\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"防御\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (em==0)   em   = TryMatch(lJP, @"元素熟知\+?([0-9]+(?:\.[0-9]+)?)\b"); if (em==0)   em   = TryMatch(l, @"ELEMENTAL\s*MASTERY\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\b", @"元素精通\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\b");

                // --- フラット値（％を含まない行のみ） ---
                if (!l.Contains("%"))
                {
                    if (hpF==0)  hpF  = TryMatch(lJP, @"HP\+([0-9]+)");        if (hpF==0)  hpF  = TryMatch(l, @"HP\s*\+\s*([0-9]+)", @"生命值\s*\+\s*([0-9]+)");
                    if (atkF==0) atkF = TryMatch(lJP, @"攻撃力\+([0-9]+)");     if (atkF==0) atkF = TryMatch(l, @"ATK\s*\+\s*([0-9]+)", @"攻击力\s*\+\s*([0-9]+)");
                    if (defF==0) defF = TryMatch(lJP, @"防御力\+([0-9]+)");     if (defF==0) defF = TryMatch(l, @"DEF\s*\+\s*([0-9]+)", @"防御\s*\+\s*([0-9]+)");
                }
            }



            return new OcrParsed {
                CR = cr, CD = cd, ATKp = atkp, HPp = hpp, DEFp = defp, EM = em,
                HPf = hpF, ATKf = atkF, DEFf = defF, ER = er,
                Debug = string.Join(" / ", lines.Take(6)),
            };
        }
    }
}
