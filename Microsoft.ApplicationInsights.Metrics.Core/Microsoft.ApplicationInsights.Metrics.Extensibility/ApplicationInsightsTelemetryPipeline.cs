using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// </summary>
    public class ApplicationInsightsTelemetryPipeline : IMetricTelemetryPipeline
    {
        /// <summary />
        public const string AggregationIntervalMonikerPropertyKey = "_MS.AggregationIntervalMs";

        private readonly ApplicationInsights.TelemetryClient _trackingClient;
        private readonly Task _completedTask = Task.FromResult(true);

        /// <summary>
        /// </summary>
        /// <param name="telemetryPipeline"></param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.Extensibility.TelemetryConfiguration telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            _trackingClient = new ApplicationInsights.TelemetryClient(telemetryPipeline);
        }

        /// <summary>
        /// </summary>
        /// <param name="telemetryClient"></param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.TelemetryClient telemetryClient)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            _trackingClient = telemetryClient;
        }

        /// <summary>
        /// </summary>
        /// <param name="metricAggregate"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task TrackAsync(MetricAggregate metricAggregate, CancellationToken cancelToken)
        {
            ValidateAggregate(metricAggregate);
            cancelToken.ThrowIfCancellationRequested();

            ApplicationInsights.DataContracts.MetricTelemetry telemetryItem = ConvertAggregateToTelemetry(metricAggregate);
            _trackingClient.Track(telemetryItem);

            return _completedTask;
        }

        /// <summary>
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task FlushAsync(CancellationToken cancelToken)
        {
            cancelToken.ThrowIfCancellationRequested();
            _trackingClient.Flush();
            return _completedTask;
        }

        private void ValidateAggregate(MetricAggregate metricAggregate)
        {
            Util.ValidateNotNull(metricAggregate, nameof(metricAggregate));
            Util.ValidateNotNull(metricAggregate.AggregationKindMoniker, nameof(metricAggregate.AggregationKindMoniker));

            if (! metricAggregate.AggregationKindMoniker.Equals(MetricAggregateKinds.SimpleStatistics.Moniker, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"This {nameof(IMetricTelemetryPipeline)} implementation ({nameof(ApplicationInsightsTelemetryPipeline)})"
                                          + $" can only accept {nameof(MetricAggregate)}-objects with the AggregationKindMoniker"
                                          + $" '{MetricAggregateKinds.SimpleStatistics.Moniker}'."
                                          + $" However, the specified aggregate has a different moniker: '{metricAggregate.AggregationKindMoniker}'. ");
            }
        }

        private ApplicationInsights.DataContracts.MetricTelemetry ConvertAggregateToTelemetry(MetricAggregate aggregate)
        {
            ApplicationInsights.DataContracts.MetricTelemetry telemetryItem = new ApplicationInsights.DataContracts.MetricTelemetry();

            // Set data values:

            telemetryItem.Name = aggregate.MetricId;
            telemetryItem.Count = aggregate.GetAggregateData<int>(MetricAggregateKinds.SimpleStatistics.DataKeys.Count, 0);
            telemetryItem.Sum = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Sum, 0.0);
            telemetryItem.Min = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Min, 0.0);
            telemetryItem.Max = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Max, 0.0);
            telemetryItem.StandardDeviation = aggregate.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev, 0.0);

            // Set timing values:

            IDictionary<string, string> props = telemetryItem.Properties;
            if (props != null)
            {
                long periodMillis = (long) aggregate.AggregationPeriodDuration.TotalMilliseconds;
                props.Add(AggregationIntervalMonikerPropertyKey, periodMillis.ToString(CultureInfo.InvariantCulture));
            }

            telemetryItem.Timestamp = aggregate.AggregationPeriodStart;

            // Copy telemetry context:

            ApplicationInsights.DataContracts.TelemetryContext telemetryContext = aggregate.AdditionalDataContext as ApplicationInsights.DataContracts.TelemetryContext;
            if (telemetryContext != null)
            {
                Util.CopyTelemetryContext(telemetryContext, telemetryItem.Context);
            }

            // Set dimensions:

            props = telemetryItem.Properties;
            if (props != null && aggregate.Dimensions != null)
            {
                foreach(KeyValuePair<string, string> dimNameValue in aggregate.Dimensions)
                {
                    if (false == String.IsNullOrWhiteSpace(dimNameValue.Key) && dimNameValue.Value != null)
                    {
                        props[dimNameValue.Key] = dimNameValue.Value;
                    }
                }
            }

            // Set SDK version moniker:

            Util.StampSdkVersionToContext(telemetryItem);

            return telemetryItem;
        }
    }
}
