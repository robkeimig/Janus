using System.Drawing;

namespace Janus
{
    public class Utility
    {
        static readonly LibJpegTurbo _libJpegTurbo = new LibJpegTurbo();

        public static Tuple Point(float x, float y, float z) => new Tuple(x, y, z, 1.0f);

        public static Tuple Vector(float x, float y, float z) => new Tuple(x, y, z, 0.0f);

        public static byte[] ConvertBitmapToJpeg(Bitmap bitmap, int quality)
        {
            var lockbits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var result = _libJpegTurbo.Compress(lockbits.Scan0, 0, bitmap.Width, bitmap.Height, LibJpegTurbo.TJPixelFormats.TJPF_RGB, LibJpegTurbo.TJSubsamplingOptions.TJSAMP_420, quality, LibJpegTurbo.TJFlags.FASTDCT);
            bitmap.UnlockBits(lockbits);
            return result;
        }
    }
}
