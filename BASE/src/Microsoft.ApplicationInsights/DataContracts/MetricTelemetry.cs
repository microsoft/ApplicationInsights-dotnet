namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Telemetry type used to track metrics. Represents a sample set of values with a specified count, sum, max, min, and standard deviation.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackmetric">Learn more</a>
    /// </summary>
    public sealed class MetricTelemetry : ITelemetry
    {
        internal const string EtwEnvelopeName = "Metric";
        internal string EnvelopeName = "AppMetrics";

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with empty
        /// properties.
        /// </summary>
        public MetricTelemetry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with properties provided.
        /// </summary>
        /// <remarks>
        /// Metrics should always be pre-aggregated across a time period before being sent.
        /// Most applications do not need to explicitly create <c>MetricTelemetry</c> objects. Instead, use one of
        /// the <c>GetMetric(..)</c> overloads on the <see cref="TelemetryClient" /> class to get a metric object
        /// for accessing SDK pre-aggregation capabilities. <br />
        /// However, you can use this ctor to create metric telemetry items if you have implemented your own metric
        /// aggregation. In that case, use <see cref="TelemetryClient.Track(ITelemetry)"/> method to send your aggregates.
        /// </remarks>
        /// <param name="name">Metric name.</param>
        /// <param name="count">Count of values taken during aggregation interval.</param>
        /// <param name="sum">Sum of values taken during aggregation interval.</param>
        /// <param name="min">Minimum value taken during aggregation interval.</param>
        /// <param name="max">Maximum of values taken during aggregation interval.</param>
        /// <param name="standardDeviation">Standard deviation of values taken during aggregation interval.</param>
        public MetricTelemetry(
            string name,
            int count,
            double sum,
            double min,
            double max,
            double standardDeviation)
            : this()
        {
            this.Name = name;
            this.Count = count;
            this.Sum = sum;
            this.Min = min;
            this.Max = max;
            this.StandardDeviation = standardDeviation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with properties provided.
        /// </summary>
        /// <remarks>
        /// Metrics should always be pre-aggregated across a time period before being sent.
        /// Most applications do not need to explicitly create <c>MetricTelemetry</c> objects. Instead, use one of
        /// the <c>GetMetric(..)</c> overloads on the <see cref="TelemetryClient" /> class to get a metric object
        /// for accessing SDK pre-aggregation capabilities. <br />
        /// However, you can use this ctor to create metric telemetry items if you have implemented your own metric
        /// aggregation. In that case, use <see cref="TelemetryClient.Track(ITelemetry)"/> method to send your aggregates.
        /// </remarks>
        /// <param name="metricNamespace">Metric namespace.</param>
        /// <param name="name">Metric name.</param>
        /// <param name="count">Count of values taken during aggregation interval.</param>
        /// <param name="sum">Sum of values taken during aggregation interval.</param>
        /// <param name="min">Minimum value taken during aggregation interval.</param>
        /// <param name="max">Maximum of values taken during aggregation interval.</param>
        /// <param name="standardDeviation">Standard deviation of values taken during aggregation interval.</param>
        public MetricTelemetry(
            string metricNamespace,
            string name,
            int count,
            double sum,
            double min,
            double max,
            double standardDeviation)
            : this()
        {
            this.MetricNamespace = metricNamespace;
            this.Name = name;
            this.Count = count;
            this.Sum = sum;
            this.Min = min;
            this.Max = max;
            this.StandardDeviation = standardDeviation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="MetricTelemetry"/> to clone from.</param>
        private MetricTelemetry(MetricTelemetry source)
        {
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
        }

        /// <summary>
        /// Gets or sets date and time when event was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence { get; set; }

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
        /// Gets or sets sum of the values of the metric samples.
        /// </summary>
        public double Sum
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of values in the sample set.
        /// </summary>
        public int? Count
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the min value of this metric across the sample set.
        /// </summary>
        public double? Min
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the max value of this metric across the sample set.
        /// </summary>
        public double? Max
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the standard deviation of this metric across the sample set.
        /// </summary>
        public double? StandardDeviation
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

        /// <summary>
        /// Deeply clones a <see cref="MetricTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public ITelemetry DeepClone()
        {
            return new MetricTelemetry(this);
        }
    }
}
