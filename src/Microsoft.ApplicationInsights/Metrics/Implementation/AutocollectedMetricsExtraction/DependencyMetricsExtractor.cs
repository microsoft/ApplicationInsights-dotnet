namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using System;

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
        /// <see cref="ReinitializeMetrics(Int32)" />-lock.
        /// </summary>
        private readonly object initializationLock = new Object();

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
        /// Initializes a new instance of the <see cref="DependencyMetricsExtractor" /> class.
        /// </summary>
        public DependencyMetricsExtractor()
        {
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
                this.ReinitializeMetrics(value);
            }
        }

        /// <summary>
        /// Pre-initialize this extractor.
        /// </summary>
        /// <param name="metricTelemetryClient">The <c>TelemetryClient</c> to be used for sending extracted metrics.</param>
        public void InitializeExtractor(TelemetryClient metricTelemetryClient)
        {
            this.metricTelemetryClient = metricTelemetryClient;
            // Benigh race where we already set the new metricTelemetryClient, but dependencyCallDurationMetric is not yet updated.
            this.ReinitializeMetrics(this.maxDependencyTypesToDiscover);
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

            //// Get dependency call success status:
            bool dependencyFailed = (dependencyCall.Success != null) && (dependencyCall.Success == false);
            string dependencySuccessString = dependencyFailed ? bool.FalseString : bool.TrueString;

            //// Get Dependency Type Name:
            //// IF (MaxDependencyTypesToDiscover == 0) THEN we do not group by Dependency Type (always use "Other").
            //// ELSE We group by Dependency Type and if dim limit is reached fall back to using "Other" (we set that in the metric config).
            string dependencyType = (this.MaxDependencyTypesToDiscover == 0)
                                                    ? MetricTerms.Autocollection.DependencyCall.TypeNames.Other
                                                    : dependencyCall.Type;

            //// If Dependency Type is not set, we use "Unknown":
            if (string.IsNullOrEmpty(dependencyType))
            {
                dependencyType = MetricTerms.Autocollection.DependencyCall.TypeNames.Unknown;
            }

            //// Now get the data series to use and use it:
            dependencyCallMetric.TryGetDataSeries(
                                                    out MetricSeries seriesToTrack,
                                                    MetricTerms.Autocollection.Metric.DependencyCallDuration.Id,
                                                    dependencySuccessString,
                                                    dependencyType);

            seriesToTrack.TrackValue(dependencyCall.Duration.TotalMilliseconds);
            isItemProcessed = true;
        }

        /// <summary>
        /// Initializes the privates and activates them atomically.
        /// </summary>
        /// <param name="maxDependencyTypesToDiscoverCount">Max number of Dependency Types to discover.</param>
        private void ReinitializeMetrics(int maxDependencyTypesToDiscoverCount)
        {
            if (maxDependencyTypesToDiscoverCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                                nameof(maxDependencyTypesToDiscoverCount), 
                                maxDependencyTypesToDiscoverCount, 
                                Invariant($"{nameof(this.MaxDependencyTypesToDiscover)} value may not be negative."));
            }

            lock (this.initializationLock)
            {
                if (maxDependencyTypesToDiscoverCount > int.MaxValue - 3)
                {
                    maxDependencyTypesToDiscoverCount = int.MaxValue - 3;
                }

                int depTypesDimValuesLimit = (maxDependencyTypesToDiscoverCount == 0)
                                                // "Other":
                                                ? 1
                                                // Discovered types + "Unknown" (when type not set):
                                                : maxDependencyTypesToDiscoverCount + 2;

                TelemetryClient thisMetricTelemetryClient = this.metricTelemetryClient;

                //// If there is no TelemetryClient, then this extractor has not been properly initialized yet.
                //// We set maxDependencyTypesToDiscover and return: 
                if (thisMetricTelemetryClient == null)
                {
                    this.dependencyCallDurationMetric = null;
                    this.maxDependencyTypesToDiscover = maxDependencyTypesToDiscoverCount;
                    return;
                }

                //// Remove the old metric before creating the new one:
                MetricManager metricManager;
                if (thisMetricTelemetryClient.TryGetMetricManager(out metricManager))
                {
                    Metric oldMetric = this.dependencyCallDurationMetric;
                    metricManager.Metrics.Remove(oldMetric);
                }

                MetricConfiguration config = new MetricConfigurationForMeasurement(
                                                            (1 * 2 * depTypesDimValuesLimit) + 1,
                                                            new[] { 1, 2, depTypesDimValuesLimit },
                                                            new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));
                config.SetUnsafeDimCapFallbackDimensionValues(null, null, MetricTerms.Autocollection.DependencyCall.TypeNames.Other);

                Metric dependencyCallDuration = thisMetricTelemetryClient.GetMetric(
                                                            metricId: MetricTerms.Autocollection.Metric.DependencyCallDuration.Name,
                                                            dimension1Name: MetricDimensionNames.TelemetryContext.Property(MetricTerms.Autocollection.MetricId.Moniker.Key),                                                            
                                                            dimension2Name: MetricTerms.Autocollection.DependencyCall.PropertyNames.Success,
                                                            dimension3Name: MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName,
                                                            metricConfiguration: config,
                                                            aggregationScope: MetricAggregationScope.TelemetryClient);

                // "Pre-book" series for "Unknown" dependenty type to make sure they are not affected by dimension caps:
                // (We do not need to pre-book the "Other" becasue we have specified it as a fallback for the TypeName 
                // dimension in case the dim-val limit for that dimension is reached.)

                if (maxDependencyTypesToDiscoverCount != 0)
                {
                    MetricSeries prebookedSeries;

                    dependencyCallDuration.TryGetDataSeries(
                                                            out prebookedSeries,
                                                            MetricTerms.Autocollection.Metric.DependencyCallDuration.Id,
                                                            Boolean.TrueString,
                                                            MetricTerms.Autocollection.DependencyCall.TypeNames.Unknown);
                    dependencyCallDuration.TryGetDataSeries(
                                                            out prebookedSeries,
                                                            MetricTerms.Autocollection.Metric.DependencyCallDuration.Id,
                                                            Boolean.FalseString,
                                                            MetricTerms.Autocollection.DependencyCall.TypeNames.Unknown);
                }

                // Benign race where dependencyCallDurationMetric config and maxDependencyTypesToDiscover may not correspond briefly:

                this.dependencyCallDurationMetric = dependencyCallDuration;
                this.maxDependencyTypesToDiscover = maxDependencyTypesToDiscoverCount;
            }
        }
    }
}
