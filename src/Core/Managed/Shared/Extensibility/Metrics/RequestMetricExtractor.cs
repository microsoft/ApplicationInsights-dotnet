using System;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    public class RequestMetricExtractor : MetricExtractorTelemetryProcessorBase
    {
        private const string Version = "1.0";

        private Metric _requestsMetric;
        private Metric _requestsFailutesMetric;
        private Metric _responseTimeMetric;

        public RequestMetricExtractor(ITelemetryProcessor nextProcessorInPipeline)
            :base(nextProcessorInPipeline, MetricTerms.Autocollection.Moniker.Key, MetricTerms.Autocollection.Moniker.Value)
        {
        }

        protected override string ExtractorVersion { get { return Version; } }

        public override void Initialize(TelemetryConfiguration configuration)
        {
            _requestsMetric         = MetricManager.CreateMetric(MetricTerms.Autocollection.MetricNames.Requests.Count);
            _requestsFailutesMetric = MetricManager.CreateMetric(MetricTerms.Autocollection.MetricNames.Requests.Failures);
            _responseTimeMetric     = MetricManager.CreateMetric(MetricTerms.Autocollection.MetricNames.Requests.ResponseTime);
        }

        public override void Process(ITelemetry item)
        {
            RequestTelemetry request = item as RequestTelemetry;
            if (request == null)
            {
                return;
            }

            _requestsMetric.Track(1.0);

            bool isFailed = (request.Success != null) && (request.Success == false);
            _requestsFailutesMetric.Track(isFailed ? 1.0 : 0.0);

            _responseTimeMetric.Track(request.Duration.TotalMilliseconds);
        }
    }
}
