namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Telemetry type used to track metrics.
    /// </summary>
    public sealed class MetricTelemetry : ITelemetry, ISupportProperties, ISupportInternalProperties
    {
        internal const string TelemetryName = "Metric";

        internal readonly string BaseType = typeof(MetricData).Name;

        internal readonly MetricData Data;
        internal readonly DataPoint Metric;
        private readonly TelemetryContext context;

        private bool isAggregation = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with empty 
        /// properties.
        /// </summary>
        public MetricTelemetry()
        {
            this.Data = new MetricData();
            this.Metric = new DataPoint();
            this.context = new TelemetryContext(this.Data.properties, new Dictionary<string, string>());

            // We always have a single 'metric'.
            this.Data.metrics.Add(this.Metric);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricTelemetry"/> class with the 
        /// specified <paramref name="metricName"/> and <paramref name="metricValue"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The <paramref name="metricName"/> is null or empty string.</exception>
        public MetricTelemetry(string metricName, double metricValue) : this()
        {
            this.Name = metricName;
            this.Value = metricValue;
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
        public TelemetryContext Context
        {
            get { return this.context; }
        }

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
        /// </summary>
        public double Value
        {
            get { return this.Metric.value; }
            set { this.Metric.value = value; }
        }

        /// <summary>
        /// Gets or sets the number of samples for this metric.
        /// </summary>
        public int? Count
        {
            get
            {
                return this.Metric.count;
            }

            set
            {
                this.Metric.count = value;
                this.UpdateKind();
            }
        }

        /// <summary>
        /// Gets or sets the min value of this metric.
        /// </summary>
        public double? Min
        {
            get
            {
                return this.Metric.min;
            }

            set
            {
                this.Metric.min = value;
                this.UpdateKind();
            }
        }

        /// <summary>
        /// Gets or sets the max value of this metric.
        /// </summary>
        public double? Max
        {
            get
            {
                return this.Metric.max;
            }

            set
            {
                this.Metric.max = value;
                this.UpdateKind();
            }
        }

        /// <summary>
        /// Gets or sets the standard deviation of this metric.
        /// </summary>
        public double? StandardDeviation
        {
            get
            {
                return this.Metric.stdDev;
            }

            set
            {
                this.Metric.stdDev = value;
                this.UpdateKind();
            }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this metric.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the telemetry was sent.
        /// </summary>
        bool ISupportInternalProperties.Sent { get; set; }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Name = Utils.PopulateRequiredStringValue(this.Name, "name", typeof(MetricTelemetry).FullName);
            this.Properties.SanitizeProperties();
        }

        private void UpdateKind() 
        {
            bool isAggregation = this.Metric.count != null || this.Metric.min != null || this.Metric.max != null || this.Metric.stdDev != null;

            if (this.isAggregation != isAggregation)
            {
                if (isAggregation)
                {
                    this.Metric.kind = DataPointType.Aggregation;
                }
                else
                {
                    this.Metric.kind = DataPointType.Measurement;
                }
            }

            this.isAggregation = isAggregation;
        }
    }
}
