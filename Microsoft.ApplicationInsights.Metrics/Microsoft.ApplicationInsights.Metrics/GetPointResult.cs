using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum GetPointResultCode : byte
    {
        /// <summary>
        /// 
        /// </summary>
        Success_NewPointCreated = 0,

        /// <summary>
        /// 
        /// </summary>
        Success_ExistingPointRetrieved = 1,

        /// <summary>
        /// 
        /// </summary>
        Failure_DimensionValuesCountLimitReached = 8,

        /// <summary>
        /// 
        /// </summary>
        Failure_TotalPointsCountLimitReached = 16,

        /// <summary>
        /// 
        /// </summary>
        Failure_PointDoesntExistCreationNotRequested = 32,

        /// <summary>
        /// 
        /// </summary>
        Failure_AsyncTimeoutReached = 128,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TPoint"></typeparam>
    public struct GetPointResult<TPoint>
    {
        private TPoint _point;
        private int _failureCoordinateIndex;
        private GetPointResultCode _resultCode;

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
        public GetPointResultCode ResultCode { get { return _resultCode; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPointCreatedNew { get { return (this.ResultCode | GetPointResultCode.Success_NewPointCreated) != 0; } }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSuccess { get { return ((this.ResultCode | GetPointResultCode.Success_NewPointCreated) != 0) 
                                                || ((this.ResultCode | GetPointResultCode.Success_ExistingPointRetrieved) != 0); } }

        internal GetPointResult(GetPointResultCode failureCode, int failureCoordinateIndex)
        {
            _resultCode = failureCode;
            _failureCoordinateIndex = failureCoordinateIndex;
            _point = default(TPoint);
        }

        internal GetPointResult(GetPointResultCode successCode, TPoint point)
        {
            _resultCode = successCode;
            _failureCoordinateIndex = -1;
            _point = point;
        }

        internal void SetAsyncTimeoutReachedFailure()
        {
            _resultCode |= GetPointResultCode.Failure_AsyncTimeoutReached;
        }
    }
}
