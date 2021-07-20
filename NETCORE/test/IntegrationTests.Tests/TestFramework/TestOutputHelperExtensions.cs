namespace IntegrationTests.Tests.TestFramework
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Xunit.Abstractions;

    public static class TestOutputHelperExtensions
    {
        public static void PrintTelemetryItems(this ITestOutputHelper testOutputHelper, TelemetryBag telemetryItems)
        {
            int i = 1;
            foreach (var item in telemetryItems.SentItems)
            {
                testOutputHelper.WriteLine("Item " + (i++) + ".");

                if (item is RequestTelemetry req)
                {
                    testOutputHelper.WriteLine("RequestTelemetry");
                    testOutputHelper.WriteLine(req.Name);
                    testOutputHelper.WriteLine(req.Duration.ToString());
                }
                else if (item is DependencyTelemetry dep)
                {
                    testOutputHelper.WriteLine("DependencyTelemetry");
                    testOutputHelper.WriteLine(dep.Name);
                }
                else if (item is TraceTelemetry trace)
                {
                    testOutputHelper.WriteLine("TraceTelemetry");
                    testOutputHelper.WriteLine(trace.Message);
                }
                else if (item is ExceptionTelemetry exc)
                {
                    testOutputHelper.WriteLine("ExceptionTelemetry");
                    testOutputHelper.WriteLine(exc.Message);
                }
                else if (item is MetricTelemetry met)
                {
                    testOutputHelper.WriteLine("MetricTelemetry");
                    testOutputHelper.WriteLine(met.Name + "" + met.Sum);
                }

                testOutputHelper.PrintProperties(item as ISupportProperties);
                testOutputHelper.WriteLine("----------------------------");
            }
        }

        public static void PrintProperties(this ITestOutputHelper testOutputHelper, ISupportProperties itemProps)
        {
            foreach (var prop in itemProps.Properties)
            {
                testOutputHelper.WriteLine(prop.Key + ":" + prop.Value);
            }
        }
    }
}
