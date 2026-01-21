namespace BasicConsoleApp
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    internal class Program
    {
        private static readonly ActivitySource MyActivitySource = new("MyCompany.MyProduct.MyLibrary");

        static void Main(string[] args)
        {

            var telemetryConfig = TelemetryConfiguration.CreateDefault();
            telemetryConfig.ConnectionString = "";

            telemetryConfig.ConfigureOpenTelemetryBuilder(builder => builder.WithTracing(tracing => tracing.AddSource("MyCompany.MyProduct.MyLibrary").AddConsoleExporter())
                                                                     .WithLogging(logging => logging.AddConsoleExporter())
                                                                     .WithMetrics(metrics => metrics.AddConsoleExporter()));

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

            // **Metrics Examples**
            telemetryClient.TrackMetric("SampleMetric", 42.0);
            telemetryClient.TrackMetric("SampleMetricWithProperties", 99.5, new System.Collections.Generic.Dictionary<string, string> { { "Environment", "Production" } });
            telemetryClient.TrackMetric(new MetricTelemetry("SampleMetricObject", 42.0));

            // GetMetric().TrackValue() - Preferred approach for metrics
            var responseTimeMetric = telemetryClient.GetMetric("ResponseTime");
            responseTimeMetric.TrackValue(123.45);
            responseTimeMetric.TrackValue(234.56);

            // GetMetric with dimensions
            var requestsPerEndpoint = telemetryClient.GetMetric("RequestsPerEndpoint", "Endpoint");
            requestsPerEndpoint.TrackValue(1, "/api/users");
            requestsPerEndpoint.TrackValue(1, "/api/orders");
            requestsPerEndpoint.TrackValue(1, "/api/users");

            // Run all ExceptionTelemetry examples
            ExceptionTelemetryExamples.Run(telemetryClient);
            // Run comprehensive metrics examples
            Console.WriteLine("\n--- Running Comprehensive Metrics Examples ---");
            MetricsExamples.RunAllScenarios(telemetryClient);

            telemetryClient.TrackTrace("A trace with properties", new System.Collections.Generic.Dictionary<string, string> { { "Key", "Value" } });
            telemetryClient.TrackTrace("A trace with severity and properties", SeverityLevel.Error, new System.Collections.Generic.Dictionary<string, string> { { "Key", "Value" } });
            telemetryClient.TrackDependency("SQL", "GetOrders", "SELECT * FROM Orders", DateTimeOffset.Now, TimeSpan.FromMilliseconds(123), true);
            telemetryClient.TrackDependency(new DependencyTelemetry("SQL", "dbserver", "GetOrders", "SELECT * FROM Orders", DateTimeOffset.Now, TimeSpan.FromMilliseconds(123), "0", true));

            telemetryClient.TrackRequest("GET Home", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200), "200", true);
            telemetryClient.TrackRequest(new RequestTelemetry("GET HomeObject", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200), "200", true));

            // 1. Simple request operation
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("TestRequest"))
            {
                Console.WriteLine("Inside Request Operation");
                telemetryClient.TrackTrace("Processing inside request operation", SeverityLevel.Information);
                Task.Delay(100).Wait(); // simulate work
            }

            // 2. Dependency operation (client span)
            using (var dep = telemetryClient.StartOperation<DependencyTelemetry>("TestDependency"))
            {
                Console.WriteLine("Inside Dependency Operation");
                dep.Telemetry.Type = "SQL";
                dep.Telemetry.Data = "SELECT * FROM Orders";
                dep.Telemetry.Target = "dbserver";
                Task.Delay(50).Wait();
            }


            // Define the parent context explicitly
            var parentTraceId = ActivityTraceId.CreateRandom();
            var parentSpanId = ActivitySpanId.CreateRandom();
            var parentContext = new ActivityContext(parentTraceId, parentSpanId, ActivityTraceFlags.Recorded);

            // Start the activity with W3C context
            var existingActivity = MyActivitySource.StartActivity(
                "ExternalActivity",
                ActivityKind.Server, // or Client, Consumer, Producer — depending on scenario
                parentContext);

            using (var op = telemetryClient.StartOperation<RequestTelemetry>(existingActivity))
            {
                Console.WriteLine("Processing external activity...");
                telemetryClient.TrackTrace("Message consumed under existing trace context.");
            }

            existingActivity.Stop();


            // Explicitly call Flush() followed by sleep is required in Console Apps.
            // This is to ensure that even if application terminates, telemetry is sent to the back-end.
            telemetryClient.Flush();
            Task.Delay(5000).Wait();
        }
    }

    /*internal class MyCustomTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            // Replace with actual properties.
            (telemetry as ISupportProperties).Properties["MyCustomKey"] = "MyCustomValue";
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
}*/
}
