using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace NewTechApp.Utility
{
    public static class ImageHelper
    {
        public static string ImagesDir
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

        public static string SaveResized(Image src, int w, int h, string oldPathToDeleteIfAny = null)
        {
            if (!Directory.Exists(ImagesDir)) Directory.CreateDirectory(ImagesDir);

            using (var bmp = new Bitmap(w, h))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.White);
                    g.DrawImage(src, new Rectangle(0, 0, w, h));
                }

                var file = Path.Combine(ImagesDir, Guid.NewGuid().ToString("N") + ".jpg");
                bmp.Save(file, ImageFormat.Jpeg);

                if (!string.IsNullOrEmpty(oldPathToDeleteIfAny) && File.Exists(oldPathToDeleteIfAny))
                {
                    try { File.Delete(oldPathToDeleteIfAny); } catch { /* ignore */ }
                }

                return file;
            }
        }
    }
}