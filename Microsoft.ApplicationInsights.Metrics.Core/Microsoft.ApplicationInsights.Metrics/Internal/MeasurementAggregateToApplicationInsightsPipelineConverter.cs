using System;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    internal class MeasurementAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricAggregateKinds.SimpleStatistics.Moniker; } }

        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = aggregate.GetAggregateData<int>(MetricAggregateKinds.SimpleStatistics.DataKeys.Count, 0);
            telemetryItem.Sum = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Sum, 0.0);
            telemetryItem.Min = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Min, 0.0);
            telemetryItem.Max = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Max, 0.0);
            telemetryItem.StandardDeviation = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev, 0.0);
        }
    }
}
