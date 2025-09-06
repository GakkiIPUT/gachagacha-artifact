using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Gacha.Services;

namespace Gacha.Services.Ocr
{
    public sealed class OcrParsed
    {
        public double CR { get; init; }
        public double CD { get; init; }
        public double ATKp { get; init; }
        public double HPp { get; init; }
        public double DEFp { get; init; }
        public double EM { get; init; }
        public double HPf { get; init; }
        public double ATKf { get; init; }
        public double DEFf { get; init; }
        public double ER { get; init; }
        public string? Title { get; init; }
        public string? SlotLabel { get; init; }
        public string? SlotKey { get; init; }
        public string? MainStatName { get; init; }
        public double MainStatValue { get; init; }
        public int Level { get; init; }
        public string? SetNameCandidate { get; init; }
        public bool Any => CR > 0 || CD > 0 || ATKp > 0 || HPp > 0 || DEFp > 0 || EM > 0;
        public string Debug { get; init; } = "";
    }

    public static class OcrParserService
    {
        // ===== 既存ユーティリティ =====
        static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var t = s;
            t = t.Replace('％', '%').Replace('．', '.')
                .Replace('，', '.')
                .Replace('：', ':').Replace('＋', '+')
                .Replace("Elem. Mastery", "Elemental Mastery")
                .Replace('ｰ', 'ー');
            t = Regex.Replace(t, @"\s+", " ").Trim();
            var map = "０１２３４５６７８９";
            for (int i = 0; i < map.Length; i++)
                t = t.Replace(map[i].ToString(), i.ToString());
            return t;
        }

        static double Num(string s)
        {
            s = s.Trim().Replace(",", "."); // カンマ小数も許容
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

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

        static bool IsSlotJP(string s, out string key)
        {
            var jp = Regex.Replace(s, @"\s+", ""); key = "";
            if (jp == "生の花") { key = "flower"; return true; }
            if (jp == "死の羽") { key = "plume"; return true; }
            if (jp == "時の砂") { key = "sands"; return true; }
            if (jp == "空の杯") { key = "goblet"; return true; }
            if (jp == "理の冠") { key = "circlet"; return true; }
            return false;
        }
        static bool IsSlotEN(string s, out string key)
        {
            s = s.ToLowerInvariant(); key = "";
            if (s.Contains("flower of life")) { key = "flower"; return true; }
            if (s.Contains("plume of death")) { key = "plume"; return true; }
            if (s.Contains("sands of eon")) { key = "sands"; return true; }
            if (s.Contains("goblet of eonothem")) { key = "goblet"; return true; }
            if (s.Contains("circlet of logos")) { key = "circlet"; return true; }
            return false;
        }
        static bool IsSlotZH(string s, out string key)
        {
            var zh = s.Replace(" ", ""); key = "";
            if (zh.Contains("生之花")) { key = "flower"; return true; }
            if (zh.Contains("死之羽")) { key = "plume"; return true; }
            if (zh.Contains("时之沙")) { key = "sands"; return true; }
            if (zh.Contains("空之杯")) { key = "goblet"; return true; }
            if (zh.Contains("理之冠")) { key = "circlet"; return true; }
            return false;
        }

        // ===== 追加: ヒントを優先採用するパース =====
        public static OcrParsed ParseWithHints(
            string uiLang,
            OcrHints hints,
            ArtifactPieceMap? pieceMap = null,
            ArtifactSetList? setList = null,
            ArtifactSubStatList? subList = null,
            ArtifactMainStatTable? mainTable = null)
        {
            // 1) 下準備：ブロックをまとめた raw を作る（従来の正規表現抽出にもかける）
            var rawBlocks = new[]
            {
                hints.Title, hints.SlotLabel, hints.MainName, hints.MainValue, hints.SetBlock, hints.SubBlock
            };
            var raw = string.Join("\n", rawBlocks.Where(s => !string.IsNullOrWhiteSpace(s)));
            var text = Normalize(raw);
            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

            // 2) 上部情報（ヒント優先）
            string? title = hints.Title?.Trim();
            string? slotLabel = hints.SlotLabel?.Trim();
            string? slotKey = null;
            if (!string.IsNullOrWhiteSpace(slotLabel))
            {
                if (!IsSlotJP(slotLabel, out slotKey) && !IsSlotEN(slotLabel, out slotKey) && !IsSlotZH(slotLabel, out slotKey))
                    slotKey = null;
            }

            // 3) メイン種別・値（ヒント優先）
            string? mainName = null;
            double mainValue = 0;

            if (!string.IsNullOrWhiteSpace(hints.MainName))
            {
                mainName = hints.MainName.Trim();
                if (mainTable != null)
                {
                    var best = mainTable.FindBestMainStat(mainName);
                    if (!string.IsNullOrWhiteSpace(best)) mainName = best;
                }
            }
            if (mainValue == 0 && !string.IsNullOrWhiteSpace(hints.MainValue))
            {
                // "46.6%" / "46,6 %" / "187" などに対応
                var s = hints.MainValue.Replace("%", "").Trim();
                mainValue = Num(s);
                // ％が含まれていたらそのまま。含まれていなくても整数/少数はそのまま保持
            }

            // 4) セット候補（薄緑ブロックから最もそれっぽい行を採用）
            string? setCandidate = null;
            if (!string.IsNullOrWhiteSpace(hints.SetBlock))
            {
                var setLines = Normalize(hints.SetBlock).Split('\n');
                // 2セット/2-Piece検出の直前行優先
                for (int i = 0; i < setLines.Length; i++)
                {
                    var l = setLines[i];
                    if (Regex.IsMatch(l, @"^(2セット|2-Piece|2件套)\b", RegexOptions.IgnoreCase))
                    {
                        if (i - 1 >= 0 && !string.IsNullOrWhiteSpace(setLines[i - 1]))
                        {
                            setCandidate = setLines[i - 1].Trim();
                            break;
                        }
                    }
                }
                // 見つからなければ行中から一番それっぽいものをセット辞書で当てる
                if (setCandidate == null && setList != null)
                {
                    foreach (var l in setLines.Reverse())
                    {
                        if (string.IsNullOrWhiteSpace(l)) continue;
                        var best = setList.FindBest(l);
                        if (!string.IsNullOrWhiteSpace(best)) { setCandidate = best; break; }
                    }
                }
            }

            // 5) まだ欠ける要素は従来パーサに委譲
            var fallback = Parse(raw, uiLang, pieceMap, setList, subList, mainTable);

            // 6) マージ（ヒント優先）
            string? finalTitle = title ?? fallback.Title;
            string? finalSlotLabel = slotLabel ?? fallback.SlotLabel;
            string? finalSlotKey = slotKey ?? fallback.SlotKey;
            string? finalMainName = mainName ?? fallback.MainStatName;
            double finalMainValue = mainValue > 0 ? mainValue : fallback.MainStatValue;
            string? finalSet = setCandidate ?? fallback.SetNameCandidate;
            int finalLevel = fallback.Level; // レベルは従来抽出でOK

            // 7) サブは従来抽出結果をそのまま採用（必要ならヒントSubBlockからの補正も可能）
            return new OcrParsed
            {
                CR = fallback.CR,
                CD = fallback.CD,
                ATKp = fallback.ATKp,
                HPp = fallback.HPp,
                DEFp = fallback.DEFp,
                EM = fallback.EM,
                HPf = fallback.HPf,
                ATKf = fallback.ATKf,
                DEFf = fallback.DEFf,
                ER = fallback.ER,
                Title = finalTitle,
                SlotLabel = finalSlotLabel,
                SlotKey = finalSlotKey,
                MainStatName = finalMainName,
                MainStatValue = finalMainValue,
                Level = finalLevel,
                SetNameCandidate = finalSet,
                Debug = fallback.Debug,
            };
        }

        // ===== 従来の全文OCRパース（既存ロジック） =====
        public static OcrParsed Parse(
            string raw,
            string uiLang,
            ArtifactPieceMap? pieceMap = null,
            ArtifactSetList? setList = null,
            ArtifactSubStatList? subList = null,
            ArtifactMainStatTable? mainTable = null)
        {
            var text = Normalize(raw);
            var lines = text.Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();

            string? title = null, slotLabel = null, slotKey = null, mainName = null, setCandidate = null;
            double mainValue = 0; int level = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i]; if (string.IsNullOrWhiteSpace(l)) continue;

                if (mainName == null)
                {
                    if (Regex.IsMatch(l, @"^(HP|ATK|DEF|Elemental Mastery|攻撃力|防御力|元素熟知|生命值|攻击力|防御|元素精通)\b", RegexOptions.IgnoreCase))
                    {
                        mainName = l.Trim();
                        if (i + 1 < lines.Length)
                        {
                            var v = lines[i + 1];
                            var m = Regex.Match(v, @"^([0-9]+(?:\.[0-9]+)?)\s*%?$");
                            if (m.Success) mainValue = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                        }
                    }
                }

                if (level == 0 && Regex.IsMatch(l, @"^\+?\s*([0-9]{1,2})\s*$"))
                    level = int.Parse(Regex.Match(l, @"([0-9]{1,2})").Groups[1].Value);

                if (slotKey == null)
                {
                    if (IsSlotJP(l, out var k1)) { slotLabel = l.Trim(); slotKey = k1; }
                    else if (IsSlotEN(l, out var k2)) { slotLabel = l.Trim(); slotKey = k2; }
                    else if (IsSlotZH(l, out var k3)) { slotLabel = l.Trim(); slotKey = k3; }
                }

                if (mainName == null)
                {
                    if (Regex.IsMatch(l,
                        @"^(HP|攻撃力|防御力|元素熟知|Elemental Mastery|ATK|DEF|HP|生命值|攻击力|防御|元素精通)\b",
                        RegexOptions.IgnoreCase))
                    {
                        mainName = l.Trim();
                        if (i + 1 < lines.Length)
                        {
                            var v2 = lines[i + 1];
                            var m2 = Regex.Match(v2, @"^([0-9]+(?:\.[0-9]+)?)\s*%?$");
                            if (m2.Success) mainValue = double.Parse(m2.Groups[1].Value, CultureInfo.InvariantCulture);
                        }
                    }
                }

                if (setCandidate == null && Regex.IsMatch(l, @"^(2セット|2-Piece|2件套)\b", RegexOptions.IgnoreCase))
                {
                    if (i - 1 >= 0 && !string.IsNullOrWhiteSpace(lines[i - 1])) setCandidate = lines[i - 1].Trim();
                }

                if (title == null && l.Length >= 4) title = l.Trim();
            }

            if (setCandidate == null)
            {
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    var l = lines[i]; if (string.IsNullOrWhiteSpace(l)) continue;
                    if (Regex.IsMatch(l, @"^\+\s*\d{1,2}\s*$")) continue;
                    if (Regex.IsMatch(l, @"[+＋]\s*\d")) continue;
                    if (Regex.IsMatch(l, @"^\d+(?:\.\d+)?\s*%?$")) continue;
                    if (IsSlotJP(l, out _) || IsSlotEN(l, out _) || IsSlotZH(l, out _)) continue;
                    setCandidate = l.Trim(); break;
                }
            }

            if (slotKey == null && pieceMap != null && !string.IsNullOrWhiteSpace(title))
            {
                var guessed = pieceMap.FindBestSlotKey(title);
                if (!string.IsNullOrEmpty(guessed))
                {
                    slotKey = guessed;
                    slotLabel ??= title;
                }
            }

            // メイン名の辞書補正
            if (!string.IsNullOrWhiteSpace(mainName) && mainTable != null)
            {
                var bestMain = mainTable.FindBestMainStat(mainName);
                if (!string.IsNullOrWhiteSpace(bestMain)) mainName = bestMain;
            }
            // セット候補の辞書補正
            if (!string.IsNullOrWhiteSpace(setCandidate) && setList != null)
            {
                var bestSet = setList.FindBest(setCandidate);
                if (!string.IsNullOrWhiteSpace(bestSet)) setCandidate = bestSet;
            }

            double cr = 0, cd = 0, atkp = 0, hpp = 0, defp = 0, em = 0, hpF = 0, atkF = 0, defF = 0, er = 0;
            foreach (var line in lines)
            {
                var l = line; var lJP = Regex.Replace(l, @"\s+", "");

                if (er == 0) er = TryMatch(l, @"元素チャージ効率\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"ENERGY\s*RECHARGE\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"元素充能效率\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (cr == 0) cr = TryMatch(lJP, @"会心率\+?([0-9]+(?:\.[0-9]+)?)%");
                if (cr == 0) cr = TryMatch(l, @"CRIT\s*RATE\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"暴击率\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (cd == 0) cd = TryMatch(lJP, @"会心ダメ(?:ー|ｰ)?(?:ジ|ージ)?\+?([0-9]+(?:\.[0-9]+)?)%");
                if (cd == 0) cd = TryMatch(l, @"CRIT\s*DMG\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"暴击伤害\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (atkp == 0) atkp = TryMatch(lJP, @"攻撃力\+?([0-9]+(?:\.[0-9]+)?)%");
                if (atkp == 0) atkp = TryMatch(l, @"ATK\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"攻击力\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (hpp == 0) hpp = TryMatch(l, @"HP\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"生命值\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (defp == 0) defp = TryMatch(lJP, @"防御力\+?([0-9]+(?:\.[0-9]+)?)%");
                if (defp == 0) defp = TryMatch(l, @"DEF\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%", @"防御\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\s*%");
                if (em == 0) em = TryMatch(lJP, @"元素熟知\+?([0-9]+(?:\.[0-9]+)?)\b");
                if (em == 0) em = TryMatch(l, @"ELEMENTAL\s*MASTERY\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\b", @"元素精通\s*\+?\s*([0-9]+(?:\.[0-9]+)?)\b");

                if (!l.Contains("%"))
                {
                    if (hpF == 0) hpF = TryMatch(lJP, @"HP\+([0-9]+)");
                    if (hpF == 0) hpF = TryMatch(l, @"HP\s*\+\s*([0-9]+)", @"生命值\s*\+\s*([0-9]+)");
                    if (atkF == 0) atkF = TryMatch(lJP, @"攻撃力\+([0-9]+)");
                    if (atkF == 0) atkF = TryMatch(l, @"ATK\s*\+\s*([0-9]+)", @"攻击力\s*\+\s*([0-9]+)");
                    if (defF == 0) defF = TryMatch(lJP, @"防御力\+([0-9]+)");
                    if (defF == 0) defF = TryMatch(l, @"DEF\s*\+\s*([0-9]+)", @"防御\s*\+\s*([0-9]+)");
                }
            }

            return new OcrParsed
            {
                CR = cr, CD = cd, ATKp = atkp, HPp = hpp, DEFp = defp, EM = em,
                HPf = hpF, ATKf = atkF, DEFf = defF, ER = er,
                Title = title, SlotLabel = slotLabel, SlotKey = slotKey,
                MainStatName = mainName, MainStatValue = mainValue, Level = level,
                SetNameCandidate = setCandidate, Debug = string.Join(" / ", lines.Take(6)),
            };
        }
    }
}
