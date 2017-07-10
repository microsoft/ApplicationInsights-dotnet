namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>
#if NET40
    [Microsoft.Diagnostics.Tracing.EventData(Name = "PartB_RequestData")]
#elif !NET45
    // .Net 4.5 has a custom implementation of RichPayloadEventSource
    [System.Diagnostics.Tracing.EventData(Name = "PartB_RequestData")]
#endif
    internal partial class RequestData : IDeepCloneable<RequestData>
    {
        public RequestData DeepClone()
        {
            var other = new RequestData();
            other.ver = this.ver;
            other.id = this.id;
            other.source = this.source;
            other.name = this.name;
            other.duration = this.duration;
            other.responseCode = this.responseCode;
            other.success = this.success;
            other.url = this.url;
            Debug.Assert(other.properties != null, "The constructor should have allocated properties dictionary");
            Debug.Assert(other.measurements != null, "The constructor should have allocated the measurements dictionary");
            Utils.CopyDictionary(this.properties, other.properties);
            Utils.CopyDictionary(this.measurements, other.measurements);
            return other;
        }
    }
}