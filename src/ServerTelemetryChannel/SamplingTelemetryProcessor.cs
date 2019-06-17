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
        private const string DependencyTelemetryName = "DEPENDENCY";
        private const string EventTelemetryName = "EVENT";
        private const string ExceptionTelemetryName = "EXCEPTION";
        private const string PageViewTelemetryName = "PAGEVIEW";
        private const string RequestTelemetryName = "REQUEST";
        private const string TraceTelemetryName = "TRACE";

        private readonly char[] listSeparators = { ';' };
        private readonly IDictionary<string, Type> allowedTypes;

        private readonly long[] proactivelySampledOutItems = new long[] { 0, 0, 0, 0, 0, 0, 0 };
        private readonly Dictionary<SamplingTelemetryItemTypes, int> typeToSamplingIndexMap = new Dictionary<SamplingTelemetryItemTypes, int>
        {
            { SamplingTelemetryItemTypes.Request, 1 },
            { SamplingTelemetryItemTypes.RemoteDependency, 2 },
            { SamplingTelemetryItemTypes.Exception, 3 },
            { SamplingTelemetryItemTypes.Event, 4 },
            { SamplingTelemetryItemTypes.PageView, 5 },
            { SamplingTelemetryItemTypes.Message, 6 },
        };

        private SamplingTelemetryItemTypes excludedTypesFlags;
        private string excludedTypesString;

        private SamplingTelemetryItemTypes includedTypesFlags;
        private string includedTypesString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingTelemetryProcessor"/> class.
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        /// </summary>
        public SamplingTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            this.SamplingPercentage = 100.0;
            this.SampledNext = next;
            this.UnsampledNext = next;
            
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
            this.UnsampledNext = unsampledNext ?? throw new ArgumentNullException(nameof(unsampledNext));
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

                SamplingTelemetryItemTypes newExcludedFlags = SamplingTelemetryItemTypes.None;

                if (!string.IsNullOrEmpty(value))
                {
                    string[] splitList = value.Split(this.listSeparators, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in splitList)
                    {
                        if (this.allowedTypes.ContainsKey(item))
                        {
                            switch (item.ToUpperInvariant())
                            {
                                case RequestTelemetryName:
                                    newExcludedFlags |= SamplingTelemetryItemTypes.Request;
                                    break;
                                case DependencyTelemetryName:
                                    newExcludedFlags |= SamplingTelemetryItemTypes.RemoteDependency;
                                    break;
                                case ExceptionTelemetryName:
                                    newExcludedFlags |= SamplingTelemetryItemTypes.Exception;
                                    break;
                                case PageViewTelemetryName:
                                    newExcludedFlags |= SamplingTelemetryItemTypes.PageView;
                                    break;
                                case TraceTelemetryName:
                                    newExcludedFlags |= SamplingTelemetryItemTypes.Message;
                                    break;
                                case EventTelemetryName:
                                    newExcludedFlags |= SamplingTelemetryItemTypes.Event;
                                    break;
                            }
                        }
                    }
                }

                this.excludedTypesFlags = newExcludedFlags;
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

                SamplingTelemetryItemTypes newIncludedFlags = SamplingTelemetryItemTypes.None;

                if (!string.IsNullOrEmpty(value))
                {
                    string[] splitList = value.Split(this.listSeparators, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in splitList)
                    {
                        if (this.allowedTypes.ContainsKey(item))
                        {
                            switch (item.ToUpperInvariant())
                            {
                                case RequestTelemetryName:
                                    newIncludedFlags |= SamplingTelemetryItemTypes.Request;
                                    break;
                                case DependencyTelemetryName:
                                    newIncludedFlags |= SamplingTelemetryItemTypes.RemoteDependency;
                                    break;
                                case ExceptionTelemetryName:
                                    newIncludedFlags |= SamplingTelemetryItemTypes.Exception;
                                    break;
                                case PageViewTelemetryName:
                                    newIncludedFlags |= SamplingTelemetryItemTypes.PageView;
                                    break;
                                case TraceTelemetryName:
                                    newIncludedFlags |= SamplingTelemetryItemTypes.Message;
                                    break;
                                case EventTelemetryName:
                                    newIncludedFlags |= SamplingTelemetryItemTypes.Event;
                                    break;
                            }
                        }
                    }
                }

                this.includedTypesFlags = newIncludedFlags;
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
            if (samplingPercentage >= 100.0 - 1.0E-12 && this.SampledNext.Equals(this.UnsampledNext))
            {
                this.SampledNext.Process(item);
                return;
            }

            //// So sampling rate is not 100%, or we must evaluate further

            //// If null was passed in as item or if sampling not supported in general, do nothing:
            var samplingSupportingTelemetry = item as ISupportSampling;
            var advancedSamplingSupportingTelemetry = item as ISupportAdvancedSampling;
            if (samplingSupportingTelemetry == null || advancedSamplingSupportingTelemetry == null)
            {
                this.UnsampledNext.Process(item);
                return;
            }

            //// If telemetry was excluded by type, do nothing:
            if (!this.IsSamplingApplicable(advancedSamplingSupportingTelemetry.ItemTypeFlag))
            {
                if (TelemetryChannelEventSource.IsVerboseEnabled)
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
                if (advancedSamplingSupportingTelemetry.IsProactivelySampledOut)
                {
                    this.AddProactivelySampledOutItems(advancedSamplingSupportingTelemetry.ItemTypeFlag, Convert.ToInt64(100 / samplingPercentage));

                    if (TelemetryChannelEventSource.IsVerboseEnabled)
                    {
                        TelemetryChannelEventSource.Log.ItemSampledOut(item.ToString());
                    }
                }
                else
                {
                    var proactivelySampledOutItemsCount = this.GetProactivelySampledOutItems(advancedSamplingSupportingTelemetry.ItemTypeFlag);
                    if (proactivelySampledOutItemsCount > 0)
                    {
                        samplingSupportingTelemetry.SamplingPercentage = (100 * samplingSupportingTelemetry.SamplingPercentage) / (100 + (proactivelySampledOutItemsCount * samplingSupportingTelemetry.SamplingPercentage));
                        this.ClearProactivelySampledOutItems(advancedSamplingSupportingTelemetry.ItemTypeFlag);
                    }

                    this.SampledNext.Process(item);
                }
            }
            else
            { 
                if (TelemetryChannelEventSource.IsVerboseEnabled)
                {
                    TelemetryChannelEventSource.Log.ItemSampledOut(item.ToString());
                }

                TelemetryDebugWriter.WriteTelemetry(item, nameof(SamplingTelemetryProcessor));
            }
        }

        private bool IsSamplingApplicable(SamplingTelemetryItemTypes telemetryItemTypeFlag)
        {
            if (this.excludedTypesFlags.HasFlag(telemetryItemTypeFlag))
            {
                return false;
            }

            if (this.includedTypesFlags != SamplingTelemetryItemTypes.None && !this.includedTypesFlags.HasFlag(telemetryItemTypeFlag))
            {
                return false;
            }

            return true;
        }

        private void AddProactivelySampledOutItems(SamplingTelemetryItemTypes telemetryItemTypeFlag, long value)
        {
            int typeIndex;
            this.typeToSamplingIndexMap.TryGetValue(telemetryItemTypeFlag, out typeIndex);
            Interlocked.Add(ref this.proactivelySampledOutItems[typeIndex], value);
        }

        private void ClearProactivelySampledOutItems(SamplingTelemetryItemTypes telemetryItemTypeFlag)
        {
            int typeIndex;
            this.typeToSamplingIndexMap.TryGetValue(telemetryItemTypeFlag, out typeIndex);
            Interlocked.Exchange(ref this.proactivelySampledOutItems[typeIndex], 0);
        }

        private long GetProactivelySampledOutItems(SamplingTelemetryItemTypes telemetryItemTypeFlag)
        {
            int typeIndex;
            this.typeToSamplingIndexMap.TryGetValue(telemetryItemTypeFlag, out typeIndex);
            return Volatile.Read(ref this.proactivelySampledOutItems[typeIndex]);
        }
    }
}
