using System;
using System.Drawing;
using System.IO;
using Tesseract;

namespace Gacha.Services.Ocr
{
    public sealed class TesseractOcrEngine : IOcrEngine
    {
        readonly string _tessdataDir;
        TesseractEngine? _eng;
        string _loadedLang = "";

        public bool IsReady => _eng != null;

        public TesseractOcrEngine(string tessdataDir)
        {
            _tessdataDir = tessdataDir;
        }

        TesseractEngine Ensure(string lang)
        {
            if (_eng != null && _loadedLang == lang) return _eng;
            _eng?.Dispose();
            _eng = new TesseractEngine(_tessdataDir, lang, EngineMode.LstmOnly);
            _loadedLang = lang;
            return _eng;
        }

        public OcrResult Recognize(Bitmap bmp, string langCode)
        {
            try
            {
                var eng = Ensure(langCode);
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Position = 0;
                using var pix = Pix.LoadFromMemory(ms.ToArray());                using var page = eng.Process(pix);
                return new OcrResult { Text = page.GetText() ?? "" };
            }
            catch (Exception ex)
            {
                return new OcrResult { Text = $"[OCR error] {ex.Message}" };
            }
        }

        public void Dispose() => _eng?.Dispose();
    }
}
