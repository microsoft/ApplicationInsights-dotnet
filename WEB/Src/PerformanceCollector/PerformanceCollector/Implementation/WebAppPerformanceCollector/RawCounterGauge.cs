﻿namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerfCollector
{
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Gauge that gives the user an aggregate of requested counters in a cache.
    /// </summary>
    internal class RawCounterGauge : ICounterValue
    {
        /// <summary>
        /// JSON identifier of the counter variable.
        /// </summary>
        private string jsonId;

        /// <summary>
        /// Identifier of the environment variable.
        /// </summary>
        private AzureWebApEnvironmentVariables environmentVariable;

        private ICachedEnvironmentVariableAccess cacheHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="RawCounterGauge"/> class.
        /// </summary>
        /// <param name="jsonId">JSON identifier of the counter variable.</param>
        /// <param name="environmentVariable">Identifier of the environment variable.</param>
        public RawCounterGauge(string jsonId, AzureWebApEnvironmentVariables environmentVariable)
            : this(jsonId, environmentVariable, CacheHelper.Instance)
        {
        }

        internal RawCounterGauge(string jsonId, AzureWebApEnvironmentVariables environmentVariable, ICachedEnvironmentVariableAccess cache)
        {
            this.jsonId = jsonId;
            this.environmentVariable = environmentVariable;
            this.cacheHelper = cache;
        }

        /// <summary>
        /// Returns the current value of the counter as a <c ref="float"/> and resets the metric.
        /// </summary>
        /// <returns>The value of the target metric.</returns>
        public double Collect()
        {
            return this.cacheHelper.GetCounterValue(this.jsonId, this.environmentVariable);
        }
    }
}
