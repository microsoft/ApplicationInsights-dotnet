namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnosticsInternals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// SelfDiagnosticsEventListener class enables the events from OpenTelemetry event sources
    /// and write the events to a local file in a circular way.
    /// </summary>
    internal class SelfDiagnosticsEventListener : EventListener
    {
        private readonly object lockObj = new object();
        private readonly EventLevel logLevel;

        // private readonly SelfDiagnosticsConfigRefresher configRefresher;
        private readonly List<EventSource> eventSourcesBeforeConstructor = new List<EventSource>();

        public SelfDiagnosticsEventListener(EventLevel logLevel/*, SelfDiagnosticsConfigRefresher configRefresher*/)
        {
            this.logLevel = logLevel;

            // this.configRefresher = configRefresher ?? throw new ArgumentNullException(nameof(configRefresher));

            List<EventSource> eventSources;
            lock (this.lockObj)
            {
                eventSources = this.eventSourcesBeforeConstructor;
                this.eventSourcesBeforeConstructor = null;
            }

            foreach (var eventSource in eventSources)
            {
#if NET452
                this.EnableEvents(eventSource, this.logLevel, (EventKeywords)(-1));
#else
                this.EnableEvents(eventSource, this.logLevel, EventKeywords.All);
#endif
            }
        }

        internal void WriteEvent(string eventMessage, ReadOnlyCollection<object> payload)
        {
            // TODO
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (ShouldSubscribe(eventSource))
            {
                // If there are EventSource classes already initialized as of now, this method would be called from
                // the base class constructor before the first line of code in SelfDiagnosticsEventListener constructor.
                // In this case logLevel is always its default value, "LogAlways".
                // Thus we should save the event source and enable them later, when code runs in constructor.
                if (this.eventSourcesBeforeConstructor != null)
                {
                    lock (this.lockObj)
                    {
                        if (this.eventSourcesBeforeConstructor != null)
                        {
                            this.eventSourcesBeforeConstructor.Add(eventSource);
                            return;
                        }
                    }
                }

#if NET452
                this.EnableEvents(eventSource, this.logLevel, (EventKeywords)(-1));
#else
                this.EnableEvents(eventSource, this.logLevel, EventKeywords.All);
#endif
            }

            base.OnEventSourceCreated(eventSource);
        }

        /// <summary>
        /// This method records the events from event sources to a local file, which is provided as a stream object by
        /// SelfDiagnosticsConfigRefresher class. The file size is bound to a upper limit. Once the write position
        /// reaches the end, it will be reset to the beginning of the file.
        /// </summary>
        /// <param name="eventData">Data of the EventSource event.</param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.WriteEvent(eventData.Message, eventData.Payload);
        }

        /// <summary>
        /// This method checks if the given EventSource Name matches known EventSources that we want to subscribe to.
        /// </summary>
        private static bool ShouldSubscribe(EventSource eventSource)
        {
            if (eventSource.Name.StartsWith("Microsoft-A", StringComparison.Ordinal))
            {
                switch (eventSource.Name)
                {
                    case "Microsoft-ApplicationInsights-Core": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/BASE/src/Microsoft.ApplicationInsights/Extensibility/Implementation/Tracing/CoreEventSource.cs
                    case "Microsoft-ApplicationInsights-WindowsServer-TelemetryChannel": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/BASE/src/ServerTelemetryChannel/Implementation/TelemetryChannelEventSource.cs

                    // AppMapCorrelation has a shared partial class: https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/Common/AppMapCorrelationEventSource.cs
                    case "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Dependency": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/DependencyCollector/DependencyCollector/Implementation/AppMapCorrelationEventSource.cs
                    case "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/Web/Web/Implementation/AppMapCorrelationEventSource.cs

                    case "Microsoft-ApplicationInsights-Extensibility-DependencyCollector": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/DependencyCollector/DependencyCollector/Implementation/DependencyCollectorEventSource.cs
                    case "Microsoft-ApplicationInsights-Extensibility-EventCounterCollector": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/EventCounterCollector/EventCounterCollector/EventCounterCollectorEventSource.cs
                    case "Microsoft-ApplicationInsights-Extensibility-PerformanceCollector": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/PerformanceCollector/PerformanceCollector/Implementation/PerformanceCollectorEventSource.cs
                    case "Microsoft-ApplicationInsights-Extensibility-PerformanceCollector-QuickPulse": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/PerformanceCollector/PerformanceCollector/Implementation/QuickPulse/QuickPulseEventSource.cs
                    case "Microsoft-ApplicationInsights-Extensibility-Web": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/Web/Web/Implementation/WebEventSource.cs
                    case "Microsoft-ApplicationInsights-Extensibility-WindowsServer": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/WindowsServer/WindowsServer/Implementation/WindowsServerEventSource.cs
                    case "Microsoft-ApplicationInsights-WindowsServer-Core": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/WEB/Src/WindowsServer/WindowsServer/Implementation/MetricManager.cs
                    case "Microsoft-ApplicationInsights-Extensibility-EventSourceListener": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/LOGGING/src/EventSource.Shared/EventSource.Shared/Implementation/EventSourceListenerEventSource.cs
                    case "Microsoft-ApplicationInsights-AspNetCore": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/master/NETCORE/src/Microsoft.ApplicationInsights.AspNetCore/Extensibility/Implementation/Tracing/AspNetCoreEventSource.cs
                    case "Microsoft-ApplicationInsights-LoggerProvider": // https://github.com/microsoft/ApplicationInsights-dotnet/blob/develop/LOGGING/src/ILogger/ApplicationInsightsLoggerEventSource.cs
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
