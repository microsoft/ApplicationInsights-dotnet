namespace FuncTest.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// A helper class for dealing with dotnet.exe processes.
    /// </summary>
    internal class DotNetCoreProcess
    {
        private readonly Process process;

        public DotNetCoreProcess(string arguments, string workingDirectory = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            process = new Process()
            {
                StartInfo = startInfo
            };
        }

        /// <summary>
        /// Get the exit code for this process, if the process has exited. If the process hasn't exited, then return null.
        /// </summary>
        public int? ExitCode
        {
            get
            {
                int? result = null;
                if (process.HasExited)
                {
                    result = process.ExitCode;
                }
                return result;
            }
        }

        /// <summary>
        /// Redirect all of the standard output text to the provided standardOutputHandler.
        /// </summary>
        /// <param name="standardOutputHandler">An action that will be invoked whenever the process writes to its standard output stream.</param>
        /// <returns></returns>
        public DotNetCoreProcess RedirectStandardOutputTo(Action<string> standardOutputHandler)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                {
                    standardOutputHandler.Invoke(e.Data);
                }
            };
            return this;
        }

        /// <summary>
        /// Redirect all of the standard error text to the provided standardErrorHandler.
        /// </summary>
        /// <param name="standardErrorHandler">An action that will be invoked whenever the process writes to its standard error stream.</param>
        /// <returns></returns>
        public DotNetCoreProcess RedirectStandardErrorTo(Action<string> standardErrorHandler)
        {
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                {
                    standardErrorHandler.Invoke(e.Data);
                }
            };
            return this;
        }

        /// <summary>
        /// Run this process and wait for it to finish.
        /// </summary>
        public DotNetCoreProcess Run()
        {
            Start();
            WaitForExit();
            return this;
        }

        /// <summary>
        /// Asynchronously start this process. This method will not wait for
        /// the process to finish before it returns.
        /// </summary>
        public DotNetCoreProcess Start()
        {
            process.Start();

            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }
            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

            return this;
        }

        /// <summary>
        /// Wait up to 10 seconds for this process to exit.
        /// </summary>
        public void WaitForExit()
        {
            WaitForExit(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Block for the provided amount of time or until this started process has exited.
        /// </summary>
        public void WaitForExit(TimeSpan timeout)
        {
            process.WaitForExit((int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Terminate the process.
        /// </summary>
        public void Kill()
        {
            process.Kill();
            
            // Kill is an async operation internally.
            //(see https://msdn.microsoft.com/en-us/library/system.diagnostics.process.kill(v=vs.110).aspx#Anchor_2)
            WaitForExit();
        }
    }
}