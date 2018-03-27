namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>
    /// There are some APIs on <c>Metric</c> that we hide from Intellisense by making them internal until the ..Extensibility namespace is imported.
    /// This class exposes them.
    /// </summary>
    public static class MetricExtensions
    {
        /// <summary>
        /// Exposes the <c>Configuration</c> property for users who imported the <c>Microsoft.ApplicationInsights.Metrics.Extensibility</c> namespace.
        /// </summary>
        /// <param name="metric">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public static MetricConfiguration GetConfiguration(this Metric metric)
        {
            return metric.configuration;
        }

        ///// <summary>
        ///// Exposes the <c>MetricManager</c> property for users who imported the <c>Microsoft.ApplicationInsights.Metrics.Extensibility</c> namespace.
        ///// </summary>
        ///// <param name="metric">ToDo: Complete documentation before stable release.</param>
        ///// <returns>ToDo: Complete documentation before stable release.</returns>
        ////public static MetricManager GetMetricManager(this Metric metric)
        ////{
        ////    return metric._metricManager;
        ////}
    }
}
