using System;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    internal class GaugeAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricSeriesConfigurationForGauge.Constants.AggregateKindMoniker; } }

        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = 1;
            telemetryItem.Sum = aggregate.GetDataValue<double>(MetricSeriesConfigurationForGauge.Constants.AggregateKindDataKeys.Last, 0.0);
            telemetryItem.Min = aggregate.GetDataValue<double>(MetricSeriesConfigurationForGauge.Constants.AggregateKindDataKeys.Min, 0.0);
            telemetryItem.Max = aggregate.GetDataValue<double>(MetricSeriesConfigurationForGauge.Constants.AggregateKindDataKeys.Max, 0.0);
            telemetryItem.StandardDeviation = null;
        }
    }
}
