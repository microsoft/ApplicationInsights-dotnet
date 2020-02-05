namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsModule
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;

    internal class DiagnosticsEventListener : EventListener
    {
        private const long AllKeyword = -1;
        private readonly EventLevel logLevel;
        private readonly DiagnosticsListener listener;
        private readonly List<EventSource> eventSourcesDuringConstruction = new List<EventSource>();

        public DiagnosticsEventListener(DiagnosticsListener listener, EventLevel logLevel)
        {
            this.listener = listener;
            this.logLevel = logLevel;

            List<EventSource> eventSources;
            lock (this.eventSourcesDuringConstruction)
            {
                eventSources = this.eventSourcesDuringConstruction;
                this.eventSourcesDuringConstruction = null;
            }

            foreach (var eventSource in eventSources)
            {
                this.EnableEvents(eventSource, this.logLevel, (EventKeywords)AllKeyword);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventSourceEvent)
        {
            if (eventSourceEvent == null || this.listener == null)
            {
                return;
            }

            var metadata = new EventMetaData
            {                
                EventSourceName = eventSourceEvent.EventSource?.Name,
                Keywords = (long)eventSourceEvent.Keywords,
                MessageFormat = eventSourceEvent.Message,
                EventId = eventSourceEvent.EventId,
                Level = eventSourceEvent.Level,
            };

            var traceEvent = new TraceEvent
            {
                MetaData = metadata,
                Payload = eventSourceEvent.Payload?.ToArray(),
            };

            this.listener.WriteEvent(traceEvent);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.StartsWith("Microsoft-ApplicationInsights-", StringComparison.Ordinal) ||
                eventSource.Name.Equals("Microsoft-AspNet-Telemetry-Correlation", StringComparison.Ordinal))
            {
                // If our constructor hasn't run yet (we're in a callback from the base class
                // constructor), just make a note of the event source. Otherwise logLevel is
                // set to the default, which is "LogAlways".
                var tmp = this.eventSourcesDuringConstruction;
                if (tmp != null)
                {
                    lock (tmp)
                    {
                        if (this.eventSourcesDuringConstruction != null)
                        {
                            this.eventSourcesDuringConstruction.Add(eventSource);
                            return;
                        }
                    }
                }

                this.EnableEvents(eventSource, this.logLevel, (EventKeywords)AllKeyword);
            }

            base.OnEventSourceCreated(eventSource);
        }
    }
}