using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace Janus
{
    public static class StreamingVideoExtensions
    {
        public static IApplicationBuilder UseStreamingVideo(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/input")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {

                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }

                else if (context.Request.Path == "/video")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync("test");
                        var frameEncoder = new FfmpegFrameEncoder(webSocket, (msg) => { Debug.WriteLine(msg); }, 1280, 720, 60);
                        var frameBuffer = new FrameBuffer(1280, 720);
                        Bitmap bmp = new Bitmap(1280, 720, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                        RectangleF rectf = new RectangleF(0, 0, 100, 80);
                        Graphics g = Graphics.FromImage(bmp);
                        g.SmoothingMode = SmoothingMode.None;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.None;
                        var iteration = 0;

                        while (true)
                        {
                            var date = DateTime.UtcNow.ToString("ss ffffff");
                            g.Clear(Color.Black);
                            var sw = new Stopwatch();
                            sw.Start();
                            iteration++;
                            g.FillRectangle(new SolidBrush(Color.Red), 0, 0, iteration, iteration);
                            g.FillRectangle(new SolidBrush(Color.FromArgb(iteration % 255, iteration % 255, iteration % 255)), iteration, iteration, iteration-5, iteration-5);
                            if (iteration > 720) {
                                iteration = 0;
                            }
                            sw.Stop();
                            g.DrawString(date, new Font("Tahoma", 14), Brushes.White, rectf);
                            g.Flush();
                            var lockbits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            byte[] testData = new byte[Math.Abs(lockbits.Stride * lockbits.Height)];
                            Marshal.Copy(lockbits.Scan0, frameBuffer.Buffer, 0, testData.Length);
                            bmp.UnlockBits(lockbits);
                            frameEncoder.WriteFrame(frameBuffer);
                            Task.Delay(1).Wait();
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            return app;
        }
    }
}
