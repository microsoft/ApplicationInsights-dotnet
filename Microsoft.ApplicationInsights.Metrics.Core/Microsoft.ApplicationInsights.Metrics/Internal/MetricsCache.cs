using System;
using System.Collections.Concurrent;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class MetricsCache
    {
        private readonly MetricManager _metricManager;
        private readonly ConcurrentDictionary<string, Metric> _metrics = new ConcurrentDictionary<string, Metric>();

        private MetricsCache(MetricManager metricManager)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            _metricManager = metricManager;
        }

        /// <summary>
        /// Ctor facade that can be used to pass as a function pointer without a local lambda instantiation.
        /// </summary>
        /// <param name="metricManager"></param>
        /// <returns></returns>
        public static MetricsCache CreateNewInstance(MetricManager metricManager)
        {
            return new MetricsCache(metricManager);
        }

        internal Metric GetOrCreateMetric(
                                string metricId,
                                string dimension1Name,
                                string dimension2Name,
                                IMetricConfiguration metricConfiguration)
        {
            metricId = metricId?.Trim();
            Util.ValidateNotNullOrWhitespace(metricId, nameof(metricId));
            
            string metricObjectId = Metric.GetObjectId(metricId, dimension1Name, dimension2Name);
            Metric metric = _metrics.GetOrAdd(metricObjectId, (key) => new Metric(
                                                                                _metricManager,
                                                                                metricId,
                                                                                dimension1Name,
                                                                                dimension2Name,
                                                                                metricConfiguration ?? MetricConfigurations.Common.Default()));

            if (metricConfiguration != null && ! metric._configuration.Equals(metricConfiguration))
            {
                throw new ArgumentException("A Metric with the specified Id and dimension names already exists, but it has a configuration"
                                          + " that is different from the specified configuration. You may not change configurations once a"
                                          + " metric was created for the first time. Either specify the same configuration every time, or"
                                          + " specify 'null' during every invocation except the first one. 'Null' will match against any"
                                          + " previously specified configuration when retrieving existing metrics, or fall back to"
                                          +$" the default when creating new metrics. (Metric Object Id: \"{metricObjectId}\".)");
            }

            return metric;
        }
    }
}