using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1008: Enums should have zero value",
            Justification = "Crafted these flags to fit into a byte to make the struct container cheaper.")]
    [Flags]
    public enum MultidimensionalPointResultCodes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        Success_NewPointCreated = 1,

        /// <summary>
        /// 
        /// </summary>
        Success_ExistingPointRetrieved = 2,

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
        Failure_PointDoesNotExistCreationNotRequested = 32,

        /// <summary>
        /// 
        /// </summary>
        Failure_AsyncTimeoutReached = 128,
    }
}
