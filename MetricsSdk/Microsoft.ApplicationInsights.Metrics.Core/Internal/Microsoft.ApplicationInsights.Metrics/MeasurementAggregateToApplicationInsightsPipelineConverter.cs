using System;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    internal class MeasurementAggregateToApplicationInsightsPipelineConverter : MetricAggregateToApplicationInsightsPipelineConverterBase
    {
        public override string AggregationKindMoniker { get { return MetricConfigurations.Common.AggregateKinds().Measurement().Moniker; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062", Justification = "telemetryItem and aggregate are validated by base")]
        protected override void PopulateDataValues(MetricTelemetry telemetryItem, MetricAggregate aggregate)
        {
            telemetryItem.Count = aggregate.GetDataValue<int>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Count, 0);
            telemetryItem.Sum = aggregate.GetDataValue<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Sum, 0.0);
            telemetryItem.Min = aggregate.GetDataValue<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Min, 0.0);
            telemetryItem.Max = aggregate.GetDataValue<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.Max, 0.0);
            telemetryItem.StandardDeviation = aggregate.GetDataValue<double>(MetricConfigurations.Common.AggregateKinds().Measurement().DataKeys.StdDev, 0.0);
        }
    }
}
