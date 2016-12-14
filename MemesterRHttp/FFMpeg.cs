using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemesterRHttp
{
    class FFMpeg
    {
        private readonly string _ffmpeg;

        public FFMpeg(string ffmpeg = "ffmpeg")
        {
            _ffmpeg = ffmpeg;
        }

        public Task<string> ExecuteAsync(string command)
        {
            var tcs = new TaskCompletionSource<string>();
            var sb = new StringBuilder();
            ProcessStartInfo p = new ProcessStartInfo(_ffmpeg, command)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = new Process
            {
                StartInfo = p,
                EnableRaisingEvents = true
            };
            proc.Exited += (sender, args) =>
            {
                tcs.TrySetResult(sb.ToString());
            };
            proc.OutputDataReceived += (sender, args) =>
            {
                sb.Append(args.Data);
            };
            proc.ErrorDataReceived += (sender, args) =>
            {
                sb.Append(args.Data);
            };
            proc.Start();
            return tcs.Task;
        }
        
    }
}