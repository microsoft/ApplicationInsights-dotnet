namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>
    /// Desbribes the result of retrieving or adding a point from/to a multidimensional metric cube.
    /// </summary>
    public struct MetricDataSeriesRetrievalResult
    {
        private MetricSeries metricSeries;
        private int failureCoordinateIndex;
        private MetricDataSeriesRetrievalResultCodes resultCode;

        internal MetricDataSeriesRetrievalResult(ConcurrentDatastructures.MultidimensionalPointResult<MetricSeries> rawResult)
        {
            this.metricSeries = rawResult.Point;
            this.failureCoordinateIndex = rawResult.FailureCoordinateIndex;
            this.resultCode = (MetricDataSeriesRetrievalResultCodes) ((byte) rawResult.ResultCode);
        }

        internal MetricDataSeriesRetrievalResult(MetricDataSeriesRetrievalResultCodes successCode, MetricSeries point)
        {
            this.resultCode = successCode;
            this.failureCoordinateIndex = -1;
            this.metricSeries = point;
        }

        /// <summary>
        /// @ToDo
        /// </summary>
        public MetricSeries MetricSeries
        {
            get { return this.metricSeries; }
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
        public MetricDataSeriesRetrievalResultCodes ResultCode
        {
            get { return this.resultCode; }
        }

        /// <summary>
        /// @ToDo
        /// </summary>
        public bool IsSeriesCreatedNew
        {
            get { return (this.ResultCode & MetricDataSeriesRetrievalResultCodes.Success_NewSeriesCreated) != 0; }
        }

        /// <summary>
        /// @ToDo
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                return ((this.ResultCode & MetricDataSeriesRetrievalResultCodes.Success_NewSeriesCreated) != 0)
                          || ((this.ResultCode & MetricDataSeriesRetrievalResultCodes.Success_ExistingSeriesRetrieved) != 0);
            }
        }

        internal void SetAsyncTimeoutReachedFailure()
        {
            this.resultCode |= MetricDataSeriesRetrievalResultCodes.Failure_AsyncTimeoutReached;
        }
    }
}
