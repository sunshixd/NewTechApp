using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NewTechApp.Auth
{
    public static class Captcha
    {
        private static readonly Random R = new Random();

        public static Tuple<Bitmap, string> Generate(int width, int height, int len)
        {
            string text = RandomText(len);
            Bitmap bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                // шум: линии
                for (int i = 0; i < 6; i++)
                {
                    using (var p = new Pen(Color.FromArgb(R.Next(50, 180), R.Next(50, 180), R.Next(50, 180)), R.Next(1, 3)))
                    {
                        g.DrawLine(p, R.Next(width), R.Next(height), R.Next(width), R.Next(height));
                    }
                }

                // текст
                using (var font = new Font("Bahnschrift Light SemiCondensed", 28, FontStyle.Bold))
                {
                    int x = 10;
                    foreach (char ch in text)
                    {
                        int y = R.Next(5, height - 35);
                        using (var b = new SolidBrush(Color.FromArgb(R.Next(0, 120), R.Next(0, 120), R.Next(0, 120))))
                        {
                            g.DrawString(ch.ToString(), font, b, x, y);
                        }
                        x += 35 + R.Next(-3, 3);
                    }
                }

                // шум: точки
                for (int i = 0; i < 200; i++)
                    bmp.SetPixel(R.Next(width), R.Next(height), Color.FromArgb(R.Next(256), R.Next(256), R.Next(256)));
            }
            return Tuple.Create(bmp, text);
        }

        private static string RandomText(int len)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var s = new char[len];
            for (int i = 0; i < len; i++) s[i] = chars[R.Next(chars.Length)];
            return new string(s);
        }
    }
}