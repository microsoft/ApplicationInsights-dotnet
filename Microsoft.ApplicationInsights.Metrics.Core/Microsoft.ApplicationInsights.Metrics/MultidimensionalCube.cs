using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics
{

    /// <summary>
    /// Represents a multi-dimensional, discrete data cube.
    /// An N-dimensional discrete cube is a data structure containing elements of type TPoint.
    /// Each point can be addressed using a coordinate-vector containing N entries of type TDimensionValue.
    /// <br />
    /// For example, the elements in the cube may be colors, space vectors or a sold item (with its properties as coordinates).
    /// However, the key usage in this library are multidimensional metrics. In such usage the coordinates are metric
    /// dimensions (TDimensionValue is String) and the cube elements (TPoint) are metric data series.
    /// <p>
    /// The cube refers to dimensions using their index: 0th, 1st, 2nd dimension and so on. For each dimension, the cube has a <c>DimensionValuesCountLimit</c>
    /// parameter. This limits the number of distinct values that may be specified for that specific dimension. Once the <c>DimensionValuesCountLimit</c> for
    /// some particular dimension is reached, the cube will no longer be able to create points whose coordinate vectors contains values for that particular
    /// dimension that do not occur in coordinate vectors for points already in the cube. The cube also has a <c>TotalPointsCountLimit</c> parameter that limits
    /// the total number of points in the cube.
    /// </p>
    /// <p>
    /// The elements of the coordinate vectors, i.e.the values of the dimensions of a point in the cube are discrete. Even if their type implies a non-discrete
    /// space (e.g.when TDimensionValue is Double) they are always treated as discrete and separate.
    /// </p>
    /// <p>
    /// This implementation assumes that creation and storage of TPoint-elements is resource intensive. It creates points lazily, only when requested.
    /// However, to minimize locking it uses pessimistic and optimistic concurrently mechanisms where possible. Two artefacts occur as a result:
    /// </p>
    /// <list type="bullet">
    ///   <item><description>The specified <c>PointsFactory</c> delegate may be executed more than once for a particular coordinates vector.
    ///     However, once a point for the specified coordinates-vector has been actually returned to the caller, always the same instance of
    ///     that point will be returned by the cube. This behaviour is consistent with the ConcurrentDictionary in the .NET Framework.</description></item>
    ///   <item><description>The <c>TryGetOrCreatePoint(..)</c> may return <c>false</c>, and then return <c>true</c> moments later when called with the
    ///     same parameters. This is becasue in order to avoid locking the cube pre-books dimension value counts (and total points counts) and later
    ///     frees them up if the creation of a new point did not complete.
    ///     Notably, this artefact does not represent any probems in practice: It occurs only in cuncurrent races when the number of values of a
    ///     dimension (or the total number of points) is close to the limit, where applications should not rely on a particular outcome of adding a
    ///     point anyway. In common cases one can assume that the result of <c>TryGetOrCreatePoint(..)</c> is, indeed, stable.
    ///     In order to control potential instability use <c>TryGetOrCreatePointAsync(..)</c> overloads.
    ///     Note, however, that those overloads do not guarantee that the result of requesting a new point is completely stable. They merely make it
    ///     very unlikely for it to change by re-trying the oprtation several times.
    ///     Nevertheless,  </description></item>
    /// </list>
    /// <p>
    /// The cube is designed to work in concurrent scenarios while minimizing the number, the duration and the scope of locks taken. However, some locks are
    /// necessary to correctly impose the DimensionValuesCountLimits and the TotalPointsCountLimit constrains.
    /// </p>
    /// </summary>
    /// <remarks>
    /// <p>** Color Example: **</p>
    /// <p>
    /// An example of a cube is an RGB color space.
    /// Assume that we use 8 bits (= 256 values) per color and that is a color class such as this:
    /// </p>
    /// <code>
    /// class Color
    /// {
    ///     public Color(byte red, byte green, byte blue) { }
    ///     //...
    /// }
    /// </code>
    /// <p>
    /// It is possible to represent the entire space using a cube as follows:
    /// </p>
    /// <code>
    /// Func{int[], Color} colorFactory = (rgbValues) =} new Color(red: rgbValues[0], green: rgbValues[1], blue: rgbValues[2]);
    /// MultidimensionalCube{int, Color} colorCube = new MultidimensionalCube{int, Color}(colorFactory, 256, 256, 256);
    /// </code>
    /// <p>
    /// We can now address specific color objects we follows:
    /// </p>
    /// <code>
    /// MultidimensionalPointResult{Color} greenResult = colorCube.TryGetOrCreatePoint(0, 255, 0);
    /// Color green = greenResult.Point;
    /// 
    /// MultidimensionalPointResult{Color} yellowResult = colorCube.TryGetOrCreatePoint(255, 255, 0);
    /// Color yellow = yellowResult.Point;
    /// 
    /// MultidimensionalPointResult{Color} azureResult = colorCube.TryGetOrCreatePoint(0, 127, 255);
    /// Color azure = azureResult.Point;
    /// </code>
    /// 
    /// <p>** Metrics Example: **</p>
    /// <p>
    /// A multi-dimensional cube is used to address data time series of a multidimensional metric.
    /// Consider that we have a metric representing the number of milliseconds required to process a request to a web-service.
    /// We collect that metric according to a number of possible groupings:
    /// </p>
    /// <list type="number">
    ///   <item><description>By the type of the request(the string “synthetic” (i.e.created by a test) or “organic” (created by a legitimate client).</description></item>
    ///   <item><description>By the response generated(an HTTP response code encoded as a string).</description></item>
    ///   <item><description>By the name of the instance that executed the request(in case of a web service scaled over multiple instances).</description></item>
    ///   <item><description>By the name of the API invoked(any URL string).</description></item>
    /// </list>
    /// <p>
    /// Each view represents a separate dimension. There is a distinct data time series for each combination of dimension-values. For example:
    /// </p>
    /// <list type="bullet">
    ///   <item><description>Type=<c>"Organic"</c>, Response=<c>"200"</c>, Instance=<c>"Instance_01"</c>, API=<c>"http://myservice.com/API1?paramA=X"</c>.</description></item>
    ///   <item><description>Type=<c>"Organic"</c>, Response=<c>"500"</c>, Instance=<c>"Instance_01"</c>, API=<c>"http://myservice.com/API2?paramB=Y"</c>.</description></item>
    /// </list>
    /// <p>
    /// So a data series representation might look like this:
    /// </p>
    /// <code>
    /// class ResponseTimeMetricSeries
    /// {
    ///     public ResponseTimeMetricSeries(string kind, string responseCode, string instanceName, string url) { }
    ///     //...
    /// }
    /// </code>
    /// <p>
    /// Note also that while some dimensions can naturally take a small amount of values(e.g.Type can only take 2 values in this example), URL can essentially
    /// take an unbounded number of values.In our example we want to limit the number of dimension values for specific dimensions in order to control resource
    /// usage. We can create a cube as follows.Note how we used different max dimension values counts that are appropriate for each respective dimension.
    /// </p>
    /// <code>
    /// Func{string[], ResponseTimeMetricSeries} metricFactory = (dimValues) =} new ResponseTimeMetricSeries(kind:         dimValues[0],
    ///                                                                                                      responseCode: dimValues[1],
    ///                                                                                                      instanceName: dimValues[2],
    ///                                                                                                              url:          dimValues[3]);
    /// MultidimensionalCube{string, ResponseTimeMetricSeries} metricsCube = new MultidimensionalCube{string, ResponseTimeMetricSeries}(metricFactory, 2, 50, 100, 1000);
    /// 
    /// MultidimensionalPointResult{ResponseTimeMetricSeries} result;
    /// 
    /// result = metricsCube.TryGetOrCreatePoint(out responseSeries01, "Organic", "200", "Instance_01", "http://myservice.com/API1?paramA=X");
    /// if (! result.Success)
    /// {
    ///     throw new SomeAppropriateException("Cannot create metric data series. Dimension cap is potentially reached.");
    /// }
    /// 
    /// ResponseTimeMetricDataSeries responseSeries01 = result.Point;
    /// 
    /// 
    /// result = metricsCube.TryGetOrCreatePoint(out responseSeries01, "Organic", "500", "Instance_01", "http://myservice.com/API2?paramB=Y");
    /// if (! result.Success)
    /// {
    ///     throw new SomeAppropriateException("Cannot create metric data series. Dimension cap is potentially reached.");
    /// }
    /// 
    /// ResponseTimeMetricSeries responseSeries02 = result.Point;
    /// </code>
    /// </remarks>
    /// <typeparam name="TDimensionValue"></typeparam>
    /// <typeparam name="TPoint"></typeparam>
    public class MultidimensionalCube<TDimensionValue, TPoint>
    {
        /// <summary>
        /// We are using a recursive implementation for points creation so we are limiting the max dimension count to prevent strack overflow.
        /// In practice this is unlikely to be ever reached.
        /// If it nevertheless becomes an issue, we can change the implementation to be iterative and increase this limit.
        /// </summary>
        private const int DimensionsCountLimit = 50;

        private readonly int[] _dimensionValuesCountLimits;
        private readonly MultidimensionalCubeDimension<TDimensionValue, TPoint> _points;
        private readonly Func<TDimensionValue[], TPoint> _pointsFactory;
        private readonly int _totalPointsCountLimit;

        private int _totalPointsCount;


        /// <summary>
        /// 
        /// </summary>
        public int DimensionsCount { get { return _dimensionValuesCountLimits.Length; } }

        /// <summary>
        /// 
        /// </summary>
        public int TotalPointsCountLimit { get { return _totalPointsCountLimit; } }

        /// <summary>
        /// 
        /// </summary>
        public int TotalPointsCount { get { return Volatile.Read(ref _totalPointsCount); } }

        /// <summary>
        /// 
        /// </summary>
        internal Func<TDimensionValue[], TPoint> PointsFactory { get { return _pointsFactory; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pointsFactory"></param>
        /// <param name="dimensionValuesCountLimits"></param>
        public MultidimensionalCube(Func<TDimensionValue[], TPoint> pointsFactory, IEnumerable<int> dimensionValuesCountLimits)
            : this(Int32.MaxValue, pointsFactory, dimensionValuesCountLimits)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalPointsCountLimit"></param>
        /// <param name="pointsFactory"></param>
        /// <param name="dimensionValuesCountLimits"></param>
        public MultidimensionalCube(int totalPointsCountLimit, Func<TDimensionValue[], TPoint> pointsFactory, IEnumerable<int> dimensionValuesCountLimits)
            : this(totalPointsCountLimit, pointsFactory, dimensionValuesCountLimits?.ToArray())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pointsFactory"></param>
        /// <param name="dimensionValuesCountLimits"></param>
        public MultidimensionalCube(Func<TDimensionValue[], TPoint> pointsFactory, params int[] dimensionValuesCountLimits)
            : this(Int32.MaxValue, pointsFactory, dimensionValuesCountLimits)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalPointsCountLimit"></param>
        /// <param name="pointsFactory"></param>
        /// <param name="dimensionValuesCountLimits"></param>
        public MultidimensionalCube(int totalPointsCountLimit, Func<TDimensionValue[], TPoint> pointsFactory, params int[] dimensionValuesCountLimits)
        {
            if (totalPointsCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(totalPointsCountLimit), $"{nameof(totalPointsCountLimit)} must be 1 or larger. Typically much larger.");
            }

            Util.ValidateNotNull(pointsFactory, nameof(pointsFactory));

            Util.ValidateNotNull(dimensionValuesCountLimits, nameof(dimensionValuesCountLimits));

            if (dimensionValuesCountLimits.Length == 0)
            {
                throw new ArgumentException("Cube must have 1 or more dimensions.", nameof(dimensionValuesCountLimits));
            }

            if (dimensionValuesCountLimits.Length > DimensionsCountLimit)
            {
                throw new ArgumentException($"Cube may not have more than ${MultidimensionalCube<TDimensionValue, TPoint>.DimensionsCountLimit} dimensions,"
                                          + $" but {dimensionValuesCountLimits.Length} dimensions were specified.");
            }

            for (int d = 0; d < dimensionValuesCountLimits.Length; d++)
            {
                if (dimensionValuesCountLimits[d] < 1)
                {
                    throw new ArgumentException($"The limit of distinct dimension values must be 1 or larger, but the limit specified for dimension {d} is {dimensionValuesCountLimits[d]}.");
                }
            }

            _totalPointsCountLimit = totalPointsCountLimit;

            _dimensionValuesCountLimits = dimensionValuesCountLimits;
            _points = new MultidimensionalCubeDimension<TDimensionValue, TPoint>(this, dimensionValuesCountLimits[0], dimensionValuesCountLimits.Length == 1);
            _pointsFactory = pointsFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public int GetDimensionValuesCountLimit(int dimension)
        {
            return _dimensionValuesCountLimits[dimension];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal bool TryIncTotalPointsCount()
        {
            int newTotalPointsCount = Interlocked.Increment(ref _totalPointsCount);

            if (newTotalPointsCount <= _totalPointsCountLimit)
            {
                return true;
            }

            Interlocked.Decrement(ref _totalPointsCount);
            return false;
        }

        internal int DecTotalPointsCount()
        {
            int newTotalPointsCount = Interlocked.Decrement(ref _totalPointsCount);
            return newTotalPointsCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<KeyValuePair<TDimensionValue[], TPoint>> GetAllPoints()
        {
            var vectors = new List<KeyValuePair<TDimensionValue[], TPoint>>();
            GetAllPoints(vectors);
            return vectors;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pointContainer"></param>
        public void GetAllPoints(IList<KeyValuePair<TDimensionValue[], TPoint>> pointContainer)
        {
            var vectors = new List<KeyValuePair<TDimensionValue[], TPoint>>();

            IReadOnlyCollection<KeyValuePair<IList<TDimensionValue>, TPoint>> reversedVectors = _points.GetAllPointsReversed();
            foreach (KeyValuePair<IList<TDimensionValue>, TPoint> rv in reversedVectors)
            {
                var v = new KeyValuePair<TDimensionValue[], TPoint>(new TDimensionValue[rv.Key.Count], rv.Value);
                int lastI = rv.Key.Count - 1;
                for (int i = lastI; i >= 0; i--)
                {
                    v.Key[lastI - i] = rv.Key[i];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public MultidimensionalPointResult<TPoint> TryGetOrCreatePoint(params TDimensionValue[] coordinates)
        {
            MultidimensionalPointResult<TPoint> result = _points.TryGetOrAddVector(coordinates);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public MultidimensionalPointResult<TPoint> TryGetPoint(params TDimensionValue[] coordinates)
        {
            MultidimensionalPointResult<TPoint> result = _points.TryGetVector(coordinates);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public Task<MultidimensionalPointResult<TPoint>> TryGetOrCreatePointAsync(params TDimensionValue[] coordinates)
        {
            return TryGetOrCreatePointAsync(timeout:        TimeSpan.FromMilliseconds(11),
                                            cancelToken:    CancellationToken.None,
                                            sleepDuration:  TimeSpan.FromMilliseconds(2),
                                            coordinates:    coordinates);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancelToken"></param>
        /// <param name="sleepDuration"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public async Task<MultidimensionalPointResult<TPoint>> TryGetOrCreatePointAsync(TimeSpan timeout, CancellationToken cancelToken, TimeSpan sleepDuration, params TDimensionValue[] coordinates)
        {
            MultidimensionalPointResult<TPoint> result = this.TryGetOrCreatePoint(coordinates);
            if (result.IsSuccess)
            {
                return result;
            }

            bool infiniteTimeout = (timeout == Timeout.InfiniteTimeSpan);

            if (! infiniteTimeout)
            { 
                if (Math.Round(timeout.TotalMilliseconds) >= (double) Int32.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout), $"{nameof(timeout)} must be smaller than {Int32.MaxValue} msec.");
                }

                if (Math.Round(timeout.TotalMilliseconds) < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout), $"{nameof(timeout)} must be positive or Infinite.");
                }
            }

            if (Math.Round(sleepDuration.TotalMilliseconds) > (double) Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(sleepDuration), $"{nameof(sleepDuration)} must be smaller than {Int32.MaxValue} msec.");
            }

            if (Math.Round(sleepDuration.TotalMilliseconds) < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sleepDuration), $"{nameof(sleepDuration)} must be non-negative.");
            }

            int timeoutMillis = (int) Math.Round(timeout.TotalMilliseconds);
            int sleepMillis = (int) Math.Round(sleepDuration.TotalMilliseconds);

            int startMillis = Environment.TickCount;
            int stopMillis = startMillis + timeoutMillis;

            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                result = this.TryGetOrCreatePoint(coordinates);
                if (result.IsSuccess)
                {
                    return result;
                }

                int currentMillis = Environment.TickCount;

                int delayMillis = infiniteTimeout
                                        ? sleepMillis
                                        : Math.Min(stopMillis - currentMillis, sleepMillis);

                if (delayMillis < 0)
                {
                    result.SetAsyncTimeoutReachedFailure();
                    return result;
                }

                await Task.Delay(delayMillis, cancelToken);
            }
        }
    }
}
