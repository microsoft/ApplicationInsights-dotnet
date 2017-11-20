using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics.ConcurrentDatastructures
{
    /// <summary />
    /// <typeparam name="TPoint"></typeparam>
    internal class MultidimensionalCube2<TPoint>
    {
        /// <summary>
        /// We are using a recursive implementation for points creation so we are limiting the max dimension count to prevent strack overflow.
        /// In practice this is unlikely to be ever reached.
        /// If it nevertheless becomes an issue, we can change the implementation to be iterative and increase this limit.
        /// </summary>
        private const int DimensionsCountLimit = 50;

        private const string PointMonikerSeparator = "\0";
        private static readonly string[] PointMonikerSeparatorAsArray = new string[1] { PointMonikerSeparator };

        private readonly SemaphoreSlim _pointCreationLock = new SemaphoreSlim(1);

        private readonly int _totalPointsCountLimit;
        private readonly int[] _dimensionValuesCountLimits;
        private readonly HashSet<string>[] _dimensionValues;
        private readonly ConcurrentDictionary<string, TPoint> _points;
        private readonly Func<string[], TPoint> _pointsFactory;

        private int _totalPointsCount;

        /// <summary>
        /// </summary>
        /// <param name="pointsFactory"></param>
        /// <param name="dimensionValuesCountLimits"></param>
        public MultidimensionalCube2(Func<string[], TPoint> pointsFactory, params int[] dimensionValuesCountLimits)
            : this(Int32.MaxValue, pointsFactory, dimensionValuesCountLimits)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="totalPointsCountLimit"></param>
        /// <param name="pointsFactory"></param>
        /// <param name="dimensionValuesCountLimits"></param>
        public MultidimensionalCube2(int totalPointsCountLimit, Func<string[], TPoint> pointsFactory, params int[] dimensionValuesCountLimits)
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
                throw new ArgumentException($"Cube may not have more than ${MultidimensionalCube2<TPoint>.DimensionsCountLimit} dimensions,"
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
            _dimensionValues = new HashSet<string>[dimensionValuesCountLimits.Length];
            _points = new ConcurrentDictionary<string, TPoint>();
            _pointsFactory = pointsFactory;

            _totalPointsCount = 0;

            for (int i = 0; i < _dimensionValues.Length; i++)
            {
                _dimensionValues[i] = new HashSet<string>();
            }
        }

        /// <summary>
        /// </summary>
        public int DimensionsCount { get { return _dimensionValuesCountLimits.Length; } }

        /// <summary>
        /// </summary>
        public int TotalPointsCountLimit { get { return _totalPointsCountLimit; } }

        /// <summary>
        /// </summary>
        public int TotalPointsCount { get { return Volatile.Read(ref _totalPointsCount); } }

        /// <summary>
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public int GetDimensionValuesCountLimit(int dimension)
        {
            ValidateDimensionIndex(dimension);
            return _dimensionValuesCountLimits[dimension];
        }

        /// <summary>
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public IReadOnlyCollection<string> GetDimensionValues(int dimension)
        {
            ValidateDimensionIndex(dimension);
            return (IReadOnlyCollection<string>) _dimensionValues[dimension];
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<KeyValuePair<string[], TPoint>> GetAllPoints()
        {
            List<KeyValuePair<string[], TPoint>> currentPoints = new List<KeyValuePair<string[], TPoint>>(TotalPointsCount);
            GetAllPoints(currentPoints);
            return currentPoints;
        }

        /// <summary>
        /// </summary>
        /// <param name="pointContainer"></param>
        /// <returns></returns>
        public int GetAllPoints(ICollection<KeyValuePair<string[], TPoint>> pointContainer)
        {
            int count = 0;
            foreach (KeyValuePair<string, TPoint> storedPoint in _points)
            {
                string[] coordinates = ParsePointMoniker(storedPoint.Key);
                KeyValuePair<string[], TPoint> parsedPoint = new KeyValuePair<string[], TPoint>(coordinates, storedPoint.Value);
                pointContainer.Add(parsedPoint);
                count++;
            }

            return count;
        }

        /// <summary>
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public MultidimensionalPointResult<TPoint> TryGetOrCreatePoint(params string[] coordinates)
        {
            string pointMoniker = GetPointMoniker(coordinates);

            TPoint point;
            bool hasPoint = _points.TryGetValue(pointMoniker, out point);

            if (hasPoint)
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Success_ExistingPointRetrieved, point);
                return result;
            }

            if (_totalPointsCount >= _totalPointsCountLimit)
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Failure_TotalPointsCountLimitReached, -1);
                return result;
            }

            _pointCreationLock.Wait();
            try
            {
                MultidimensionalPointResult<TPoint> result = TryCreatePoint(coordinates, pointMoniker);
                return result;
            }
            finally
            {
                _pointCreationLock.Release();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public Task<MultidimensionalPointResult<TPoint>> TryGetOrCreatePointAsync(params string[] coordinates)
        {
            return TryGetOrCreatePointAsync(CancellationToken.None, coordinates);
        }

        /// <summary>
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public async Task<MultidimensionalPointResult<TPoint>> TryGetOrCreatePointAsync(CancellationToken cancelToken, params string[] coordinates)
        {
            string pointMoniker = GetPointMoniker(coordinates);

            TPoint point;
            bool hasPoint = _points.TryGetValue(pointMoniker, out point);

            if (hasPoint)
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Success_ExistingPointRetrieved, point);
                return result;
            }

            if (_totalPointsCount >= _totalPointsCountLimit)
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Failure_TotalPointsCountLimitReached, -1);
                return result;
            }

            await _pointCreationLock.WaitAsync(cancelToken).ConfigureAwait(continueOnCapturedContext: false);
            try
            {
                MultidimensionalPointResult<TPoint> result = TryCreatePoint(coordinates, pointMoniker);
                return result;
            }
            finally
            {
                _pointCreationLock.Release();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public MultidimensionalPointResult<TPoint> TryGetPoint(params string[] coordinates)
        {
            string pointMoniker = GetPointMoniker(coordinates);

            TPoint point;
            bool hasPoint = _points.TryGetValue(pointMoniker, out point);

            if (hasPoint)
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Success_ExistingPointRetrieved, point);
                return result;
            }
            else
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Failure_PointDoesNotExistCreationNotRequested, -1);
                return result;
            }
        }

        private static string[] ParsePointMoniker(string pointMoniker)
        {
            string[] coordinates = pointMoniker.Split(PointMonikerSeparatorAsArray, StringSplitOptions.None);
            return coordinates;
        }

        private static string BuildPointMoniker(string[] coordinates)
        {
            if (coordinates.Length == 0)
            {
                return String.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < coordinates.Length; i++)
            {
                if (coordinates[i] == null)
                {
                    throw new ArgumentNullException($"{nameof(coordinates)}[{i}]", $"The specified {nameof(coordinates)}-vector contains null at index {i}.");
                }

                if (coordinates[i].Contains(PointMonikerSeparator))
                {
                    throw new ArgumentException($"The value at index {i} of the specified {nameof(coordinates)}-vector contains"
                                              + $" an invalid character sub-sequence. Complete coordinate value: \"{coordinates[i]}\"."
                                              + $" Invalid sub-sequence: \"{PointMonikerSeparator}\".");
                }

                if (i > 0)
                {
                    builder.Append(PointMonikerSeparator);
                }

                builder.Append(coordinates[i]);
            }

            return builder.ToString();
        }

        private MultidimensionalPointResult<TPoint> TryCreatePoint(string[] coordinates, string pointMoniker)
        {
#pragma warning disable SA1509 // Opening braces must not be preceded by blank line
            // We already have tried getting the existng point and failed.
            // We also checked that _totalPointsCountLimit was not reached.
            // Lastly, we took a lock.
            // Now we can begin the slow path.

            // First, we need to try retrieving the point again, now under the lock:
            TPoint point;
            bool hasPoint = _points.TryGetValue(pointMoniker, out point);
            if (hasPoint)
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Success_ExistingPointRetrieved, point);
                return result;
            }

            // Then, check total count again now that we are under lock:
            if (_totalPointsCount >= _totalPointsCountLimit)
            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Failure_TotalPointsCountLimitReached, -1);
                return result;
            }

            // Examine each dimension and see if it reached values count limit. If not, track the new value:

            int reachedValsLimitDim = -1;
            BitArray valueAddedToDims = new BitArray(length: coordinates.Length, defaultValue: false);

            for (int i = 0; i < coordinates.Length; i++)
            {
                HashSet<string> dimVals = _dimensionValues[i];
                string coordinateVal = coordinates[i];

                if ((dimVals.Count >= _dimensionValuesCountLimits[i]) && (false == dimVals.Contains(coordinateVal)))
                {
                    reachedValsLimitDim = i;
                    break;
                }

                bool added = dimVals.Add(coordinates[i]);
                valueAddedToDims.Set(i, added);
            }

            // We hit the _dimensionValuesCountLimits at some dimension.
            // Remove what we just added to dim value sets and give up.

            if (reachedValsLimitDim != -1)
            {
                for (int i = 0; i <= reachedValsLimitDim; i++)
                {
                    if (valueAddedToDims.Get(i))
                    {
                        _dimensionValues[i].Remove(coordinates[i]);
                    }
                }

                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Failure_SubdimensionsCountLimitReached, reachedValsLimitDim);
                return result;
            }

            // Create new point:

            try
            {
                point = _pointsFactory(coordinates);
            }
            catch (Exception ex)
            {
                // User code in _pointsFactory may throw. In that case we need to clean up from the added value containers:
                for (int i = 0; i <= reachedValsLimitDim; i++)
                {
                    if (valueAddedToDims.Get(i))
                    {
                        _dimensionValues[i].Remove(coordinates[i]);
                    }
                }

                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;  // This line will never be reached
            }

            { 
                bool added = _points.TryAdd(pointMoniker, point);
                if (false == added)
                {
                    throw new InvalidOperationException($"Internal Metrics SDK bug. Please report this! (pointMoniker: {pointMoniker})");
                }
            }

            // Inc total points coint.
            _totalPointsCount++;

            {
                var result = new MultidimensionalPointResult<TPoint>(MultidimensionalPointResultCodes.Success_NewPointCreated, point);
                return result;
            }
#pragma warning restore SA1509 // Opening braces must not be preceded by blank line
        }

        private void ValidateDimensionIndex(int dimension)
        {
            if (dimension < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension index may not be negative.");
            }

            if (dimension >= DimensionsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(dimension), $"Dimension index (zero-based) exceeds the number of dimensions of this cube ({DimensionsCount}).");
            }
        }

        private string GetPointMoniker(string[] coordinates)
        {
            Util.ValidateNotNull(coordinates, nameof(coordinates));

            if (coordinates.Length != DimensionsCount)
            {
                throw new ArgumentException(
                            $"The specified {nameof(coordinates)}-vector has {coordinates.Length} dimensions."
                          + $" However this has {DimensionsCount} dimensions.",
                            nameof(coordinates));
            }

            return BuildPointMoniker(coordinates);
        }
    }
}
