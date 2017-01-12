//-----------------------------------------------------------------------
// <copyright file="EventSourceListenerEventSource.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EventSourceListener.Implementation
{
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
        [Event(NoEventSourcesConfiguredEventId, Level = EventLevel.Warning, Keywords = Keywords.Configuration,
                Message = "No EventSources configured for the EventSourceListenerModule")]
        public void NoEventSourcesConfigured()
        {
            this.WriteEvent(NoEventSourcesConfiguredEventId);
        }

        public sealed class Keywords
        {
            public const EventKeywords Configuration = (EventKeywords)0x01;
        }
    }
}
