namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Metrics;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    /// <summary>
    /// Represents a telemetry processor for sampling telemetry at a fixed-rate before sending to Application Insights.
    /// </summary>
    public sealed class SamplingTelemetryProcessor : ITelemetryProcessor, ITelemetryModule, IDisposable
    {
        private const string DependencyTelemetryName = "Dependency";
        private const string EventTelemetryName = "Event";
        private const string ExceptionTelemetryName = "Exception";
        private const string PageViewTelemetryName = "PageView";
        private const string RequestTelemetryName = "Request";
        private const string TraceTelemetryName = "Trace";

        private const string SamplingRateMetricName = "Sampling Rate (Preview)";

        private static readonly string UniqueProcessorIdMetricPropertyName = typeof(SamplingTelemetryProcessor) + ".UniqueId";
        private static readonly string IncludedTypesMetricPropertyName = typeof(SamplingTelemetryProcessor) + ".IncludedTypes";
        private static readonly string ExcludedTypesMetricPropertyName = typeof(SamplingTelemetryProcessor) + ".ExcludedTypes";

        private readonly char[] listSeparators = { ';' };
        private readonly IDictionary<string, Type> allowedTypes;

        private HashSet<Type> excludedTypesHashSet;
        private string excludedTypesString;

        private HashSet<Type> includedTypesHashSet;
        private string includedTypesString;

        private readonly string uniqueProcessorId = Guid.NewGuid().ToString("D");

        private MetricManager metricManager = null;
        private Metric samplingRateMetric = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingTelemetryProcessor"/> class.
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        /// </summary>
        public SamplingTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }

            this.SamplingPercentage = 100.0;
            this.Next = next;
            this.excludedTypesHashSet = new HashSet<Type>();
            this.includedTypesHashSet = new HashSet<Type>();
            this.allowedTypes = new Dictionary<string, Type>(6, StringComparer.OrdinalIgnoreCase)
            {
                { DependencyTelemetryName, typeof(DependencyTelemetry) },
                { EventTelemetryName, typeof(EventTelemetry) },
                { ExceptionTelemetryName, typeof(ExceptionTelemetry) },
                { PageViewTelemetryName, typeof(PageViewTelemetry) },
                { RequestTelemetryName, typeof(RequestTelemetry) },
                { TraceTelemetryName, typeof(TraceTelemetry) },
            };
        }

        /// <summary>
        /// Gets or sets a semicolon separated list of telemetry types that should not be sampled.
        /// Allowed type names: Dependency, Event, Exception, PageView, Request, Trace. 
        /// Types listed are excluded even if they are set in IncludedTypes.
        /// </summary>
        public string ExcludedTypes
        {
            get
            {
                return this.excludedTypesString;
            }

            set
            {
                this.excludedTypesString = value;
                this.samplingRateMetric = null;

                HashSet<Type> newExcludedTypesHashSet = new HashSet<Type>();
                if (!string.IsNullOrEmpty(value))
                {
                    string[] splitList = value.Split(this.listSeparators, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in splitList)
                    {
                        if (this.allowedTypes.ContainsKey(item))
                        {
                            newExcludedTypesHashSet.Add(this.allowedTypes[item]);
                        }
                    }
                }

                Interlocked.Exchange(ref this.excludedTypesHashSet, newExcludedTypesHashSet);
            }
        }

        /// <summary>
        /// Gets or sets a semicolon separated list of telemetry types that should be sampled. 
        /// If left empty all types are included implicitly. 
        /// Types are not included if they are set in ExcludedTypes.
        /// </summary>
        public string IncludedTypes
        {
            get
            {
                return this.includedTypesString;
            }

            set
            {
                this.includedTypesString = value;
                this.samplingRateMetric = null;

                HashSet<Type> newIncludedTypesHashSet = new HashSet<Type>();
                if (!string.IsNullOrEmpty(value))
                {
                    string[] splitList = value.Split(this.listSeparators, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in splitList)
                    {
                        if (this.allowedTypes.ContainsKey(item))
                        {
                            newIncludedTypesHashSet.Add(this.allowedTypes[item]);
                        }
                    }
                }

                Interlocked.Exchange(ref this.includedTypesHashSet, newIncludedTypesHashSet);
            }
        }

        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100) for all <see cref="ITelemetry"/>
        /// objects logged in this <see cref="TelemetryClient"/>.
        /// </summary>
        /// <remarks>
        /// All sampling percentage must be in a ratio of 100/N where N is a whole number (2, 3, 4, …). E.g. 50 for 1/2 or 33.33 for 1/3.
        /// Failure to follow this pattern can result in unexpected / incorrect computation of values in the portal.
        /// </remarks>
        public double SamplingPercentage { get; set; }

        /// <summary>
        /// Gets or sets the next TelemetryProcessor in call chain.
        /// </summary>
        private ITelemetryProcessor Next { get; set; }

        /// <summary>
        /// Initializes this processor using the correct telemetry pipeline configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            this.metricManager = (configuration == null)
                                        ? new MetricManager()
                                        : new MetricManager(new TelemetryClient(configuration));
        }

        /// <summary>
        /// Process a collected telemetry item.
        /// </summary>
        /// <param name="item">A collected Telemetry item.</param>
        public void Process(ITelemetry item)
        {
            double samplingPercentage = this.SamplingPercentage;

            // If sampling rate is 100% we log the sapling rate as a metric and do nothing else:
            if (samplingPercentage >= 100.0 - 1.0E-12)
            {
                
                TrackSamplingRate(100.0);
                this.Next.Process(item);
                return;
            }

            // So sampling rate is not 100%.

            // If null was passed in as item or if sampling not supported in general, do nothing (logging sampling rate does not apply):
            var samplingSupportingTelemetry = item as ISupportSampling;
            if (samplingSupportingTelemetry == null)
            {
                this.Next.Process(item);
                return;
            }

            // If telemetry was excuded by type, do nothing (logging sampling rate does not apply):
            if (IsSamplingApplicable(item.GetType()))
            {
                if (TelemetryChannelEventSource.Log.IsVerboseEnabled)
                {
                    TelemetryChannelEventSource.Log.SamplingSkippedByType(item.ToString());
                }
                this.Next.Process(item);
                return;
            }

            // If telemetry wasalready sampled, do nothing (logging sampling rate does not apply):
            bool itemAlreadySampled = samplingSupportingTelemetry.SamplingPercentage.HasValue;
            if (itemAlreadySampled)
            {
                this.Next.Process(item);
                return;
            }

            // Ok, now we can actually sample:

            samplingSupportingTelemetry.SamplingPercentage = samplingPercentage;
            bool isSampledIn = SamplingScoreGenerator.GetSamplingScore(item) < samplingPercentage;

            TrackSamplingRate(samplingPercentage);

            if (isSampledIn)
            {
                this.Next.Process(item);
            }
            else
            { 
                if (TelemetryChannelEventSource.Log.IsVerboseEnabled)
                {
                    TelemetryChannelEventSource.Log.ItemSampledOut(item.ToString());
                }

                TelemetryDebugWriter.WriteTelemetry(item, this.GetType().Name);
            }
        }

        private bool IsSamplingApplicable(Type telemetryItemType)
        {
            var excludedTypesHashSetRef = this.excludedTypesHashSet;
            var includedTypesHashSetRef = this.includedTypesHashSet;

            if (excludedTypesHashSetRef.Count > 0 && excludedTypesHashSetRef.Contains(telemetryItemType))
            {
                return false;
            }

            if (includedTypesHashSetRef.Count > 0 && ! includedTypesHashSetRef.Contains(telemetryItemType))
            {
                return false;
            }

            return true;
        }

        private void TrackSamplingRate(double samplingPercentage)
        {
            Metric samplingMetric = this.samplingRateMetric;
            if (samplingMetric == null)
            {
                MetricManager metricManager = this.metricManager;
                if (metricManager == null)
                {
                    return;
                }

                // There is an edge case where there may be several sampling processors in the pipeline.
                // To account for that, the aggregated metric documents will be marked with sufficinet info to differentiate the sampling rates.
                // In general, if the user is only interested in whether the sampling rate is 100% or not 100% across, these properties my be ignored.
                samplingMetric = metricManager.CreateMetric(SamplingRateMetricName,
                                                            new Dictionary<string, string>()
                                                            {
                                                                [UniqueProcessorIdMetricPropertyName] = this.uniqueProcessorId,
                                                                [IncludedTypesMetricPropertyName] = this.IncludedTypes,
                                                                [ExcludedTypesMetricPropertyName] = this.ExcludedTypes,
                                                            });

                Metric prevSamplingMetric = Interlocked.CompareExchange(ref this.samplingRateMetric, samplingMetric, null);
                samplingMetric = prevSamplingMetric ?? samplingMetric;
            }

            samplingMetric.Track(samplingPercentage);
        }


        /// <summary>
        /// Disposes of the comtained disposable fields.
        /// </summary>
        public void Dispose()
        {
            IDisposable metricMgr = this.metricManager;
            if (metricMgr != null)
            {
                // benign race
                metricMgr.Dispose();
                this.metricManager = null;
            }
        }
    }
}
