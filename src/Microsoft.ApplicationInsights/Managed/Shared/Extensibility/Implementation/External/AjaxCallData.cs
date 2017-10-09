namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
#if NET45
    // .Net 4.5 has a custom implementation of RichPayloadEventSource
#else
    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>
    [System.Diagnostics.Tracing.EventData(Name = "PartB_AjaxCallData")]
    internal partial class AjaxCallData
    {
    }
#endif
}
