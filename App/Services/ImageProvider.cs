using System;

namespace Gacha.Services
{
    public static class ImageProvider
    {
        public sealed record Paths(string Path, string Placeholder);

        public static (Paths PathInfo, bool HasImage) Resolve(string setKey, string slotKey)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var img = System.IO.Path.Combine(baseDir, "Assets", "Artifacts", setKey, $"{slotKey}.png");
            var ph = System.IO.Path.Combine(baseDir, "Assets", "Placeholder", $"{slotKey}.png");
            var has = System.IO.File.Exists(img);
            if (!has && !System.IO.File.Exists(ph))
            {
                ph = System.IO.Path.Combine(baseDir, "Assets", "Placeholder", "unknown.png");
            }
            return (new Paths(img, ph), has);
        }
    }
}
