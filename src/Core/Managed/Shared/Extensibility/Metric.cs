namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Represents aggregator for a single time series of a given metric.
    /// </summary>
    public class Metric : IEquatable<Metric>
    {
        /// <summary>
        /// Aggregator manager for the aggregator.
        /// </summary>
        private readonly MetricManager manager;

        /// <summary>
        /// Metric aggregator id to look for in the aggregator dictionary.
        /// </summary>
        private readonly string aggregatorId;

        /// <summary>
        /// Aggregator hash code.
        /// </summary>
        private readonly int hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="Metric"/> class.
        /// </summary>
        /// <param name="manager">Aggregator manager handling this instance.</param>
        /// <param name="name">Metric name.</param>
        /// <param name="dimensions">Metric dimensions.</param>
        internal Metric(
            MetricManager manager,
            string name, 
            IDictionary<string, string> dimensions = null)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            this.manager = manager;
            this.Name = name;
            this.Dimensions = dimensions;

            this.aggregatorId = Metric.GetAggregatorId(name, dimensions);
            this.hashCode = this.aggregatorId.GetHashCode();
        }

        /// <summary>
        /// Gets metric name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a set of metric dimensions and their values.
        /// </summary>
        public IDictionary<string, string> Dimensions { get; private set; }

        /// <summary>
        /// Adds a value to the time series.
        /// </summary>
        /// <param name="value">Metric value.</param>
        public void Track(double value)
        {
            SimpleMetricStatisticsAggregator aggregator = this.manager.GetStatisticsAggregator(this);
            aggregator.Track(value);

            this.ForwardToProcessors(value);
        }

        /// <summary>
        /// Returns the hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object. </param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(Metric other)
        {
            if (other == null)
            {
                return false;
            }

            return this.aggregatorId.Equals(other.aggregatorId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return this.Equals(obj as Metric);
        }

        /// <summary>
        /// Generates id of the aggregator serving time series specified in the parameters.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="dimensions">Optional metric dimensions.</param>
        /// <returns>Aggregator id that can be used to get aggregator.</returns>
        private static string GetAggregatorId(string name, IDictionary<string, string> dimensions = null)
        {
            StringBuilder aggregatorIdBuilder = new StringBuilder(name ?? "n/a");

            if (dimensions != null)
            {
                var sortedDimensions = dimensions.OrderBy((pair) => { return pair.Key; });

                foreach (KeyValuePair<string, string> pair in sortedDimensions)
                {
                    aggregatorIdBuilder.AppendFormat(CultureInfo.InvariantCulture, "\n{0}\t{1}", pair.Key ?? string.Empty, pair.Value ?? string.Empty);
                }
            }

            return aggregatorIdBuilder.ToString();
        }

        /// <summary>
        /// Forwards value to metric processors.
        /// </summary>
        /// <param name="value">Value tracked on time series.</param>
        private void ForwardToProcessors(double value)
        {
            // create a local reference to metric processor collection
            // if collection changes after that - it will be copied not affecting local reference
            IList<IMetricProcessor> metricProcessors = this.manager.MetricProcessors;

            if (metricProcessors != null)
            {
                int processorCount = metricProcessors.Count;

                for (int i = 0; i < processorCount; i++)
                {
                    IMetricProcessor processor = metricProcessors[i];

                    try
                    {
                        processor.Track(this, value);
                    }
                    catch (Exception ex)
                    {
                        CoreEventSource.Log.FailedToRunMetricProcessor(processor.GetType().FullName, ex.ToString());
                    }
                }
            }
        }
    }
}
