namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>
#if NET40
    [Microsoft.Diagnostics.Tracing.EventData(Name = "PartB_RemoteDependencyData")]
#elif !NET45
    // .Net 4.5 has a custom implementation of RichPayloadEventSource
    [System.Diagnostics.Tracing.EventData(Name = "PartB_RemoteDependencyData")]
#endif
    internal partial class RemoteDependencyData
    {
        public RemoteDependencyData DeepClone()
        {
            var other = new RemoteDependencyData();
            other.ver = this.ver;
            other.name = this.name;
            other.id = this.id;
            other.resultCode = this.resultCode;
            other.duration = this.duration;
            other.success = this.success;
            other.data = this.data;
            other.target = this.target;
            other.type = this.type;
            Debug.Assert(other.properties != null, "The constructor should have allocated properties dictionary");
            Debug.Assert(other.measurements != null, "The constructor should have allocated the measurements dictionary");
            Utils.CopyDictionary(this.properties, other.properties);
            Utils.CopyDictionary(this.measurements, other.measurements);
            return other;
        }
    }
}