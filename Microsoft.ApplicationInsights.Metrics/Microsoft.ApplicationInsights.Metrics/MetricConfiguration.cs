using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    public static class MetricConfiguration
    {
        public static readonly IMetricConfiguration SimpleMeasurement = new SimpleLifetimeCounterMetricConfiguration();
        public static readonly IMetricConfiguration SimpleLifetimeCounter = new SimpleLifetimeCounterMetricConfiguration();
    }
}
