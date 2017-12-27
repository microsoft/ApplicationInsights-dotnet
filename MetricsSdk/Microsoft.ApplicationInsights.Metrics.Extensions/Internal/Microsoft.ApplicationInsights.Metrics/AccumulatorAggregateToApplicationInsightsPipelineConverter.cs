using System;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    internal class AccumulatorAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricConfigurations.Common.AggregateKinds().Accumulator().Moniker; } }

        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = 1;
            telemetryItem.Sum = aggregate.GetDataValue<double>(MetricConfigurations.Common.AggregateKinds().Accumulator().DataKeys.Sum, 0.0);
            telemetryItem.Min = aggregate.GetDataValue<double>(MetricConfigurations.Common.AggregateKinds().Accumulator().DataKeys.Min, 0.0);
            telemetryItem.Max = aggregate.GetDataValue<double>(MetricConfigurations.Common.AggregateKinds().Accumulator().DataKeys.Max, 0.0);
            telemetryItem.StandardDeviation = null;
        }
    }
}
