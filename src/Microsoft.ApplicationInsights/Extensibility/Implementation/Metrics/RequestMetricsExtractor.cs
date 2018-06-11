namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Metrics;

    /// <summary>
    /// An instance of this class is contained within the <see cref="AutocollectedMetricsExtractor"/> telemetry processor.
    /// It extracts auto-collected, pre-aggregated (aka. "standard") metrics from RequestTelemetry objects which represent
    /// invocations of the monitored service.
    /// </summary>
    internal class RequestMetricsExtractor : ISpecificAutocollectedMetricsExtractor
    {
        private MetricSeries responseTimeSuccessSeries;
        private MetricSeries responseTimeFailureSeries;

        public RequestMetricsExtractor()
        {
        }

        public string ExtractorName { get; } = "Requests";

        public string ExtractorVersion { get; } = "1.1";

        public void InitializeExtractor(TelemetryClient metricTelemetryClient)
        {
            if (metricTelemetryClient == null)
            {
                this.responseTimeSuccessSeries = null;
                this.responseTimeFailureSeries = null;
            }
            else
            {
                Metric responseTimeMetric = metricTelemetryClient.GetMetric(
                                                    MetricTerms.Autocollection.Metric.RequestDuration.Name,
                                                    MetricTerms.Autocollection.Request.PropertyNames.Success,
                                                    MetricTerms.Autocollection.MetricId.Moniker.Key,
                                                    MetricConfigurations.Common.Measurement(),
                                                    MetricAggregationScope.TelemetryClient);

                responseTimeMetric.TryGetDataSeries(out this.responseTimeSuccessSeries, Boolean.TrueString, MetricTerms.Autocollection.Metric.RequestDuration.Id);
                responseTimeMetric.TryGetDataSeries(out this.responseTimeFailureSeries, Boolean.FalseString, MetricTerms.Autocollection.Metric.RequestDuration.Id);
            }
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

            MetricSeries metricSeries = isFailed
                                ? this.responseTimeFailureSeries
                                : this.responseTimeSuccessSeries;

            if (metricSeries != null)
            {
                isItemProcessed = true;
                metricSeries.TrackValue(request.Duration.TotalMilliseconds);
            }
            else
            {
                isItemProcessed = false;
            }
        }
    }
}