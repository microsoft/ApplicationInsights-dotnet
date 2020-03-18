namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    internal class TraceSeverityLevelDimensionExtractor : IDimensionExtractor
    {
        public int MaxValues { get; set; }

        public string DefaultValue { get; set; } = MetricTerms.Autocollection.Common.PropertyValues.Unknown;

        public string Name { get; set; } = MetricTerms.Autocollection.TraceCount.PropertyNames.SeverityLevel;

        public string ExtractDimension(ITelemetry item)
        {
            var trace = item as TraceTelemetry;
            if (trace != null)
            {
                var sevLevel = (int)trace.SeverityLevel.GetValueOrDefault();
                return sevLevel.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
