using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WorkerServiceSampleWithApplicationInsights
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

                    // Application Insights

                    // Add custom TelemetryInitializer
                    services.AddSingleton<ITelemetryInitializer, MyCustomTelemetryInitializer>();

                    // Add custom TelemetryProcessor
                    services.AddApplicationInsightsTelemetryProcessor<MyCustomTelemetryProcessor>();

                    // Example on Configuring TelemetryModules.
                    // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Not a real api key, this is example code.")]
                    services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((mod, opt) => mod.AuthenticationApiKey = "put_actual_authentication_key_here");

                    // instrumentation key is read automatically from appsettings.json
                    services.AddApplicationInsightsTelemetryWorkerService();
                });
    }
}
