namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics;

    /// <summary>
    /// An instance of this class is contained within the <see cref="AutocollectedMetricsExtractor"/> telemetry processor.
    /// It extracts auto-collected, pre-aggregated (aka. "standard") metrics from RequestTelemetry objects which represent invocations of the monitored service.
    /// </summary>
    internal class RequestMetricsExtractor : ISpecificAutocollectedMetricsExtractor
    {
        private Metric responseSuccessTimeMetric;
        private Metric responseFailureTimeMetric;

        public RequestMetricsExtractor()
        {
        }

        public string ExtractorName { get; } = typeof(RequestMetricsExtractor).FullName;

        public string ExtractorVersion { get; } = "1.0";

        public void InitializeExtractor(MetricManager metricManager)
        {
            this.responseSuccessTimeMetric = metricManager.CreateMetric(
                                                                MetricTerms.Autocollection.MetricNames.Request.Duration,
                                                                new Dictionary<string, string>()
                                                                {
                                                                    [MetricTerms.Autocollection.Request.PropertyNames.Success] = Boolean.TrueString,
                                                                });

            this.responseFailureTimeMetric = metricManager.CreateMetric(
                                                                MetricTerms.Autocollection.MetricNames.Request.Duration,
                                                                new Dictionary<string, string>()
                                                                {
                                                                    [MetricTerms.Autocollection.Request.PropertyNames.Success] = Boolean.FalseString,
                                                                });
        }

        public void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
        {
            RequestTelemetry request = fromItem as RequestTelemetry;
            if (request == null)
            {
                isItemProcessed = false;
                return;
            }

            bool isFailed = (request.Success.HasValue)
                                ? (request.Success == false)
                                : true;

            Metric metric = isFailed
                                ? this.responseFailureTimeMetric
                                : this.responseSuccessTimeMetric;

            if (metric != null)
            {
                isItemProcessed = true;
                metric.Track(request.Duration.TotalMilliseconds);
            }
            else
            {
                isItemProcessed = false;
            }
        }
    }
}