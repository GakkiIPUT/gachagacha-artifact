using System.Drawing;
using System.Drawing.Imaging;

namespace Gacha.Services
{
    public static class CaptureService
    {
        // DIPやスケールは一旦考慮せず、実画面ピクセル座標で取得
        public static Bitmap Capture(int x, int y, int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(x, y, 0, 0, new Size(w, h));
            return bmp;
        }
    }
}
