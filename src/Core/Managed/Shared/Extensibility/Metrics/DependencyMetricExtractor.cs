using System;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    public class DependencyMetricExtractor : MetricExtractorTelemetryProcessorBase
    {
        private const string Version = "1.0";

        private Metric _dependencyCallsDurationMetric;
        private Metric _dependencyCallsFailuresMetric;

        public DependencyMetricExtractor(ITelemetryProcessor nextProcessorInPipeline)
            :base(nextProcessorInPipeline, MetricTerms.Autocollection.Moniker.Key, MetricTerms.Autocollection.Moniker.Value)
        {
        }

        protected override string ExtractorVersion { get { return Version; } }

        public override void Initialize(TelemetryConfiguration configuration)
        {
            _dependencyCallsDurationMetric = MetricManager.CreateMetric(MetricTerms.Autocollection.MetricNames.DependencyCalls.Duration);
            _dependencyCallsFailuresMetric = MetricManager.CreateMetric(MetricTerms.Autocollection.MetricNames.DependencyCalls.Failures);
        }

        public override void Process(ITelemetry item)
        {
            DependencyTelemetry dependencyCall = item as DependencyTelemetry;
            if (dependencyCall == null)
            {
                return;
            }

            bool isFailed = (dependencyCall.Success != null) && (dependencyCall.Success == false);
            _dependencyCallsFailuresMetric.Track(isFailed ? 0.0 : 1.0);

            _dependencyCallsDurationMetric.Track(dependencyCall.Duration.TotalMilliseconds);
        }
    }
}
