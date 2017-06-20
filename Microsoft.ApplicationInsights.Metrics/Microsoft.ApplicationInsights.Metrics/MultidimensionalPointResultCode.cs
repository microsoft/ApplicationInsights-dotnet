using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum MultidimensionalPointResultCode : byte
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
}
