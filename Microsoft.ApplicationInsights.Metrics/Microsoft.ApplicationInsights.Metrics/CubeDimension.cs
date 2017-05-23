using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class CubeDimension<TDimensionValue, TPoint>
    {
        private readonly MultidimensionalCube<TDimensionValue, TPoint> _ownerCube;
        private readonly int _subdimensionsCountLimit;
        private readonly object _subElementCreationLock = new object();
        private ConcurrentDictionary<TDimensionValue, object> _elements = new ConcurrentDictionary<TDimensionValue, object>();

        private int _subdimensionsCount = 0;

        public CubeDimension(MultidimensionalCube<TDimensionValue, TPoint> ownerCube, int subdimensionsCountLimit)
        {
            _ownerCube = ownerCube;
            _subdimensionsCountLimit = subdimensionsCountLimit;
        }

        private bool TryIncSubdimensionsCount()
        {
            int newSubdimensionsCount = Interlocked.Increment(ref _subdimensionsCount);

            if (newSubdimensionsCount <= _subdimensionsCountLimit)
            {
                return true;
            }

            Interlocked.Decrement(ref _subdimensionsCount);
            return false;
        }

        private int DecSubdimensionsCount()
        {
            int newSubdimensionsCount = Interlocked.Decrement(ref _subdimensionsCount);
            return newSubdimensionsCount;
        }

        public bool TryGetOrAddVector(out TPoint point, TDimensionValue[] coordinates)
        {
            Util.ValidateNotNull(coordinates, nameof(coordinates));

            if (coordinates.Length != _ownerCube.DimensionsCount)
            {
                throw new ArgumentException(
                            $"The specified {nameof(coordinates)}-vector has {coordinates.Length} dimensions."
                          + $" However {nameof(_ownerCube)} has {_ownerCube.DimensionsCount} dimensions.",
                            nameof(coordinates));
            }

            bool result = this.TryGetOrAddVectorInternal(out point, coordinates, currentDim: 0, createIfNotExists: true);
            return result;
        }

        public bool TryGetVector(out TPoint point, TDimensionValue[] coordinates)
        {
            Util.ValidateNotNull(coordinates, nameof(coordinates));

            if (coordinates.Length != _ownerCube.DimensionsCount)
            {
                throw new ArgumentException(
                            $"The specified {nameof(coordinates)}-vector has {coordinates.Length} dimensions."
                          + $" However {nameof(_ownerCube)} has {_ownerCube.DimensionsCount} dimensions.",
                            nameof(coordinates));
            }

            bool result = this.TryGetOrAddVectorInternal(out point, coordinates, currentDim: 0, createIfNotExists: false);
            return result;
        }

        private bool TryGetOrAddVectorInternal(out TPoint point, TDimensionValue[] coordinates, int currentDim, bool createIfNotExists)
        {
            TDimensionValue subElementKey = coordinates[currentDim];
            bool isLastDimensionLevel = (currentDim == coordinates.Length - 1);

            // Try and get the referenced element:
            object subElement;
            bool subElementExists = _elements.TryGetValue(subElementKey, out subElement);

            // If the referenced element exists, we can simply proceed:
            if (subElementExists)
            {
                if (isLastDimensionLevel)
                {
                    point = (TPoint) subElement;
                    return true;
                }
                else
                {
                    CubeDimension<TDimensionValue, TPoint> subDim = (CubeDimension<TDimensionValue, TPoint>) subElement;
                    bool hasPoint = subDim.TryGetOrAddVectorInternal(out point, coordinates, currentDim + 1, createIfNotExists);
                    return hasPoint;
                }
            }
            else // so - subElementExists does NOT exist: 
            {
                // If we are not to create new elements, we are done:
                if (! createIfNotExists)
                {
                    point = default(TPoint);
                    return false;
                }

                bool hasPoint = isLastDimensionLevel
                                    ? this.TryAddPoint(out point, coordinates, currentDim)
                                    : this.TryAddSubvector(out point, coordinates, currentDim);
                return hasPoint;
            }
        }

        private bool TryAddPoint(out TPoint point, TDimensionValue[] coordinates, int currentDim)
        {
            point = default(TPoint);

            // We need to create a new sub-element, but we need to make sure that we do not exceed max dimension count in a concurrent situation.
            // To avoid locking as much as possible (it will be inevitable in some situations inside of the concurrent dictionary), we use an
            // interlocked increment of the dimension count early in the process to pre-book the new element. If all creation steps do not
            // complete succesfully, we will undo the increment.
            // Fortunately, it is a relatively rare case:
            // The increment will be successful only if the specified coordinates vector does not (yet) exist, but the max dimension limit is not yet exhaustet.
            // This can occur no more than (approx.) Cm = Md1 * Md2 * ... * Mdn times, where Mdi is the max dimension limit for dimension i. 

            // Check if we reached the dimensions count limit. If we did, we give up. Otherwise we start tracking whether we need to undo the increment later:
            if (! this.TryIncSubdimensionsCount())
            {
                return false;
            }

            bool mustRestoreSubdimensionsCount = true;
            try
            {
                // We are on the last level and we need to create the actual point. However, before doing that we need to check and pre-book the total
                // count limit using the same pattern as the dimension values limit:
                if (! _ownerCube.TryIncTotalPointsCount())
                {
                    return false;
                }

                bool mustRestoreTotalPointsCount = true;
                try
                {
                    TPoint newPoint = _ownerCube.PointsFactory(coordinates);

                    TDimensionValue subElementKey = coordinates[currentDim];
                    bool couldInsert = _elements.TryAdd(subElementKey, newPoint);

                    // There is a race on someone calling GetOrAddVector(..) with the same coordinates. So point may or may not already be in the list.
                    if (couldInsert)
                    {
                        point = newPoint;
                        mustRestoreTotalPointsCount = false;
                        mustRestoreSubdimensionsCount = false;
                    }
                    else
                    {
                        // If the point was already in the list, then that point created by the race winner is the one we want.
                        point = (TPoint) _elements[subElementKey];
                    }

                    return true;
                }
                finally
                {
                    if (mustRestoreTotalPointsCount)
                    {
                        _ownerCube.DecTotalPointsCount();
                    }
                }
            }
            finally
            {
                if (mustRestoreSubdimensionsCount)
                {
                    this.DecSubdimensionsCount();
                }
            }
        }

        private bool TryAddSubvector(out TPoint point, TDimensionValue[] coordinates, int currentDim)
        {
            point = default(TPoint);

            // Note the comment near the top if TryAddPoint(..) about the applied minimal locking strategy.

            // Check if we reached the dimensions count limit. If we did, we give up. Otherwise we start tracking whether we need to undo the increment later:
            if (! this.TryIncSubdimensionsCount())
            {
                return false;
            }

            bool mustRestoreSubdimensionsCount = true;
            try
            {
                TDimensionValue subElementKey = coordinates[currentDim];
                point = default(TPoint);

                // Do a soft-check to see if we reached the total points limit. If we do, there is no need to bother:
                // (We will do a hard check and pre-booking later when we actually about to create the point.)
                if (_ownerCube.TotalPointsCount >= _ownerCube.TotalPointsCountLimit)
                {
                    return false;
                }

                // We are not at the last level. Create the subdimension. Note, we are not under lock, so someone might be creating the same dimention concurrently:
                TPoint newPoint;
                CubeDimension<TDimensionValue, TPoint> newSubDim = new CubeDimension<TDimensionValue, TPoint>(_ownerCube, _ownerCube.GetDimensionValuesCountLimit(currentDim + 1));
                bool hasNewPoint = newSubDim.TryGetOrAddVectorInternal(out newPoint, coordinates, currentDim + 1, createIfNotExists: true);

                // Becasue we have not yet inserted newSubDim into _elements, any operations on newSubDim are not under concurrency.
                // There are no point-vectors yet pointing to the sub-space of newSubDim, so no DimensionValuesCountLimit can be reached.
                // So, hasNewPoint can be false only if TotalPointsCountLimit was reached. We just bail out:
                if (! hasNewPoint)
                {
                    return false;
                }

                // The new point has been created and we need to add its sub-space to the list.
                // However, there is a race on someone calling GetOrAddVector(..) with the same coordinates. So newSubDim may or may not already be in the list.
                bool couldInsert = _elements.TryAdd(subElementKey, newSubDim);

                if (couldInsert)
                {
                    // Success. We created and inserted a new sub-space:
                    point = newPoint;
                    mustRestoreSubdimensionsCount = false;
                    return true;
                }
                else
                {
                    // The point was already in the list. It's that other point (created by the race winner) that we want.
                    // We need to discard the sub-space and the point we just created: Decrement total points count and Decrement the dimension value count.
                    // After that we just call TryGetOrAddVectorInternal(..) again on the same recursion level.
                    _ownerCube.DecTotalPointsCount();
                }
            }
            finally
            {
                if (mustRestoreSubdimensionsCount)
                {
                    this.DecSubdimensionsCount();
                }
            }

            bool hasPoint = this.TryGetOrAddVectorInternal(out point, coordinates, currentDim, createIfNotExists: true);
            return hasPoint;
        }
    }
}
