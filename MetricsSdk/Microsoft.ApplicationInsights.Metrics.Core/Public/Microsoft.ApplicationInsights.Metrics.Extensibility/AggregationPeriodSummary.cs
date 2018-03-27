using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    ///  
    /// </summary>
    public class AggregationPeriodSummary
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="persistentAggregates"></param>
        /// <param name="nonpersistentAggregates"></param>
        public AggregationPeriodSummary(IReadOnlyList<MetricAggregate> persistentAggregates, IReadOnlyList<MetricAggregate> nonpersistentAggregates)
        {
            PersistentAggregates = persistentAggregates;
            NonpersistentAggregates = nonpersistentAggregates;
        }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<MetricAggregate> PersistentAggregates { get; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<MetricAggregate> NonpersistentAggregates { get; }
    }
}
