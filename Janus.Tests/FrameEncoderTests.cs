using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Janus.Tests
{
    [TestClass]
    public class FrameEncoderTests
    {
        //[TestMethod]
        //public void CanEncodeFrames()
        //{
        //    var outputStream = new MemoryStream();
        //    var frameEncoder = new FrameEncoder(outputStream, (log) => { Debug.WriteLine(log); });
        //    var frameBuffer = new FrameBuffer(1920, 1080, 24);

        //    for (int frame = 0; frame < 1000; frame++)
        //    {
        //        for (int x = 0; x < 1920 * 1080 * 3; x++)
        //        {
        //            if (x % 3 == 0)
        //            {
        //                frameBuffer.Buffer[x] = 0xFF;
        //            }
        //            else
        //            {
        //                frameBuffer.Buffer[x] = 0x00;
        //            }
        //        }

        //        frameEncoder.WriteFrame(frameBuffer);
        //    }

        //    outputStream.Seek(0, SeekOrigin.Begin);

        //    using (var fs = new FileStream("test.ts", FileMode.Create))
        //    {
        //        outputStream.CopyTo(fs);
        //    }
        //}
    }
}
