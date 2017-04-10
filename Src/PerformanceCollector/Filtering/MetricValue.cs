namespace Microsoft.ApplicationInsights.Extensibility.Filtering
{
    using System;

    /// <summary>
    /// A wrapper around a Metric and its value to make access to the metric's value easier.
    /// </summary>
    internal class MetricValue
    {
        public MetricValue(Metric metric, double value)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            this.Metric = metric;
            this.Value = value;
        }

        public Metric Metric { get; }

        public string MetricName => this.Metric.Name;

        public double Value { get; }
    }
}