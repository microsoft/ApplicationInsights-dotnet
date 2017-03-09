using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FuncTest.Helpers
{
    internal class DotNetCoreTestWebApplication : TestWebApplication
    {
        private DotNetCoreProcess process;
        private StreamWriter stdoutFile;
        private StreamWriter stderrFile;

        internal override void Deploy()
        {
            string applicationDllFileName = this.AppName + ".dll";
            string applicationDllPath = Path.Combine(this.AppFolder, applicationDllFileName);
            if (File.Exists(applicationDllPath))
            {
                string arguments = $"\"{applicationDllFileName}\" {this.Port}";
                string output = "";
                string error = "";

                stdoutFile = new StreamWriter("StandardOutput.txt");
                stderrFile = new StreamWriter("StandardError.txt");

                process = new DotNetCoreProcess(arguments, workingDirectory: this.AppFolder)
                    .RedirectStandardOutputTo((string outputMessage) =>
                    {
                        output += outputMessage;
                        stdoutFile.WriteLine(outputMessage);
                    })
                    .RedirectStandardErrorTo((string errorMessage) =>
                    {
                        error += errorMessage;
                        stderrFile.WriteLine(errorMessage);
                    })
                    .Start();

                bool serverStarted = false;
                while (!serverStarted)
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        process.WaitForExit();
                        Assert.Inconclusive($"Failed to start .NET Core server using command 'dotnet.exe {arguments}': {error}");
                    }
                    else if (output.Contains("Now listening on"))
                    {
                        serverStarted = true;
                    }
                    else
                    {
                        // Let someone else run with the hope that the dotnet.exe process will run.
                        Thread.Yield();
                    }
                }
            }
        }

        internal override void Remove()
        {
            if (process != null)
            {
                process.Kill();
                process = null;
            }

            if (stdoutFile != null)
            {
                stdoutFile.Close();
                stdoutFile = null;
            }

            if (stderrFile != null)
            {
                stderrFile.Close();
                stderrFile = null;
            }
        }
    }
}
