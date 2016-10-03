namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an object used to process metric telemetry prior to aggregation of metric values.
    /// </summary>
    public interface IMetricProcessor
    {
        /// <summary>
        /// Process metric value.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="dimensions">Metric dimensions.</param>
        void Track(string metricName, double value, IDictionary<string, string> dimensions = null);
    }
}
