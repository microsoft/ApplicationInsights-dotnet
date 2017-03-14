using System;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Microsoft.ApplicationInsights.Extensibility
{
    /// <summary>
    /// Participates in the telemetry pipeline as a telemetry processor and extracts auto-collected, pre-aggregated
    /// metrics from DependencyTelemetry objects which represent calls to external dependencies.
    /// </summary>
    /// <remarks>
    /// * Auto-Discovering Dependency Types:*
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
    public sealed class DependencyMetricExtractor : MetricExtractorTelemetryProcessorBase
    {
        /// <summary>
        /// This private data structure groups two metrics for successful and failed calls to a group of dependencies.
        /// </summary>
        private class SucceessAndFailureMetrics
        {
            public SucceessAndFailureMetrics() : this(null, null) { }

            public SucceessAndFailureMetrics(Metric successMetric, Metric failureMetric)
            {
                this.Success = successMetric;
                this.Failure = failureMetric;
            }

            public Metric Success { get; private set; }
            public Metric Failure { get; private set; }
        }   // private class SucceessAndFailureMetrics

        /// <summary>
        /// This orivate data sructure groups several privats of the outer class (DependencyMetricExtractor).
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

        /// <summary>
        /// The default value for the <see cref="MaxDependencyTypesToDiscover"/> property if it is not set to a different value.
        /// See also the remarks about the <see cref="DependencyMetricExtractor"/> class for additional info about the use
        /// the of <c>MaxDependencyTypesToDiscover</c>-property.
        /// </summary>
        public const int MaxDependenctTypesToDiscoverDefault = 10;

        /// <summary>
        /// Version of this extractor. Used by the infrastructure to mark processed telemetry.
        /// Change this value when publically observed bahavior changes in any way.
        /// </summary>
        private const string Version = "1.0";


        /// <summary>
        /// Groups privates to ensure atomic updates via replacements.
        /// </summary>
        private MetricsCache metrics = null;


        /// <summary>
        /// Creates a new <c>DependencyMetricExtractor</c>.
        /// </summary>
        /// <param name="nextProcessorInPipeline">Subsequent telementry processor.</param>
        public DependencyMetricExtractor(ITelemetryProcessor nextProcessorInPipeline)
            :base(nextProcessorInPipeline, MetricTerms.Autocollection.Moniker.Key, MetricTerms.Autocollection.Moniker.Value)
        {
        }


        /// <summary>
        /// Controls the maximum number of auto-discovered dependency types.
        /// See also the remarks about the <see cref="DependencyMetricExtractor"/> class for additional info about the use the of this property.
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

                this.MetricManager?.Flush();
                this.ReinitializeMetrics(value);
            }
        }


        /// <summary>
        /// Exposes the version of this Extractor's public contracts to the base class.
        /// </summary>
        protected override string ExtractorVersion { get { return Version; } }


        /// <summary>
        /// Initializes the internal metrics trackers based on settings.
        /// </summary>
        /// <param name="unusedConfiguration">Is not currently used.</param>
        public override void InitializeExtractor(TelemetryConfiguration unusedConfiguration)
        {
            ReinitializeMetrics(this.metrics?.MaxDependencyTypesToDiscover ?? MaxDependenctTypesToDiscoverDefault);
        }

        /// <summary>
        /// Extracts appropriate data points for auto-collected, pre-aggregated metrics from a single <c>DependencyTelemetry</c> item.
        /// </summary>
        /// <param name="fromItem">The telemetry item from which to extract the metric data points.</param>
        /// <param name="isItemProcessed">Whether of not the specified item was processed (aka not ignored) by this extractor.</param>
        public override void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
        {
            // If this item is not a DependencyTelemetry, we will not process it:
            DependencyTelemetry dependencyCall = fromItem as DependencyTelemetry;
            if (dependencyCall == null)
            {
                isItemProcessed = false;
                return;
            }

            // If there is no MetricManager, then this extractor has not been properly initialized yet:
            if (MetricManager == null)
            {
                // This will be caught and properly logged by the base class:
                throw new InvalidOperationException("Cannot execute ExtractMetrics becasue this metrics extractor has not been initialized (no metrics manager).");
            }

            // If locals have not been initialized, then this extractor has not been properly initialized yet:
            MetricsCache allMetrics = this.metrics;
            if (allMetrics == null)
            {
                // This will be caught and properly logged by the base class:
                throw new InvalidOperationException("Cannot execute ExtractMetrics becasue this metrics extractor has not been initialized (no metrics cache).");
            }

            // Get dependency call success status:
            bool dependencyFailed = (dependencyCall.Success != null) && (dependencyCall.Success == false);

            // Now we ned to determine which data series to use:
            Metric metricToTrack = null;

            if (allMetrics.MaxDependencyTypesToDiscover == 0)
            {
                // If auto-discovering dependency types is disabled, just pick series based on success status:
                metricToTrack = (dependencyFailed)
                                    ? allMetrics.Default.Failure
                                    : allMetrics.Default.Success;
            }
            else
            {
                // Pick series based on dependency type (and success status):
                string dependencyType = dependencyCall.Type;

                if (dependencyType == null)
                {
                    // If dependency type is not set, we use "Unknown":
                    metricToTrack = (dependencyFailed)
                                    ? allMetrics.Unknown.Failure
                                    : allMetrics.Unknown.Success;
                }
                else
                {
                    // See if we have already duscovered the current dependency type:
                    SucceessAndFailureMetrics typeMetrics;
                    bool previouslyDiscovered = allMetrics.ByType.TryGetValue(dependencyType, out typeMetrics);

                    if (!previouslyDiscovered)
                    {
                        // We have not seen the current dependency type yet:

                        if (allMetrics.ByType.Count >= allMetrics.MaxDependencyTypesToDiscover)
                        {
                            // If the limit of types to discover is already reached, just use "Other":
                            metricToTrack = (dependencyFailed)
                                    ? allMetrics.Default.Failure
                                    : allMetrics.Default.Success;
                        }
                        else
                        {
                            // So we have not yet reached the limit.
                            // We will need to take a lock to make sure that the number of discovered types is used correctly as a limit.
                            // Note: this is a very rare case. It is expected to occur only MaxDependencyTypesToDiscover times.
                            // In case of very high contention, this may happen a little more often,
                            // but will no longer happen once the MaxDependencyTypesToDiscover limit is reached.
                            try
                            {
                                typeMetrics = allMetrics.ByType.GetOrAdd(
                                        dependencyType,
                                        (depType) =>
                                        {
                                            lock (allMetrics.TypeDiscoveryLock)
                                            {
                                                if (allMetrics.DependencyTypesDiscoveredCount >= allMetrics.MaxDependencyTypesToDiscover)
                                                {
                                                    throw new InvalidOperationException("MaxDependencyTypesToDiscover reached.");
                                                }

                                                allMetrics.DependencyTypesDiscoveredCount++;

                                                return new SucceessAndFailureMetrics(
                                                    MetricManager.CreateMetric(
                                                            MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                                            new Dictionary<string, string>() {
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.TrueString,  // SUCCESS metric
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = depType,
                                                            }),
                                                    MetricManager.CreateMetric(
                                                            MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                                            new Dictionary<string, string>() {
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString, // FAILURE metric
                                                                [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = depType,
                                                            }));
                                            }
                                        });
                            }
                            catch(InvalidOperationException)
                            {
                                // Limit was reached concurrently. We will use "Other" after all:
                                metricToTrack = (dependencyFailed)
                                    ? allMetrics.Default.Failure
                                    : allMetrics.Default.Success;
                            }

                            // Use the newly created metric for thisnewly discovered dependency type:
                            metricToTrack = (dependencyFailed)
                                    ? typeMetrics.Failure
                                    : typeMetrics.Success;
                        }
                    }
                }
            }
            
            // Now that we selected the right metric, track the value:
            isItemProcessed = true;
            metricToTrack.Track(dependencyCall.Duration.TotalMilliseconds);
        }

        /// <summary>
        /// Initializes the privates and activates them atomically.
        /// </summary>
        /// <param name="maxDependencyTypesToDiscoverCount"></param>
        private void ReinitializeMetrics(int maxDependencyTypesToDiscoverCount)
        {
            MetricManager metricManager = this.MetricManager;
            if (metricManager == null)
            {
                return;
            }

            if (maxDependencyTypesToDiscoverCount == 0)
            {
                MetricsCache newMetrics = new MetricsCache();

                newMetrics.Default = new SucceessAndFailureMetrics(
                        metricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>() {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.TrueString,      // SUCCESS metric
                                }),
                        metricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>() {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString,     // FAILURE metric
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
                        metricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>() {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.TrueString,      // SUCCESS metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Other,
                                }),
                        metricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>() {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString,     // FAILURE metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Other,
                                }));

                newMetrics.Unknown = new SucceessAndFailureMetrics(
                        metricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>() {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.TrueString,      // SUCCESS metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Unknown,
                                }),
                        metricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>() {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString,     // FAILURE metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Unknown,
                                }));

                newMetrics.ByType = new ConcurrentDictionary<string, SucceessAndFailureMetrics>();
                newMetrics.MaxDependencyTypesToDiscover = maxDependencyTypesToDiscoverCount;
                newMetrics.TypeDiscoveryLock = new Object();
                newMetrics.DependencyTypesDiscoveredCount = 0;

                this.metrics = newMetrics;
            }
        }
    }
}
