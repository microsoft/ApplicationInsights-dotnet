#if !Wp80

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
#if CORE_PCL || NET45 || WINRT || UWP || NET46
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
#if NET40 || NET35
    using Microsoft.Diagnostics.Tracing;
#endif

    internal class DiagnosticsEventListener : EventListener
    {
        private const long AllKeyword = -1;
        private readonly EventLevel logLevel;
        private readonly DiagnosticsListener listener;

        public DiagnosticsEventListener(DiagnosticsListener listener, EventLevel logLevel)
        {
            this.listener = listener;
            this.logLevel = logLevel;
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventSourceEvent)
        {
            var metadata = new EventMetaData
            {
                Keywords = (long)eventSourceEvent.Keywords,
                MessageFormat = eventSourceEvent.Message,
                EventId = eventSourceEvent.EventId,
                Level = eventSourceEvent.Level
            };

            var traceEvent = new TraceEvent
            {
                MetaData = metadata,
                Payload = eventSourceEvent.Payload != null ? eventSourceEvent.Payload.ToArray() : null
            };

            this.listener.WriteEvent(traceEvent);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.StartsWith("Microsoft-ApplicationInsights-", StringComparison.Ordinal))
            {
                this.EnableEvents(eventSource, this.logLevel, (EventKeywords)AllKeyword);
            }

            base.OnEventSourceCreated(eventSource);
        }
    }
}
#endif