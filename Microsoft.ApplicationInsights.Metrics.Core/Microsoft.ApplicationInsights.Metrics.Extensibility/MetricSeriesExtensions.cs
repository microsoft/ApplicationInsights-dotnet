using System;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// There are some APIs on <c>MetricSeries</c> that we hide from Intellisense by making them internal until the ..Extensibility namespace is imported.
    /// This class exposes them.
    /// </summary>
    public static class MetricSeriesExtensions
    {
        /// <summary>
        /// Exposes the <c>Configuration</c> property for users who imported the <c>Microsoft.ApplicationInsights.Metrics.Extensibility</c> namespace.
        /// </summary>
        /// <param name="metricSeries"></param>
        /// <returns></returns>
        public static IMetricSeriesConfiguration GetConfiguration(this MetricSeries metricSeries)
        {
            return metricSeries._configuration;
        }
    }
}
