namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Telemetry type used to track aggregated metric information.
    /// </summary>
    public sealed class AggregatedMetricTelemetry : ITelemetry, ISupportProperties
    {
        internal const string TelemetryName = "Metric";

        internal readonly string BaseType = typeof(MetricData).Name;

        internal readonly MetricData Data;
        internal readonly DataPoint Metric;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregatedMetricTelemetry"/> class with empty 
        /// properties.
        /// </summary>
        public AggregatedMetricTelemetry()
        {
            this.Data = new MetricData();
            this.Metric = new DataPoint();
            this.Context = new TelemetryContext(this.Data.properties);

            this.Metric.kind = DataPointType.Aggregation;

            // We always have a single 'metric'.
            this.Data.metrics.Add(this.Metric);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregatedMetricTelemetry"/> class with properties provided.
        /// </summary>
        /// <remarks>
        /// Metric statistics provided are assumed to be calculated over a period of time equaling 1 minute.
        /// </remarks>
        /// <param name="metricName">Metric name.</param>
        /// <param name="count">Count of values taken during aggregation interval.</param>
        /// <param name="sum">Sum of values taken during aggregation interval.</param>
        /// <param name="min">Minimum value taken during aggregation interval.</param>
        /// <param name="max">Maximum of values taken during aggregation interval.</param>
        /// <param name="standardDeviation">Standard deviation of values taken during aggregation interval.</param>
        public AggregatedMetricTelemetry(
            string metricName, 
            int count,
            double sum,
            double min,
            double max,
            double standardDeviation)
            : this()
        {
            this.Name = metricName;
            this.Count = count;
            this.Sum = sum;
            this.Min = min;
            this.Max = max;
            this.StandardDeviation = standardDeviation;
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
        public string Name
        {
            get { return this.Metric.name; }
            set { this.Metric.name = value; }
        }

        /// <summary>
        /// Gets or sets the number of samples for this metric.
        /// </summary>
        public int Count
        {
            get { return this.Metric.count ?? 0; }
            set { this.Metric.count = value; }
        }

        /// <summary>
        /// Gets or sets the value of this metric.
        /// </summary>
        public double Sum
        {
            get { return this.Metric.value; }
            set { this.Metric.value = value; }
        }

        /// <summary>
        /// Gets or sets the min value of this metric.
        /// </summary>
        public double Min
        {
            get { return this.Metric.min ?? 0; }
            set { this.Metric.min = value; }
        }

        /// <summary>
        /// Gets or sets the max value of this metric.
        /// </summary>
        public double Max
        {
            get { return this.Metric.max ?? 0; }
            set { this.Metric.max = value; }
        }

        /// <summary>
        /// Gets or sets the standard deviation of this metric.
        /// </summary>
        public double StandardDeviation
        {
            get { return this.Metric.stdDev ?? 0; }
            set { this.Metric.stdDev = value; }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this metric.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Name = Utils.PopulateRequiredStringValue(this.Name, "name", typeof(AggregatedMetricTelemetry).FullName);
            this.Properties.SanitizeProperties();
            this.Count = this.Count < 0 ? 0 : this.Count;
            this.Sum = Utils.SanitizeNanAndInfinity(this.Sum);
            this.Min = Utils.SanitizeNanAndInfinity(this.Min);
            this.Max = Utils.SanitizeNanAndInfinity(this.Max);
            this.StandardDeviation = Utils.SanitizeNanAndInfinity(this.StandardDeviation);
        }
    }
}
