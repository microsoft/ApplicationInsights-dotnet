using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// </summary>
    public class MetricAggregate
    {
        // We want to make the aggregate thread safe, but we expect no signiicant contention, so a simple lock will suffice.
        private readonly object _lock = new object();

        private DateTimeOffset _aggregationPeriodStart;
        private TimeSpan _aggregationPeriodDuration;

        /// <summary>
        /// </summary>
        /// <param name="metricNamespace"></param>
        /// <param name="metricId"></param>
        /// <param name="aggregationKindMoniker"></param>
        public MetricAggregate(string metricNamespace, string metricId, string aggregationKindMoniker)
        {
            Util.ValidateNotNull(metricNamespace, nameof(metricNamespace));
            Util.ValidateNotNull(metricId, nameof(metricId));
            Util.ValidateNotNull(aggregationKindMoniker, nameof(aggregationKindMoniker));

            MetricNamespace = metricNamespace;
            MetricId = metricId;
            AggregationKindMoniker = aggregationKindMoniker;

            _aggregationPeriodStart = default(DateTimeOffset);
            _aggregationPeriodDuration = default(TimeSpan);

            Dimensions = new ConcurrentDictionary<string, string>();
            Data = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// </summary>
        public string MetricNamespace { get; }

        /// <summary>
        /// </summary>
        public string MetricId { get; }

        /// <summary>
        /// </summary>
        public string AggregationKindMoniker { get; }

        /// <summary>
        /// </summary>
        public DateTimeOffset AggregationPeriodStart
        {
            get
            {
                lock (_lock)
                {
                    return _aggregationPeriodStart;
                }
            }

            set
            {
                lock (_lock)
                {
                    _aggregationPeriodStart = value;
                }
            }
        }

        /// <summary>
        /// </summary>
        public TimeSpan AggregationPeriodDuration
        {
            get
            {
                lock (_lock)
                {
                    return _aggregationPeriodDuration;
                }
            }

            set
            {
                lock (_lock)
                {
                    _aggregationPeriodDuration = value;
                }
            }
        }

        
        /// <summary>
        /// </summary>
        public IDictionary<string, string> Dimensions { get; }

        /// <summary>
        /// </summary>
        public IDictionary<string, object> Data { get; }

        /// <summary>
        /// This is aconvenience method to retrieve the object at <c>Data[dataKey]</c>.
        /// It attempts to convert that object to the specified type <c>T</c>. If the conversion fails, the specified <c>defaultValue</c> is returned.
        /// </summary>
        /// <typeparam name="T">Type to which to convert the object at <c>Data[dataKey]</c>.</typeparam>
        /// <param name="dataKey">Key for the data item.</param>
        /// <param name="defaultValue">The value to return if conversion fails.</param>
        /// <returns></returns>
        public T GetDataValue<T>(string dataKey, T defaultValue)
        {
            object dataValue;
            if (Data.TryGetValue(dataKey, out dataValue))
            {
                try
                {
                    T value = (T) Convert.ChangeType(dataValue, typeof(T));
                    return value;
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
    }
}
