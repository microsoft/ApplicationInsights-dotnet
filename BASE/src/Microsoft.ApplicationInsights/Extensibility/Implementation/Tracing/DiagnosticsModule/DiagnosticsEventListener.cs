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
            if (this.ShouldSubscribe(eventSource))
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

        /// <summary>
        /// This method checks if the given EventSource Name matches known EventSources that we want to subscribe to.
        /// </summary>
        private bool ShouldSubscribe(EventSource eventSource)
        {
            if (eventSource.Name.StartsWith("Microsoft-A", StringComparison.Ordinal))
            {
                switch (eventSource.Name)
                {
                    case "Microsoft-ApplicationInsights-Core": 
                    case "Microsoft-ApplicationInsights-Data": 
                    case "Microsoft-ApplicationInsights-WindowsServer-TelemetryChannel": 
                    case "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Dependency":
                    case "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web":
                    case "Microsoft-ApplicationInsights-Extensibility-DependencyCollector":
                    case "Microsoft-ApplicationInsights-Extensibility-EventCounterCollector":
                    case "Microsoft-ApplicationInsights-Extensibility-HostingStartup":
                    case "Microsoft-ApplicationInsights-Extensibility-PerformanceCollector":
                    case "Microsoft-ApplicationInsights-Extensibility-PerformanceCollector-QuickPulse":
                    case "Microsoft-ApplicationInsights-Extensibility-Web":
                    case "Microsoft-ApplicationInsights-Extensibility-WindowsServer":
                    case "Microsoft-ApplicationInsights-WindowsServer-Core":
                    case "Microsoft-ApplicationInsights-Extensibility-EventSourceListener":
                    case "Microsoft-ApplicationInsights-AspNetCore":
                    case "Microsoft-AspNet-Telemetry-Correlation": // https://github.com/aspnet/Microsoft.AspNet.TelemetryCorrelation/blob/master/src/Microsoft.AspNet.TelemetryCorrelation/AspNetTelemetryCorrelationEventSource.cs
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }
    }
}