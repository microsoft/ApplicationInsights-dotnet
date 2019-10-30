namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;
    using static System.FormattableString;

    /// <summary>
    /// An instance of this class is contained within the <see cref="AutocollectedMetricsExtractor"/> telemetry processor.
    /// It extracts auto-collected, pre-aggregated (aka. "standard") metrics from DependencyTelemetry objects which represent
    /// invocations of remote dependencies performed by the monitored service.
    /// </summary>
    internal class DependencyMetricsExtractor : ISpecificAutocollectedMetricsExtractor
    {
        /// <summary>
        /// The default value for the <see cref="MaxDependencyTypesToDiscover"/> property.        
        /// </summary>
        public const int MaxDependencyTypesToDiscoverDefault = 15;

        /// <summary>
        /// The default value for the <see cref="MaxCloudRoleInstanceValuesToDiscover"/> property.
        /// </summary>
        public const int MaxTargetValuesToDiscoverDefault = 50;

        /// <summary>
        /// The default value for the <see cref="MaxCloudRoleInstanceValuesToDiscover"/> property.
        /// </summary>
        public const int MaxCloudRoleInstanceValuesToDiscoverDefault = 2;

        /// <summary>
        /// The default value for the <see cref="MaxCloudRoleInstanceValuesToDiscover"/> property.
        /// </summary>
        public const int MaxCloudRoleNameValuesToDiscoverDefault = 2;

        /// <summary>
        /// The <c>TelemetryClient</c> to be used for creating and sending the metrics by this extractor.
        /// </summary>
        private TelemetryClient metricTelemetryClient = null;

        /// <summary>
        /// Extracted metric.
        /// </summary>
        private Metric dependencyCallDurationMetric = null;

        private List<AggregateDimension> aggregateDimensions = new List<AggregateDimension>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyMetricsExtractor" /> class.
        /// </summary>
        public DependencyMetricsExtractor()
        {
            aggregateDimensions.Add(GetIdDimension());
            aggregateDimensions.Add(GetSuccessDimension());
            aggregateDimensions.Add(GetSyntheticDimension());
            aggregateDimensions.Add(GetTypeDimension());
            aggregateDimensions.Add(GetTargetDimension());
            aggregateDimensions.Add(GetRoleInstanceDimension());
            aggregateDimensions.Add(GetRoleNameDimension());
        }

        /// <summary>
        /// Gets the name of this extractor.
        /// All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// The respective logic is in the <see cref="AutocollectedMetricsExtractor"/>-class.
        /// </summary>
        public string ExtractorName { get; } = "Dependencies";

        /// <summary>
        /// Gets the version of this extractor.
        /// All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// The respective logic is in the <see cref="AutocollectedMetricsExtractor"/>-class.
        /// </summary>
        public string ExtractorVersion { get; } = "1.1";

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered dependency types.
        /// </summary>
        public int MaxDependencyTypesToDiscover { get; set; } = MaxDependencyTypesToDiscoverDefault;

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered Dependency Target values.
        /// </summary>
        public int MaxDependencyTargetValuesToDiscover { get; set; } = MaxTargetValuesToDiscoverDefault;
        
        /// <summary>
        /// Gets or sets the maximum number of auto-discovered Cloud RoleInstance values.
        /// </summary>
        public int MaxCloudRoleInstanceValuesToDiscover { get; set; } = MaxCloudRoleInstanceValuesToDiscoverDefault;

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered Cloud RoleName values.
        /// </summary>
        public int MaxCloudRoleNameValuesToDiscover { get; set; } = MaxCloudRoleNameValuesToDiscoverDefault;

        /// <summary>
        /// Pre-initialize this extractor.
        /// </summary>
        /// <param name="metricTelemetryClient">The <c>TelemetryClient</c> to be used for sending extracted metrics.</param>
        public void InitializeExtractor(TelemetryClient metricTelemetryClient)
        {
            this.metricTelemetryClient = metricTelemetryClient;
            
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
                        MetricTerms.Autocollection.Metric.DependencyCallDuration.Name,
                        this.aggregateDimensions[0].Name,
                        this.aggregateDimensions[1].Name,
                        this.aggregateDimensions[2].Name,
                        this.aggregateDimensions[3].Name,
                        this.aggregateDimensions[4].Name,
                        this.aggregateDimensions[5].Name,
                        this.aggregateDimensions[6].Name);

            this.dependencyCallDurationMetric = this.metricTelemetryClient.GetMetric(
                                                        metricIdentifier: metricIdentifier,
                                                        metricConfiguration: config,
                                                        aggregationScope: MetricAggregationScope.TelemetryClient);
        }

        /// <summary>
        /// Extracts appropriate data points for auto-collected, pre-aggregated metrics from a single <c>DependencyTelemetry</c> item.
        /// </summary>
        /// <param name="fromItem">The telemetry item from which to extract the metric data points.</param>
        /// <param name="isItemProcessed">Whether of not the specified item was processed (aka not ignored) by this extractor.</param>
        public void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
        {
            //// If this item is not a DependencyTelemetry, we will not process it:
            DependencyTelemetry dependencyCall = fromItem as DependencyTelemetry;
            if (dependencyCall == null)
            {
                isItemProcessed = false; 
                return;
            }

            //// If there is no Metric, then this extractor has not been properly initialized yet:
            if (this.dependencyCallDurationMetric == null)
            {
                //// This should be caught and properly logged by the base class:
                throw new InvalidOperationException(Invariant($"Cannot execute {nameof(this.ExtractMetrics)}.")
                                                  + Invariant($" There is no {nameof(this.dependencyCallDurationMetric)}.")
                                                  + Invariant($" Either this metrics extractor has not been initialized, or it has been disposed."));
            }

            int i = 0;
            string[] dimValues = new string[this.aggregateDimensions.Count];
            foreach (var dim in this.aggregateDimensions)
            {
                dimValues[i] = dim.GetDimensionValue(dependencyCall);
                if (string.IsNullOrEmpty(dimValues[i]))
                {
                    dimValues[i] = dim.DefaultValue;
                }

                i++;
            }

            this.dependencyCallDurationMetric.TrackValue(dependencyCall.Duration.TotalMilliseconds,
                dimValues[0], dimValues[1], dimValues[2], dimValues[3], dimValues[4], dimValues[5], dimValues[6]);
            isItemProcessed = true;
        }

        private AggregateDimension GetIdDimension()
        {
            var idDimension = new AggregateDimension();
            idDimension.DefaultValue = MetricTerms.Autocollection.Metric.DependencyCallDuration.Id;
            idDimension.MaxValues = 1;
            idDimension.Name = MetricDimensionNames.TelemetryContext.Property(MetricTerms.Autocollection.MetricId.Moniker.Key);
            idDimension.GetDimensionValue = (item) =>
            {
                return MetricTerms.Autocollection.Metric.DependencyCallDuration.Id;
            };

            return idDimension;
        }

        private AggregateDimension GetSuccessDimension()
        {
            var successDimension = new AggregateDimension();
            successDimension.DefaultValue = bool.TrueString;
            successDimension.MaxValues = 2;
            successDimension.Name = MetricTerms.Autocollection.DependencyCall.PropertyNames.Success;
            successDimension.GetDimensionValue = (item) =>
            {
                var dep = item as DependencyTelemetry;
                bool dependencyFailed = (dep.Success != null) && (dep.Success == false);
                string dependencySuccessString = dependencyFailed ? bool.FalseString : bool.TrueString;
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
                var dep = item as DependencyTelemetry;
                bool isSynthetic = dep.Context.Operation.SyntheticSource != null;
                string isSyntheticString = isSynthetic ? bool.TrueString : bool.FalseString;
                return isSyntheticString;
            };

            return syntheticDimension;
        }

        private AggregateDimension GetTypeDimension()
        {
            var typeDimension = new AggregateDimension();
            typeDimension.DefaultValue = MetricTerms.Autocollection.Common.PropertyValues.Unknown;
            typeDimension.MaxValues = MaxDependencyTypesToDiscover;
            typeDimension.Name = MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName;
            typeDimension.GetDimensionValue = (item) =>
            {
                var dep = item as DependencyTelemetry;
                return dep.Type;
            };

            return typeDimension;
        }

        private AggregateDimension GetTargetDimension()
        {
            var targetDimension = new AggregateDimension();
            targetDimension.DefaultValue = MetricTerms.Autocollection.Common.PropertyValues.Unknown;
            targetDimension.MaxValues = MaxDependencyTypesToDiscover;
            targetDimension.Name = MetricTerms.Autocollection.DependencyCall.PropertyNames.Target;
            targetDimension.GetDimensionValue = (item) =>
            {
                var dep = item as DependencyTelemetry;
                return dep.Target;
            };

            return targetDimension;
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
