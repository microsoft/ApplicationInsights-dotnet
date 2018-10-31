using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace PerfTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var activeConfiguration = TelemetryConfiguration.Active;
            activeConfiguration.InstrumentationKey = "c351b2d8-10f5-45c9-902d-05100da0f8a6";

            var channel = new MyChannel();
            activeConfiguration.TelemetryChannel = channel;

            var builder = activeConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
            builder.Use((next) => { return new AutocollectedMetricsExtractor(next); });
            builder.UseSampling(5, excludedTypes: "Event");
            builder.UseSampling(5, includedTypes: "Event");
            builder.Use((next) => { return new CallBackInvokerTelemetryProcessor(next); });
            builder.Build();

            activeConfiguration.TelemetryInitializers.Add(new MyCallbackSupportingInitializers());

            var telemetryClient = new TelemetryClient(activeConfiguration);
            for (int i = 0; i < 50; i++)
            {
                telemetryClient.TrackDependency("SQL", "MySqlDb", "Name", "Select name from details",
                                DateTimeOffset.Now, TimeSpan.FromMilliseconds(200), "200", false);
            }
        }

        static void DoPerfTest()
        {
            var activeConfiguration = TelemetryConfiguration.Active;
            activeConfiguration.InstrumentationKey = "c351b2d8-10f5-45c9-902d-05100da0f8a6";

            var channel = new MyChannel();
            activeConfiguration.TelemetryChannel = channel;

            var builder = activeConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
            builder.Use((next) => { return new AutocollectedMetricsExtractor(next); });
            builder.UseSampling(5, excludedTypes: "Event");
            builder.UseSampling(5, includedTypes: "Event");
            builder.Build();

            var telemetryClient = new TelemetryClient(activeConfiguration);

            int IterationMax = 25;
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
                        for (int j = 0; i < 50000; i++)
                        {
                            telemetryClient.TrackDependency("SQL", "MySqlDb", "Name", "Select name from details",
                                DateTimeOffset.Now, TimeSpan.FromMilliseconds(200), "200", (j % 2 == 0) ? true : false);
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

            Console.WriteLine("Avge" + runs.Average());
        }
    }

    internal class MyChannel : ITelemetryChannel
    {
        public MyChannel()
        {
        }

        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Dispose()
        {

        }

        public void Flush()
        {

        }

        public void Send(ITelemetry item)
        {

        }
    }
}
