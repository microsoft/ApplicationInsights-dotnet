using System;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    internal class MeasurementAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricConfigurations.Common.AggregateKinds().Measurement().Moniker; } }

        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = aggregate.GetAggregateData<int>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Count, 0);
            telemetryItem.Sum = aggregate.GetAggregateData<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Sum, 0.0);
            telemetryItem.Min = aggregate.GetAggregateData<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Min, 0.0);
            telemetryItem.Max = aggregate.GetAggregateData<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Max, 0.0);
            telemetryItem.StandardDeviation = aggregate.GetAggregateData<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.StdDev, 0.0);
        }
    }
}
