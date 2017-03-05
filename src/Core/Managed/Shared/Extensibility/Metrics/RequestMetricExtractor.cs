using System;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    public class RequestMetricExtractor : MetricExtractorTelemetryProcessorBase
    {
        private const string Version = "1.0";

        private Metric responseSuccessTimeMetric;
        private Metric responseFailureTimeMetric;

        public RequestMetricExtractor(ITelemetryProcessor nextProcessorInPipeline)
            :base(nextProcessorInPipeline, MetricTerms.Autocollection.Moniker.Key, MetricTerms.Autocollection.Moniker.Value)
        {
        }

        protected override string ExtractorVersion { get { return Version; } }

        public override void InitializeExtractor(TelemetryConfiguration configuration)
        {
            this.responseSuccessTimeMetric = MetricManager.CreateMetric(MetricTerms.Autocollection.MetricNames.Request.Duration,
                                                                        new Dictionary<string, string>() {
                                                                            [MetricTerms.Autocollection.Request.PropertyName.Success] = Boolean.TrueString,
                                                                        });

            this.responseFailureTimeMetric = MetricManager.CreateMetric(MetricTerms.Autocollection.MetricNames.Request.Duration,
                                                                        new Dictionary<string, string>() {
                                                                            [MetricTerms.Autocollection.Request.PropertyName.Success] = Boolean.FalseString,
                                                                        });
        }

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
