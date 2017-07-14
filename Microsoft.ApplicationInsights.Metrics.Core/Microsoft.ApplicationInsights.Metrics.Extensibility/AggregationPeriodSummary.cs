using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public class AggregationPeriodSummary
    {
        public IReadOnlyCollection<ITelemetry> UnfilteredValuesAggregates { get; }
        public IReadOnlyCollection<ITelemetry> FilteredAggregates { get; }
            
        public AggregationPeriodSummary(IReadOnlyCollection<ITelemetry> unfilteredValuesAggregates, IReadOnlyCollection<ITelemetry> filteredAggregates)
        {
            UnfilteredValuesAggregates = unfilteredValuesAggregates;
            FilteredAggregates = filteredAggregates;
        }
    }
}
