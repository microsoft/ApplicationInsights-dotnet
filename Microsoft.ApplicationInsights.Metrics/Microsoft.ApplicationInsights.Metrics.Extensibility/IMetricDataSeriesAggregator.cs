using System;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public interface IMetricDataSeriesAggregator
    {
        DateTimeOffset PeriodStart { get; }
        DateTimeOffset PeriodEnd { get; }
        bool IsCompleted { get; }
        MetricDataSeries DataSeries { get; }
        
        void Initialize(DateTimeOffset periodEnd, IMetricValueFilter valueFilter);
        ITelemetry CompleteAggregation(DateTimeOffset periodEnd);
        ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd);

        void TrackValue(uint metricValue);
        void TrackValue(double metricValue);
        void TrackValue(object metricValue);
    }
}
