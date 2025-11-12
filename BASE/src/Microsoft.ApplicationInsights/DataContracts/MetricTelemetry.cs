namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Telemetry type used to track metrics. Represents a sample set of values with a specified count, sum, max, min, and standard deviation.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackmetric">Learn more</a>
    /// </summary>
    public sealed class MetricTelemetry : ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "Metric";
        internal string EnvelopeName = "AppMetrics";

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with the
        /// specified <paramref name="metricName"/> and <paramref name="metricValue"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The <paramref name="metricName"/> is null or empty string.</exception>
        public MetricTelemetry(string metricName, double metricValue)
        {
            this.Name = metricName;
            this.Value = metricValue;
        }

        /// <summary>
        /// Gets or sets date and time when event was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public TelemetryContext Context { get; }

        /// <summary>
        /// Gets or sets the name of the metric.
        /// </summary>
        public string MetricNamespace
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the metric.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of this metric.
        /// </summary>
        public double Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this metric.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get;
        }
    }
}
