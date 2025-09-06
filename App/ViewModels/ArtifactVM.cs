using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Gacha.Services;

namespace Gacha.ViewModels
{
    public sealed class ArtifactVM : INotifyPropertyChanged
    {
        public long GoodId { get; set; } // GOODの artifact.id（無ければ0）
        public event PropertyChangedEventHandler? PropertyChanged;

        void Raise([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        // --- OCRで拾った確認用の情報（スコア計算には未使用） ---
        public string? OcrSlotLabel { get; set; }      // 例: 時の砂 / Sands of Eon / 时之沙
        public string? OcrSetCandidate { get; set; }   // 緑行の直上などから拾った候補文字列
        public string? OcrMainName { get; set; }       // 例: 元素熟知 / HP% / ATK% など
        public double OcrMainValue { get; set; }      // 例: 187 / 46.6 など
                                                      // スコア対象外サブステ（確認用に保持）
        public double ER { get; set; }               // 元素チャージ効率 %
        public double HPf { get; set; }               // HP 実数
        public double ATKf { get; set; }               // 攻撃 実数
        public double DEFf { get; set; }               // 防御 実数

        // 表示用のまとめ（空なら出さない）
        public string NonScoringSummary =>
    string.Join(" / ", new[]
            {
                    ER  > 0 ? $"ER {ER:F1}%"   : null,
                    HPf > 0 ? $"HP {HPf:F0}"   : null,
                    ATKf> 0 ? $"ATK {ATKf:F0}" : null,
                    DEFf> 0 ? $"DEF {DEFf:F0}" : null,
            }.Where(s => s != null)) ?? string.Empty;

        public string MainStatDisplay =>
    !string.IsNullOrWhiteSpace(MainText)
                ? MainText
                : !string.IsNullOrWhiteSpace(OcrMainName)
                    ? (OcrMainValue > 0 ? $"{OcrMainName} {OcrMainValue}" : OcrMainName!)
                    : string.Empty;
        // 基本
        public string SetKey { get; init; } = "";
        public string SlotKey { get; init; } = ""; // flower/plume/sands/goblet/circlet
        public int Rarity { get; init; }
        public int Level { get; set; }
        public string MainText { get; set; } = ""; // 表示のみ（計算はサブのみ）

        // サブ（％は 3.9 → 3.9）
        public double CR { get; set; }
        public double CD { get; set; }
        public double ATKp { get; set; }
        public double HPp { get; set; }
        public double DEFp { get; set; }
        public double EM { get; set; }
        public bool IsLocked { get; set; }

        // 画像
        public string ImagePath { get; set; } = "";
        public string PlaceholderPath { get; set; } = "";
        public bool HasImage { get; set; }

        // スコア（表示1桁。内部倍精度）
        double _score;
        public double Score
        {
            get => _score;
            private set
            {
                _score = value;
                Raise();
                Raise(nameof(ScoreDisplay));
            }
        }
        public string ScoreDisplay => $"{Math.Round(Score, 1):F1}";

        // ローカライズ名
        public string SetName => LocalizationService.TrSet(SetKey);
        public string SlotName => LocalizationService.TrSlot(SlotKey);

        // 補助表示
        public string SetSlotText => $"{SetName} / {SlotName} ★{Rarity} +{Level}";
        public string SubSummaryCRCD => $"CR {CR:F1}%  CD {CD:F1}%";
        public string SubSummaryOthers =>
            $"ATK {ATKp:F1}%  HP {HPp:F1}%  DEF {DEFp:F1}%  EM {EM:F1}";
        public string SubDetailText =>
            $"CR {CR:F1}% / CD {CD:F1}% / ATK {ATKp:F1}% / HP {HPp:F1}% / DEF {DEFp:F1}% / EM {EM:F1}";

        public void UpdateScore(ScoreService.Preset preset)
        {
            var s = new ScoreService.Substats
            {
                CR = CR,
                CD = CD,
                ATKp = ATKp,
                HPp = HPp,
                DEFp = DEFp,
                EM = EM,
            };
            Score = ScoreService.CalcScore(s, preset);
        }

        public void OnLocaleChanged()
        {
            // 表示系プロパティを更新通知（SetName/SlotName/SetSlotText）
            Raise(nameof(SetName));
            Raise(nameof(SlotName));
            Raise(nameof(SetSlotText));
        }
    }
}
