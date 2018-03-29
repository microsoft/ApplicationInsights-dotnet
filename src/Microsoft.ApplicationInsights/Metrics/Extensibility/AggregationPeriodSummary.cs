namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>@ToDo: Complete documentation before stable release. {390}</summary>
    /// @PublicExposureCandidate
    internal class AggregationPeriodSummary
    {
        /// <summary>@ToDo: Complete documentation before stable release. {487}</summary>
        /// <param name="persistentAggregates">@ToDo: Complete documentation before stable release. {672}</param>
        /// <param name="nonpersistentAggregates">@ToDo: Complete documentation before stable release. {290}</param>
        public AggregationPeriodSummary(IReadOnlyList<MetricAggregate> persistentAggregates, IReadOnlyList<MetricAggregate> nonpersistentAggregates)
        {
            this.PersistentAggregates = persistentAggregates;
            this.NonpersistentAggregates = nonpersistentAggregates;
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {315}</summary>
        public IReadOnlyList<MetricAggregate> PersistentAggregates { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {776}</summary>
        public IReadOnlyList<MetricAggregate> NonpersistentAggregates { get; }
    }
}
