using System;

namespace Gacha.Services
{
    public static class ScoreService
    {
        public enum Preset
        {
            CV_ONLY,
            CV_ATK,
            CV_HP,
            CV_DEF,
            CV_EM,
        }

        public sealed class Substats
        {
            public double CR { get; init; }   // 会心率（％）
            public double CD { get; init; }   // 会心ダメ（％）
            public double ATKp { get; init; } // 攻撃％
            public double HPp { get; init; }  // HP％
            public double DEFp { get; init; } // 防御％
            public double EM { get; init; }   // 元素熟知
            public double ER { get; init; }   // 未使用
        }

        public static double CalcScore(Substats s, Preset preset)
        {
            double cv = 2.0 * s.CR + 1.0 * s.CD;
            return preset switch
            {
                Preset.CV_ONLY => cv,
                Preset.CV_ATK => cv + s.ATKp,
                Preset.CV_HP => cv + s.HPp,
                Preset.CV_DEF => cv + 0.8 * s.DEFp,
                Preset.CV_EM => cv + 0.25 * s.EM,
                _ => cv
            };
        }
    }
}
