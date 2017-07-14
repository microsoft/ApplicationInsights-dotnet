using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    public static class MetricConfiguration
    {
        public static readonly IMetricConfiguration SimpleUIntMeasurement       = new SimpleMeasurementMetricConfiguration(lifetimeCounter: false, supportDoubleValues: false);
        public static readonly IMetricConfiguration SimpleDoubleMeasurement     = new SimpleMeasurementMetricConfiguration(lifetimeCounter: false, supportDoubleValues: true);
        public static readonly IMetricConfiguration SimpleUIntLifetimeCounter   = new SimpleMeasurementMetricConfiguration(lifetimeCounter: true, supportDoubleValues: false);
    }
}
