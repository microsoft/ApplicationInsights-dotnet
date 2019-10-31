namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Metrics;
    using static System.FormattableString;

    /// <summary>
    /// An instance of this class is contained within the <see cref="AutocollectedMetricsExtractor"/> telemetry processor.
    /// It extracts auto-collected, pre-aggregated (aka. "standard") metrics from RequestTelemetry objects which represent
    /// invocations of the monitored service.
    /// </summary>
    internal class RequestMetricsExtractor : ISpecificAutocollectedMetricsExtractor
    {
        /// <summary>
        /// The default value for the <see cref="MaxResponseCodeToDiscover"/> property.        
        /// </summary>
        public const int MaxResponseCodeToDiscoverDefault = 10;

        /// <summary>
        /// The default value for the <see cref="MaxCloudRoleInstanceValuesToDiscover"/> property.
        /// </summary>
        public const int MaxCloudRoleInstanceValuesToDiscoverDefault = 2;

        /// <summary>
        /// The default value for the <see cref="MaxCloudRoleInstanceValuesToDiscover"/> property.
        /// </summary>
        public const int MaxCloudRoleNameValuesToDiscoverDefault = 2;

        /// <summary>
        /// Extracted metric.
        /// </summary>
        private Metric requestDurationMetric = null;

        private List<AggregateDimension> aggregateDimensions = new List<AggregateDimension>();

        public RequestMetricsExtractor()
        {
            aggregateDimensions.Add(GetIdDimension());
            aggregateDimensions.Add(GetSuccessDimension());
            aggregateDimensions.Add(GetSyntheticDimension());
            aggregateDimensions.Add(GetResponseCodeDimension());
            aggregateDimensions.Add(GetRoleInstanceDimension());
            aggregateDimensions.Add(GetRoleNameDimension());
        }

        public string ExtractorName { get; } = "Requests";

        public string ExtractorVersion { get; } = "1.1";

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered Request response code.
        /// </summary>
        public int MaxResponseCodeToDiscover { get; set; } = MaxResponseCodeToDiscoverDefault;

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered Cloud RoleInstance values.
        /// </summary>
        public int MaxCloudRoleInstanceValuesToDiscover { get; set; } = MaxCloudRoleInstanceValuesToDiscoverDefault;

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered Cloud RoleName values.
        /// </summary>
        public int MaxCloudRoleNameValuesToDiscover { get; set; } = MaxCloudRoleNameValuesToDiscoverDefault;

        public void InitializeExtractor(TelemetryClient metricTelemetryClient)
        {
            int seriesCountLimit = 1;
            int[] valuesPerDimensionLimit = new int[this.aggregateDimensions.Count];
            int i = 0;

            foreach (var dim in this.aggregateDimensions)
            {
                int dimLimit = 1;
                if (dim.MaxValues == 0)
                {
                    dimLimit = 1;
                }
                else
                {
                    dimLimit = dim.MaxValues;
                }

                seriesCountLimit = seriesCountLimit * (1 + dimLimit);
                valuesPerDimensionLimit[i++] = dimLimit;
            }

            MetricConfiguration config = new MetricConfigurationForMeasurement(
                                                            seriesCountLimit,
                                                            valuesPerDimensionLimit,
                                                            new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));
            config.ApplyDimensionCapping = true;
            config.DimensionCappedString = MetricTerms.Autocollection.Common.PropertyValues.DimensionCapFallbackValue;

            MetricIdentifier metricIdentifier = new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace,
                        MetricTerms.Autocollection.Metric.RequestDuration.Name,
                        this.aggregateDimensions[0].Name,
                        this.aggregateDimensions[1].Name,
                        this.aggregateDimensions[2].Name,
                        this.aggregateDimensions[3].Name,
                        this.aggregateDimensions[4].Name,
                        this.aggregateDimensions[5].Name);

            this.requestDurationMetric = metricTelemetryClient.GetMetric(
                                                        metricIdentifier: metricIdentifier,
                                                        metricConfiguration: config,
                                                        aggregationScope: MetricAggregationScope.TelemetryClient);
        }

        public void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
        {
            RequestTelemetry request = fromItem as RequestTelemetry;
            if (request == null)
            {
                isItemProcessed = false;
                return;
            }

            //// If there is no Metric, then this extractor has not been properly initialized yet:
            if (this.requestDurationMetric == null)
            {
                //// This should be caught and properly logged by the base class:
                throw new InvalidOperationException(Invariant($"Cannot execute {nameof(this.ExtractMetrics)}.")
                                                  + Invariant($" There is no {nameof(this.requestDurationMetric)}.")
                                                  + Invariant($" Either this metrics extractor has not been initialized, or it has been disposed."));
            }

            int i = 0;
            string[] dimValues = new string[this.aggregateDimensions.Count];
            foreach (var dim in this.aggregateDimensions)
            {
                dimValues[i] = dim.GetDimensionValue(request);
                if (string.IsNullOrEmpty(dimValues[i]))
                {
                    dimValues[i] = dim.DefaultValue;
                }

                i++;
            }

            this.requestDurationMetric.TrackValue(request.Duration.TotalMilliseconds,
                dimValues[0], dimValues[1], dimValues[2], dimValues[3], dimValues[4], dimValues[5]);
            isItemProcessed = true;
        }

        private AggregateDimension GetIdDimension()
        {
            var idDimension = new AggregateDimension();
            idDimension.DefaultValue = MetricTerms.Autocollection.Metric.RequestDuration.Id;
            idDimension.MaxValues = 1;
            idDimension.Name = MetricDimensionNames.TelemetryContext.Property(MetricTerms.Autocollection.MetricId.Moniker.Key);
            idDimension.GetDimensionValue = (item) =>
            {
                return MetricTerms.Autocollection.Metric.RequestDuration.Id;
            };

            return idDimension;
        }

        private AggregateDimension GetSuccessDimension()
        {
            var successDimension = new AggregateDimension();
            successDimension.DefaultValue = bool.TrueString;
            successDimension.MaxValues = 2;
            successDimension.Name = MetricTerms.Autocollection.Request.PropertyNames.Success;
            successDimension.GetDimensionValue = (item) =>
            {
                var request = item as RequestTelemetry;

                bool isFailed = request.Success.HasValue
                                ? (request.Success.Value == false)
                                : false;
                string dependencySuccessString = isFailed ? bool.FalseString : bool.TrueString;
                return dependencySuccessString;
            };

            return successDimension;
        }

        private AggregateDimension GetSyntheticDimension()
        {
            var syntheticDimension = new AggregateDimension();
            syntheticDimension.DefaultValue = bool.TrueString;
            syntheticDimension.MaxValues = 2;
            syntheticDimension.Name = MetricTerms.Autocollection.Common.PropertyNames.Synthetic;
            syntheticDimension.GetDimensionValue = (item) =>
            {
                var request = item as RequestTelemetry;
                bool isSynthetic = request.Context.Operation.SyntheticSource != null;
                string isSyntheticString = isSynthetic ? bool.TrueString : bool.FalseString;
                return isSyntheticString;
            };

            return syntheticDimension;
        }

        private AggregateDimension GetResponseCodeDimension()
        {
            var typeDimension = new AggregateDimension();
            typeDimension.DefaultValue = MetricTerms.Autocollection.Common.PropertyValues.Unknown;
            typeDimension.MaxValues = MaxResponseCodeToDiscover;
            typeDimension.Name = MetricTerms.Autocollection.Request.PropertyNames.ResultCode;
            typeDimension.GetDimensionValue = (item) =>
            {
                var req = item as RequestTelemetry;
                return req.ResponseCode;
            };

            return typeDimension;
        }

        private AggregateDimension GetRoleInstanceDimension()
        {
            var roleInstanceDimension = new AggregateDimension();
            roleInstanceDimension.DefaultValue = MetricTerms.Autocollection.Common.PropertyValues.Unknown;
            roleInstanceDimension.MaxValues = MaxCloudRoleInstanceValuesToDiscover;
            roleInstanceDimension.Name = MetricTerms.Autocollection.Common.PropertyNames.CloudRoleInstance;
            roleInstanceDimension.GetDimensionValue = (item) =>
            {
                return item.Context.Cloud.RoleInstance;
            };

            return roleInstanceDimension;
        }

        private AggregateDimension GetRoleNameDimension()
        {
            var roleNameDimension = new AggregateDimension();
            roleNameDimension.DefaultValue = MetricTerms.Autocollection.Common.PropertyValues.Unknown;
            roleNameDimension.MaxValues = MaxCloudRoleInstanceValuesToDiscover;
            roleNameDimension.Name = MetricTerms.Autocollection.Common.PropertyNames.CloudRoleName;
            roleNameDimension.GetDimensionValue = (item) =>
            {
                return item.Context.Cloud.RoleName;
            };

            return roleNameDimension;
        }
    }
}