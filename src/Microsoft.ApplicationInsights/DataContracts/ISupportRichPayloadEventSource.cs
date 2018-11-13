namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// 
    /// </summary>
    internal interface ISupportRichPayloadEventSource
    {
        EventKeywords EventSourceKeyword { get; }

        object Data { get; }

        string TelemetryName { get;  }
    }
}
