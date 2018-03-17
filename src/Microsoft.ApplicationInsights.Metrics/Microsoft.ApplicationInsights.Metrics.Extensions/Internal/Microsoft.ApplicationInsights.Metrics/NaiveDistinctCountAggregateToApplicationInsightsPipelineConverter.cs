using System;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    internal class NaiveDistinctCountAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricSeriesConfigurationForNaiveDistinctCount.Constants.AggregateKindMoniker; } }

        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = aggregate.GetDataValue<int>(MetricSeriesConfigurationForNaiveDistinctCount.Constants.AggregateKindDataKeys.TotalCount, 0);
            telemetryItem.Sum = (double) aggregate.GetDataValue<int>(MetricSeriesConfigurationForNaiveDistinctCount.Constants.AggregateKindDataKeys.DistinctCount, 0);
            telemetryItem.Min = null;
            telemetryItem.Max = null;
            telemetryItem.StandardDeviation = null;
        }
    }
}
