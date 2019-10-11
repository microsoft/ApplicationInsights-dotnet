namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using static System.FormattableString;

    /// <summary>Encapsulates the configuration for a metric and its respective data time series.</summary>
    public class MetricConfiguration : IEquatable<MetricConfiguration>
    {
        private readonly int hashCode;

        private readonly int[] valuesPerDimensionLimits = new int[MetricIdentifier.MaxDimensionsCount];

        private IEnumerable<string> unsafeDimCapFallbackDimensionValues = null;

        /// <summary>Creates a new instance of <c>MetricConfiguration</c>.</summary>
        /// <param name="seriesCountLimit">How many data time series a metric can contain as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="valuesPerDimensionLimit">How many different values each of the dimensions of a metric can
        /// have as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="seriesConfig">The configuration for how each series of this metric should be aggregated.</param>
        public MetricConfiguration(
                                int seriesCountLimit,
                                int valuesPerDimensionLimit,
                                IMetricSeriesConfiguration seriesConfig)
        {
            if (seriesCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(
                                                    nameof(seriesCountLimit),
                                                    Invariant($"Metrics must allow at least one data series (but {seriesCountLimit} was specified)."));
            }

            this.SeriesCountLimit = seriesCountLimit;

            if (valuesPerDimensionLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(valuesPerDimensionLimit));
            }

            for (int d = 0; d < this.valuesPerDimensionLimits.Length; d++)
            {
                this.valuesPerDimensionLimits[d] = valuesPerDimensionLimit;
            }

            Util.ValidateNotNull(seriesConfig, nameof(seriesConfig));
            this.SeriesConfig = seriesConfig;

            this.hashCode = this.ComputeHashCode();
        }

        /// <summary>Creates a new instance of <c>MetricConfiguration</c>.</summary>
        /// <param name="seriesCountLimit">How many data time series a metric can contain as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="valuesPerDimensionLimits">How many different values each of the dimensions of a metric can
        /// have as a maximum. If this enumeration contains less elements than the number of supported dimensions,
        /// then the last specified element is replicated for subsequent dimensions. If this enumeration contains
        /// too many elements, superfluous elements are ignored.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="seriesConfig">The configuration for how each series of this metric should be aggregated.</param>
        public MetricConfiguration(
                                int seriesCountLimit,
                                IEnumerable<int> valuesPerDimensionLimits,
                                IMetricSeriesConfiguration seriesConfig)
        {
            if (seriesCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(
                                                    nameof(seriesCountLimit),
                                                    Invariant($"Metrics must allow at least one data series (but {seriesCountLimit} was specified)."));
            }

            this.SeriesCountLimit = seriesCountLimit;

            if (valuesPerDimensionLimits == null)
            {
                throw new ArgumentNullException(nameof(valuesPerDimensionLimits));
            }

            int lastLim = 0, d = 0;
            foreach (int lim in valuesPerDimensionLimits)
            {
                lastLim = lim;

                if (lastLim < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(valuesPerDimensionLimits) + "[" + d + "]");
                }

                this.valuesPerDimensionLimits[d] = lastLim;

                d++;
                if (d >= MetricIdentifier.MaxDimensionsCount)
                {
                    break;
                }
            }

            for (; d < this.valuesPerDimensionLimits.Length; d++)
            {
                this.valuesPerDimensionLimits[d] = lastLim;
            }

            Util.ValidateNotNull(seriesConfig, nameof(seriesConfig));
            this.SeriesConfig = seriesConfig;

            this.hashCode = this.ComputeHashCode();
        }

        /// <summary>Gets how many data time series a metric can contain as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</summary>
        public int SeriesCountLimit { get; }

        /// <summary>Gets the configuration for how each series of this metric should be aggregated.</summary>
        public IMetricSeriesConfiguration SeriesConfig { get; }

        /// <summary>
        /// Gets the maximum number of distinct values for a dimension identified by the specified 1-based dimension index.
        /// </summary>
        /// <param name="dimensionNumber">1-based dimension number. Currently it can be <c>1</c>...<c>10</c>.</param>
        /// <returns>The maximum number of distinct values for the specified dimension.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233: Operations should not overflow", Justification = "No overflow")]
        public int GetValuesPerDimensionLimit(int dimensionNumber)
        {
            MetricIdentifier.ValidateDimensionNumberForGetter(dimensionNumber, MetricIdentifier.MaxDimensionsCount);

            int dimensionIndex = dimensionNumber - 1;
            return this.valuesPerDimensionLimits[dimensionIndex];
        }

        /// <summary>Gets whether tho objects describe identical configuration.</summary>
        /// <param name="obj">A configuration object.</param>
        /// <returns>Whether tho objects describe identical configuration.</returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricConfiguration;
                if (otherConfig != null)
                {
                    return this.Equals(otherConfig);
                }
            }

            return false;
        }

        /// <summary>Gets whether tho objects describe identical configuration.</summary>
        /// <param name="other">A configuration object.</param>
        /// <returns>Whether tho objects describe identical configuration.</returns>
        public virtual bool Equals(MetricConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            if ((this.SeriesCountLimit != other.SeriesCountLimit)
                    || (this.valuesPerDimensionLimits?.Length != other.valuesPerDimensionLimits?.Length)
                    || (false == this.GetType().Equals(other.GetType()))
                    || (false == this.SeriesConfig.Equals(other.SeriesConfig)))
            {
                return false;
            }

            if (this.valuesPerDimensionLimits == other.valuesPerDimensionLimits)
            {
                return true;
            }

            if (this.valuesPerDimensionLimits == null || other.valuesPerDimensionLimits == null)
            {
                return false;
            }

            for (int d = 0; d < this.valuesPerDimensionLimits.Length; d++)
            {
                if (this.valuesPerDimensionLimits[d] != other.valuesPerDimensionLimits[d])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Gets the Hash Code for this object.</summary>
        /// <returns>The Hash Code for this object.</returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        /// <summary>
        /// This is an unsafe API intended for internal SDK use only. Do not expose publicly!
        /// The APIs that offer access to metric series(e.g. <c>Metric.TryGetDataSeries(..)</c>, <c>Metric.TrackValue(..)</c>, etc.)
        /// return <c>false</c> if a limit for the number of series per dimension or for the entire metric has been reached.
        /// Those limits are set in the <c>MetricConfiguration</c> parameters (<c>valuesPerDimensionLimits</c> and 
        /// <c>seriesCountLimit</c>). The right way of dealing with cases where those limits are reached is domain-specific 
        /// and is left to the user.
        /// 
        /// The SDK has a feature for extracting pre-aggregated metrics from standard documents (e.g.Requests, Dependencies, ...).
        /// For those extracted metrics, for very specific dimensions, when a <c>valuesPerDimensionLimit</c> is reached, 
        /// we want to use a fallback dimension value(e.g. "Other").
        /// 
        /// This API allows specifying such a fallback dimension value for each dimension.
        /// 
        /// This API must be used only internally and only after thorough considerations.
        /// Note that dimension values are usually encountered in random order and so the values rolled into "Other" will vary
        /// across application instances, across service instances or across restarts of the same instance. 
        /// After such data is ingested from multiple instances and aggregated, a correct query (display) of metrics that have 
        /// used the fallback dimension value is highly problematic. 
        /// In a nutshell, if you see it, the values are likely wrong. :) 
        /// The SDK uses this fallback as a kind of "graceful" failure only for cases where the dimensions are low-cardinality 
        /// and are highly unlikely to reach the limit in practice.
        /// 
        /// Note that this API only addresses the per-dimension limits (<c>valuesPerDimensionLimits</c>), it does not address
        /// the metric-wide <c>seriesCountLimit</c>.
        /// </summary>
        /// <param name="dimCapFallbackDimensionValues">Specifies a fallback dimension name for each dimension (e.g. "Other"). 
        /// You can specify <c>null</c> for a dimension – in that case no fallback is used and when the number of values for 
        /// that dimension reaches the limit, no new metric series are created and no values are tracked for those specific 
        /// dimension values. If this enumeration contains more elements than the number of dimensions, the superfluous 
        /// elements are ignored; if this enumeration contains less values than the number of dimensions in a metric,
        /// <c>null</c> is assumed for uncovered dimensions. (Note that this is different from how <c>valuesPerDimensionLimits</c> 
        /// is treated.)</param>
        internal void SetUnsafeDimCapFallbackDimensionValues(IEnumerable<string> dimCapFallbackDimensionValues)
        {
            this.unsafeDimCapFallbackDimensionValues = dimCapFallbackDimensionValues;
        }

        /// <summary>
        /// This is an unsafe API intended for internal SDK use only. Do not expose publicly!
        /// See XML-Docs for <seealso cref="MetricConfiguration.SetUnsafeDimCapFallbackDimensionValues(IEnumerable{String})" />.
        /// </summary>
        /// <param name="dimCapFallbackDimensionValues">Specifies a fallback dimension name for each dimension (e.g. "Other").</param>
        internal void SetUnsafeDimCapFallbackDimensionValues(params string[] dimCapFallbackDimensionValues)
        {
            this.SetUnsafeDimCapFallbackDimensionValues((IEnumerable<string>)dimCapFallbackDimensionValues);
        }

        /// <summary>
        /// This is an unsafe API intended for internal SDK use only. Do not expose publicly!
        /// See XML-Docs for <seealso cref="MetricConfiguration.SetUnsafeDimCapFallbackDimensionValues(IEnumerable{String})" />.
        /// </summary>
        /// <param name="dimCapFallbackDimensionValues">Specifies a fallback dimension name for each dimension (e.g. "Other").</param>
        /// <returns>Whether fallback dimension names have been specified.</returns>
        internal bool TryGetUnsafeDimCapFallbackDimensionValues(out IEnumerable<string> dimCapFallbackDimensionValues)
        {
            dimCapFallbackDimensionValues = this.unsafeDimCapFallbackDimensionValues;
            return dimCapFallbackDimensionValues != null;
        }

        private int ComputeHashCode()
        {
            return Util.CombineHashCodes(
                                        this.SeriesCountLimit.GetHashCode(),
                                        Util.CombineHashCodes(this.valuesPerDimensionLimits),
                                        this.SeriesConfig.GetType().FullName.GetHashCode(),
                                        this.SeriesConfig.GetHashCode());
        }
    }
}
