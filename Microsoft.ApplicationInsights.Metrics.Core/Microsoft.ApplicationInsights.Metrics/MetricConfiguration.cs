using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    public static class MetricConfiguration
    {
        public static readonly IMetricSeriesConfiguration SimpleUIntMeasurement       = new SimpleMeasurementMetricSeriesConfiguration(lifetimeCounter: false, supportDoubleValues: false);
        public static readonly IMetricSeriesConfiguration SimpleDoubleMeasurement     = new SimpleMeasurementMetricSeriesConfiguration(lifetimeCounter: false, supportDoubleValues: true);
        public static readonly IMetricSeriesConfiguration SimpleUIntLifetimeCounter   = new SimpleMeasurementMetricSeriesConfiguration(lifetimeCounter: true, supportDoubleValues: false);
    }
}
