namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Manages OpenTelemetry Meter and Histogram instruments for metrics tracking.
    /// Provides a centralized cache for histogram instances to ensure efficient metric recording.
    /// </summary>
    internal sealed class MetricsManager : IDisposable
    {
        private readonly Meter meter;
        private readonly ConcurrentDictionary<string, Histogram<double>> histograms;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsManager"/> class.
        /// </summary>
        /// <param name="meterName">The name of the meter (defaults to Microsoft.ApplicationInsights).</param>
        public MetricsManager(string meterName = "Microsoft.ApplicationInsights")
        {
            this.meter = new Meter(meterName);
            this.histograms = new ConcurrentDictionary<string, Histogram<double>>();
        }

        /// <summary>
        /// Gets or creates a histogram instrument for the specified metric name.
        /// Histogram instances are cached and reused for efficiency.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="metricNamespace">Optional namespace for the metric (prepended to name).</param>
        /// <returns>A histogram instrument that can be used to record metric values.</returns>
        public Histogram<double> GetOrCreateHistogram(string metricName, string metricNamespace = null)
        {
            // Create a composite key if namespace is provided
            string key = string.IsNullOrEmpty(metricNamespace) 
                ? metricName 
                : $"{metricNamespace}-{metricName}";

            return this.histograms.GetOrAdd(key, _ => 
                this.meter.CreateHistogram<double>(key));
        }

        /// <summary>
        /// Disposes the meter and all associated instruments.
        /// </summary>
        public void Dispose()
        {
            this.meter.Dispose();
        }
    }
}
