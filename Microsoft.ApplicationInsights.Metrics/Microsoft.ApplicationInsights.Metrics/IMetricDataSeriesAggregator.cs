using System;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics
{
    public interface IMetricDataSeriesAggregator
    {
        IMetricDataSeriesConfiguration Configuration { get; }
        DateTimeOffset PeriodStart { get; }
        MetricDataSeries MetricDataSeries { get; set; }
        bool NeedsRetainState { get; set; }

        void TrackPreviousState(IMetricDataSeriesAggregator previousAggregator);
        void TrackValue(double metricValue);
        ITelemetry Complete(DateTimeOffset periodEnd);
    }
}
