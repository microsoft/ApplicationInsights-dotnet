using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics.TestUtil
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class Util

    {
        public const string AggregationIntervalMonikerPropertyKey = "_MS.AggregationIntervalMs";
        public const double MaxAllowedPrecisionError = 0.00001;

        public static void AssertAreEqual<T>(T[] array1, T[] array2)
        {
            if (array1 == array2)
            {
                return;
            }

            Assert.IsNotNull(array1);
            Assert.IsNotNull(array2);

            Assert.AreEqual(array1.Length, array1.Length);

            for(int i = 0; i < array1.Length; i++)
            {
                Assert.AreEqual(array1[i], array2[i], message: $" at index {i}");
            }
        }

        public static bool AreEqual<T>(T[] array1, T[] array2)
        {
            if (array1 == array2)
            {
                return true;
            }

            if (array1 == null)
            {
                return false;
            }

            if (array2 == null)
            {
                return false;
            }

            if (array1.Length != array1.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] == null && array2[i] == null)
                {
                    continue;
                }

                if (array1 == null)
                {
                    return false;
                }

                if (array2 == null)
                {
                    return false;
                }

                if (! array1[i].Equals(array2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static TelemetryConfiguration CreateAITelemetryConfig(out IList<ITelemetry> telemetrySentToChannel)
        {
            StubTelemetryChannel channel = new StubTelemetryChannel();
            string iKey = Guid.NewGuid().ToString("D");
            TelemetryConfiguration telemetryConfig = new TelemetryConfiguration(iKey, channel);

            var channelBuilder = new TelemetryProcessorChainBuilder(telemetryConfig);
            channelBuilder.Build();

            foreach (ITelemetryProcessor initializer in telemetryConfig.TelemetryInitializers)
            {
                ITelemetryModule m = initializer as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }

            foreach (ITelemetryProcessor processor in telemetryConfig.TelemetryProcessors)
            {
                ITelemetryModule m = processor as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }

            telemetrySentToChannel = channel.TelemetryItems;
            return telemetryConfig;
        }

        public static void ValidateNumericAggregateValues(ITelemetry aggregate, string name, int count, double sum, double max, double min, double stdDev, DateTimeOffset timestamp, string periodMs)
        {
            ValidateNumericAggregateValues(aggregate, name, count, sum, max, min, stdDev, timestamp, periodMs);

            var metricAggregate = (MetricTelemetry) aggregate;
            Assert.AreEqual(timestamp, metricAggregate.Timestamp, "metricAggregate.Timestamp mismatch");
            Assert.AreEqual(periodMs, metricAggregate?.Properties?[Util.AggregationIntervalMonikerPropertyKey], "metricAggregate.Properties[AggregationIntervalMonikerPropertyKey] mismatch");
        }

        public static void ValidateNumericAggregateValues(ITelemetry aggregate, string name, int count, double sum, double max, double min, double stdDev)
        {
            Assert.IsNotNull(aggregate);

            MetricTelemetry metricAggregate = aggregate as MetricTelemetry;

            Assert.IsNotNull(metricAggregate);

            Assert.AreEqual(name, metricAggregate.Name, "metricAggregate.Name mismatch");
            Assert.AreEqual(count, metricAggregate.Count, "metricAggregate.Count mismatch");
            Assert.AreEqual(sum, metricAggregate.Sum, Util.MaxAllowedPrecisionError, "metricAggregate.Sum mismatch");
            Assert.AreEqual(max, metricAggregate.Max.Value, Util.MaxAllowedPrecisionError, "metricAggregate.Max mismatch");
            Assert.AreEqual(min, metricAggregate.Min.Value, Util.MaxAllowedPrecisionError, "metricAggregate.Min mismatch");

            // For very large numbers we perform an approx comparison.
            if (Math.Abs(stdDev) > Int64.MaxValue)
            {
                double expectedStdDevScale = Math.Floor(Math.Log10(Math.Abs(stdDev)));
                double actualStdDevScale = Math.Floor(Math.Log10(Math.Abs(metricAggregate.StandardDeviation.Value)));
                Assert.AreEqual(expectedStdDevScale, actualStdDevScale, "metricAggregate.StandardDeviation (exponent) mismatch");
                Assert.AreEqual(
                            stdDev / Math.Pow(10, expectedStdDevScale),
                            metricAggregate.StandardDeviation.Value / Math.Pow(10, actualStdDevScale),
                            Util.MaxAllowedPrecisionError,
                            "metricAggregate.StandardDeviation (significant part) mismatch");
            }
            else
            {
                Assert.AreEqual(stdDev, metricAggregate.StandardDeviation.Value, Util.MaxAllowedPrecisionError, "metricAggregate.StandardDeviation mismatch");
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
