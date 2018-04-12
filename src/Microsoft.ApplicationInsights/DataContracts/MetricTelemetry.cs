namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Telemetry type used to track metric aggregates.
    /// Represents a result of aggregating a set of values with a specified count, sum, max, min, and standard deviation.
    /// </summary>
    /// <remarks>
    /// <c>MetricTelemetry</c> instances are automatically created by the SDK to represent metric aggregates
    /// across time periods. Most applications do not need to create any <c>MetricTelemetry</c> instances
    /// directly. Instead, use any of the <c>TelemetryClient.GetMetric(..)</c> overloads or the
    /// <see cref="Microsoft.ApplicationInsights.Metrics.MetricManager" /> class to take advantage of the SDK's 
    /// metric aggregation capabilities.
    /// You can explicitly create instances of <c>MetricTelemetry</c> if you have implemented your own metric aggregation subsystem.
    /// </remarks>
    public sealed class MetricTelemetry : ITelemetry, ISupportProperties
    {
        internal const string TelemetryName = "Metric";

        internal readonly string BaseType = typeof(MetricData).Name;

        internal readonly MetricData Data;
        internal readonly DataPoint Metric;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with empty
        /// properties.
        /// </summary>
        public MetricTelemetry()
        {
            this.Data = new MetricData();
            this.Metric = new DataPoint();
            this.Context = new TelemetryContext(this.Data.properties);

            this.Metric.kind = DataPointType.Aggregation;

            // We always have a single 'metric'.
            this.Data.metrics.Add(this.Metric);
        }

        /// <summary>
        /// Obsolete: <c>MetricTelemetry</c> objects should always represent aggregates across a time period;
        /// for that, use <see cref="MetricTelemetry(String, Int32, Double, Double, Double, Double)" />.<br />
        /// Initializes a new instance of the <see cref="MetricTelemetry" /> class with the
        /// specified <paramref name="metricName"/> and <paramref name="metricValue"/>.
        /// </summary>
        [Obsolete("Use MetricTelemetry(string name, int count, double sum, double min, double max, double standardDeviation) to create meaningful metric aggregates.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public MetricTelemetry(string metricName, double metricValue) : this()
        {
            this.Name = metricName;
#pragma warning disable 618
            this.Value = metricValue;
#pragma warning restore 618
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with properties provided.
        /// </summary>
        /// <remarks>
        /// <c>MetricTelemetry</c> instances are automatically created by the SDK to represent metric aggregates
        /// across time periods. Most applications do not need to create any <c>MetricTelemetry</c> instances
        /// directly. Instead, use any of the <c>TelemetryClient.GetMetric(..)</c> overloads or the
        /// <see cref="Microsoft.ApplicationInsights.Metrics.MetricManager" /> class to take advantage of the SDK's 
        /// metric aggregation capabilities.
        /// You can explicitly create instances of <c>MetricTelemetry</c> by calling this ctor if you have implemented
        /// your own metric aggregation subsystem.
        /// </remarks>
        /// <param name="name">Metric name.</param>
        /// <param name="count">Count of values observed during aggregation interval.</param>
        /// <param name="sum">Sum of values observed during aggregation interval.</param>
        /// <param name="min">Minimum value observed during aggregation interval.</param>
        /// <param name="max">Maximum of values observed during aggregation interval.</param>
        /// <param name="standardDeviation">Standard deviation of values observed during aggregation interval.</param>
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
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="MetricTelemetry"/> to clone from.</param>
        private MetricTelemetry(MetricTelemetry source)
        {
            this.Data = source.Data.DeepClone();
            this.Metric = source.Metric.DeepClone();
            this.Context = source.Context.DeepClone(this.Data.properties);
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
        }

        /// <summary>
        /// Gets or sets date and time when event was recorded. It represents the start of the aggregation interval for this aggregate.
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
        /// Gets or sets the value of this metric.
        /// Obsolete: Use the <see cref="Sum" /> property instead.
        /// </summary>
        [Obsolete("This property is obsolete. Use Sum property instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double Value
        {
            get { return this.Metric.value; }
            set { this.Metric.value = value; }
        }

        /// <summary>
        /// Gets or sets sum of the values represented in this aggregate.
        /// </summary>
        public double Sum
        {
            get { return this.Metric.value; }
            set { this.Metric.value = value; }
        }

        /// <summary>
        /// Gets or sets the number of values represented in this aggregate.
        /// </summary>
        public int? Count
        {
            get { return this.Metric.count.HasValue ? this.Metric.count : 1; }
            set { this.Metric.count = value; }
        }

        /// <summary>
        /// Gets or sets the min value represented in this aggregate.
        /// </summary>
        public double? Min
        {
            get { return this.Metric.min; }
            set { this.Metric.min = value; }
        }

        /// <summary>
        /// Gets or sets the max value represented in this aggregate.
        /// </summary>
        public double? Max
        {
            get { return this.Metric.max; }
            set { this.Metric.max = value; }
        }

        /// <summary>
        /// Gets or sets the standard deviation represented in this aggregate.
        /// </summary>
        public double? StandardDeviation
        {
            get { return this.Metric.stdDev; }
            set { this.Metric.stdDev = value; }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this metric.
        /// These properties describe the dimensions and the respective values of the metric time series that contains the data
        /// point represented by this aggregate.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Deeply clones a <see cref="MetricTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public ITelemetry DeepClone()
        {
            return new MetricTelemetry(this);
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Name = Utils.PopulateRequiredStringValue(this.Name, "name", typeof(MetricTelemetry).FullName);
            this.Properties.SanitizeProperties();
            this.Sum = Utils.SanitizeNanAndInfinity(this.Sum);

            // note: we set count to 1 if it isn't a positive integer
            // thinking that if it is zero (negative case is clearly broken)
            // that most likely means somebody created instance but forgot to set count
            this.Count = (!this.Count.HasValue) || (this.Count <= 0) ? 1 : this.Count;

            if (this.Min.HasValue)
            {
                this.Min = Utils.SanitizeNanAndInfinity(this.Min.Value);
            }

            if (this.Max.HasValue)
            {
                this.Max = Utils.SanitizeNanAndInfinity(this.Max.Value);
            }

            if (this.StandardDeviation.HasValue)
            {
                this.StandardDeviation = Utils.SanitizeNanAndInfinity(this.StandardDeviation.Value);
            }
        }
    }
}
