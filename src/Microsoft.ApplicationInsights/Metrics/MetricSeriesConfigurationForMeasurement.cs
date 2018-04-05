namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    /// <summary>@ToDo: Complete documentation before stable release. {023}</summary>
    public class MetricSeriesConfigurationForMeasurement : IMetricSeriesConfiguration
    {
        private readonly bool restrictToUInt32Values;
        private readonly int hashCode;

        static MetricSeriesConfigurationForMeasurement()
        {
            MetricAggregateToTelemetryPipelineConverters.Registry.Add(
                                                                    typeof(ApplicationInsightsTelemetryPipeline),
                                                                    Constants.AggregateKindMoniker,
                                                                    new MeasurementAggregateToApplicationInsightsPipelineConverter());
        }

        /// <summary>@ToDo: Complete documentation before stable release. {650}</summary>
        /// <param name="restrictToUInt32Values">@ToDo: Complete documentation before stable release. {153}</param>
        public MetricSeriesConfigurationForMeasurement(bool restrictToUInt32Values)
        {
            this.restrictToUInt32Values = restrictToUInt32Values;

            this.hashCode = Util.CombineHashCodes(this.restrictToUInt32Values.GetHashCode());
        }

        /// <summary>Gets a value indicating whether @ToDo: Complete documentation before stable release. {612}</summary>
        public bool RequiresPersistentAggregation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return false; }
        }

        /// <summary>Gets a value indicating whether @ToDo: Complete documentation before stable release. {691}</summary>
        public bool RestrictToUInt32Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this.restrictToUInt32Values; }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {287}</summary>
        /// <param name="dataSeries">@ToDo: Complete documentation before stable release. {864}</param>
        /// <param name="aggregationCycleKind">@ToDo: Complete documentation before stable release. {203}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {983}</returns>
        public IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
        {
            IMetricSeriesAggregator aggregator = new MeasurementAggregator(this, dataSeries, aggregationCycleKind);
            return aggregator;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {894}</summary>
        /// <param name="obj">@ToDo: Complete documentation before stable release. {102}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {488}</returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricSeriesConfigurationForMeasurement;
                if (otherConfig != null)
                {
                    return this.Equals(otherConfig);
                }
            }

            return false;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {278}</summary>
        /// <param name="other">@ToDo: Complete documentation before stable release. {067}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {117}</returns>
        public bool Equals(IMetricSeriesConfiguration other)
        {
            return this.Equals((object)other);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {078}</summary>
        /// <param name="other">@ToDo: Complete documentation before stable release. {374}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {070}</returns>
        public bool Equals(MetricSeriesConfigurationForMeasurement other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.RestrictToUInt32Values == other.RestrictToUInt32Values;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {869}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {755}</returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        /// <summary>
        /// Defines constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
        /// </summary>
        public static class Constants
        {
            /// <summary>
            /// The kind moniker for aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
            /// </summary>
            public const string AggregateKindMoniker = "Microsoft.Azure.Measurement";

            /// <summary>
            /// Defines constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
            /// </summary>
            public static class AggregateKindDataKeys
            {
                /// <summary>
                /// The name of the Count field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public const string Count = "Count";

                /// <summary>
                /// The name of the Sum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public const string Sum = "Sum";

                /// <summary>
                /// The name of the Minimum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public const string Min = "Min";

                /// <summary>
                /// The name of the Maximum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public const string Max = "Max";

                /// <summary>
                /// The name of the Standard Deviation field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public const string StdDev = "StdDev";
            }
        }

        /// <summary>
        /// Groups constants used by metric aggregates produced by aggregators that are configured by metric configurations represented through
        /// instances of <see cref="MetricSeriesConfigurationForMeasurement"/>. This class cannot be instantiated. To access the constants, use the 
        /// extension method <c>MetricConfigurations.Common.Measurement().Constants()</c> or <see cref="MetricSeriesConfigurationForMeasurement.Constants"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class AggregateKindConstants
        {
            internal static readonly AggregateKindConstants Instance = new AggregateKindConstants();

            private AggregateKindConstants()
            {
            }

            /// <summary>
            /// Gets the kind moniker for aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
            /// </summary>
            public string AggregateKindMoniker
            {
                get { return Constants.AggregateKindMoniker; }
            }

            /// <summary>
            /// Gets constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
            /// </summary>
            public DataKeysConstants AggregateKindDataKeys
            {
                get { return DataKeysConstants.Instance; }
            }

            /// <summary>
            /// Defines constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
            /// </summary>
            public static class Constants
            {
                /// <summary>
                /// The kind moniker for aggregates produced by aggregators that are configured by metric configurations represented
                /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
                /// </summary>
                public const string AggregateKindMoniker = "Microsoft.Azure.Measurement";

                /// <summary>
                /// Defines constants used to refer to data fields contained within aggregates produced by aggregators that are configured
                /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
                /// </summary>
                public static class AggregateKindDataKeys
                {
                    /// <summary>
                    /// The name of the Count field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                    /// </summary>
                    public const string Count = "Count";

                    /// <summary>
                    /// The name of the Sum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                    /// </summary>
                    public const string Sum = "Sum";

                    /// <summary>
                    /// The name of the Minimum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                    /// </summary>
                    public const string Min = "Min";

                    /// <summary>
                    /// The name of the Maximum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                    /// </summary>
                    public const string Max = "Max";

                    /// <summary>
                    /// The name of the Standard Deviation field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                    /// </summary>
                    public const string StdDev = "StdDev";
                }
            }

            /// <summary>
            /// Groups constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>.
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class DataKeysConstants
            {
                internal static readonly DataKeysConstants Instance = new DataKeysConstants();

                private DataKeysConstants()
                {
                }

                /// <summary>
                /// Gets the name of the Count field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public string Count
                {
                    get { return Constants.AggregateKindDataKeys.Count; }
                }

                /// <summary>
                /// Gets the name of the Sum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public string Sum
                {
                    get { return Constants.AggregateKindDataKeys.Sum; }
                }

                /// <summary>
                /// Gets the name of the Minimum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public string Min
                {
                    get { return Constants.AggregateKindDataKeys.Min; }
                }

                /// <summary>
                /// Gets the name of the Maximum field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public string Max
                {
                    get { return Constants.AggregateKindDataKeys.Max; }
                }

                /// <summary>
                /// Gets the name of the Standard Deviation field in <see cref="MetricAggregate"/> objects produced by measurement aggregators.
                /// </summary>
                public string StdDev
                {
                    get { return Constants.AggregateKindDataKeys.StdDev; }
                }
            }
        }
    }
}
