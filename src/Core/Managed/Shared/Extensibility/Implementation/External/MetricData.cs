namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>
#if NET40
    [Microsoft.Diagnostics.Tracing.EventData(Name = "PartB_MetricData")]
#elif !NET45
    // .Net 4.5 has a custom implementation of RichPayloadEventSource
    [System.Diagnostics.Tracing.EventData(Name = "PartB_MetricData")]
#endif
    internal partial class MetricData : IDeepCloneable<MetricData>
    {
        public MetricData DeepClone()
        {
            var other = new MetricData();
            other.ver = this.ver;

            Debug.Assert(other.metrics != null, "The constructor should have allocated metrics list");
            foreach (var metric in this.metrics)
            {
                other.metrics.Add(metric.DeepClone());
            }

            Debug.Assert(other.properties != null, "The constructor should have allocated properties dictionary");
            Utils.CopyDictionary(this.properties, other.properties);
            return other;
        }
    }
}