namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public class MetricsCollection : ICollection<Metric>
    {
        private readonly MetricManager _metricManager;
        private readonly ConcurrentDictionary<MetricIdentifier, Metric> _metrics = new ConcurrentDictionary<MetricIdentifier, Metric>();

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricManager">ToDo: Complete documentation before stable release.</param>
        internal MetricsCollection(MetricManager metricManager)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            _metricManager = metricManager;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int Count
        {
            get { return _metrics.Count; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricIdentifier">ToDo: Complete documentation before stable release.</param>
        /// <param name="metricConfiguration">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public Metric GetOrCreate(
                                MetricIdentifier metricIdentifier,
                                MetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNull(metricIdentifier, nameof(metricIdentifier));
            
            Metric metric = _metrics.GetOrAdd(
                                            metricIdentifier,
                                            (key) => new Metric(
                                                                _metricManager,
                                                                metricIdentifier,
                                                                metricConfiguration ?? MetricConfigurations.Common.Default()));

            if (metricConfiguration != null && false == metric._configuration.Equals(metricConfiguration))
            {
                throw new ArgumentException("A Metric with the specified Namespace, Id and dimension names already exists, but it has a configuration"
                                          + " that is different from the specified configuration. You may not change configurations once a"
                                          + " metric was created for the first time. Either specify the same configuration every time, or"
                                          + " specify 'null' during every invocation except the first one. 'Null' will match against any"
                                          + " previously specified configuration when retrieving existing metrics, or fall back to"
                                         + $" the default when creating new metrics. ({nameof(metricIdentifier)} = \"{metricIdentifier.ToString()}\".)");
            }

            return metric;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public void Clear()
        {
            _metrics.Clear();
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metric">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public bool Contains(Metric metric)
        {
            if (metric == null)
            {
                return false;
            }

            return _metrics.ContainsKey(metric.Identifier);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="array">ToDo: Complete documentation before stable release.</param>
        /// <param name="arrayIndex">ToDo: Complete documentation before stable release.</param>
        public void CopyTo(Metric[] array, int arrayIndex)
        {
            Util.ValidateNotNull(array, nameof(array));

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            _metrics.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metric">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public bool Remove(Metric metric)
        {
            if (metric == null)
            {
                return false;
            }

            Metric removedMetric;
            return _metrics.TryRemove(metric.Identifier, out removedMetric);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public IEnumerator<Metric> GetEnumerator()
        {
            return _metrics.Values.GetEnumerator();
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// The Add(..) method is not supported. To add a new metric, use the GetOrCreate(..) method.
        /// </summary>
        /// <param name="unsupported">ToDo: Complete documentation before stable release.</param>
        void ICollection<Metric>.Add(Metric unsupported)
        {
            throw new NotSupportedException($"The Add(..) method is not supported by this {nameof(MetricsCollection)}."
                                           + " To add a new metric, use the GetOrCreate(..) method.");
        }
    }
}