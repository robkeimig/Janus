namespace Janus
{
    public class FrameBuffer
    {
        private const int ColorDepth = 24;

        public FrameBuffer(int width, int height)
        {
            var bufferSizeBytes = width * height * ColorDepth / 8;
            Buffer = new byte[bufferSizeBytes];
        }

        public byte[] Buffer { get; }

        public void SetPixel(int x, int y, int r, int g, int b)
        {
            var index = y * 1280 * (ColorDepth / 8) + (x * (ColorDepth / 8));
            Buffer[index] = (byte)r;
            Buffer[index + 1] = (byte)g;
            Buffer[index + 2] = (byte)b;
        }
    }
}
