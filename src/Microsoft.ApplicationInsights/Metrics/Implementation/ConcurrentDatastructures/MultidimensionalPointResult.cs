namespace Microsoft.ApplicationInsights.Metrics.ConcurrentDatastructures
{
    using System;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    /// <typeparam name="TPoint">Type of the set over which the cube is build. For metics, it is a metric series.</typeparam>
    internal struct MultidimensionalPointResult<TPoint>
    {
        private TPoint _point;
        private int _failureCoordinateIndex;
        private MultidimensionalPointResultCodes _resultCode;

        internal MultidimensionalPointResult(MultidimensionalPointResultCodes failureCode, int failureCoordinateIndex)
        {
            _resultCode = failureCode;
            _failureCoordinateIndex = failureCoordinateIndex;
            _point = default(TPoint);
        }

        internal MultidimensionalPointResult(MultidimensionalPointResultCodes successCode, TPoint point)
        {
            _resultCode = successCode;
            _failureCoordinateIndex = -1;
            _point = point;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public TPoint Point
        {
            get { return _point; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int FailureCoordinateIndex
        {
            get { return _failureCoordinateIndex; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public MultidimensionalPointResultCodes ResultCode
        {
            get { return _resultCode; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public bool IsPointCreatedNew
        {
            get { return (this.ResultCode & MultidimensionalPointResultCodes.Success_NewPointCreated) != 0; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public bool IsSuccess
        {
            get
            {
                return ((this.ResultCode & MultidimensionalPointResultCodes.Success_NewPointCreated) != 0)
                          || ((this.ResultCode & MultidimensionalPointResultCodes.Success_ExistingPointRetrieved) != 0);
            }
        }

        internal void SetAsyncTimeoutReachedFailure()
        {
            _resultCode |= MultidimensionalPointResultCodes.Failure_AsyncTimeoutReached;
        }
    }
}
