//-----------------------------------------------------------------------
// <copyright file="EventSourceListenerEventSource.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EventSourceListener.Implementation
{
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// EventSource for reporting errors and warnings from the EventSourceListener telemetry module.
    /// </summary>
    [EventSource(Name = ProviderName)]
    internal sealed class EventSourceListenerEventSource : EventSource
    {
        public const string ProviderName = "Microsoft-ApplicationInsights-Extensibility-EventSourceListener";
        public static readonly EventSourceListenerEventSource Log = new EventSourceListenerEventSource();

        private const int NoEventSourcesConfiguredEventId = 1;

        private EventSourceListenerEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        public string ApplicationName { [NonEvent]get; [NonEvent]private set; }

        [Event(NoEventSourcesConfiguredEventId, Level = EventLevel.Warning, Keywords = Keywords.Configuration,
        Message = "No EventSources configured for the EventSourceListenerModule")]
        public void NoEventSourcesConfigured(string applicationName = null)
        {
            this.WriteEvent(NoEventSourcesConfiguredEventId, applicationName ?? this.ApplicationName);
        }

        [NonEvent]
        private string GetApplicationName()
        {
            string name;
            try
            {
                name = AppDomain.CurrentDomain.FriendlyName;
            }
            catch
            {
                name = "(unknown)";
            }

            return name;
        }

        public sealed class Keywords
        {
            public const EventKeywords Configuration = (EventKeywords)0x01;
        }
    }
}
