namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Collections.Generic;

    /// <summary>
    /// A dummy queue sender to keep the data to be sent to the portal before the initialize method is called.
    /// This is due to the fact that initialize method cannot be called without the configuration and 
    /// the event listener write event is triggered before the diagnosticTelemetryModule initialize method is triggered.
    /// </summary>
    internal class PortalDiagnosticsQueueSender : IDiagnosticsSender
    {
        public PortalDiagnosticsQueueSender()
        {
            this.EventData = new List<TraceEvent>();
        }

        public IList<TraceEvent> EventData { get; private set; }

        public void Send(TraceEvent eventData)
        {
            this.EventData.Add(eventData);
        }
    }
}
