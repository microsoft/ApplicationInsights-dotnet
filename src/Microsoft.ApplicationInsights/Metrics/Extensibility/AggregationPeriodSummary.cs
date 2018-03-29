namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>@ToDo: Complete documentation before stable release.</summary>
    /// @PublicExposureCandidate
    internal class AggregationPeriodSummary
    {
        /// <summary>@ToDo: Complete documentation before stable release.</summary>
        /// <param name="persistentAggregates">@ToDo: Complete documentation before stable release.</param>
        /// <param name="nonpersistentAggregates">@ToDo: Complete documentation before stable release.</param>
        public AggregationPeriodSummary(IReadOnlyList<MetricAggregate> persistentAggregates, IReadOnlyList<MetricAggregate> nonpersistentAggregates)
        {
            this.PersistentAggregates = persistentAggregates;
            this.NonpersistentAggregates = nonpersistentAggregates;
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release.</summary>
        public IReadOnlyList<MetricAggregate> PersistentAggregates { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release.</summary>
        public IReadOnlyList<MetricAggregate> NonpersistentAggregates { get; }
    }
}
