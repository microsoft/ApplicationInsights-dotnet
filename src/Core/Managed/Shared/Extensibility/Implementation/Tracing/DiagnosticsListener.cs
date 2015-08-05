namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
#if CORE_PCL || NET45 || WINRT || UWP || NET46
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
#if NET40 || NET35
    using Microsoft.Diagnostics.Tracing;
#endif

    /// <summary>
    /// Subscriber to ETW Event source events, which sends data to other Senders (F5 and Portal).
    /// </summary>
    internal class DiagnosticsListener
#if Wp80
 : IDisposable
#else
        : EventListener
#endif
    {
        private const long AllKeyword = -1;

        private readonly IList<IDiagnosticsSender> diagnosticsSenders = new List<IDiagnosticsSender>();
#if !Wp80
        private HashSet<WeakReference> eventSources = new HashSet<WeakReference>();
#endif
        private EventLevel logLevel = EventLevel.Error;

        public DiagnosticsListener(IList<IDiagnosticsSender> senders)
        {
            if (senders == null || senders.Count < 1)
            {
                throw new ArgumentNullException("senders");
            }

            this.diagnosticsSenders = senders;
        }

        private DiagnosticsListener()
        {
        }

        public EventLevel LogLevel
        {
            get
            {
                return this.logLevel;
            }

            set
            {
                this.logLevel = value;
#if !Wp80
                HashSet<WeakReference> aliveEventSources = new HashSet<WeakReference>();
                foreach (WeakReference s in this.eventSources)
                {
                    EventSource source = (EventSource)s.Target;
                    if (source != null)
                    {
                        this.EnableEvents(source, this.LogLevel, (EventKeywords)AllKeyword);
                        aliveEventSources.Add(s);
                    }
                }

                this.eventSources = aliveEventSources;
#endif
            }
        }

        public void WriteEvent(TraceEvent eventData)
        {
            if (eventData.MetaData != null && eventData.MetaData.MessageFormat != null)
            {
                // check severity because it is not done in Silverlight EventSource implementation 
                if (eventData.MetaData.Level <= this.LogLevel)
                {
                    foreach (var sender in this.diagnosticsSenders)
                    {
                        sender.Send(eventData);
                    }
                }
            }
        }

#if Wp80
        public void Dispose()
        {
        }

#endif
#if !Wp80
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
            
            this.WriteEvent(traceEvent);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.StartsWith("Microsoft-ApplicationInsights-", StringComparison.Ordinal))
            {
                this.eventSources.Add(new WeakReference(eventSource));
                this.EnableEvents(eventSource, this.LogLevel, (EventKeywords)AllKeyword);
            }

            base.OnEventSourceCreated(eventSource);
        }
#endif
    }
}
