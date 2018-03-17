using System;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    internal class AccumulatorAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricSeriesConfigurationForAccumulator.Constants.AggregateKindMoniker; } }

        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = 1;
            telemetryItem.Sum = aggregate.GetDataValue<double>(MetricSeriesConfigurationForAccumulator.Constants.AggregateKindDataKeys.Sum, 0.0);
            telemetryItem.Min = aggregate.GetDataValue<double>(MetricSeriesConfigurationForAccumulator.Constants.AggregateKindDataKeys.Min, 0.0);
            telemetryItem.Max = aggregate.GetDataValue<double>(MetricSeriesConfigurationForAccumulator.Constants.AggregateKindDataKeys.Max, 0.0);
            telemetryItem.StandardDeviation = null;
        }
    }
}
