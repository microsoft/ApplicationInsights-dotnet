namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>
    /// Desbribes the result of retrieving or adding a point from/to a multidimensional metric cube.
    /// </summary>
    /// <typeparam name="TPoint">Type of the cube element set type (e.g. <c>MetricSeries</c>)</typeparam>
    public struct MultidimensionalPointResult<TPoint>
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

        /// <summary>
        /// @ToDo
        /// </summary>
        public TPoint Point
        {
            get { return this.point; }
        }

        /// <summary>
        /// @ToDo
        /// </summary>
        public int FailureCoordinateIndex
        {
            get { return this.failureCoordinateIndex; }
        }

        /// <summary>
        /// @ToDo
        /// </summary>
        public MultidimensionalPointResultCodes ResultCode
        {
            get { return this.resultCode; }
        }

        /// <summary>
        /// @ToDo
        /// </summary>
        public bool IsPointCreatedNew
        {
            get { return (this.ResultCode & MultidimensionalPointResultCodes.Success_NewPointCreated) != 0; }
        }

        /// <summary>
        /// @ToDo
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
            this.resultCode |= MultidimensionalPointResultCodes.Failure_AsyncTimeoutReached;
        }
    }
}
