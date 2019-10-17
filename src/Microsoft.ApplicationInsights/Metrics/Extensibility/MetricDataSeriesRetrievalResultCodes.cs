namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using Microsoft.ApplicationInsights.Metrics.ConcurrentDatastructures;
    using System;

    /// <summary>
    /// Desbribes the kind of the result of retrieving or adding a point from/to a multidimensional metric cube.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1008: Enums should have zero value",
            Justification = "Crafted these flags to fit into a byte to make the struct container cheaper.")]
    [Flags]
    public enum MetricDataSeriesRetrievalResultCodes : byte
    {
        /// <summary>
        /// A new series was created and returned in this result.
        /// </summary>
        Success_NewSeriesCreated = MultidimensionalPointResultCodes.Success_NewPointCreated,

        /// <summary>
        /// A series already existed, it was retreived and returned in this result.
        /// </summary>
        Success_ExistingSeriesRetrieved = MultidimensionalPointResultCodes.Success_ExistingPointRetrieved,

        /// <summary>
        /// A series could not be created becasue the sub-dimsnions count was reached.
        /// </summary>
        Failure_SubdimensionsCountLimitReached = MultidimensionalPointResultCodes.Failure_SubdimensionsCountLimitReached,

        /// <summary>
        /// A series could not be created becasue the total series limit was reached.
        /// </summary>
        Failure_TotalSeriesCountLimitReached = MultidimensionalPointResultCodes.Failure_TotalPointsCountLimitReached,

        /// <summary>
        /// A series could not be retreived becasue it does not exist and creation was not requested.
        /// </summary>
        Failure_SeriesDoesNotExistCreationNotRequested = MultidimensionalPointResultCodes.Failure_PointDoesNotExistCreationNotRequested,

        /// <summary>
        /// Timeout reached.
        /// </summary>
        Failure_AsyncTimeoutReached = MultidimensionalPointResultCodes.Failure_AsyncTimeoutReached,
    }
}
