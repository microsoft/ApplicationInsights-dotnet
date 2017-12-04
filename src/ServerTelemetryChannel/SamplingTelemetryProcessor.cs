namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    
    /// <summary>
    /// Represents a telemetry processor for sampling telemetry at a fixed-rate before sending to Application Insights.
    /// </summary>
    public sealed class SamplingTelemetryProcessor : ITelemetryProcessor
    {
        private const string DependencyTelemetryName = "Dependency";
        private const string EventTelemetryName = "Event";
        private const string ExceptionTelemetryName = "Exception";
        private const string PageViewTelemetryName = "PageView";
        private const string RequestTelemetryName = "Request";
        private const string TraceTelemetryName = "Trace";

        private readonly char[] listSeparators = { ';' };
        private readonly IDictionary<string, Type> allowedTypes;

        private HashSet<Type> excludedTypesHashSet;
        private string excludedTypesString;

        private HashSet<Type> includedTypesHashSet;
        private string includedTypesString;

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
            this.SampledNext = next;
            this.UnsampledNext = next;

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

        internal SamplingTelemetryProcessor(ITelemetryProcessor unsampledNext, ITelemetryProcessor sampledNext) : this(sampledNext)
        {
            if (unsampledNext == null)
            {
                throw new ArgumentNullException("unsampledNext");
            }

            this.UnsampledNext = unsampledNext;
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
        /// Gets or sets the next TelemetryProcessor in call chain to send evaluated (sampled) telemetry items to.
        /// </summary>
        private ITelemetryProcessor SampledNext { get; set; }

        /// <summary>
        /// Gets or sets the next TelemetryProcessor to call in the chain if the ITelemetry item passed in is not sampled. Note that 
        /// for the public instances of this class (those created by naming the module in ApplicationInsights.config) this property
        /// will be equal to the <see cref="SampledNext"/> property.
        /// </summary>
        private ITelemetryProcessor UnsampledNext { get; set; }

        /// <summary>
        /// Process a collected telemetry item.
        /// </summary>
        /// <param name="item">A collected Telemetry item.</param>
        public void Process(ITelemetry item)
        {
            double samplingPercentage = this.SamplingPercentage;

            //// If sampling rate is 100% and we aren't distinguishing between evaluated/unevaluated items, there is nothing to do:
            if (this.SampledNext.Equals(this.UnsampledNext) && samplingPercentage >= 100.0 - 1.0E-12)
            {
                this.SampledNext.Process(item);
                return;
            }

            //// So sampling rate is not 100%, or we must evaluate further

            //// If null was passed in as item or if sampling not supported in general, do nothing:
            var samplingSupportingTelemetry = item as ISupportSampling;
            if (samplingSupportingTelemetry == null)
            {
                this.UnsampledNext.Process(item);
                return;
            }

            //// If telemetry was excluded by type, do nothing:
            if (!this.IsSamplingApplicable(item.GetType()))
            {
                if (TelemetryChannelEventSource.Log.IsVerboseEnabled)
                {
                    TelemetryChannelEventSource.Log.SamplingSkippedByType(item.ToString());
                }

                this.UnsampledNext.Process(item);
                return;
            }

            //// If telemetry was already sampled, do nothing:
            bool itemAlreadySampled = samplingSupportingTelemetry.SamplingPercentage.HasValue;
            if (itemAlreadySampled)
            {
                this.UnsampledNext.Process(item);
                return;
            }

            //// Ok, now we can actually sample:

            samplingSupportingTelemetry.SamplingPercentage = samplingPercentage;
            bool isSampledIn = SamplingScoreGenerator.GetSamplingScore(item) < samplingPercentage;

            if (isSampledIn)
            {
                this.SampledNext.Process(item);
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

            if (includedTypesHashSetRef.Count > 0 && !includedTypesHashSetRef.Contains(telemetryItemType))
            {
                return false;
            }

            return true;
        }
    }
}
