namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using System;
    using Microsoft.ApplicationInsights.Channel;    

    internal class AggregateDimension
    {
        public int MaxValues { get; set; }
        public string DefaultValue { get; set; }

        public string Name { get; set; }

        public Func<ITelemetry, string> GetDimensionValue;
    }
}
