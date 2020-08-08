using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsModule;

namespace TestFileLogConsoleApp
{
    /// <summary>
    /// THIS APP IS JUST FOR TESTING.
    /// I WILL DELETE THIS PROJECT WHEN SUBMITTING MY FINAL PR
    /// </summary>
    class Program
    {
        const string Directory = "C:\\TEMP\\";
        const int Number = 10000;

        static void Main(string[] args)
        {
            Console.WriteLine($"Number {Number}");

            OneSingleThread();
            OneMultiThread();

            TwoSingleThread();
            TwoMultiThread();

            Console.ReadLine();
        }

        private static void TestingFileWrite()
        {

            ////var config = TelemetryConfiguration.CreateDefault();
            //var config = TelemetryConfiguration.Active;
            ////config.InstrumentationKey = "testikey";

            //var old = new FileDiagnosticsTelemetryModule
            //{
            //    LogFilePath = "C:\\TEMP\\",
            //    Severity = "Verbose"
            //};
            //old.Initialize(config);

            //TelemetryModules.Instance.Modules.Add(old);

            //var dtm = TelemetryModules.Instance.Modules.OfType<DiagnosticsTelemetryModule>().Single();
            //dtm.IsFileLogEnabled = true;
            //dtm.Severity = "Verbose";

            //var client = new TelemetryClient(config);

            //client.TrackEvent("test event");
            //client.TrackTrace("test trace");

            //Thread.Sleep(10000);


            //Console.ReadKey();
        }

        private static void OneSingleThread()
        {
            var sender1 = new FileDiagnosticsSender
            {
                LogDirectory = Directory,
                Enabled = true,
            };


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < Number; i++)
            {
                sender1.Send($"{i:D5}");
            }
            stopwatch.Stop();

            Console.WriteLine($"One - SingleThread - Time elapsed: {stopwatch.Elapsed.TotalMilliseconds}ms");
        }

        private static void OneMultiThread()
        {
            var sender1 = new FileDiagnosticsSender
            {
                LogDirectory = Directory,
                Enabled = true,
            };


            var tasks = new List<Task>();

            for (int i = 0; i < Number; i++ )
            {
                var message = $"{i:D5}";
                tasks.Add(new Task(() => sender1.Send(message)));
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //tasks.ForEach(x => x.Start());
            Parallel.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks.ToArray());

            stopwatch.Stop();

            Console.WriteLine($"One - MultiThread - Time elapsed: {stopwatch.Elapsed.TotalMilliseconds}ms");
        }


        private static void TwoSingleThread()
        {
            var sender2 = new FileDiagnosticsSender2
            {
                LogDirectory = Directory,
                Enabled = true,
            };


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < Number; i++)
            {
                sender2.Send($"{i:D5}");
            }

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            sender2.Flush();
            stopwatch.Stop();

            Console.WriteLine($"Two - MultiThread - Time elapsed: {stopwatch.Elapsed.TotalMilliseconds}ms; Timer invoked {sender2.dequeueInvokedCount} times.");
        }

        private static void TwoMultiThread()
        {
            var sender2 = new FileDiagnosticsSender2
            {
                LogDirectory = Directory,
                Enabled = true,
            };


            var tasks = new List<Task>();

            for (int i = 0; i < Number; i++)
            {
                var message = $"{i:D5}";
                tasks.Add(new Task(() => sender2.Send(message)));
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //tasks.ForEach(x => x.Start());
            //Parallel.ForEach(tasks.GetRange(0,100), x => x.Start());
            //Task.Delay(TimeSpan.FromSeconds(10)).Wait();
            //Parallel.ForEach(tasks.GetRange(100, 100), x => x.Start());
            //Task.Delay(TimeSpan.FromSeconds(10)).Wait();
            //Parallel.ForEach(tasks.GetRange(200, Number-200), x => x.Start());

            Parallel.ForEach(tasks, x => x.Start());
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            Task.WaitAll(tasks.ToArray());

            sender2.Flush();
            stopwatch.Stop();

            Console.WriteLine($"Two - MultiThread - Time elapsed: {stopwatch.Elapsed.TotalMilliseconds}ms; Timer invoked {sender2.dequeueInvokedCount} times.");
        }
    }
}
