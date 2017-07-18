using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public class Metric : IEquatable<Metric>, IComparable<Metric>
    {
        private const string NullMetricObjectId = "null";

        private static void EnsureConfigurationValid(int dimensionCount,
                                                     IMetricConfiguration configuration)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));
            Util.ValidateNotNull(configuration.SeriesConfig, nameof(configuration.SeriesConfig));

            if (configuration.ValuesPerDimensionLimit < 1)
            {
                throw new ArgumentException("Multidimensional metrics must allow at least one dimension-value per dimesion"
                                         + $" (but {configuration.ValuesPerDimensionLimit} was specified).",
                                            nameof(configuration.ValuesPerDimensionLimit));
            }

            if (configuration.SeriesCountLimit < 1)
            {
                throw new ArgumentException("Metrics must allow at least one data series"
                                         + $" (but { configuration.SeriesCountLimit } was specified).",
                                            nameof(configuration.SeriesCountLimit));
            }

            if (dimensionCount > 0 && configuration.SeriesCountLimit < 2)
            {
                throw new ArgumentException("Multidimensional metrics must allow at least two data series:"
                                         + " 1 for the basic (zero-dimensional) series and 1 additional series"
                                         + $" (but { configuration.SeriesCountLimit } was specified).",
                                            nameof(configuration.SeriesCountLimit));
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
                }
                else
                {
                    throw new ArgumentException($"{nameof(dimension1Name)} may not be null (or white space) if {dimension2Name} is present.");
                }

                dimension2Name = dimension2Name.Trim();
                if (dimension2Name.Length == 0)
                {
                    throw new ArgumentException($"{nameof(dimension2Name)} may not be empty (or whitespace only). Dimension names may be 'null' to"
                                               + " indicate the absence of a dimension, but if present, they must contain at least 1 printable character.");
                }
            }
            else if (hasDim1)
            {
                dimensionCount = 1;

                dimension1Name = dimension1Name.Trim();
                if (dimension1Name.Length == 0)
                {
                    throw new ArgumentException($"{nameof(dimension1Name)} may not be empty (or whitespace only). Dimension names may be 'null' to"
                                               + " indicate the absence of a dimension, but if present, they must contain at least 1 printable character.");
                }
            }
            else
            {
                dimensionCount = 0;
            }
        }

        internal static string GetObjectId(string metricId, string dimension1Name, string dimension2Name)
        {
            Util.ValidateNotNull(metricId, nameof(metricId));

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


        private readonly MetricManager _metricManager;
        private readonly string _objectId;
        private readonly int _hashCode;
        private readonly MetricSeries _zeroDimSeries;
        private readonly MultidimensionalCube<string, MetricSeries> _metricSeries;
        private readonly string[] _dimensionNames;


        /// <summary>
        /// 
        /// </summary>
        public string MetricId { get; }

        /// <summary>
        /// 
        /// </summary>
        public int DimensionsCount { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Dimension1Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Dimension2Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public IMetricConfiguration Configuration { get; }

        /// <summary>
        /// 
        /// </summary>
        public int SeriesCount { get { return 1 + (_metricSeries?.TotalPointsCount ?? 0); } }

        internal Metric(MetricManager metricManager, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration configuration)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            Util.ValidateNotNullOrWhitespace(metricId, nameof(metricId));

            int dimCount;
            EnsureDimensionNamesValid(out dimCount, ref dimension1Name, ref dimension2Name);

            EnsureConfigurationValid(dimCount, configuration);

            _metricManager = metricManager;

            MetricId = metricId.Trim();
            DimensionsCount = dimCount;
            Dimension1Name = dimension1Name;
            Dimension2Name = dimension2Name;
            Configuration = configuration;

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
                    _metricSeries = new MultidimensionalCube<string, MetricSeries>(
                            pointsFactory:              CreateNewMetricSeries,
                            dimensionValuesCountLimits: new int[1] { configuration.ValuesPerDimensionLimit });
                    break;

                case 2:
                    _dimensionNames = new string[2] { dimension1Name, dimension2Name };
                    _metricSeries = new MultidimensionalCube<string, MetricSeries>(
                            pointsFactory:              CreateNewMetricSeries,
                            dimensionValuesCountLimits: new int[2] { configuration.ValuesPerDimensionLimit, configuration.ValuesPerDimensionLimit });
                    break;

                default:
                    throw new Exception("Internal coding bug. Please report!");
            }

            _zeroDimSeries = CreateNewMetricSeries(dimensionValues: null);
            
        }

        private MetricSeries CreateNewMetricSeries(string[] dimensionValues)
        {
            MetricSeries series = _metricManager.CreateNewSeries(MetricId, Configuration.SeriesConfig);
            if (dimensionValues != null)
            {
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

                    series.Context.Properties[dimensionName] = dimensionValue;
                }
            }

            return series;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<KeyValuePair<string[], MetricSeries>> GetAllSeries()
        {

            var series = new List<KeyValuePair<string[], MetricSeries>>(SeriesCount);
            series.Add(new KeyValuePair<string[], MetricSeries>(new string[0], _zeroDimSeries));
            _metricSeries.GetAllPoints(series);
            return series;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        public bool TryGetDataSeries(out MetricSeries series)
        {
            series = _zeroDimSeries;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="dimension1Value"></param>
        /// <returns></returns>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value)
        {
            return TryGetDataSeries(out series, dimension1Value, createIfNotExists: true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="dimension1Value"></param>
        /// <param name="createIfNotExists"></param>
        /// <returns></returns>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value, bool createIfNotExists)
        {
            series = GetMetricSeries(createIfNotExists, dimension1Value);
            return (series != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="dimension1Value"></param>
        /// <param name="dimension2Value"></param>
        /// <returns></returns>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value, string dimension2Value)
        {
            return TryGetDataSeries(out series, dimension1Value, dimension2Value, createIfNotExists: true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="dimension1Value"></param>
        /// <param name="dimension2Value"></param>
        /// <param name="createIfNotExists"></param>
        /// <returns></returns>
        public bool TryGetDataSeries(out MetricSeries series, string dimension1Value, string dimension2Value, bool createIfNotExists)
        {
            series = GetMetricSeries(createIfNotExists, dimension1Value, dimension2Value);
            return (series != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>

        public void TrackValue(uint metricValue)
        {
            _zeroDimSeries.TrackValue(metricValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        public void TrackValue(double metricValue)
        {
            _zeroDimSeries.TrackValue(metricValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        public void TrackValue(object metricValue)
        {
            _zeroDimSeries.TrackValue(metricValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        /// <param name="dimension1Value"></param>
        /// <returns></returns>
        public bool TryTrackValue(uint metricValue, string dimension1Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        /// <param name="dimension1Value"></param>
        /// <returns></returns>
        public bool TryTrackValue(double metricValue, string dimension1Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        /// <param name="dimension1Value"></param>
        /// <returns></returns>
        public bool TryTrackValue(object metricValue, string dimension1Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        /// <param name="dimension1Value"></param>
        /// <param name="dimension2Value"></param>
        /// <returns></returns>
        public bool TryTrackValue(uint metricValue, string dimension1Value, string dimension2Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value, dimension2Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        /// <param name="dimension1Value"></param>
        /// <param name="dimension2Value"></param>
        /// <returns></returns>
        public bool TryTrackValue(double metricValue, string dimension1Value, string dimension2Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value, dimension2Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        /// <param name="dimension1Value"></param>
        /// <param name="dimension2Value"></param>
        /// <returns></returns>
        public bool TryTrackValue(object metricValue, string dimension1Value, string dimension2Value)
        {
            MetricSeries series = GetMetricSeries(true, dimension1Value, dimension2Value);
            series?.TrackValue(metricValue);
            return (series != null);
        }

        private MetricSeries GetMetricSeries(bool createIfNotExists, params string[] dimensionValues)
        {
            if (dimensionValues == null || dimensionValues.Length == 0)
            {
                return _zeroDimSeries;
            }

            if (DimensionsCount != 1)
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
                Task<MultidimensionalPointResult<MetricSeries>> t = _metricSeries.TryGetOrCreatePointAsync(Configuration.NewSeriesCreationTimeout,
                                                                                                           CancellationToken.None,
                                                                                                           Configuration.NewSeriesCreationRetryDelay,
                                                                                                           dimensionValues);
                result = t.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
            }
            else
            {
                result = _metricSeries.TryGetPoint(dimensionValues);
            }
            return result.IsSuccess ? result.Point : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Metric other)
        {
            if (other == null)
            {
                return false;
            }

            return _objectId.Equals(other._objectId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        int IComparable<Metric>.CompareTo(Metric other)
        {
            return _objectId.CompareTo(other?._objectId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}