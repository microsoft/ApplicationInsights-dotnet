using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetricConfiguration : IEquatable<IMetricConfiguration>
    {
        /// <summary>
        /// 
        /// </summary>
        int SeriesCountLimit { get; }

        /// <summary>
        /// 
        /// </summary>
        int ValuesPerDimensionLimit { get; }


        ///// <summary>
        ///// </summary>
        //TimeSpan NewSeriesCreationTimeout { get; }

        ///// <summary>
        ///// </summary>
        //TimeSpan NewSeriesCreationRetryDelay { get; }


        /// <summary>
        /// 
        /// </summary>
        IMetricSeriesConfiguration SeriesConfig { get; }
    }
}