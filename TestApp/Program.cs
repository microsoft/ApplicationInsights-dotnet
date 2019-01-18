using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch s1 = new Stopwatch();
            s1.Start();
            // DoPerf();
            Do100ItemSend();
            s1.Stop();
            Console.WriteLine(s1.Elapsed.TotalSeconds);
        }

        private static void Do100ItemSend()
        {
            var activeConfiguration = TelemetryConfiguration.Active;
            activeConfiguration.InstrumentationKey = "c351b2d8-10f5-45c9-902d-05100da0f8a6";

            var channel = new ServerTelemetryChannel();
            channel.Initialize(activeConfiguration);
            activeConfiguration.TelemetryChannel = channel;
            var telemetryClient = new TelemetryClient(activeConfiguration);
            for (int j = 0; j < 100; j++)
            {
                var req = new RequestTelemetry("Http", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200),
                    "200", (j % 2 == 0) ? true : false);
                req.Url = new Uri("http://www.google.com");

                var dep = new DependencyTelemetry("Http", "MyTarget", "bing.com", "bing.com?url=true", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200),
                    "200", (j % 2 == 0) ? true : false);

                telemetryClient.TrackRequest(req);
                //telemetryClient.TrackDependency(dep);
            }

            telemetryClient.Flush();
            Thread.Sleep(10000);

            Console.ReadLine();
        }

        private static void DoPerf()

        {
            Stopwatch s1 = new Stopwatch();
            s1.Start();

            var activeConfiguration = TelemetryConfiguration.Active;
            activeConfiguration.InstrumentationKey = "c351b2d8-10f5-45c9-902d-05100da0f8a6";

            var channel = new ServerTelemetryChannel();
            channel.Initialize(activeConfiguration);
            activeConfiguration.TelemetryChannel = channel;

            var builder = activeConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
            // builder.Use((next) => { return new AutocollectedMetricsExtractor(next); });
            // builder.UseSampling(5, excludedTypes: "Event");
            // builder.UseSampling(5, includedTypes: "Event");
            builder.Build();

            var telemetryClient = new TelemetryClient(activeConfiguration);

            int IterationMax = 500;
            int TaskCount = Environment.ProcessorCount;

            long[] runs = new long[IterationMax - 1];


            Stopwatch sw;

            for (int iter = 0; iter < IterationMax; iter++)
            {
                sw = new Stopwatch();
                sw.Start();
                Task[] tasks = new Task[TaskCount];

                for (int i = 0; i < TaskCount; i++)
                {
                    tasks[i] = new Task(() =>
                    {
                        for (int j = 0; j < 250; j++)
                        {
                            var req = new RequestTelemetry("Http", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200),
                                "200", (j % 2 == 0) ? true : false);
                            req.Url = new Uri("http://www.google.com");

                            var dep = new DependencyTelemetry("Http", "MyTarget", "bing.com", "bing.com?url=true", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200),
                                "200", (j % 2 == 0) ? true : false);

                            telemetryClient.TrackRequest(req);
                            telemetryClient.TrackDependency(dep);
                        }
                    });
                }

                for (int i = 0; i < TaskCount; i++)
                {
                    tasks[i].Start();
                }

                Task.WaitAll(tasks);

                sw.Stop();
                Console.WriteLine(sw.ElapsedMilliseconds);

                if (iter > 0)
                {
                    runs[iter - 1] = sw.ElapsedMilliseconds;
                }
            }

            // Thread.Sleep(30000);

            Console.WriteLine("Avge" + runs.Average());
        }
    }
}
