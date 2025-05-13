namespace BasicConsoleApp
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    internal class Program
    {
        static void Main(string[] args)
        {
            var telemetryConfig = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
            };

            // Add custom TelemetryInitializer.
            telemetryConfig.TelemetryInitializers.Add(new MyCustomTelemetryInitializer());

            // Add custom TelemetryProcessor and build.
            var builder = telemetryConfig.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
            builder.Use(next => new MyCustomTelemetryProcessor(next));
            builder.Build();

            // Initialize the TelemetryClient
            var telemetryClient = new TelemetryClient(telemetryConfig);

            // **The following lines are examples of tracking different telemetry types.**

            telemetryClient.TrackEvent("SampleEvent");
            telemetryClient.TrackEvent(new EventTelemetry("SampleEventObject"));

            telemetryClient.TrackTrace("A trace message");
            telemetryClient.TrackTrace("A warning", SeverityLevel.Warning);
            telemetryClient.TrackTrace("A trace with properties", new System.Collections.Generic.Dictionary<string, string> { { "Key", "Value" } });
            telemetryClient.TrackTrace("A trace with severity and properties", SeverityLevel.Error, new System.Collections.Generic.Dictionary<string, string> { { "Key", "Value" } });
            telemetryClient.TrackTrace(new TraceTelemetry("TraceTelemetry object", SeverityLevel.Information));

            telemetryClient.TrackMetric("SampleMetric", 42.0);
            telemetryClient.TrackMetric(new MetricTelemetry("SampleMetricObject", 42.0));

            telemetryClient.TrackException(new InvalidOperationException("Something went wrong"));

            telemetryClient.TrackDependency("SQL", "GetOrders", "SELECT * FROM Orders", DateTimeOffset.Now, TimeSpan.FromMilliseconds(123), true);
            telemetryClient.TrackDependency(new DependencyTelemetry("SQL", "dbserver", "GetOrders", "SELECT * FROM Orders", DateTimeOffset.Now, TimeSpan.FromMilliseconds(123), "0", true));

            telemetryClient.TrackRequest("GET Home", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200), "200", true);
            telemetryClient.TrackRequest(new RequestTelemetry("GET HomeObject", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200), "200", true));

            // Explicitly call Flush() followed by sleep is required in Console Apps.
            // This is to ensure that even if application terminates, telemetry is sent to the back-end.
            telemetryClient.Flush();
            Task.Delay(500000).Wait();
        }
    }

    internal class MyCustomTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            // Replace with actual properties.
            (telemetry as ISupportProperties)?.Properties["MyCustomKey"] = "MyCustomValue";
        }
    }

    internal class MyCustomTelemetryProcessor : ITelemetryProcessor
    {
        ITelemetryProcessor next;

        public MyCustomTelemetryProcessor(ITelemetryProcessor next)
        {
            this.next = next;

        }

        public void Process(ITelemetry item)
        {
            // Example: Filter out all RequestTelemetry items.
            if (item is not RequestTelemetry)
            {
                this.next.Process(item);
            }
        }
    }
}
