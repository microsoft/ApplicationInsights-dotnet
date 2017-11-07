using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.ConcurrentDatastructures;
using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Represents a zero- or multi-dimensional metric.<br />
    /// Contains convenience methods to track, aggregate and send values.<br />
    /// A <c>Metric</c> instance groups one or more <c>MetricSeries</c> that actually track and aggregate values along with
    /// naming and configuration attributes that identify the metric and define how it will be aggregated. 
    /// </summary>
    public sealed class Metric : IEquatable<Metric>
    {
        private const string NullMetricObjectId = "null";

        private static readonly char[] InvalidMetricChars = new char[] { '\0', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>', '=', ',' };
        
        private readonly string _objectId;
        private readonly int _hashCode;
        private readonly MetricSeries _zeroDimSeries;
        private readonly IReadOnlyList<KeyValuePair<string[], MetricSeries>> _zeroDimSeriesList;
        private readonly string[] _dimensionNames;

        //private readonly MultidimensionalCube<string, MetricSeries> _metricSeries;
        private readonly MultidimensionalCube2<MetricSeries> _metricSeries;

        internal readonly IMetricConfiguration _configuration;
        //internal readonly MetricManager _metricManager;
        private readonly MetricManager _metricManager;

        internal Metric(MetricManager metricManager, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration configuration)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            Util.ValidateNotNullOrWhitespace(metricId, nameof(metricId));

            int dimCount;
            EnsureDimensionNamesValid(out dimCount, ref dimension1Name, ref dimension2Name);

            EnsureConfigurationValid(dimCount, configuration);

            _metricManager = metricManager;

            MetricId = metricId;
            DimensionsCount = dimCount;
            _configuration = configuration;

            _objectId = GetObjectId(metricId, dimension1Name, dimension2Name);
            _hashCode = _objectId.GetHashCode();

            switch (dimCount)
            {
                case 0:
                    _dimensionNames = new string[0];
                    _metricSeries = null;
                    break;

                case 1:
                    _dimensionNames = new string[1] { dimension1Name };

                    //_metricSeries = new MultidimensionalCube<string, MetricSeries>(
                    //        totalPointsCountLimit:      configuration.SeriesCountLimit - 1,
                    //        pointsFactory:              CreateNewMetricSeries,
                    //        subdimensionsCountLimits:   new int[1] { configuration.ValuesPerDimensionLimit });

                    _metricSeries = new MultidimensionalCube2<MetricSeries>(
                            totalPointsCountLimit:      configuration.SeriesCountLimit - 1,
                            pointsFactory:              CreateNewMetricSeries,
                            dimensionValuesCountLimits: new int[1] { configuration.ValuesPerDimensionLimit });
                    break;

                case 2:
                    _dimensionNames = new string[2] { dimension1Name, dimension2Name };

                    //_metricSeries = new MultidimensionalCube<string, MetricSeries>(
                    //        totalPointsCountLimit:      configuration.SeriesCountLimit - 1,
                    //        pointsFactory:              CreateNewMetricSeries,
                    //        subdimensionsCountLimits:   new int[2] { configuration.ValuesPerDimensionLimit, configuration.ValuesPerDimensionLimit });

                    _metricSeries = new MultidimensionalCube2<MetricSeries>(
                            totalPointsCountLimit: configuration.SeriesCountLimit - 1,
                            pointsFactory: CreateNewMetricSeries,
                            dimensionValuesCountLimits: new int[2] { configuration.ValuesPerDimensionLimit, configuration.ValuesPerDimensionLimit });
                    break;

                default:
                    throw new Exception("Internal coding bug. Please report!");
            }

            _zeroDimSeries = CreateNewMetricSeries(dimensionValues: null);

            _zeroDimSeriesList = (dimCount == 0)
                    ? new KeyValuePair<string[], MetricSeries>[1] { new KeyValuePair<string[], MetricSeries>(new string[0], _zeroDimSeries) }
                    : null;
        }

        /// <summary>
        /// The ID (name) of this metric.
        /// </summary>
        public string MetricId { get; }

        /// <summary>
        /// The dimensionality of this metric.
        /// </summary>
        public int DimensionsCount { get; }

        /// <summary>
        /// The current number of metric series contained in this metric. 
        /// Each metric contains a special zero-dimension series, plus one series per unique dimension-values combination.
        /// </summary>
        public int SeriesCount { get { return 1 + (_metricSeries?.TotalPointsCount ?? 0); } }

        /// <summary>
        /// Gets the name of a dimension identified by the specified 1-based dimension index.
        /// </summary>
        /// <param name="dimensionNumber">1-based dimension number. Currently it can be <c>1</c> or <c>2</c>.</param>
        /// <returns>The name of the specified dimension.</returns>
        public string GetDimensionName(int dimensionNumber)
        {
            ValidateDimensionNumberForGetter(dimensionNumber);
            return _dimensionNames[dimensionNumber - 1];
        }

        /// <summary>
        /// Gets the values known for dimension identified by the specified 1-based dimension index.
        /// </summary>
        /// <param name="dimensionNumber">1-based dimension number. Currently it can be <c>1</c> or <c>2</c>.</param>
        /// <returns>The values known for the specified dimension.</returns>
        public IReadOnlyCollection<string> GetDimensionValues(int dimensionNumber)
        {
            ValidateDimensionNumberForGetter(dimensionNumber);

            int dimensionIndex = dimensionNumber - 1;
            return _metricSeries.GetDimensionValues(dimensionIndex);
        }
        
        /// <summary>
        /// Gets all metric series contained in this metric.
        /// Each metric contains a special zero-dimension series, plus one series per unique dimension-values combination.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
                                                "Microsoft.Design",
                                                "CA1024: Use properties where appropriate",
                                                Justification = "Completes with non-trivial effort. Method is approproiate.")]
        public IReadOnlyList<KeyValuePair<string[], MetricSeries>> GetAllSeries()
        {
            if (DimensionsCount == 0)
            {
                return _zeroDimSeriesList;
            }

            var series = new List<KeyValuePair<string[], MetricSeries>>(SeriesCount);
            series.Add(new KeyValuePair<string[], MetricSeries>(new string[0], _zeroDimSeries));
            _metricSeries.GetAllPoints(series);
            return series;
        }

        /// <summary>
        /// Gets a <c>MetricSeries</c> associated with this metric.<br />
        /// This overload gets the zero-dimensional <c>MetricSeries</c> associated with this metric.
        /// Every metric, regardless of its dimensionality, has such a zero-dimensional <c>MetricSeries</c>.
        /// </summary>
        /// <param name="series">Will be set to the zero-dimensional <c>MetricSeries</c> associated with this metric</param>
        /// <returns><c>True</c>.</returns>
        public bool TryGetDataSeries(out MetricSeries series)
        {
            series = _zeroDimSeries;
            return true;
        }

        /// <summary>
        /// Gets or creates the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// This overload may only be used with 1-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="series">If this method returns <c>True</c>: Will be set to the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// Otherwise: Will be set to <c>null</c>.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <returns><c>True</c> if the <c>MetricSeries</c> indicated by the specified dimension name could be retrieved (or created);
        /// <c>False</c> if the indicated series could not be retrieved or created because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value)
        {
            return TryGetDataSeries(out series, dimension1Value, createIfNotExists: true);
        }

        /// <summary>
        /// Gets or creates the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// This overload may only be used with 1-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="series">If this method returns <c>True</c>: Will be set to the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// Otherwise: Will be set to <c>null</c>.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="createIfNotExists">Whether to attempt creating a metric series for the specified dimension value if it does not exist.</param>
        /// <returns><c>True</c> if the <c>MetricSeries</c> indicated by the specified dimension name could be retrieved or created;
        /// <c>False</c> if the indicated series does not could not be retrieved or created because <c>createIfNotExists</c> is <c>false</c>
        /// or because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value, bool createIfNotExists)
        {
            series = GetMetricSeries(createIfNotExists, dimension1Value);
            return (series != null);
        }

        /// <summary>
        /// Gets or creates the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// This overload may only be used with 2-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="series">If this method returns <c>True</c>: Will be set to the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// Otherwise: Will be set to <c>null</c>.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <returns><c>True</c> if the <c>MetricSeries</c> indicated by the specified dimension name could be retrieved (or created);
        /// <c>False</c> if the indicated series could not be retrieved or created because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value, string dimension2Value)
        {
            return TryGetDataSeries(out series, dimension1Value, dimension2Value, createIfNotExists: true);
        }

        /// <summary>
        /// Gets or creates the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// This overload may only be used with 2-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="series">If this method returns <c>True</c>: Will be set to the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// Otherwise: Will be set to <c>null</c>.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <param name="createIfNotExists">Whether to attempt creating a metric series for the specified dimension values if it does not exist.</param>
        /// <returns><c>True</c> if the <c>MetricSeries</c> indicated by the specified dimension name could be retrieved or created;
        /// <c>False</c> if the indicated series does not could not be retrieved or created because <c>createIfNotExists</c> is <c>false</c>
        /// or because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value, string dimension2Value, bool createIfNotExists)
        {
            series = GetMetricSeries(createIfNotExists, dimension1Value, dimension2Value);
            return (series != null);
        }

        /// <summary>
        /// Tracks the specified value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.<br />
        /// This method uses the zero-dimensional <c>MetricSeries</c> associated with this metric.
        /// Use <c>TryTrackValue(..)</c> to track values into <c>MetricSeries</c> associated with specific dimension-values in multi-dimensional metrics.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        public void TrackValue(double metricValue)
        {
            _zeroDimSeries.TrackValue(metricValue);
        }

        /// <summary>
        /// Tracks the specified value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.<br />
        /// This method uses the zero-dimensional <c>MetricSeries</c> associated with this metric.
        /// Use <c>TryTrackValue(..)</c> to track values into <c>MetricSeries</c> associated with specific dimension-values in multi-dimensional metrics.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        public void TrackValue(object metricValue)
        {
            _zeroDimSeries.TrackValue(metricValue);
        }


        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 1-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryTrackValue(double metricValue, string dimension1Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension value.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 1-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryTrackValue(object metricValue, string dimension1Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 2-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryTrackValue(double metricValue, string dimension1Value, string dimension2Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value, dimension2Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// Tracks the specified value using the <c>MetricSeries</c> associated with the specified dimension values.<br />
        /// An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        /// This overload may only be used with 2-dimensional metrics. Use other overloads to specify a matching number of dimension values for this metric.
        /// </summary>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimension1Value">The value of the 1st dimension.</param>
        /// <param name="dimension2Value">The value of the 2nd dimension.</param>
        /// <returns><c>True</c> if the specified value was added to the <c>MetricSeries</c> indicated by the specified dimension name;
        /// <c>False</c> if the indicated series could not be created because a dimension cap or a metric series cap was reached.</returns>
        /// <exception cref="InvalidOperationException">If the number of specified dimension names does not match the dimensionality of this <c>Metric</c>.</exception>
        public bool TryTrackValue(object metricValue, string dimension1Value, string dimension2Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value, dimension2Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// Determines whether the specified object is a metric that is equal to this metric based on the respective metric IDs and
        /// the number and the names of dimensions.
        /// </summary>
        /// <param name="obj">Another object.</param>
        /// <returns>Whether the specified other metric is equal to this metric based on the respective metric IDs and the number and
        /// the names of dimensions.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Metric otherMetric = obj as Metric;
            return Equals(otherMetric);
        }

        /// <summary>
        /// Determines whether the specified other metric is equal to this metric based on the respective metric IDs and the number and 
        /// the names of dimensions.
        /// </summary>
        /// <param name="other">Another metric.</param>
        /// <returns>Whether the specified other metric is equal to this metric based on the respective metric IDs and the number and the
        /// names of dimensions.</returns>
        public bool Equals(Metric other)
        {
            if (other == null)
            {
                return false;
            }

            return _objectId.Equals(other._objectId);
        }

        /// <summary>
        /// Gets the hash code for this <c>Metric</c> instance.
        /// </summary>
        /// <returns>Hash code for this <c>Metric</c> instance.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        internal static string GetObjectId(string metricId, string dimension1Name, string dimension2Name)
        {
            Util.ValidateNotNull(metricId, nameof(metricId));
            ValidateInvalidChars(metricId, nameof(metricId));

            int dimensionCount;
            EnsureDimensionNamesValid(out dimensionCount, ref dimension1Name, ref dimension2Name);

            string metricObjectId;
            switch (dimensionCount)
            {
                case 1:
                    metricObjectId = $"{metricId}[{dimensionCount}](\"{dimension1Name}\")";
                    break;
                case 2:
                    metricObjectId = $"{metricId}[{dimensionCount}](\"{dimension1Name}\", \"{dimension2Name}\")";
                    break;
                default:
                    metricObjectId = $"{metricId}[{dimensionCount}]()";
                    break;
            }

            metricObjectId = metricObjectId.ToUpperInvariant();
            return metricObjectId;
        }

        private static void EnsureConfigurationValid(
                                    int dimensionCount,
                                    IMetricConfiguration configuration)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));
            Util.ValidateNotNull(configuration.SeriesConfig, nameof(configuration.SeriesConfig));

            if (dimensionCount > 0 && configuration.ValuesPerDimensionLimit < 1)
            {
                throw new ArgumentException("Multidimensional metrics must allow at least one dimension-value per dimesion"
                                         + $" (but {configuration.ValuesPerDimensionLimit} was specified"
                                         + $" in {nameof(configuration)}.{nameof(configuration.ValuesPerDimensionLimit)}).");
            }

            if (configuration.SeriesCountLimit < 1)
            {
                throw new ArgumentException("Metrics must allow at least one data series"
                                         + $" (but {configuration.SeriesCountLimit} was specified)"
                                         + $" in {nameof(configuration)}.{nameof(configuration.SeriesCountLimit)}).");
            }

            if (dimensionCount > 0 && configuration.SeriesCountLimit < 2)
            {
                throw new ArgumentException("Multidimensional metrics must allow at least two data series:"
                                         + " 1 for the basic (zero-dimensional) series and 1 additional series"
                                         + $" (but {configuration.SeriesCountLimit} was specified)"
                                         + $" in {nameof(configuration)}.{nameof(configuration.SeriesCountLimit)}).");
            }
        }

        private static void EnsureDimensionNamesValid(out int dimensionCount, ref string dimension1Name, ref string dimension2Name)
        {
            dimensionCount = 0;
            bool hasDim1 = (dimension1Name != null);
            bool hasDim2 = (dimension2Name != null);

            if (hasDim2)
            {
                if (hasDim1)
                {
                    dimensionCount = 2;
                    EnsureDimensionNameValid(ref dimension1Name, nameof(dimension1Name));
                }
                else
                {
                    throw new ArgumentException($"{nameof(dimension1Name)} may not be null (or white space) if {nameof(dimension2Name)} is present.");
                }

                EnsureDimensionNameValid(ref dimension2Name, nameof(dimension2Name));
            }
            else if (hasDim1)
            {
                dimensionCount = 1;

                EnsureDimensionNameValid(ref dimension1Name, nameof(dimension1Name));
            }
            else
            {
                dimensionCount = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureDimensionNameValid(ref string nameValue, string nameMoniker)
        {
            nameValue = nameValue.Trim();

            if (nameValue.Length == 0)
            {
                throw new ArgumentException($"{nameMoniker} may not be empty (or whitespace only). Dimension names may be 'null' to"
                                           + " indicate the absence of a dimension, but if present, they must contain at least 1 printable character.");
            }

            ValidateInvalidChars(nameValue, nameMoniker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateInvalidChars(string nameValue, string nameMoniker)
        {
            int pos = nameValue.IndexOfAny(InvalidMetricChars);
            if (pos >= 0)
            {
                throw new ArgumentException($"{nameMoniker} contains a disallowed character (\"{nameValue}\").");
            }
        }

        private void ValidateDimensionNumberForGetter(int dimensionNumber)
        {
            if (dimensionNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                                nameof(dimensionNumber),
                                $"{dimensionNumber} is an invalid {nameof(dimensionNumber)}. Note that {nameof(dimensionNumber)} is a 1-based index.");
            }

            if (dimensionNumber > 2)
            {
                throw new ArgumentOutOfRangeException(
                                nameof(dimensionNumber),
                                $"{dimensionNumber} is an invalid {nameof(dimensionNumber)}. Currently only {nameof(dimensionNumber)} = 1 or 2 are supported.");
            }

            if (DimensionsCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(dimensionNumber), "Cannot access demension becasue this metric has no dimensions.");
            }

            if (dimensionNumber > DimensionsCount)
            {
                throw new ArgumentOutOfRangeException($"Cannot access dimension for {nameof(dimensionNumber)}={dimensionNumber}"
                                                    + $" becasue this metric only has {DimensionsCount} dimensions.");
            }
        }

        private MetricSeries CreateNewMetricSeries(string[] dimensionValues)
        {
            KeyValuePair<string, string>[] dimValsNames = null;
            
            if (dimensionValues != null)
            {
                dimValsNames = new KeyValuePair<string, string>[dimensionValues.Length];

                for (int d = 0; d < dimensionValues.Length; d++)
                {
                    string dimensionName = _dimensionNames[d];
                    string dimensionValue = dimensionValues[d];

                    if (dimensionValue == null)
                    {
                        throw new ArgumentNullException($"The value for dimension number {d} is null.");
                    }

                    if (String.IsNullOrWhiteSpace(dimensionValue))
                    {
                        throw new ArgumentNullException($"The value for dimension number {d} is empty or white-space.");
                    }


                    dimValsNames[d] = new KeyValuePair<string, string>(dimensionName, dimensionValue);
                }
            }

            MetricSeries series = _metricManager.CreateNewSeries(MetricId, dimValsNames, _configuration.SeriesConfig);
            return series;
        }

        
        private MetricSeries GetMetricSeries(bool createIfNotExists, params string[] dimensionValues)
        {
            if (dimensionValues == null || dimensionValues.Length == 0)
            {
                return _zeroDimSeries;
            }

            if (DimensionsCount != dimensionValues.Length)
            {
                throw new InvalidOperationException($"Attempted to get a metric series by specifying {dimensionValues.Length} dimension(s),"
                                                  + $" but this metric has {DimensionsCount} dimensions.");
            }

            for (int d = 0; d < dimensionValues.Length; d++)
            {
                Util.ValidateNotNullOrWhitespace(dimensionValues[d], $"{nameof(dimensionValues)}[{d}]");
            }

            MultidimensionalPointResult<MetricSeries> result;
            if (createIfNotExists)
            {
                //Task<MultidimensionalPointResult<MetricSeries>> t = _metricSeries.TryGetOrCreatePointAsync(
                //                                                                                           _configuration.NewSeriesCreationRetryDelay,
                //                                                                                           _configuration.NewSeriesCreationTimeout,
                //                                                                                           CancellationToken.None,
                //                                                                                           dimensionValues);
                //result = t.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

                result = _metricSeries.TryGetOrCreatePoint(dimensionValues);
            }
            else
            {
                result = _metricSeries.TryGetPoint(dimensionValues);
            }

            return result.IsSuccess ? result.Point : null;
        }
    }
}