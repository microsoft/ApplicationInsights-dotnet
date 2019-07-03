using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace EventCounterCollector.Tests
{
    internal class EventCounterCollectorDiagnosticListener : EventListener
    {
        public ConcurrentQueue <string> EventsReceived { get; private set; }
        public EventCounterCollectorDiagnosticListener()
        {
            this.EventsReceived = new ConcurrentQueue<string>();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.EventsReceived.Enqueue(eventData.EventName);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (string.Equals(eventSource.Name, "Microsoft-ApplicationInsights-Extensibility-EventCounterCollector", StringComparison.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }

    }
}
