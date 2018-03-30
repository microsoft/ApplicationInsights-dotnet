namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>@ToDo: Complete documentation before stable release. {406}</summary>
    public class MetricAggregate
    {
        // We want to make the aggregate thread safe, but we expect no signiicant contention, so a simple lock will suffice.
        private readonly object updateLock = new object();

        private DateTimeOffset aggregationPeriodStart;
        private TimeSpan aggregationPeriodDuration;

        /// <summary>@ToDo: Complete documentation before stable release. {394}</summary>
        /// <param name="metricNamespace">@ToDo: Complete documentation before stable release. {704}</param>
        /// <param name="metricId">@ToDo: Complete documentation before stable release. {274}</param>
        /// <param name="aggregationKindMoniker">@ToDo: Complete documentation before stable release. {781}</param>
        public MetricAggregate(string metricNamespace, string metricId, string aggregationKindMoniker)
        {
            Util.ValidateNotNull(metricNamespace, nameof(metricNamespace));
            Util.ValidateNotNull(metricId, nameof(metricId));
            Util.ValidateNotNull(aggregationKindMoniker, nameof(aggregationKindMoniker));

            this.MetricNamespace = metricNamespace;
            this.MetricId = metricId;
            this.AggregationKindMoniker = aggregationKindMoniker;

            this.aggregationPeriodStart = default(DateTimeOffset);
            this.aggregationPeriodDuration = default(TimeSpan);

            this.Dimensions = new ConcurrentDictionary<string, string>();
            this.Data = new ConcurrentDictionary<string, object>();
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {747}</summary>
        public string MetricNamespace { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {848}</summary>
        public string MetricId { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {959}</summary>
        public string AggregationKindMoniker { get; }

        /// <summary>Gets or sets @ToDo: Complete documentation before stable release. {050}</summary>
        public DateTimeOffset AggregationPeriodStart
        {
            get
            {
                lock (this.updateLock)
                {
                    return this.aggregationPeriodStart;
                }
            }

            set
            {
                lock (this.updateLock)
                {
                    this.aggregationPeriodStart = value;
                }
            }
        }

        /// <summary>Gets or sets @ToDo: Complete documentation before stable release. {309}</summary>
        public TimeSpan AggregationPeriodDuration
        {
            get
            {
                lock (this.updateLock)
                {
                    return this.aggregationPeriodDuration;
                }
            }

            set
            {
                lock (this.updateLock)
                {
                    this.aggregationPeriodDuration = value;
                }
            }
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {840}</summary>
        public IDictionary<string, string> Dimensions { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {034}</summary>
        public IDictionary<string, object> Data { get; }

        /// <summary>
        /// This is aconvenience method to retrieve the object at <c>Data[dataKey]</c>.
        /// It attempts to convert that object to the specified type <c>T</c>. If the conversion fails, the specified <c>defaultValue</c> is returned.
        /// </summary>
        /// <typeparam name="T">Type to which to convert the object at <c>Data[dataKey]</c>.</typeparam>
        /// <param name="dataKey">Key for the data item.</param>
        /// <param name="defaultValue">The value to return if conversion fails.</param>
        /// <returns>@ToDo: Complete documentation before stable release. {843}</returns>
        public T GetDataValue<T>(string dataKey, T defaultValue)
        {
            object dataValue;
            if (this.Data.TryGetValue(dataKey, out dataValue))
            {
                try
                {
                    T value = (T)Convert.ChangeType(dataValue, typeof(T), CultureInfo.InvariantCulture);
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
