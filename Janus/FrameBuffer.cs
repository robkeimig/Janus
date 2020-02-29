using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Janus
{
    public class FrameBuffer
    {
        private readonly byte[] _buffer;
        private readonly int _width;
        private readonly int _height;
        private readonly int _colorDepth;
        public FrameBuffer(int width=1280, int height=720, int colorDepth=32)
        {
            _width = width;
            _height = height;
            _colorDepth = colorDepth;
            _buffer = new byte[width * height * colorDepth / 8];
        }

        public byte[] Buffer => _buffer;

        public void SetPixel(int x, int y, int r, int g, int b)
        {
            var index = y * 1280 * (_colorDepth / 8) + (x * (_colorDepth / 8));
            _buffer[index] = (byte)r;
            _buffer[index + 1] = (byte)g;
            _buffer[index + 2] = (byte)b;
        }

        public void DrawTimestamp()
        {
            Bitmap bmp = new Bitmap(100, 80, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            RectangleF rectf = new RectangleF(0, 0, 100, 80);
            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var date = DateTime.UtcNow.ToString("ss ffffff");
            g.DrawString(date, new Font("Tahoma", 14), Brushes.White, rectf);
            g.Flush();
            for(int x=0; x<100; x++)
            for(int y=0; y<80; y++)
            {
                var color = bmp.GetPixel(x, y);
                SetPixel(x, y, color.R, color.G, color.B);
            }
            Console.WriteLine(date);
        }
    }
}
