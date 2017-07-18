using System;

using Microsoft.ApplicationInsights.Metrics;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights
{
    public static class TelemetryClientExtensions
    {

        public static Metric GetMetric(this TelemetryClient telemetryClient,
                                       string metricId)
        {
            return GetOrCreateMetric(telemetryClient, metricId, dimension1Name: null, dimension2Name: null, metricConfiguration: null);
        }

        public static Metric GetMetric(this TelemetryClient telemetryClient,
                                      string metricId,
                                      IMetricConfiguration metricConfiguration)
        {
            return GetOrCreateMetric(telemetryClient, metricId, dimension1Name: null, dimension2Name: null, metricConfiguration: metricConfiguration);
        }

        public static Metric GetMetric(this TelemetryClient telemetryClient,
                                       string metricId,
                                       string dimension1Name)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));

            return GetOrCreateMetric(telemetryClient, metricId, dimension1Name, dimension2Name: null, metricConfiguration: null);
        }

        public static Metric GetMetric(this TelemetryClient telemetryClient,
                                       string metricId,
                                       string dimension1Name,
                                       IMetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));

            return GetOrCreateMetric(telemetryClient, metricId, dimension1Name, dimension2Name: null, metricConfiguration: metricConfiguration);
        }

        public static Metric GetMetric(this TelemetryClient telemetryClient,
                                       string metricId,
                                       string dimension1Name,
                                       string dimension2Name)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));
            Util.ValidateNotNullOrWhitespace(dimension2Name, nameof(dimension2Name));

            return GetOrCreateMetric(telemetryClient, metricId, dimension1Name, dimension2Name, metricConfiguration: null);
        }

        public static Metric GetMetric(this TelemetryClient telemetryClient,
                                       string metricId,
                                       string dimension1Name,
                                       string dimension2Name,
                                       IMetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNullOrWhitespace(dimension1Name, nameof(dimension1Name));
            Util.ValidateNotNullOrWhitespace(dimension2Name, nameof(dimension2Name));

            return GetOrCreateMetric(telemetryClient, metricId, dimension1Name, dimension2Name, metricConfiguration);
        }

        private static Metric GetOrCreateMetric(TelemetryClient telemetryClient,
                                                string metricId,
                                                string dimension1Name,
                                                string dimension2Name,
                                                IMetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));
            

            TelemetryConfiguration pipeline = Util.GetTelemetryConfiguration(telemetryClient);
            MetricsCache cache = pipeline.Metrics().GetOrCreateCache(MetricsCache.CreateNewInstance);

            if (cache == null)
            {
                throw new InvalidOperationException($"telemetryConfiguration.Metrics().GetOrCreateCache(..) unexpectedly returned null."
                                                  + $" This indicates that multiple extensions attempt to use"
                                                  + $" the \"Cache\" extension point of the {nameof(MetricManager)} in a conflicting manner.");
            }

            Metric metric = cache.GetOrCreateMetric(metricId, dimension1Name, dimension2Name, metricConfiguration);
            return metric;
        }

    }
}
