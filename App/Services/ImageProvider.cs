using System;
using System.IO;

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

        // 追加: 画像を所定のパスへ保存（上書き可）
        public static Paths SaveImage(string setKey, string slotKey, string sourceFile)
        {
            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.Combine(baseDir, "Assets", "images", setKey);
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(sourceFile);
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            var dest = Path.Combine(dir, $"{slotKey}{ext}");
            File.Copy(sourceFile, dest, overwrite: true);
            // 直後に Resolve で最新パスを取得
            var (p, _) = Resolve(setKey, slotKey);
            return p;
        }

        public static void OpenImagesFolder()
        {
            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.Combine(baseDir, "Assets", "images");
            Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }




    }
}
