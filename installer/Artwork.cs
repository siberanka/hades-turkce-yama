using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HadesTurkce
{
    // Original geometric artwork; no game textures, screenshots or logos.
    public static class Artwork
    {
        static void Block(Graphics g, Color color, int x, int y, int w, int h)
        { using (var b = new SolidBrush(color)) g.FillRectangle(b, x, y, w, h); }
        public static Bitmap Emblem(int size)
        {
            var image = new Bitmap(size, size);
            using (var g = Graphics.FromImage(image))
            {
                g.ScaleTransform(size / 32f, size / 32f);
                var gold = Color.FromArgb(235, 190, 99);
                var red = Color.FromArgb(191, 53, 65);
                Block(g, Color.FromArgb(20, 24, 29), 2, 2, 28, 28);
                using (var p = new Pen(gold, 1)) g.DrawPolygon(p, new[] { new Point(16, 1), new Point(31, 16), new Point(16, 31), new Point(1, 16) });
                Block(g, red, 11, 13, 11, 11); Block(g, red, 9, 16, 15, 5);
                Block(g, red, 13, 9, 7, 5); Block(g, gold, 15, 5, 3, 6);
                Block(g, gold, 11, 8, 3, 3); Block(g, gold, 19, 8, 3, 3);
                Block(g, gold, 12, 16, 2, 2); Block(g, gold, 17, 15, 2, 2);
                Block(g, gold, 15, 20, 2, 2); Block(g, gold, 20, 19, 2, 2);
            }
            return image;
        }
        public static Bitmap Scene(int variant)
        {
            var image = new Bitmap(640, 1000);
            Color glow = variant == 0 ? Color.FromArgb(73, 156, 129) : variant == 1 ? Color.FromArgb(184, 61, 56) : Color.FromArgb(115, 107, 172);
            using (var g = Graphics.FromImage(image))
            {
                using (var b = new LinearGradientBrush(new Rectangle(0, 0, 640, 1000), Color.FromArgb(12, 22, 29), glow, 90f)) g.FillRectangle(b, 0, 0, 640, 1000);
                // Distant terraces and archways vary between three underworld scenes.
                for (int layer = 0; layer < 5; layer++)
                {
                    Color stone = Color.FromArgb(26 - layer * 3, 34 - layer * 3, 41 - layer * 3);
                    int y = 210 + layer * 105;
                    Block(g, stone, 0, y, 640, 42);
                    for (int col = 0; col < 5; col++)
                    {
                        int x = col * 158 - 40 + variant * 11;
                        Block(g, stone, x, y - 175, 33, 180);
                        Block(g, stone, x - 9, y - 180, 51, 16);
                        Block(g, stone, x - 6, y - 16, 45, 19);
                        using (var p = new Pen(Color.FromArgb(35, glow), 2)) g.DrawLine(p, x + 9, y - 160, x + 9, y - 25);
                    }
                }
                using (var p = new Pen(Color.FromArgb(80, glow), 3))
                {
                    for (int i = 0; i < 16; i++)
                    {
                        float y = 560 + i * 27;
                        g.DrawBezier(p, -70, y, 180, y - 100, 380, y + 130, 710, y - 25);
                    }
                }
                // Foreground Greek key and steps, with an ember-lit doorway.
                Block(g, Color.FromArgb(11, 15, 21), 190, 230, 260, 470);
                using (var p = new Pen(Color.FromArgb(120, 215, 172, 99), 6)) g.DrawRectangle(p, 207, 247, 226, 430);
                using (var p = new Pen(Color.FromArgb(75, glow), 5)) g.DrawRectangle(p, 229, 270, 182, 405);
                for (int step = 0; step < 6; step++) Block(g, Color.FromArgb(38 - step * 3, 39 - step * 3, 45 - step * 3), 175 - step * 19, 677 + step * 21, 290 + step * 38, 19);
                for (int side = 0; side < 2; side++)
                {
                    int x = side == 0 ? 104 : 510;
                    Block(g, Color.FromArgb(189, 146, 77), x, 438, 13, 125);
                    using (var b = new SolidBrush(Color.FromArgb(213, 97, 49))) g.FillPolygon(b, new[] { new Point(x - 9, 439), new Point(x + 4, 377), new Point(x + 11, 409), new Point(x + 24, 393), new Point(x + 22, 439) });
                    Block(g, Color.FromArgb(245, 191, 94), x + 3, 412, 7, 24);
                }
                var random = new Random(720 + variant);
                for (int i = 0; i < 80; i++) Block(g, Color.FromArgb(95, glow), random.Next(640), random.Next(900), 2, 2);
            }
            return image;
        }
    }
}
