using System.Diagnostics;
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

        public async Task<string> Execute(string command, int maxWaitTimeMs = 2000)
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
            await WaitForExitAsync(proc, CancellationToken.None);
            return sb.ToString();
        }

        public static Task WaitForExitAsync(Process process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(tcs.SetCanceled);

            return tcs.Task;
        }
    }
}