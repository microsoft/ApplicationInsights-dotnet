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

        private static void ValidateAggregate(MetricAggregate metricAggregate)
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

        private static ApplicationInsights.DataContracts.MetricTelemetry ConvertAggregateToTelemetry(MetricAggregate aggregate)
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

            // Populate TelemetryContext:
            IEnumerable<KeyValuePair<string, string>> nonContextDimensions;
            PopulateTelemetryContext(aggregate.Dimensions, telemetryItem.Context, out nonContextDimensions);

            // Set dimensions. We do this after the context, becasue dimensions take precedence (i.e. we potentially overwrite):
            if (nonContextDimensions != null)
            {
                foreach (KeyValuePair<string, string> nonContextDimension in nonContextDimensions)
                {
                    telemetryItem.Properties[nonContextDimension.Key] = nonContextDimension.Value;
                }
            }

            // Set SDK version moniker:

            Util.StampSdkVersionToContext(telemetryItem);

            return telemetryItem;
        }

        private static void PopulateTelemetryContext(
                                                IDictionary<string, string> dimensions,
                                                Microsoft.ApplicationInsights.DataContracts.TelemetryContext telemetryContext,
                                                out IEnumerable<KeyValuePair<string, string>> nonContextDimensions)
        {
            if (dimensions == null)
            {
                nonContextDimensions = null;
                return;
            }

            List<KeyValuePair<string, string>> nonContextDimensionList = null;

            foreach (KeyValuePair<string, string> dimension in dimensions)
            {
                if (String.IsNullOrWhiteSpace(dimension.Key) || dimension.Value == null)
                {
                    continue;
                }

                switch (dimension.Key)
                {
                    case MetricDimensionNames.TelemetryContext.InstrumentationKey:
                        telemetryContext.InstrumentationKey = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Cloud.RoleInstance:
                        telemetryContext.Cloud.RoleInstance = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Cloud.RoleName:
                        telemetryContext.Cloud.RoleName = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Component.Version:
                        telemetryContext.Component.Version = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Device.Id:
                        telemetryContext.Device.Id = dimension.Value;
                        break;

                    #pragma warning disable CS0618  // Type or member is obsolete
                    case MetricDimensionNames.TelemetryContext.Device.Language:
                        telemetryContext.Device.Language = dimension.Value;
                        break;
                    #pragma warning restore CS0618  // Type or member is obsolete

                    case MetricDimensionNames.TelemetryContext.Device.Model:
                        telemetryContext.Device.Model = dimension.Value;
                        break;

                    #pragma warning disable CS0618  // Type or member is obsolete
                    case MetricDimensionNames.TelemetryContext.Device.NetworkType:
                        telemetryContext.Device.NetworkType = dimension.Value;
                        break;
                    #pragma warning restore CS0618  // Type or member is obsolete

                    case MetricDimensionNames.TelemetryContext.Device.OemName:
                        telemetryContext.Device.OemName = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Device.OperatingSystem:
                        telemetryContext.Device.OperatingSystem = dimension.Value;
                        break;

                    #pragma warning disable CS0618  // Type or member is obsolete
                    case MetricDimensionNames.TelemetryContext.Device.ScreenResolution:
                        telemetryContext.Device.ScreenResolution = dimension.Value;
                        break;
                    #pragma warning restore CS0618  // Type or member is obsolete

                    case MetricDimensionNames.TelemetryContext.Device.Type:
                        telemetryContext.Device.Type = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Location.Ip:
                        telemetryContext.Location.Ip = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Operation.CorrelationVector:
                        telemetryContext.Operation.CorrelationVector = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Operation.Id:
                        telemetryContext.Operation.Id = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Operation.Name:
                        telemetryContext.Operation.Name = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Operation.ParentId:
                        telemetryContext.Operation.ParentId = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Operation.SyntheticSource:
                        telemetryContext.Operation.SyntheticSource = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Session.Id:
                        telemetryContext.Session.Id = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.Session.IsFirst:
                        try
                        {
                            telemetryContext.Session.IsFirst = Convert.ToBoolean(dimension.Value);
                        }
                        catch
                        {
                            try
                            {
                                int val = Convert.ToInt32(dimension.Value);
                                if (val == 1)
                                {
                                    telemetryContext.Session.IsFirst = true;
                                }
                                else if (val == 0)
                                {
                                    telemetryContext.Session.IsFirst = false;
                                }
                            }
                            catch { }
                        }
                        break;

                    case MetricDimensionNames.TelemetryContext.User.AccountId:
                        telemetryContext.User.AccountId = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.User.AuthenticatedUserId:
                        telemetryContext.User.AuthenticatedUserId = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.User.Id:
                        telemetryContext.User.Id = dimension.Value;
                        break;

                    case MetricDimensionNames.TelemetryContext.User.UserAgent:
                        telemetryContext.User.UserAgent = dimension.Value;
                        break;

                    default:
                        string dimensionName;
                        if (MetricDimensionNames.TelemetryContext.IsProperty(dimension.Key, out dimensionName))
                        {
                            telemetryContext.Properties[dimensionName] = dimension.Value;
                        }
                        else
                        {
                            if (nonContextDimensionList == null)
                            {
                                nonContextDimensionList = new List<KeyValuePair<string, string>>(dimensions.Count);
                            }

                            nonContextDimensionList.Add(dimension);
                        }
                        break;
                }
            }

            nonContextDimensions = nonContextDimensionList;
        }
    }
}
