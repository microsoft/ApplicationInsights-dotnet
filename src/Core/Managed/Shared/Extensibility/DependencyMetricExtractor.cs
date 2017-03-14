using System;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Microsoft.ApplicationInsights.Extensibility
{
    public class DependencyMetricExtractor : MetricExtractorTelemetryProcessorBase
    {
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
        }

        private class MetricsCache
        {
            public ConcurrentDictionary<string, SucceessAndFailureMetrics> ByType = null;
            public SucceessAndFailureMetrics Default = null;
            public SucceessAndFailureMetrics Unknown = null;
            public int MaxDependencyTypesToDiscover = MaxDependenctTypesToDiscoverDefault;
            public int DependencyTypesDiscoveredCount = 0;
            public object TypeDiscoveryLock = null;
        }

        public const int MaxDependenctTypesToDiscoverDefault = 10;
        private const string Version = "1.0";

        private MetricsCache metrics = null;

        public DependencyMetricExtractor(ITelemetryProcessor nextProcessorInPipeline)
            :base(nextProcessorInPipeline, MetricTerms.Autocollection.Moniker.Key, MetricTerms.Autocollection.Moniker.Value)
        {
        }

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

        protected override string ExtractorVersion { get { return Version; } }

        public override void InitializeExtractor(TelemetryConfiguration unusedConfiguration)
        {
            ReinitializeMetrics(this.metrics?.MaxDependencyTypesToDiscover ?? MaxDependenctTypesToDiscoverDefault);
        }

        public override void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
        {
            DependencyTelemetry dependencyCall = fromItem as DependencyTelemetry;
            if (dependencyCall == null)
            {
                isItemProcessed = false;
                return;
            }

            if (MetricManager == null)
            {
                // This will be caught and properly logged by the base class:
                throw new InvalidOperationException("Cannot execute ExtractMetrics becasue this metrics extractor has not been initialized (no metrics manager).");
            }

            MetricsCache allMetrics = this.metrics;
            if (allMetrics == null)
            {
                // This will be caught and properly logged by the base class:
                throw new InvalidOperationException("Cannot execute ExtractMetrics becasue this metrics extractor has not been initialized (no metrics cache).");
            }

            bool dependencyFailed = (dependencyCall.Success != null) && (dependencyCall.Success == false);
            Metric metricToTrack = null;

            if (allMetrics.MaxDependencyTypesToDiscover == 0)
            {
                metricToTrack = (dependencyFailed)
                                    ? allMetrics.Default.Failure
                                    : allMetrics.Default.Success;
            }
            else
            {
                string dependencyType = dependencyCall.Type;

                if (dependencyType == null)
                {
                    metricToTrack = (dependencyFailed)
                                    ? allMetrics.Unknown.Failure
                                    : allMetrics.Unknown.Success;
                }
                else
                {
                    SucceessAndFailureMetrics typeMetrics;
                    bool previouslyDiscovered = allMetrics.ByType.TryGetValue(dependencyType, out typeMetrics);

                    // We are aiming to discover one or more types and we have encountered a non-null type that has not been previously seen.
                    // We will see if we reached the limit of types to discover already. If we did, the current item will go into the Other bucket.
                    // In case that we have not yet reached the limit, we will need to take a lock.
                    // This is a very rare case. It is expected to occur only MaxDependencyTypesToDiscover times.
                    // In case of very high contention, this may happen more often, but will no longer happen once the MaxDependencyTypesToDiscover limit is reached.
                    if (!previouslyDiscovered)
                    {
                        if (allMetrics.ByType.Count >= allMetrics.MaxDependencyTypesToDiscover)
                        {
                            metricToTrack = (dependencyFailed)
                                    ? allMetrics.Default.Failure
                                    : allMetrics.Default.Success;
                        }
                        else
                        {
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
                                metricToTrack = (dependencyFailed)
                                    ? allMetrics.Default.Failure
                                    : allMetrics.Default.Success;
                            }

                            metricToTrack = (dependencyFailed)
                                    ? typeMetrics.Failure
                                    : typeMetrics.Success;
                        }
                    }
                }
            }
            
            isItemProcessed = true;
            metricToTrack.Track(dependencyCall.Duration.TotalMilliseconds);
        }

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
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Other,
                                }),
                        metricManager.CreateMetric(
                                MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                                new Dictionary<string, string>() {
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString,     // FAILURE metric
                                    [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Other,
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
