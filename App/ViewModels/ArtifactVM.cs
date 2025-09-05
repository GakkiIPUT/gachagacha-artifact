using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Gacha.Services;

namespace Gacha.ViewModels
{
    public sealed class ArtifactVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        void Raise([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // 基本
        public string SetKey { get; init; } = "";
        public string SlotKey { get; init; } = ""; // flower/plume/sands/goblet/circlet
        public int Rarity { get; init; }
        public int Level { get; init; }
        public string MainText { get; init; } = ""; // 表示のみ（計算はサブのみ）

        // サブ（％は 3.9 → 3.9）
        public double CR { get; init; }
        public double CD { get; init; }
        public double ATKp { get; init; }
        public double HPp { get; init; }
        public double DEFp { get; init; }
        public double EM { get; init; }
        public bool IsLocked { get; init; }

        // 画像
        public string ImagePath { get; init; } = "";
        public string PlaceholderPath { get; init; } = "";
        public bool HasImage { get; init; }

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
