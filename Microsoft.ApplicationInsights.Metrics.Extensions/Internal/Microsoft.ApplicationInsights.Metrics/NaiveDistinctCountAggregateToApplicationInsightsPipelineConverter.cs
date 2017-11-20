using System;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    internal class NaiveDistinctCountAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricAggregateKinds.NaiveDistinctCount.Moniker; } }

        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = aggregate.GetAggregateData<int>(MetricAggregateKinds.NaiveDistinctCount.DataKeys.TotalCount, 0);
            telemetryItem.Sum = (double) aggregate.GetAggregateData<int>(MetricAggregateKinds.NaiveDistinctCount.DataKeys.DistinctCount, 0);
            telemetryItem.Min = null;
            telemetryItem.Max = null;
            telemetryItem.StandardDeviation = null;
        }
    }
}
