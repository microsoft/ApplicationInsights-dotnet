namespace Functional.IisExpress
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Functional.Helpers.Debugger;

    public class IisExpress
    {
        private const string IssExpressLocation = @"%programfiles%\IIS Express\iisexpress.exe";
        private const string ConfigParameter = "config";
        private const string ConfigFileName = "applicationHost.config";

        private readonly Process hostProcess;

        private IisExpress(
            IisExpressConfiguration parameters,
            bool attachVsDebugger)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException("parameters");
            }

            if (string.IsNullOrEmpty(parameters.Path))
            {
                throw new ArgumentNullException("parameters.Path");
            }

            string configFilePath = string.Format("{0}\\{1}", parameters.Path, ConfigFileName);
            File.WriteAllText(configFilePath, parameters.GetConfigFile());

            Trace.TraceInformation(
                "Starting IIS Express with configuration file: {0}",
                configFilePath);

            var executablePath = Environment.ExpandEnvironmentVariables(IssExpressLocation);
            // executing x64 bit version (unable to use Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) from x86 process)

            if (true == parameters.UseX64Process)
            {
                executablePath = executablePath.Replace(" (x86)", string.Empty);
            }

            var psi = new ProcessStartInfo
                          {
                              FileName = executablePath,
                              Arguments = string.Format("/{0}:\"{1}\" ", ConfigParameter, configFilePath),
                              RedirectStandardOutput = true,
                              UseShellExecute = false,
                          };

            foreach (var envVariable in parameters.EnvironmentVariables)
            {
               psi.EnvironmentVariables.Add(
                   envVariable.Key, 
                   envVariable.Value); 
            }

            this.hostProcess = 
                true == attachVsDebugger
                    ? StartAndAttachVsDebugger(psi)
                    : Process.Start(psi);

            if (null == this.hostProcess)
            {
                throw new InvalidOperationException(
                    "Unable to start process",
                    new Win32Exception());
            }

            this.WaitForHostInitialization();
        }

        public static IisExpress Start(IisExpressConfiguration parameters,
            bool attachVsDebugger = false)
        {
            return new IisExpress(parameters, attachVsDebugger);
        }

        public void Stop()
        {
            const int processExitWaitTimeout = 15000;

            Trace.TraceInformation("Stopping iisexpress.exe: pid={0}", this.hostProcess.Id);

            SendStopMessageToProcess(this.hostProcess.Id);

            Trace.TraceInformation("Waiting for exit of iisexpress.exe: pid={0}", this.hostProcess.Id);
            if (false == this.hostProcess.WaitForExit(processExitWaitTimeout))
            {
                Trace.TraceWarning("iisexpress.exe process hasn't exited during expected time, terminating!");
                this.hostProcess.Kill();
            }
            else
            {
                Trace.TraceInformation("iisexpress.exe successfully exited: pid={0}", this.hostProcess.Id);
            }

            this.hostProcess.Close();
        }

        private static void SendStopMessageToProcess(int pid)
        {
            try
            {
                for (var ptr = NativeMethods.GetTopWindow(IntPtr.Zero);
                    ptr != IntPtr.Zero;
                    ptr = NativeMethods.GetWindow(ptr, 2))
                {
                    uint num;
                    NativeMethods.GetWindowThreadProcessId(ptr, out num);
                    if (pid == num)
                    {
                        var handleRef = new HandleRef(null, ptr);
                        NativeMethods.PostMessage(handleRef, 0x12, IntPtr.Zero, IntPtr.Zero);

                        return;
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError(exc.ToString());
            }
        }

        private static Process StartAndAttachVsDebugger(
            ProcessStartInfo psi)
        {
            var hostProcess = Process.Start(psi);
            if (null != hostProcess)
            {
                var debugger = hostProcess.FindParentByName("devenv");
                if (null != debugger)
                {
                    debugger.AttachManagedTo(hostProcess);
                }
            }

            return hostProcess;
        }

        private void WaitForHostInitialization()
        {
            const int outputLineReadingTimeoutMs = 500;

            Trace.TraceInformation(
                "Waiting for iisexpress.exe initialization: pid={0}",
                this.hostProcess.Id);

            var line = string.Empty;
            
            while (null != line
                && false == line.StartsWith("Registration completed")
                && false == line.StartsWith("IIS Express is running.") 
                && false == this.hostProcess.StandardOutput.EndOfStream)
            {
                Thread.Sleep(outputLineReadingTimeoutMs);

                line = this.hostProcess.StandardOutput.ReadLine();
                Trace.TraceInformation("Reading console output of iisexpress.exe: {0}", line);
            }

            Trace.TraceInformation(
                "iisexpress.exe initialization complete: pid={0}",
                this.hostProcess.Id);
        }

        internal class NativeMethods
        {
            // Methods
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetTopWindow(IntPtr hWnd);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        }
    }
}