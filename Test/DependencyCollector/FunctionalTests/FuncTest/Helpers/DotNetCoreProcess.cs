namespace FuncTest.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// A helper class for dealing with dotnet.exe processes.
    /// </summary>
    internal class DotNetCoreProcess
    {
        private readonly Process process;

        public DotNetCoreProcess(string arguments, string workingDirectory = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(DotNetExePath, arguments)
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

        /// <summary>
        /// Get the path to the dotnet.exe file. This will search the current working directory,
        /// the directory specified in the NetCorePath environment variable, and each of the
        /// directories specified in the Path environment variable. If the dotnet.exe file still
        /// can't be found, then this will return null.
        /// </summary>
        public static string DotNetExePath
        {
            get
            {
                if (dotnetExePath == null)
                {
                    List<string> envPaths = new List<string>();
                    envPaths.Add(@".\");
                    envPaths.Add(Environment.GetEnvironmentVariable(NetCorePathEnvVariableName));
                    envPaths.AddRange(Environment.GetEnvironmentVariable(PathEnvVariableName).Split(';'));

                    foreach (string envPath in envPaths)
                    {
                        if (!string.IsNullOrWhiteSpace(envPath))
                        {
                            string tempDotNetExePath = envPath;
                            if (!tempDotNetExePath.EndsWith(dotnetExe, StringComparison.InvariantCultureIgnoreCase))
                            {
                                tempDotNetExePath = Path.Combine(tempDotNetExePath, dotnetExe);
                            }

                            if (File.Exists(tempDotNetExePath))
                            {
                                dotnetExePath = tempDotNetExePath;
                                break;
                            }
                        }
                    }
                }
                return dotnetExePath;
            }
        }

        /// <summary>
        /// Check whether or not the dotnet.exe file exists at its expected path.
        /// </summary>
        /// <returns></returns>
        public static bool HasDotNetExe()
        {
            return !string.IsNullOrEmpty(DotNetExePath);
        }

        private static string dotnetExePath;

        private const string dotnetExe = "dotnet.exe";
        private const string NetCorePathEnvVariableName = "NetCorePath";
        private const string PathEnvVariableName = "Path";
    }
}