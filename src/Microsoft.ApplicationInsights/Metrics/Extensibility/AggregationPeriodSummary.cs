namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public class AggregationPeriodSummary
    {
        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="persistentAggregates">ToDo: Complete documentation before stable release.</param>
        /// <param name="nonpersistentAggregates">ToDo: Complete documentation before stable release.</param>
        public AggregationPeriodSummary(IReadOnlyList<MetricAggregate> persistentAggregates, IReadOnlyList<MetricAggregate> nonpersistentAggregates)
        {
            PersistentAggregates = persistentAggregates;
            NonpersistentAggregates = nonpersistentAggregates;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public IReadOnlyList<MetricAggregate> PersistentAggregates { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public IReadOnlyList<MetricAggregate> NonpersistentAggregates { get; }
    }
}
