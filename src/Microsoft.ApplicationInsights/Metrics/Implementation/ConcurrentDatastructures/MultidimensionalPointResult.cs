namespace Microsoft.ApplicationInsights.Metrics.ConcurrentDatastructures
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release. {768}</summary>
    /// <typeparam name="TPoint">Type of the set over which the cube is build. For metics, it is a metric series.</typeparam>
    internal struct MultidimensionalPointResult<TPoint>
    {
        private TPoint point;
        private int failureCoordinateIndex;
        private MultidimensionalPointResultCodes resultCode;

        internal MultidimensionalPointResult(MultidimensionalPointResultCodes failureCode, int failureCoordinateIndex)
        {
            this.resultCode = failureCode;
            this.failureCoordinateIndex = failureCoordinateIndex;
            this.point = default(TPoint);
        }

        internal MultidimensionalPointResult(MultidimensionalPointResultCodes successCode, TPoint point)
        {
            this.resultCode = successCode;
            this.failureCoordinateIndex = -1;
            this.point = point;
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {917}</summary>
        public TPoint Point
        {
            get { return this.point; }
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {528}</summary>
        public int FailureCoordinateIndex
        {
            get { return this.failureCoordinateIndex; }
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {621}</summary>
        public MultidimensionalPointResultCodes ResultCode
        {
            get { return this.resultCode; }
        }

        /// <summary>Gets a value indicating whether @ToDo: Complete documentation before stable release. {353}</summary>
        public bool IsPointCreatedNew
        {
            get { return (this.ResultCode & MultidimensionalPointResultCodes.Success_NewPointCreated) != 0; }
        }

        /// <summary>Gets a value indicating whether @ToDo: Complete documentation before stable release. {268}</summary>
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
            this.resultCode |= MultidimensionalPointResultCodes.Failure_AsyncTimeoutReached;
        }
    }
}
