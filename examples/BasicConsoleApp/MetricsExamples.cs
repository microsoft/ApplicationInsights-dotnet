namespace BasicConsoleApp
{
    using Microsoft.ApplicationInsights;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Demonstrates all metric tracking scenarios using Application Insights.
    /// </summary>
    public static class MetricsExamples
    {
        public static void RunAllScenarios(TelemetryClient telemetryClient)
        {
            Console.WriteLine("\n=== Comprehensive Metrics Examples ===\n");

            // Scenario 1: Simple metric tracking (no dimensions)
            TrackSimpleMetric(telemetryClient);

            // Scenario 2: Metric with properties (dimensions as tags)
            TrackMetricWithProperties(telemetryClient);

            // Scenario 3: GetMetric with 0 dimensions
            TrackWithGetMetric0D(telemetryClient);

            // Scenario 4: GetMetric with 1 dimension
            TrackWithGetMetric1D(telemetryClient);

            // Scenario 5: GetMetric with 2 dimensions
            TrackWithGetMetric2D(telemetryClient);

            // Scenario 6: GetMetric with 3 dimensions
            TrackWithGetMetric3D(telemetryClient);

            // Scenario 7: GetMetric with 4 dimensions
            TrackWithGetMetric4D(telemetryClient);

            // Scenario 8: GetMetric with MetricIdentifier (9 dimensions)
            TrackWithGetMetricIdentifier9D(telemetryClient);

            // Scenario 9: Metric with namespace via TrackMetric
            TrackMetricWithNamespace(telemetryClient);

            // Scenario 10: High-volume metric tracking
            TrackHighVolumeMetrics(telemetryClient);

            // Scenario 11: TrackMetric with properties (alternative to GetMetric for 5+ dimensions)
            TrackMetricWithManyDimensions(telemetryClient);

            Console.WriteLine("\n✓ All metric scenarios completed!\n");
        }

        private static void TrackSimpleMetric(TelemetryClient client)
        {
            Console.WriteLine("1. Simple metric (no dimensions):");
            client.TrackMetric("SimpleCounter", 42.0);
            Console.WriteLine("   Tracked: SimpleCounter = 42.0\n");
        }

        private static void TrackMetricWithProperties(TelemetryClient client)
        {
            Console.WriteLine("2. Metric with properties:");
            var properties = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "WestUS" }
            };
            client.TrackMetric("RequestDuration", 123.45, properties);
            Console.WriteLine("   Tracked: RequestDuration = 123.45 [Environment=Production, Region=WestUS]\n");
        }

        private static void TrackWithGetMetric0D(TelemetryClient client)
        {
            Console.WriteLine("3. GetMetric with 0 dimensions:");
            var metric = client.GetMetric("ProcessingTime");
            metric.TrackValue(15.5);
            metric.TrackValue(20.3);
            metric.TrackValue(18.7);
            Console.WriteLine("   Tracked 3 values: 15.5, 20.3, 18.7\n");
        }

        private static void TrackWithGetMetric1D(TelemetryClient client)
        {
            Console.WriteLine("4. GetMetric with 1 dimension:");
            var metric = client.GetMetric("ResponseTime", "StatusCode");
            metric.TrackValue(45.2, "200");
            metric.TrackValue(150.8, "404");
            metric.TrackValue(52.3, "200");
            Console.WriteLine("   Tracked: StatusCode=200 (45.2, 52.3), StatusCode=404 (150.8)\n");
        }

        private static void TrackWithGetMetric2D(TelemetryClient client)
        {
            Console.WriteLine("5. GetMetric with 2 dimensions:");
            var metric = client.GetMetric("DatabaseQuery", "Database", "Operation");
            metric.TrackValue(12.5, "UsersDB", "SELECT");
            metric.TrackValue(25.3, "OrdersDB", "INSERT");
            metric.TrackValue(8.7, "UsersDB", "SELECT");
            Console.WriteLine("   Tracked: Database=UsersDB + Operation=SELECT (2 values), Database=OrdersDB + Operation=INSERT\n");
        }

        private static void TrackWithGetMetric3D(TelemetryClient client)
        {
            Console.WriteLine("6. GetMetric with 3 dimensions:");
            var metric = client.GetMetric("ApiLatency", "Endpoint", "Method", "Region");
            metric.TrackValue(35.0, "/api/users", "GET", "US-West");
            metric.TrackValue(42.5, "/api/orders", "POST", "US-East");
            metric.TrackValue(28.3, "/api/users", "GET", "EU-West");
            Console.WriteLine("   Tracked: 3 API calls with Endpoint + Method + Region dimensions\n");
        }

        private static void TrackWithGetMetric4D(TelemetryClient client)
        {
            Console.WriteLine("7. GetMetric with 4 dimensions:");
            var metric = client.GetMetric("CacheHit", "CacheType", "Region", "Tenant", "Environment");
            metric.TrackValue(1.0, "Redis", "US-West", "TenantA", "Prod");
            metric.TrackValue(0.0, "Memory", "US-East", "TenantB", "Prod");
            metric.TrackValue(1.0, "Redis", "EU-West", "TenantA", "Staging");
            Console.WriteLine("   Tracked: Cache hits with CacheType + Region + Tenant + Environment dimensions\n");
        }

        private static void TrackWithGetMetricIdentifier9D(TelemetryClient client)
        {
            Console.WriteLine("8. GetMetric with MetricIdentifier (9 dimensions):");
            
            // Create a MetricIdentifier with 9 dimensions
            var metricIdentifier = new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                metricNamespace: "MyApp.Advanced",
                metricId: "ComplexTransaction",
                dimension1Name: "Region",
                dimension2Name: "Service",
                dimension3Name: "Operation",
                dimension4Name: "Tenant",
                dimension5Name: "Environment",
                dimension6Name: "Version",
                dimension7Name: "DeploymentRing",
                dimension8Name: "ClientType",
                dimension9Name: "Protocol");
            
            var metric = client.GetMetric(metricIdentifier);
            
            // Track values with all 9 dimension values
            metric.TrackValue(125.5, "US-West", "API", "CreateOrder", "TenantA", "Prod", "v2.1", "Ring3", "Mobile", "HTTPS");
            metric.TrackValue(98.3, "EU-Central", "API", "UpdateOrder", "TenantB", "Prod", "v2.1", "Ring3", "Web", "HTTPS");
            metric.TrackValue(210.7, "Asia-East", "API", "CreateOrder", "TenantA", "Staging", "v2.2", "Ring1", "Mobile", "HTTP2");
            
            Console.WriteLine("   Tracked: Complex transactions with 9 dimensions (Region, Service, Operation, Tenant, Environment, Version, DeploymentRing, ClientType, Protocol)\n");
        }

        private static void TrackMetricWithNamespace(TelemetryClient client)
        {
            Console.WriteLine("9. Metric with namespace via TrackMetric:");
            
            // For metrics with namespace and properties, use TrackMetric with string name
            var properties1 = new Dictionary<string, string>
            {
                { "PoolType", "ReadPool" }
            };
            // Note: Namespace support would require using GetMetric(MetricIdentifier) with namespace parameter
            client.TrackMetric("ConnectionPoolSize", 50.0, properties1);
            
            var properties2 = new Dictionary<string, string>
            {
                { "PoolType", "WritePool" }
            };
            client.TrackMetric("ConnectionPoolSize", 25.0, properties2);
            
            Console.WriteLine("   Tracked: ConnectionPoolSize with PoolType property\n");
        }

        private static void TrackHighVolumeMetrics(TelemetryClient client)
        {
            Console.WriteLine("10. High-volume metric tracking:");
            var metric = client.GetMetric("HighVolumeCounter", "Source");
            
            for (int i = 0; i < 100; i++)
            {
                metric.TrackValue(i, "Batch1");
            }
            
            Console.WriteLine("   Tracked 100 values in batch\n");
        }

        private static void TrackMetricWithManyDimensions(TelemetryClient client)
        {
            Console.WriteLine("11. TrackMetric with properties (for 5+ dimensions):");
            
            // For scenarios requiring more than 4 dimensions, use TrackMetric with properties
            var properties = new Dictionary<string, string>
            {
                { "PaymentMethod", "CreditCard" },
                { "Currency", "USD" },
                { "Country", "US" },
                { "Status", "Approved" },
                { "Channel", "Online" },
                { "CardType", "Visa" }
            };
            client.TrackMetric("TransactionProcessing", 99.99, properties);
            
            Console.WriteLine("   Tracked: Transaction with 6 dimensions using properties\n");
        }
    }
}
