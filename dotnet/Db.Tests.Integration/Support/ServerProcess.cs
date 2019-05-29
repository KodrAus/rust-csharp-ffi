using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Db.Tests.Integration.Support
{
    class ServerProcess : IDisposable
    {
        private readonly ManualResetEvent _errorComplete = new ManualResetEvent(false);
        private readonly StringWriter _output = new StringWriter();
        private readonly ManualResetEvent _outputComplete = new ManualResetEvent(false);
        private readonly Process _process;

        private readonly object _sync = new object();

        public ServerProcess(string binPath, string listenUrl, string dataPath)
        {
            var startInfo = new ProcessStartInfo
            {
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = binPath,
                Arguments = $"--urls \"{listenUrl}\" --datapath \"{dataPath}\""
            };

            _process = Process.Start(startInfo);

            if (_process == null) throw new InvalidOperationException("Failed to start server");

            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    _outputComplete.Set();
                else
                    WriteOutput(e.Data);
            };
            _process.BeginOutputReadLine();

            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    _errorComplete.Set();
                else
                    WriteOutput(e.Data);
            };
            _process.BeginErrorReadLine();
        }

        public string Output
        {
            get
            {
                lock (_sync)
                {
                    return _output.ToString();
                }
            }
        }

        public bool HasExited => _process.HasExited;

        public void Dispose()
        {
            try
            {
                _process.Kill();
                WaitForExit();
            }
            catch
            {
                // Ignored
            }
        }

        private void WriteOutput(string line)
        {
            lock (_sync)
            {
                _output.WriteLine(line);
            }
        }

        public int WaitForExit(TimeSpan? timeout = null)
        {
            _process.WaitForExit((int) (timeout ?? Timeout.InfiniteTimeSpan).TotalMilliseconds);

            if (!_outputComplete.WaitOne(TimeSpan.FromSeconds(30)))
                throw new IOException("STDOUT did not complete in time");

            if (!_errorComplete.WaitOne(TimeSpan.FromSeconds(30)))
                throw new IOException("STDERR did not complete in time");

            return _process.ExitCode;
        }
    }
}