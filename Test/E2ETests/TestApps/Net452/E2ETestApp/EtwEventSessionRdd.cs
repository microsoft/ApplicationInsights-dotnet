using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using System.IO;

namespace E2ETestApp
{
    public class EtwEventSessionRdd : IDisposable
    {
        private readonly string[] providers =
        {
            "Microsoft-ApplicationInsights-Extensibility-DependencyCollector"
               /* ,
            "Microsoft-ApplicationInsights-Core",
            "Microsoft-ApplicationInsights-WindowsServer-TelemetryChannel",
            "Microsoft-ApplicationInsights-Extensibility-Rtia-SharedCore" */
        };

        private const string SessionName = "RddTelemetryFunctionalTest";

        private TraceEventSession session;

        public void Start()
        {
            if (!(TraceEventSession.IsElevated() ?? false))
            {
                //Console.WriteLine("WARNING! To turn on ETW events you need to be Administrator, please run from an Admin process.");
                return;
            }

            // Same session name is reused to prevent multiple orphaned sessions in case if dispose is not done when test stopped in debug
            // Important! Note that session can leave longer that the process and it is important to dispose it
            session = new TraceEventSession(SessionName, null);
            foreach (var provider in this.providers)
            {
                this.session.EnableProvider(provider);
            }
            this.session.StopOnDispose = true;

            this.session.Source.Dynamic.All += Process;
            this.session.Source.UnhandledEvents += Process;

            Task.Run(() =>
            {
                // Blocking call. Will end when session is disposed
                this.session.Source.Process();
            });

            //Console.WriteLine("Etw session started");
        }

        public bool FailureDetected { get; set; }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (this.session != null)
            {
                this.session.Dispose();
                this.session = null;
            }

            //Console.WriteLine("Etw session stopped");
        }

        private void Process(TraceEvent data)
        {
            if (this.IsInterestingTrace(data))
            {
                //Console.WriteLine("{0}. Application Trace. Level: {1}; Id: {2}; Message: {3}; ", data.TimeStamp.ToString("hh:mm:ss", CultureInfo.CurrentCulture), data.Level, data.ID, data.FormattedMessage);
                this.WriteToFile(string.Format("{0}. Application Trace. Level: {1}; Id: {2}; Message: {3}; ",
                    data.TimeStamp.ToString("hh:mm:ss", CultureInfo.CurrentCulture), data.Level, data.ID, data.FormattedMessage));
            }
        }

        private bool IsInterestingTrace(TraceEvent data)
        {
            // vshub.exe internally uses ApplicationInsights
            if (data.PayloadNames.Length > 1 && data.PayloadString(1) == "vshub.exe")
            {
                return false;
            }

            bool result = false;

            if (data.PayloadNames.Length > 0)
            {
                int id = (int)data.ID;

                // Not system event
                if ((id > 0) && (id < 65534))
                {
                    string domainName = data.PayloadString(data.PayloadNames.Length - 1);
                    result = domainName.StartsWith("/LM/W3SVC", StringComparison.Ordinal);
                }
            }

            return result;
        }

        private void WriteToFile(string data)
        {
            string MyDirectoryPath = "c:\\mylogs";
            string filename = "logs.txt";
            if (!Directory.Exists(MyDirectoryPath))
            {
                Directory.CreateDirectory(MyDirectoryPath);
            }

            var target = Path.Combine(MyDirectoryPath, filename);

            File.AppendAllText(target, data + "\n");

        }
    }
}