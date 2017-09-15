using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;

namespace FuncTest.Helpers
{
    internal class DotNetCoreTestWebApplication : TestWebApplication
    {
        private DotNetCoreProcess process;

        internal string PublishFolder { get; set; }

        /// <summary>Gets the app folder.</summary>
        internal override string AppFolder
        {
            get
            {
                return string.Join(Path.DirectorySeparatorChar.ToString(), new string[] { base.AppFolder, PublishFolder, "publish" });
            }
        }

        internal override void Deploy()
        {
            string applicationDllFileName = this.AppName + ".dll";
            string applicationDllPath = Path.Combine(this.AppFolder, applicationDllFileName);
            if (File.Exists(applicationDllPath) && DotNetCoreProcess.HasDotNetExe())
            {
                string arguments = $"\"{applicationDllFileName}\" {this.Port}";
                string output = "";
                string error = "";

                process = new DotNetCoreProcess(arguments, workingDirectory: this.AppFolder)
                    .RedirectStandardOutputTo((string outputMessage) =>
                    {
                        output += outputMessage;
                    })
                    .RedirectStandardErrorTo((string errorMessage) =>
                    {
                        error += errorMessage;
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
        }
    }
}