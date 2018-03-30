namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using static System.FormattableString;

    /// <summary>@ToDo: Complete documentation before stable release. {092}</summary>
    public sealed class MetricsCollection : ICollection<Metric>
    {
        private readonly MetricManager metricManager;
        private readonly ConcurrentDictionary<MetricIdentifier, Metric> metrics = new ConcurrentDictionary<MetricIdentifier, Metric>();

        /// <summary>@ToDo: Complete documentation before stable release. {109}</summary>
        /// <param name="metricManager">@ToDo: Complete documentation before stable release. {758}</param>
        internal MetricsCollection(MetricManager metricManager)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            this.metricManager = metricManager;
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {304}</summary>
        public int Count
        {
            get { return this.metrics.Count; }
        }

        /// <summary>Gets a value indicating whether @ToDo: Complete documentation before stable release. {712}</summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {799}</summary>
        /// <param name="metricIdentifier">@ToDo: Complete documentation before stable release. {564}</param>
        /// <param name="metricConfiguration">@ToDo: Complete documentation before stable release. {324}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {708}</returns>
        public Metric GetOrCreate(
                                MetricIdentifier metricIdentifier,
                                MetricConfiguration metricConfiguration)
        {
            Util.ValidateNotNull(metricIdentifier, nameof(metricIdentifier));
            
            Metric metric = this.metrics.GetOrAdd(
                                            metricIdentifier,
                                            (key) => new Metric(
                                                                this.metricManager,
                                                                metricIdentifier,
                                                                metricConfiguration ?? MetricConfigurations.Common.Default()));

            if (metricConfiguration != null && false == metric.configuration.Equals(metricConfiguration))
            {
                throw new ArgumentException("A Metric with the specified Namespace, Id and dimension names already exists, but it has a configuration"
                                          + " that is different from the specified configuration. You may not change configurations once a"
                                          + " metric was created for the first time. Either specify the same configuration every time, or"
                                          + " specify 'null' during every invocation except the first one. 'Null' will match against any"
                                          + " previously specified configuration when retrieving existing metrics, or fall back to"
                               + Invariant($" the default when creating new metrics. ({nameof(metricIdentifier)} = \"{metricIdentifier.ToString()}\".)"));
            }

            return metric;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {200}</summary>
        public void Clear()
        {
            this.metrics.Clear();
        }

        /// <summary>@ToDo: Complete documentation before stable release. {628}</summary>
        /// <param name="metric">@ToDo: Complete documentation before stable release. {398}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {479}</returns>
        public bool Contains(Metric metric)
        {
            if (metric == null)
            {
                return false;
            }

            return this.metrics.ContainsKey(metric.Identifier);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {200}</summary>
        /// <param name="array">@ToDo: Complete documentation before stable release. {377}</param>
        /// <param name="arrayIndex">@ToDo: Complete documentation before stable release. {290}</param>
        public void CopyTo(Metric[] array, int arrayIndex)
        {
            Util.ValidateNotNull(array, nameof(array));

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            this.metrics.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {040}</summary>
        /// <param name="metric">@ToDo: Complete documentation before stable release. {667}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {197}</returns>
        public bool Remove(Metric metric)
        {
            if (metric == null)
            {
                return false;
            }

            Metric removedMetric;
            return this.metrics.TryRemove(metric.Identifier, out removedMetric);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {533}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {064}</returns>
        public IEnumerator<Metric> GetEnumerator()
        {
            return this.metrics.Values.GetEnumerator();
        }

        /// <summary>@ToDo: Complete documentation before stable release. {222}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {354}</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// The Add(..) method is not supported. To add a new metric, use the GetOrCreate(..) method.
        /// </summary>
        /// <param name="unsupported">@ToDo: Complete documentation before stable release. {021}</param>
        void ICollection<Metric>.Add(Metric unsupported)
        {
            throw new NotSupportedException(Invariant($"The Add(..) method is not supported by this {nameof(MetricsCollection)}.")
                                                     + " To add a new metric, use the GetOrCreate(..) method.");
        }
    }
}