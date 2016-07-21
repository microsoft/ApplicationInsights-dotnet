namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
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
        /// Gets or sets the next TelemetryProcessor in call chain.
        /// </summary>
        private ITelemetryProcessor Next { get; set; }

        /// <summary>
        /// Process a collected telemetry item.
        /// </summary>
        /// <param name="item">A collected Telemetry item.</param>
        public void Process(ITelemetry item)
        {
            if (this.SamplingPercentage < 100.0 - 1.0E-12)
            {
                var samplingSupportingTelemetry = item as ISupportSampling;

                if (samplingSupportingTelemetry != null)
                {
                    var excludedTypesHashSetRef = this.excludedTypesHashSet;
                    var includedTypesHashSetRef = this.includedTypesHashSet;

                    if (excludedTypesHashSetRef.Count > 0 && excludedTypesHashSetRef.Contains(item.GetType()))
                    {
                        if (TelemetryChannelEventSource.Log.IsVerboseEnabled)
                        {
                            TelemetryChannelEventSource.Log.SamplingSkippedByType(item.ToString());
                        }
                    }
                    else if (includedTypesHashSetRef.Count > 0 && !includedTypesHashSetRef.Contains(item.GetType()))
                    {
                        if (TelemetryChannelEventSource.Log.IsVerboseEnabled)
                        {
                            TelemetryChannelEventSource.Log.SamplingSkippedByType(item.ToString());
                        }
                    }
                    else if (!samplingSupportingTelemetry.SamplingPercentage.HasValue)
                    {
                        samplingSupportingTelemetry.SamplingPercentage = this.SamplingPercentage;

                        if (!this.IsSampledIn(item))
                        {
                            if (TelemetryChannelEventSource.Log.IsVerboseEnabled)
                            {
                                TelemetryChannelEventSource.Log.ItemSampledOut(item.ToString());
                            }

                            TelemetryDebugWriter.WriteTelemetry(item, this.GetType().Name);
                            return;
                        }
                    }
                }
            }

            this.Next.Process(item);
        }

        private bool IsSampledIn(ITelemetry telemetry)
        {
            // check if telemetry supports sampling
            var samplingSupportingTelemetry = telemetry as ISupportSampling;

            if (samplingSupportingTelemetry == null)
            {
                return true;
            }

            // check sampling < 100% is specified
            if (samplingSupportingTelemetry.SamplingPercentage >= 100.0)
            {
                return true;
            }
            
            return SamplingScoreGenerator.GetSamplingScore(telemetry) < samplingSupportingTelemetry.SamplingPercentage;
        }
    }
}
