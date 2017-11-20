using System;

namespace Microsoft.ApplicationInsights.ConcurrentDatastructures
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TPoint"></typeparam>
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

        /// <summary>
        /// 
        /// </summary>
        public TPoint Point { get { return _point; } }

        /// <summary>
        /// 
        /// </summary>
        public int FailureCoordinateIndex { get { return _failureCoordinateIndex; } }

        /// <summary>
        /// 
        /// </summary>
        public MultidimensionalPointResultCodes ResultCode { get { return _resultCode; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPointCreatedNew { get { return (this.ResultCode & MultidimensionalPointResultCodes.Success_NewPointCreated) != 0; } }

        /// <summary>
        /// 
        /// </summary>
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
