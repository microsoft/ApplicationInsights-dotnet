using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    class MetricEntensions
    {
    }

    /// <summary>
    /// There are some APIs on <c>Metric</c> that we hide from Intellisense by making them internal until the ..Extensibility namespace is imported.
    /// This class exposes them.
    /// </summary>
    public static class MetricExtensions
    {
        /// <summary>
        /// Exposes the <c>Configuration</c> property for users who imported the <c>Microsoft.ApplicationInsights.Metrics.Extensibility</c> namespace.
        /// </summary>
        /// <param name="metric"></param>
        /// <returns></returns>
        public static IMetricConfiguration GetConfiguration(this Metric metric)
        {
            return metric._configuration;
        }
    }
}
