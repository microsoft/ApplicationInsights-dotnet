using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    public interface IMetricAggregateToTelemetryPipelineConverter
    {
        /// <summary />
        /// <param name="aggregate"></param>
        /// <returns></returns>
        object Convert(MetricAggregate aggregate);
    }
}
