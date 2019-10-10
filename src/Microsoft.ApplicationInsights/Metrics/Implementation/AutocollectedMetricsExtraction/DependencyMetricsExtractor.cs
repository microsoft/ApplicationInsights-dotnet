namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;
    using static System.FormattableString;

    /// <summary>
    /// An instance of this class is contained within the <see cref="AutocollectedMetricsExtractor"/> telemetry processor.
    /// It extracts auto-collected, pre-aggregated (aka. "standard") metrics from DependencyTelemetry objects which represent
    /// invocations of remote dependencies performed by the monitored service.
    /// </summary>
    /// <remarks>
    /// Auto-Discovering Dependency Types: **
    /// Dependency call duration is collected as a metric for failed and successful calls separately, and grouped by dependency type.
    /// It is essential to control the number of data series produced by this extractor: It must be a small, bounded value.
    /// However, this extractor needs to support different modules that collect information about different kinds of dependencies.
    /// To meet these constraints, the extractor will auto-discover dependency types, but it will not auto-discover more types than
    /// the number controlled by the <see cref="DependencyMetricsExtractor.MaxDependencyTypesToDiscover" /> property.
    /// The first <c>MaxDependencyTypesToDiscover</c> dependency types encountered will be tracked separately.
    /// Additional types will all be grouped as "<c>Other</c>".
    /// Customers should set this value to a value such that "<c>Other</c>" does not actually occur in practice.
    /// As a guidance, a good value will be approximately in range 1 - 20. If significantly more types are expected, it should be
    /// examined whether the dependency type field is used appropriately.
    /// If <c>MaxDependencyTypesToDiscover</c> is set to <c>0</c>, dependency calls will not be grouped by type.
    /// </remarks>
    internal class DependencyMetricsExtractor : ISpecificAutocollectedMetricsExtractor
    {
        /// <summary>
        /// The default value for the <see cref="MaxDependencyTypesToDiscover"/> property if it is not set to a different value.
        /// See also the remarks about the <see cref="DependencyMetricsExtractor"/> class for additional info about the use
        /// the of <c>MaxDependencyTypesToDiscover</c>-property.
        /// </summary>
        public const int MaxDependenctTypesToDiscoverDefault = 15;

        /// <summary>
        /// The default value for the <see cref="MaxCloudRoleInstanceValuesToDiscover"/> property if it is not set to a different value.
        /// See also the remarks about the <see cref="DependencyMetricsExtractor"/> class for additional info about the use
        /// the of <c>MaxDependencyTypesToDiscover</c>-property.
        /// </summary>
        public const int MaxCloudRoleInstanceValuesToDiscoverDefault = 5;

        /// <summary>
        /// The <c>TelemetryClient</c> to be used for creating and sending the metrics by this extractor.
        /// </summary>
        private TelemetryClient metricTelemetryClient = null;

        /// <summary>
        /// Extracted metric.
        /// </summary>
        private Metric dependencyCallDurationMetric = null;

        /// <summary>
        /// Maximum number of auto-discovered dependency types.
        /// </summary>
        private int maxDependencyTypesToDiscover = MaxDependenctTypesToDiscoverDefault;

        /// <summary>
        /// Maximum number of auto-discovered cloud role instance values.
        /// </summary>
        private int maxCloudRoleInstanceValuesToDiscover = MaxCloudRoleInstanceValuesToDiscoverDefault;

        private List<AggregateDimension> aggregateDimensions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyMetricsExtractor" /> class.
        /// </summary>
        public DependencyMetricsExtractor()
        {
            aggregateDimensions = new List<AggregateDimension>();


            var idDimension = new AggregateDimension();
            idDimension.DefaultValue = MetricTerms.Autocollection.Metric.DependencyCallDuration.Id;
            idDimension.MaxValues = 1;
            idDimension.Name = MetricDimensionNames.TelemetryContext.Property(MetricTerms.Autocollection.MetricId.Moniker.Key);
            idDimension.GetFieldValue = (item) =>
            {
                return MetricTerms.Autocollection.Metric.DependencyCallDuration.Id;
            };

            aggregateDimensions.Add(idDimension);

            var successDimension = new AggregateDimension();
            successDimension.DefaultValue = bool.TrueString;
            successDimension.MaxValues = null;
            successDimension.Name = MetricTerms.Autocollection.DependencyCall.PropertyNames.Success;
            successDimension.GetFieldValue = (item) => 
            {
                var dep = item as DependencyTelemetry;
                bool dependencyFailed = (dep.Success != null) && (dep.Success == false);
                string dependencySuccessString = dependencyFailed ? bool.FalseString : bool.TrueString;
                return dependencySuccessString;
            };

            aggregateDimensions.Add(successDimension);

            var typeDimension = new AggregateDimension();
            typeDimension.DefaultValue = MetricTerms.Autocollection.DependencyCall.TypeNames.Unknown;
            typeDimension.MaxValues = MaxDependencyTypesToDiscover;
            typeDimension.Name = MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName;
            typeDimension.GetFieldValue = (item) =>
            {
                var dep = item as DependencyTelemetry;
                return dep.Type;
            };

            aggregateDimensions.Add(typeDimension);

            var roleInstanceDimension = new AggregateDimension();
            roleInstanceDimension.DefaultValue = MetricTerms.Autocollection.Common.CloudRoleInstanceNames.Unknown;
            roleInstanceDimension.MaxValues = MaxCloudRoleInstanceValuesToDiscover;
            roleInstanceDimension.Name = MetricTerms.Autocollection.Common.PropertyNames.CloudRoleInstance;
            roleInstanceDimension.GetFieldValue = (item) =>
            {
                return item.Context.Cloud.RoleInstance;
            };

            aggregateDimensions.Add(roleInstanceDimension);
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
        /// See also the remarks about the <see cref="DependencyMetricsExtractor"/> class for additional info about the use the of this property.
        /// </summary>
        public int MaxDependencyTypesToDiscover
        {
            get
            {
                return this.maxDependencyTypesToDiscover;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxDependencyTypesToDiscover value may not be negative.");
                }

                this.metricTelemetryClient?.Flush();
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered Cloud RoleInstance values.
        /// See also the remarks about the <see cref="DependencyMetricsExtractor"/> class for additional info about the use the of this property.
        /// </summary>
        public int MaxCloudRoleInstanceValuesToDiscover
        {
            get
            {
                return this.maxCloudRoleInstanceValuesToDiscover;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxCloudRoleInstanceValuesToDiscover value may not be negative.");
                }

                this.metricTelemetryClient?.Flush();
            }
        }

        /// <summary>
        /// Pre-initialize this extractor.
        /// </summary>
        /// <param name="metricTelemetryClient">The <c>TelemetryClient</c> to be used for sending extracted metrics.</param>
        public void InitializeExtractor(TelemetryClient metricTelemetryClient)
        {
            this.metricTelemetryClient = metricTelemetryClient;

            int seriesCountLimit = 1;
            int[] valuesPerDimension = new int[this.aggregateDimensions.Count];

            int i = 0;
            foreach (var dim in this.aggregateDimensions)
            {
                int dimLimit = 1;
                if(dim.MaxValues == null)
                {  // bool
                    dimLimit = 2;
                }
                else if (dim.MaxValues == 0)
                {
                    dimLimit = 1;
                }
                else
                {
                    dimLimit = dim.MaxValues.Value;
                }

                seriesCountLimit = seriesCountLimit * dimLimit;
                valuesPerDimension[i++] = dimLimit;
            }

            MetricConfiguration config = new MetricConfigurationForMeasurement(
                                                            (seriesCountLimit) + 1,
                                                            valuesPerDimension,
                                                            new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            switch (this.aggregateDimensions.Count)
            {
                case 1:
                    this.dependencyCallDurationMetric = this.metricTelemetryClient.GetMetric(
                                                        metricId: MetricTerms.Autocollection.Metric.DependencyCallDuration.Name,
                                                        dimension1Name: this.aggregateDimensions[0].Name,
                                                        metricConfiguration: config,
                                                        aggregationScope: MetricAggregationScope.TelemetryClient);
                    break;
                case 2:
                    this.dependencyCallDurationMetric = this.metricTelemetryClient.GetMetric(
                                                        metricId: MetricTerms.Autocollection.Metric.DependencyCallDuration.Name,
                                                        dimension1Name: this.aggregateDimensions[0].Name,
                                                        dimension2Name: this.aggregateDimensions[1].Name,
                                                        metricConfiguration: config,
                                                        aggregationScope: MetricAggregationScope.TelemetryClient);
                    break;
                case 3:
                    this.dependencyCallDurationMetric = this.metricTelemetryClient.GetMetric(
                                                        metricId: MetricTerms.Autocollection.Metric.DependencyCallDuration.Name,
                                                        dimension1Name: this.aggregateDimensions[0].Name,
                                                        dimension2Name: this.aggregateDimensions[1].Name,
                                                        dimension3Name: this.aggregateDimensions[2].Name,
                                                        metricConfiguration: config,
                                                        aggregationScope: MetricAggregationScope.TelemetryClient);
                    break;
                case 4:
                    this.dependencyCallDurationMetric = this.metricTelemetryClient.GetMetric(
                                                        metricId: MetricTerms.Autocollection.Metric.DependencyCallDuration.Name,
                                                        dimension1Name: this.aggregateDimensions[0].Name,
                                                        dimension2Name: this.aggregateDimensions[1].Name,
                                                        dimension3Name: this.aggregateDimensions[2].Name,
                                                        dimension4Name: this.aggregateDimensions[3].Name,
                                                        metricConfiguration: config,
                                                        aggregationScope: MetricAggregationScope.TelemetryClient);
                    break;
                case 5:
                    MetricIdentifier metricIdentifier = new MetricIdentifier(MetricIdentifier.DefaultMetricNamespace,
                        MetricTerms.Autocollection.Metric.DependencyCallDuration.Name,
                        this.aggregateDimensions[0].Name,
                        this.aggregateDimensions[1].Name,
                        this.aggregateDimensions[2].Name,
                        this.aggregateDimensions[3].Name,
                        this.aggregateDimensions[4].Name);

                    this.dependencyCallDurationMetric = this.metricTelemetryClient.GetMetric(
                                                        metricIdentifier: metricIdentifier,
                                                        metricConfiguration: config,
                                                        aggregationScope: MetricAggregationScope.TelemetryClient);
                    break;
            }
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

            Metric dependencyCallMetric = this.dependencyCallDurationMetric;

            //// If there is no Metric, then this extractor has not been properly initialized yet:
            if (dependencyCallMetric == null)
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
                dimValues[i] = dim.GetFieldValue(dependencyCall);
                if(string.IsNullOrEmpty(dimValues[i]))
                {
                    dimValues[i] = dim.DefaultValue;
                }

                i++;
            }
            bool tracked = false;

            switch (this.aggregateDimensions.Count)
            {
                case 1:
                        tracked = dependencyCallMetric.TrackValue(dependencyCall.Duration.TotalMilliseconds,
                            dimValues[0]);
                    break;
                case 2:
                    tracked = dependencyCallMetric.TrackValue(dependencyCall.Duration.TotalMilliseconds,
                            dimValues[0],
                            dimValues[1]);
                    break;
                case 3:
                    tracked = dependencyCallMetric.TrackValue(dependencyCall.Duration.TotalMilliseconds,
                            dimValues[0],
                            dimValues[1],
                            dimValues[2]);
                    break;
                case 4:
                    tracked = dependencyCallMetric.TrackValue(dependencyCall.Duration.TotalMilliseconds,
                            dimValues[0],
                            dimValues[1],
                            dimValues[2],
                            dimValues[3]);
                    break;
                case 5:
                    tracked = dependencyCallMetric.TrackValue(dependencyCall.Duration.TotalMilliseconds,
                            dimValues[0],
                            dimValues[1],
                            dimValues[2],
                            dimValues[3],
                            dimValues[4]);
                    break;
            }

            if(!tracked)
            {
                var dimValueCountDependencyType = dependencyCallMetric.GetDimensionValues(3).Count;
                var dimValueCountCloudRoleInstance = dependencyCallMetric.GetDimensionValues(4).Count;
                this.metricTelemetryClient.TrackTrace($"StandardMetric Pre-Aggregation failed as dimension caps were reached." +
                    $" MaxRoleInstance Values: {MaxCloudRoleInstanceValuesToDiscover} Actual: {dimValueCountCloudRoleInstance}, " +
                    $"MaxDepTypes: {MaxDependencyTypesToDiscover} Actual: {dimValueCountDependencyType}");
            }

            isItemProcessed = true;
        }
    }
}
