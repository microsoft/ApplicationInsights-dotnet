//-----------------------------------------------------------------------
// <copyright file="EventSourceModuleDiagnosticListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EtwTelemetryCollector.Tests
{
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;

    internal class EventSourceModuleDiagnosticListener : EventListener
    {
        public EventSourceModuleDiagnosticListener()
        {
            this.EventsReceived = new List<EventWrittenEventArgs>();
        }

        public IList<EventWrittenEventArgs> EventsReceived { get; private set; }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.EventsReceived.Add(eventData);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-ApplicationInsights-Extensibility-EventSourceListener")
            {
                EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }
    }
}
