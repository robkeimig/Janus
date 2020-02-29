using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Janus
{
    public class FrameEncoder 
    {
        private const string Encoder = @"ffmpeg.exe";
        private const string InputArgs = "-y -f rawvideo -pix_fmt rgb24 -s 1280x720 -use_wallclock_as_timestamps 1 -i -";
        private const string OutputArgs = "-f mpegts -codec:v mpeg1video -b:v 1250k -r 70 pipe:1";
        private readonly WebSocket _websocket;
        private readonly Process _process;

        public FrameEncoder(WebSocket websocket, Action<string> logger=null, int width=1280, int height=720, int frameRate=60)
        {
            _websocket = websocket;

            _process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Encoder,
                    Arguments = $"{InputArgs} {OutputArgs}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            _process.Start();
            _process.BeginErrorReadLine();
            ProcessOutput();

            if (logger != null)
            {
                _process.ErrorDataReceived += (s, e) => logger(e.Data);
            }
        }

        public void WriteFrame(FrameBuffer frameBuffer)
        {
            _process.StandardInput.BaseStream.Write(frameBuffer.Buffer, 0, frameBuffer.Buffer.Length);
            _process.StandardInput.BaseStream.Flush();
        }

        private void ProcessOutput()
        {
            Task.Run(async ()=>
            {
                var buffer = new byte[512];
                var lastReadBytes = 0;

                do
                {
                    lastReadBytes = _process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);

                    try
                    {
                        if (lastReadBytes > 0)
                        {
                            await _websocket.SendAsync(new ArraySegment<byte>(buffer, 0, lastReadBytes), WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                    catch { }
                    
                } 
                while (lastReadBytes > 0);
            });
        }
    }
}
