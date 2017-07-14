using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TPoint"></typeparam>
    public struct MultidimensionalPointResult<TPoint>
    {
        private TPoint _point;
        private int _failureCoordinateIndex;
        private MultidimensionalPointResultCode _resultCode;

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
        public MultidimensionalPointResultCode ResultCode { get { return _resultCode; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPointCreatedNew { get { return (this.ResultCode & MultidimensionalPointResultCode.Success_NewPointCreated) != 0; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSuccess { get { return ((this.ResultCode & MultidimensionalPointResultCode.Success_NewPointCreated) != 0) 
                                                || ((this.ResultCode & MultidimensionalPointResultCode.Success_ExistingPointRetrieved) != 0); } }

        internal MultidimensionalPointResult(MultidimensionalPointResultCode failureCode, int failureCoordinateIndex)
        {
            _resultCode = failureCode;
            _failureCoordinateIndex = failureCoordinateIndex;
            _point = default(TPoint);
        }

        internal MultidimensionalPointResult(MultidimensionalPointResultCode successCode, TPoint point)
        {
            _resultCode = successCode;
            _failureCoordinateIndex = -1;
            _point = point;
        }

        internal void SetAsyncTimeoutReachedFailure()
        {
            _resultCode |= MultidimensionalPointResultCode.Failure_AsyncTimeoutReached;
        }
    }
}
