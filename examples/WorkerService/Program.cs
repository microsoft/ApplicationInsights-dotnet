
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;

namespace WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                    // instrumentation key is read automatically from appsettings.json
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureOpenTelemetryTracerProvider(tracer => tracer.AddProcessor(new MyCustomProcessor()));
                });

        internal class MyCustomProcessor : BaseProcessor<Activity>
        {
            private readonly string name;

            public MyCustomProcessor(string name = "MyProcessor")
            {
                this.name = name;
            }

            public override void OnStart(Activity activity)
            {
                Debug.WriteLine($"{this.name}.OnStart({activity.DisplayName})");
            }

            public override void OnEnd(Activity activity)
            {
                Debug.WriteLine($"{this.name}.OnEnd({activity.DisplayName})");
                activity.SetTag("MyCustomProcessorTag", "MyCustomProcessorValue");
            }

            protected override bool OnForceFlush(int timeoutMilliseconds)
            {
                Debug.WriteLine($"{this.name}.OnForceFlush({timeoutMilliseconds})");
                return true;
            }

            protected override bool OnShutdown(int timeoutMilliseconds)
            {
                Debug.WriteLine($"{this.name}.OnShutdown({timeoutMilliseconds})");
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Debug.WriteLine($"{this.name}.Dispose({disposing})");
            }
            public void Initialize(ITelemetry telemetry)
            {
                // Replace with actual properties.
                (telemetry as ISupportProperties).Properties["MyCustomKey"] = "MyCustomValue";
            }
        }
    }
}
