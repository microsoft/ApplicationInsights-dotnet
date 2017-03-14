namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
#if NET45
    // .Net 4.5 has a custom implementation of RichPayloadEventSource
#elif NETSTANDARD1_3
// TODO: This file can be included once RichPayloadEventSource is in the netstandard1.3 project
#else
    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>
#if NET40
    [Microsoft.Diagnostics.Tracing.EventData(Name = "PartB_AvailabilityData")]
#else
    [System.Diagnostics.Tracing.EventData(Name = "PartB_AvailabilityData")]
#endif
    internal partial class AvailabilityData
    {
    }
#endif
}
