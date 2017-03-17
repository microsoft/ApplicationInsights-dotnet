namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Participates in the telemetry pipeline as a telemetry processor and extracts auto-collected, pre-aggregated
    /// metrics from RequestTelemetry objects which represent invocations of the monitored service.
    /// </summary>
    public sealed class RequestMetricExtractor : MetricExtractorTelemetryProcessorBase
    {
        /// <summary>
        /// Version of this extractor. Used by the infrastructure to mark processed telemetry.
        /// Change this value when publically observed behavior changes in any way.
        /// </summary>
        private const string Version = "1.0";

        private Metric responseSuccessTimeMetric;
        private Metric responseFailureTimeMetric;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMetricExtractor" /> class.
        /// </summary>
        /// <param name="nextProcessorInPipeline">Subsequent telemetry processor.</param>
        public RequestMetricExtractor(ITelemetryProcessor nextProcessorInPipeline)
            : base(nextProcessorInPipeline, MetricTerms.Autocollection.Moniker.Key, MetricTerms.Autocollection.Moniker.Value)
        {
        }

        /// <summary>
        /// Exposes the version of this Extractor's public contracts to the base class.
        /// </summary>
        protected override string ExtractorVersion
        {
            get
            {
                return Version;
            }
        }

        /// <summary>
        /// Initializes the internal metrics trackers based on settings.
        /// </summary>
        /// <param name="unusedConfiguration">Is not currently used.</param>
        public override void InitializeExtractor(TelemetryConfiguration unusedConfiguration)
        {
            this.responseSuccessTimeMetric = MetricManager.CreateMetric(
                                                                MetricTerms.Autocollection.MetricNames.Request.Duration,
                                                                new Dictionary<string, string>()
                                                                {
                                                                    [MetricTerms.Autocollection.Request.PropertyName.Success] = Boolean.TrueString,
                                                                });

            this.responseFailureTimeMetric = MetricManager.CreateMetric(
                                                                MetricTerms.Autocollection.MetricNames.Request.Duration,
                                                                new Dictionary<string, string>()
                                                                {
                                                                    [MetricTerms.Autocollection.Request.PropertyName.Success] = Boolean.FalseString,
                                                                });
        }

        /// <summary>
        /// Extracts appropriate data points for auto-collected, pre-aggregated metrics from a single <c>RequestTelemetry</c> item.
        /// </summary>
        /// <param name="fromItem">The telemetry item from which to extract the metric data points.</param>
        /// <param name="isItemProcessed">Whether of not the specified item was processed (aka not ignored) by this extractor.</param>
        public override void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
        {
            RequestTelemetry request = fromItem as RequestTelemetry;
            if (request == null)
            {
                isItemProcessed = false;
                return;
            }

            bool isFailed = (request.Success != null) && (request.Success == false);
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
