using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace MemesterRHttp
{
    class FFMpeg
    {
        private readonly string _ffmpeg;
        private string _cw;

        public FFMpeg(string ffmpeg = "ffmpeg")
        {
            _ffmpeg = ffmpeg;
        }

        public string Execute(string command, int maxWaitTimeMs = 2000)
        {
            var sb = new StringBuilder();
            ProcessStartInfo p = new ProcessStartInfo(_ffmpeg, command)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            var proc = Process.Start(p);
            proc.OutputDataReceived += (sender, args) =>
            {
                sb.Append(args.Data);
            };
            
            var done = proc.WaitForExit(maxWaitTimeMs);
            if (!done) sb.Append($"Waited more than {maxWaitTimeMs}ms, aborted");
            return sb.ToString();
        }
    }
}