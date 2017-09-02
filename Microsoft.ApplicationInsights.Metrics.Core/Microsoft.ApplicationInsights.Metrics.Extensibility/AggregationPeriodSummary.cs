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
        /// <param name="unfilteredValuesAggregates"></param>
        /// <param name="filteredAggregates"></param>
        public AggregationPeriodSummary(IReadOnlyCollection<ITelemetry> unfilteredValuesAggregates, IReadOnlyCollection<ITelemetry> filteredAggregates)
        {
            UnfilteredValuesAggregates = unfilteredValuesAggregates;
            FilteredAggregates = filteredAggregates;
        }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyCollection<ITelemetry> UnfilteredValuesAggregates { get; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyCollection<ITelemetry> FilteredAggregates { get; }
    }
}
