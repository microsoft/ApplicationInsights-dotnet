using System;
using System.Diagnostics;

namespace E2ETests.Helpers
{
    public class CommandLineUtils
    {
        public static string ExecuteCommandInCmd(string command, bool ignoreErrors = false)
        {
            Trace.WriteLine("Executing cmd command: " + command);
            ProcessStartInfo commandInfo = new ProcessStartInfo("cmd", command);
            Process process = new Process { StartInfo = commandInfo };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error =  process.StandardError.ReadToEnd();
            Trace.WriteLine("Output from cmd command:" + output);            
            process.WaitForExit();

            // No output and something in error is fatal.
            // Presence of error alone is typically okay, as some Docker cmd outputs are treated by cmd as errors whereas they are not.
            if(string.IsNullOrEmpty(output) && !string.IsNullOrEmpty(error) && !ignoreErrors)
            {
                Trace.WriteLine("Error from cmd command:" + error);
                string exMessage = "Error:";
                if (error.Contains("build path"))
                {
                    // This is fatal error and indicates test app is not built before running tests.
                    exMessage += "Please make sure product solution (\\Src\\Microsoft.ApplicationInsights.Web.sln)" +
                        "and Test solution  \\Test\\E2ETests\\DependencyCollectionTests.sln are built first before running the tests.";
                    throw new Exception(exMessage);
                }
                else if (error.Contains("Network nat"))
                {
                    // This is fatal error and indicates Docker is not setup correctly.
                    exMessage +="Docker has errors deploying. Make sure docker engine is switched to Windows, and you can run docker hello-world successfully before running tests again.";
                    throw new Exception(exMessage);
                }
                else
                {
                    exMessage += "Docker has errors deploying. Make sure Docker for Windows is installed, docker engine is switched to Windows, and you can run docker hello-world successfully before running tests again.";
                    throw new Exception(exMessage + error);
                }
            }
            return output;
        }
    }
}
