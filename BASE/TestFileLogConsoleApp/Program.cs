using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

namespace TestFileLogConsoleApp
{
    /// <summary>
    /// THIS APP IS JUST FOR TESTING.
    /// I WILL DELETE THIS PROJECT WHEN SUBMITTING MY FINAL PR
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //var config = TelemetryConfiguration.CreateDefault();
            var config = TelemetryConfiguration.Active;
            //config.InstrumentationKey = "testikey";

            var old = new FileDiagnosticsTelemetryModule
            {
                LogFilePath = "C:\\TEMP\\",
                Severity = "Verbose"
            };
            old.Initialize(config);

            TelemetryModules.Instance.Modules.Add(old);

            var dtm = TelemetryModules.Instance.Modules.OfType<DiagnosticsTelemetryModule>().Single();
            dtm.IsFileLogEnabled = true;
            dtm.Severity = "Verbose";

            var client = new TelemetryClient(config);

            client.TrackEvent("test event");
            client.TrackTrace("test trace");

            Thread.Sleep(10000);


            _ = Console.ReadKey();
        }
    }
}
