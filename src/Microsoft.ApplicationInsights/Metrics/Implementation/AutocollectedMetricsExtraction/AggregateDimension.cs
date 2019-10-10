namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics
{
    using Microsoft.ApplicationInsights.Channel;
    using System;

    internal class AggregateDimension
    {
        public int? MaxValues { get; set; }
        public string DefaultValue { get; set; }

        public string Name { get; set; }

        public Func<ITelemetry, string> GetFieldValue;
    }
}
