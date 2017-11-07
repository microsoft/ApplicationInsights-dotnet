using System;
using System.Diagnostics;

namespace E2ETests.Helpers
{
    public class CommandLineUtils
    {
        public static string ExecuteCommandInCmd(string command)
        {
            Trace.WriteLine("Executing cmd command: " + command);
            ProcessStartInfo commandInfo = new ProcessStartInfo("cmd", command);
            Process process = new Process { StartInfo = commandInfo };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Output from cmd command:" + output);
            process.WaitForExit();

            return output;
        }
    }
}
