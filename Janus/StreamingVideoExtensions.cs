using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
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
                try
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
                        var width = 1280;
                        var height = 720;
                        var bitsPerPixel = 24;

                        Bitmap drawTarget = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        RectangleF rectf = new RectangleF(0, 0, 100, 80);
                        Graphics g = Graphics.FromImage(drawTarget);
                        g.SmoothingMode = SmoothingMode.None;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.None;
                        var iteration = 0;
                        var sw = Stopwatch.StartNew();

                        while (true)
                        {
                            var frameStartTick = sw.ElapsedTicks;
                            var date = DateTime.UtcNow.ToString("ss ffffff");
                            //Console.WriteLine(date);
                            g.Clear(Color.Black);
                            var jpegsw = new Stopwatch();
                            sw.Start();
                            iteration++;
                            g.FillRectangle(new SolidBrush(Color.FromArgb(iteration % 255, 255, iteration % 122)), 100, 80, iteration, iteration);
                            g.FillRectangle(new SolidBrush(Color.FromArgb(iteration % 255, iteration % 255, iteration % 255)), 100+iteration, 80+iteration, iteration - 5, iteration - 5);

                            if (iteration > height)
                            {
                                iteration = 0;
                            }

                            try
                            {
                                g.DrawString(date, new Font("Tahoma", 14), Brushes.White, rectf);
                                g.Flush();
                                jpegsw.Start();
                                var jpeg = new Utility().ConvertBitmapToJpeg(drawTarget, 80);
                                jpegsw.Stop();
                                if (iteration % 5 == 0) { Console.WriteLine($"JPEG Encoder Latency - {jpegsw.ElapsedMilliseconds} ms ({jpegsw.ElapsedTicks} ticks) - {width}x{height} ({bitsPerPixel}bpp) = {width * height * bitsPerPixel / 1000000} Megabits - Compressed: {jpeg.Length} Bytes"); }
                                var jpegLengthBytes = BitConverter.GetBytes(jpeg.Length);
                                await context.Response.Body.WriteAsync(jpegLengthBytes, 0, jpegLengthBytes.Length).ConfigureAwait(false);
                                await context.Response.Body.WriteAsync(jpeg, 0, jpeg.Length).ConfigureAwait(false);
                                await context.Response.Body.FlushAsync().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {

                            }

                            double frameElapsedMicroSeconds = 0;
                            
                            while (frameElapsedMicroSeconds < 30 * 1000)
                            {
                                var frameEndTick = sw.ElapsedTicks;
                                var frameElapsedTicks = frameEndTick - frameStartTick;
                                frameElapsedMicroSeconds = ((double)frameElapsedTicks / (double)Stopwatch.Frequency) * 1000000;
                                //Console.WriteLine(frameElapsedMicroSeconds);
                                Thread.SpinWait(5);
                            }
                        }
                    }
                    else
                    {
                        await next();
                    }
                }
                catch (Exception ex) 
                { }
                
            });

            return app;
        }
    }
}
