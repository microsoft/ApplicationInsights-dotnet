namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
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
        private MetricV1 responseSuccessTimeMetric;
        private MetricV1 responseFailureTimeMetric;

        public RequestMetricsExtractor()
        {
        }

        public string ExtractorName { get; } = "Requests";

        public string ExtractorVersion { get; } = "1.0";

        public void InitializeExtractor(MetricManagerV1 metricManager)
        {
            this.responseSuccessTimeMetric = metricManager.CreateMetric(
                    MetricTerms.Autocollection.Metric.RequestDuration.Name,
                    new Dictionary<string, string>()
                    {
                        [MetricTerms.Autocollection.Request.PropertyNames.Success] = Boolean.TrueString,
                        [MetricTerms.Autocollection.MetricId.Moniker.Key] = MetricTerms.Autocollection.Metric.RequestDuration.Id,
                    });

            this.responseFailureTimeMetric = metricManager.CreateMetric(
                    MetricTerms.Autocollection.Metric.RequestDuration.Name,
                    new Dictionary<string, string>()
                    {
                        [MetricTerms.Autocollection.Request.PropertyNames.Success] = Boolean.FalseString,
                        [MetricTerms.Autocollection.MetricId.Moniker.Key] = MetricTerms.Autocollection.Metric.RequestDuration.Id,
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

            bool isFailed = request.Success.HasValue
                                ? (request.Success.Value == false)
                                : false;

            MetricV1 metric = isFailed
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