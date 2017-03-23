namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics;

    /// <summary>
    /// An instance of this class is contained within the <see cref="AutocollectedMetricsExtractor"/> telemetry processor.
    /// It extracts auto-collected, pre-aggregated (aka. "standard") metrics from DependencyTelemetry objects which represent invocations of the monitored service.
    /// </summary>
    /// <remarks>
    /// Auto-Discovering Dependency Types: **
    /// Dependency call duration is collected as a metric for failed and successful calls separately, and grouped by dependency type.
    /// It is essential to control the number of data series produced by this extractor: It must be a small, bounded value.
    /// However, this extractor needs to support different modules that collect information about different kinds of dependencies.
    /// To meet these constraints, the extractor will auto-discover dependency types, but it will not auto-discover more types than
    /// the number controlled by the <see cref="MaxDependencyTypesToDiscover"/> property.
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
        /// The <c>MetricManager</c> to be used for creating and sending the metrics by this extractor.
        /// </summary>
        private MetricManager metricManager = null;

        /// <summary>
        /// Groups privates to ensure atomic updates via replacements.
        /// </summary>
        private MetricsCache metrics = new MetricsCache();

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyMetricsExtractor"/> class.
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
        public string ExtractorName { get; } = typeof(DependencyMetricsExtractor).FullName;

        /// <summary>
        /// Gets the version of this extractor.
        /// All telemetry that has been processed by this extractor will be tagged by adding the
        /// string "<c>(Name: {ExtractorName}, Ver:{ExtractorVersion})</c>" to the <c>xxx.ProcessedByExtractors</c> property.
        /// The respective logic is in the <see cref="AutocollectedMetricsExtractor"/>-class.
        /// </summary>
        public string ExtractorVersion { get; } = "1.0";

        /// <summary>
        /// Gets or sets the maximum number of auto-discovered dependency types.
        /// See also the remarks about the <see cref="DependencyMetricsExtractor"/> class for additional info about the use the of this property.
        /// </summary>
        public int MaxDependencyTypesToDiscover
        {
            get
            {
                return this.metrics.MaxDependencyTypesToDiscover;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxDependencyTypesToDiscover value may not be negative.");
                }

                this.metricManager?.Flush();
                this.ReinitializeMetrics(value);
            }
        }

        /// <summary>
        /// Initializes the internal metrics trackers based on settings.
        /// </summary>
        /// <param name="metricManager">The <c>MetricManager</c> to be used for creating and sending the metrics by this extractor.</param>
        public void InitializeExtractor(MetricManager metricManager)
        {
            this.metricManager = metricManager;
            this.ReinitializeMetrics(this.metrics?.MaxDependencyTypesToDiscover ?? MaxDependenctTypesToDiscoverDefault);
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

            MetricManager thisMetricManager = this.metricManager;
            MetricsCache thisMetrics = this.metrics;

            //// If there is no MetricManager, then this extractor has not been properly initialized yet:
            if (thisMetricManager == null)
            {
                //// This will be caught and properly logged by the base class:
                throw new InvalidOperationException("Cannot execute ExtractMetrics becasue this metrics extractor has not been initialized (no metrics manager).");
            }

            //// Get dependency call success status:
            bool dependencyFailed = (dependencyCall.Success != null) && (dependencyCall.Success == false);

            //// Now we ned to determine which data series to use:
            Metric metricToTrack = null;

            if (thisMetrics.MaxDependencyTypesToDiscover == 0)
            {
                //// If auto-discovering dependency types is disabled, just pick series based on success status:
                metricToTrack = dependencyFailed
                                    ? thisMetrics.Default.Failure
                                    : thisMetrics.Default.Success;
            }
            else
            {
                //// Pick series based on dependency type (and success status):
                string dependencyType = dependencyCall.Type;

                if (dependencyType == null || dependencyType.Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    //// If dependency type is not set, we use "Unknown":
                    metricToTrack = dependencyFailed
                                        ? thisMetrics.Unknown.Failure
                                        : thisMetrics.Unknown.Success;
                }
                else
                {
                    //// See if we have already discovered the current dependency type:
                    SucceessAndFailureMetrics typeMetrics;
                    bool previouslyDiscovered = thisMetrics.ByType.TryGetValue(dependencyType, out typeMetrics);

                    if (previouslyDiscovered)
                    {
                        metricToTrack = dependencyFailed
                                    ? typeMetrics.Failure
                                    : typeMetrics.Success;
                    }
                    else
                    {
                        //// We have not seen the current dependency type yet:

                        if (thisMetrics.ByType.Count >= thisMetrics.MaxDependencyTypesToDiscover)
                        {
                            //// If the limit of types to discover is already reached, just use "Other":
                            metricToTrack = dependencyFailed
                                    ? thisMetrics.Default.Failure
                                    : thisMetrics.Default.Success;
                        }
                        else
                        {
                            //// So we have not yet reached the limit.
                            //// We will need to take a lock to make sure that the number of discovered types is used correctly as a limit.
                            //// Note: this is a very rare case. It is expected to occur only MaxDependencyTypesToDiscover times.
                            //// In case of very high contention, this may happen a little more often,
                            //// but will no longer happen once the MaxDependencyTypesToDiscover limit is reached.

                            const string TypeDiscoveryLimitReachedMessage = "Cannot discover more dependency types becasue the MaxDependencyTypesToDiscover limit"
                                                                          + " is reached. This is a control-flow exception that should not propagate outside "
                                                                          + "the metric extraction logic.";

                            try
                            {
                                typeMetrics = thisMetrics.ByType.GetOrAdd(
                                        dependencyType,
                                        (depType) =>
                                        {
                                            lock (thisMetrics.TypeDiscoveryLock)
                                            {
                                                if (thisMetrics.DependencyTypesDiscoveredCount >= thisMetrics.MaxDependencyTypesToDiscover)
                                                {
                                                    throw new InvalidOperationException(TypeDiscoveryLimitReachedMessage);
                                                }

                                                thisMetrics.DependencyTypesDiscoveredCount++;

                                                return new SucceessAndFailureMetrics(
                                                    thisMetricManager.CreateMetric(
                                                            MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                                            new Dictionary<string, string>()
                                                            {
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.TrueString,  // SUCCESS metric
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName] = depType,
                                                            }),
                                                    thisMetricManager.CreateMetric(
                                                            MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                                            new Dictionary<string, string>()
                                                            {
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.FalseString, // FAILURE metric
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName] = depType,
                                                            }));
                                            }
                                        });
                            }
                            catch (InvalidOperationException ex)
                            {
                                if (!ex.Message.Equals(TypeDiscoveryLimitReachedMessage, StringComparison.Ordinal))
                                {
#if NET40
                                    throw;
#else
                                    ExceptionDispatchInfo.Capture(ex).Throw();
#endif
                                }

                                //// Limit was reached concurrently. We will use "Other" after all:
                                metricToTrack = dependencyFailed
                                    ? thisMetrics.Default.Failure
                                    : thisMetrics.Default.Success;
                            }

                            //// Use the newly created metric for thisnewly discovered dependency type:
                            metricToTrack = dependencyFailed
                                    ? typeMetrics.Failure
                                    : typeMetrics.Success;
                        }
                    }   // else OF if (previouslyDiscovered)
                }
            }
            
            //// Now that we selected the right metric, track the value:
            isItemProcessed = true;
            metricToTrack.Track(dependencyCall.Duration.TotalMilliseconds);
        }

        /// <summary>
        /// Initializes the privates and activates them atomically.
        /// </summary>
        /// <param name="maxDependencyTypesToDiscoverCount">Max number of Dependency Types to discover.</param>
        private void ReinitializeMetrics(int maxDependencyTypesToDiscoverCount)
        {
            MetricManager thisMetricManager = this.metricManager;
            if (thisMetricManager == null)
            {
                MetricsCache newMetrics = new MetricsCache();
                newMetrics.MaxDependencyTypesToDiscover = maxDependencyTypesToDiscoverCount;
                this.metrics = newMetrics;
                return;
            }

            if (maxDependencyTypesToDiscoverCount == 0)
            {
                MetricsCache newMetrics = new MetricsCache();

                newMetrics.Default = new SucceessAndFailureMetrics(
                        thisMetricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>()
                                {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.TrueString,      // SUCCESS metric
                                }),
                        thisMetricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>()
                                {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.FalseString,     // FAILURE metric
                                }));

                newMetrics.Unknown = null;
                newMetrics.ByType = null;
                newMetrics.MaxDependencyTypesToDiscover = maxDependencyTypesToDiscoverCount;
                newMetrics.TypeDiscoveryLock = null;
                newMetrics.DependencyTypesDiscoveredCount = 0;

                this.metrics = newMetrics;
            }
            else
            {
                MetricsCache newMetrics = new MetricsCache();

                newMetrics.Default = new SucceessAndFailureMetrics(
                        thisMetricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>()
                                {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.TrueString,      // SUCCESS metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeNames.Other,
                                }),
                        thisMetricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>()
                                {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.FalseString,     // FAILURE metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeNames.Other,
                                }));

                newMetrics.Unknown = new SucceessAndFailureMetrics(
                        thisMetricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>()
                                {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.TrueString,      // SUCCESS metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeNames.Unknown,
                                }),
                        thisMetricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>()
                                {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.Success] = Boolean.FalseString,     // FAILURE metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyNames.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeNames.Unknown,
                                }));

                newMetrics.ByType = new ConcurrentDictionary<string, SucceessAndFailureMetrics>();
                newMetrics.MaxDependencyTypesToDiscover = maxDependencyTypesToDiscoverCount;
                newMetrics.TypeDiscoveryLock = new object();
                newMetrics.DependencyTypesDiscoveredCount = 0;

                this.metrics = newMetrics;
            }
        }

        /// <summary>
        /// This private data structure groups two metrics for successful and failed calls to a group of dependencies.
        /// </summary>
        private class SucceessAndFailureMetrics
        {
            public SucceessAndFailureMetrics()
                : this(null, null)
            {
            }

            public SucceessAndFailureMetrics(Metric successMetric, Metric failureMetric)
            {
                this.Success = successMetric;
                this.Failure = failureMetric;
            }

            public Metric Success { get; private set; }

            public Metric Failure { get; private set; }
        }   // private class SucceessAndFailureMetrics

        /// <summary>
        /// This private data structure groups several privates of the outer class (DependencyMetricsExtractor).
        /// It allows for a lock-free atomic update of all the represented settings and values.
        /// </summary>
        private class MetricsCache
        {
            public ConcurrentDictionary<string, SucceessAndFailureMetrics> ByType = null;
            public SucceessAndFailureMetrics Default = null;
            public SucceessAndFailureMetrics Unknown = null;
            public int MaxDependencyTypesToDiscover = MaxDependenctTypesToDiscoverDefault;
            public int DependencyTypesDiscoveredCount = 0;
            public object TypeDiscoveryLock = null;
        }   // private class MetricsCache
    }
}
