using System;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics
{
    public interface IMetricDataSeriesAggregator
    {
        DateTimeOffset PeriodStart { get; }
        DateTimeOffset PeriodEnd { get; }
        MetricDataSeries MetricDataSeries { get; set; }
        
        void TrackValue(double metricValue);
        ITelemetry CompleteAggregationPeriod(DateTimeOffset periodEnd);
        void SetValueFilter(IMetricValueFilter valueFilter);
    }
}
