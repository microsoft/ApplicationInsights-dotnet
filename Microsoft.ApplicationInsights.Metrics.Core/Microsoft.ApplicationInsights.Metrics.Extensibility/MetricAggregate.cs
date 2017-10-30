using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// </summary>
    public class MetricAggregate
    {
        // We want to make the aggregate thread safe, but we expect no signiicant contention, so a simple lock will suffice.
        private readonly object _lock = new object();

        private DateTimeOffset _aggregationPeriodStart;
        private TimeSpan _aggregationPeriodDuration;
        private object _additionalDataContext;

        /// <summary>
        /// </summary>
        /// <param name="metricId"></param>
        /// <param name="aggregationKindMoniker"></param>
        public MetricAggregate(string metricId, string aggregationKindMoniker)
        {
            Util.ValidateNotNull(metricId, nameof(metricId));
            Util.ValidateNotNull(aggregationKindMoniker, nameof(aggregationKindMoniker));

            MetricId = metricId;
            AggregationKindMoniker = aggregationKindMoniker;

            _aggregationPeriodStart = default(DateTimeOffset);
            _aggregationPeriodDuration = default(TimeSpan);

            Dimensions = new ConcurrentDictionary<string, string>();
            AggregateData = new ConcurrentDictionary<string, object>();

            _additionalDataContext = null;
        }

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
                lock(_lock)
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
        public IDictionary<string, object> AggregateData { get; }

        /// <summary>
        /// </summary>
        public object AdditionalDataContext
        {
            get
            {
                lock (_lock)
                {
                    return _additionalDataContext;
                }
            }
            set
            {
                lock (_lock)
                {
                    _additionalDataContext = value;
                }
            }
        }
    }
}
