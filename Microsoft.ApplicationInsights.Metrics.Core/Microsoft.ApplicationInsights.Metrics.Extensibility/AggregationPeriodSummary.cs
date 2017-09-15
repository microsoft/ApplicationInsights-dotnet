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
        public AggregationPeriodSummary(IReadOnlyList<ITelemetry> persistentAggregates, IReadOnlyList<ITelemetry> nonpersistentAggregates)
        {
            PersistentAggregates = persistentAggregates;
            NonpersistentAggregates = nonpersistentAggregates;
        }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<ITelemetry> PersistentAggregates { get; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<ITelemetry> NonpersistentAggregates { get; }
    }
}
