using System.Diagnostics;
using System.Net.WebSockets;
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
                if (context.Request.Path == "/video")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync("test");
                        var frameEncoder = new FrameEncoder(webSocket, (msg) => { Debug.WriteLine(msg); }, 1280, 720, 60);
                        var frameBuffer = new FrameBuffer(1280, 720, 32);
                        var max = 1280 * 720 * 32;
                        long iter = 0;

                        while(true)
                        {
                            frameBuffer.Buffer[iter++] = 0xff;
                            frameBuffer.DrawTimestamp();
                            frameEncoder.WriteFrame(frameBuffer);
                            Task.Delay(5).Wait();
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
