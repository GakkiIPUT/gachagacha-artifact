using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gacha.ViewModels;

namespace Gacha.Services
{
    public static class ArtifactKeyService
    {
        public static string ComputeKey(ArtifactVM a)
        {
            // GOODのidがあれば最優先でキーにする
            if (a.GoodId > 0) return $"gid:{a.GoodId}";

            // 値で指紋化（サブは昇順で並べてから結合）
            var subs = new (string k, double v)[] {
                ("CR", a.CR), ("CD", a.CD), ("ATKp", a.ATKp),
                ("HPp", a.HPp), ("DEFp", a.DEFp), ("EM", a.EM)
            }.Where(x => x.v > 0.0)
             .OrderBy(x => x.k)
             .Select(x => $"{x.k}:{x.v:F1}");

            var raw = $"set:{a.SetKey}|slot:{a.SlotKey}|r:{a.Rarity}|lv:{a.Level}|main:{a.MainText}|{string.Join("|", subs)}";

            // キーを短く安定化（SHA1）
            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes); // 大文字16進
        }
    }
}
