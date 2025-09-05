using System;
using System.Drawing;

namespace Gacha.Services.Ocr
{
    public sealed class OcrResult
    {
        public string Text { get; init; } = "";
    }

    public interface IOcrEngine : IDisposable
    {
        bool IsReady { get; }
        OcrResult Recognize(Bitmap bmp, string langCode);
    }
}
