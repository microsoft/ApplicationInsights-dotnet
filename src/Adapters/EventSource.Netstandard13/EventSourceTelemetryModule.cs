//-----------------------------------------------------------------------
// <copyright file="EventSourceTelemetryModule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EventSourceListener
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using Microsoft.ApplicationInsights.EventSourceListener.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Implementation;
    using Microsoft.ApplicationInsights.TraceEvent.Shared.Implementation;

    /// <summary>
    /// A module to trace data submitted via .NET framework <seealso cref="System.Diagnostics.Tracing.EventSource" /> class.
    /// </summary>
    public class EventSourceTelemetryModule : EventListener, ITelemetryModule
    {
        private static readonly Guid TplEventSourceGuid = new Guid("2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5");
        private static readonly long TaskFlowActivityIds = 0x80;

        private TelemetryClient client;
        private bool initialized; // Relying on the fact that default value in .NET Framework is false
        // The following does not really need to be a ConcurrentQueue, but the ConcurrentQueue has a very convenient-to-use TryDequeue method.
        private ConcurrentQueue<EventSource> appDomainEventSources;
        private List<EventSource> enabledEventSources;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourceTelemetryModule"/> class.
        /// </summary>
        public EventSourceTelemetryModule()
        {
            this.Sources = new List<EventSourceListeningRequest>();
            this.enabledEventSources = new List<EventSource>();
        }

        /// <summary>
        /// Gets the list of EventSource listening requests (information about which EventSources should be traced).
        /// </summary>
        public IList<EventSourceListeningRequest> Sources { get; private set; }

        /// <summary>
        /// Initializes the telemetry module and starts tracing EventSources specified via <see cref="Sources"/> property.
        /// </summary>
        /// <param name="configuration">Module configuration.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("evl:");

            if (this.Sources.Count == 0)
            {
                EventSourceListenerEventSource.Log.NoSourcesConfigured(nameof(EventSourceListener.EventSourceTelemetryModule));
                return;
            }

            // See OnEventSourceCreated() for the reason why we are locking on 'this' here.
            lock (this)
            {
                if (this.initialized)
                {
                    // Source listening requests might have changed between initializations. Let's start from a clean slate
                    foreach (EventSource eventSource in this.enabledEventSources)
                    {
                        this.DisableEvents(eventSource);
                    }
                    this.enabledEventSources.Clear();
                }

                try
                {
                    if (this.appDomainEventSources != null)
                    {
                        EventSource eventSource;
                        ConcurrentQueue<EventSource> futureIntializationSources = new ConcurrentQueue<EventSource>();

                        while (this.appDomainEventSources.TryDequeue(out eventSource))
                        {
                            this.EnableAsNecessary(eventSource);
                            futureIntializationSources.Enqueue(eventSource);
                        }

                        this.appDomainEventSources = futureIntializationSources;
                    }
                }
                finally
                {
                    this.initialized = true;
                }
            }
        }

        /// <summary>
        /// Processes a new EventSource event.
        /// </summary>
        /// <param name="eventData">Event to process.</param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (this.initialized)
            {
                eventData.Track(this.client);
            }
        }

        /// <summary>
        /// Processes notifications about new EventSource creation.
        /// </summary>
        /// <param name="eventSource">EventSource instance.</param>
        /// <remarks>When an instance of an EventListener is created, it will immediately receive notifications about all EventSources already existing in the AppDomain.
        /// Then, as new EventSources are created, the EventListener will receive notifications about them.</remarks>
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // There is a bug in the EventListener library that causes this override to be called before the object is fully constructed.
            // We need to remember all EventSources we get notified about to do the initialization correctly (event if it happens multiple times).
            // If we are initialized, we can follow up with enabling the source straight away.

            // Locking on 'this' is generally a bad practice because someone from outside could put a lock on us, and this is outside of our control.
            // But in the case of this class it is an unlikely scenario, and because of the bug described above,
            // we cannot rely on construction to prepare a private lock object for us.

            // Also note that we are using a queue to cover the case when EnableEvents() called from Initialize()
            // may result in reentrant call into OnEventSourceCreated().
            lock (this)
            {
                if (this.appDomainEventSources == null)
                {
                    this.appDomainEventSources = new ConcurrentQueue<EventSource>();
                }

                this.appDomainEventSources.Enqueue(eventSource);

                if (this.initialized)
                {
                    this.EnableAsNecessary(eventSource);
                }
            }
        }

        /// <summary>
        /// Enables a single EventSource for tracing.
        /// </summary>
        /// <param name="eventSource">EventSource to enable.</param>
        private void EnableAsNecessary(EventSource eventSource)
        {
            // Special case: enable TPL activity flow for better tracing of nested activities.
            if (eventSource.Guid == EventSourceTelemetryModule.TplEventSourceGuid)
            {
                this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)EventSourceTelemetryModule.TaskFlowActivityIds);
                this.enabledEventSources.Add(eventSource);
            }
            else if (eventSource.Name == EventSourceListenerEventSource.ProviderName)
            {
                // Tracking ourselves does not make much sense
                return;
            }
            else
            {
                EventSourceListeningRequest listeningRequest = this.Sources?.FirstOrDefault(s => s.Name == eventSource.Name);
                if (listeningRequest != null)
                {
                    // LIMITATION: There is a known issue where if we listen to the FrameworkEventSource, the dataflow pipeline may hang when it
                    // tries to process the Threadpool event. The reason is the dataflow pipeline itself is using Task library for scheduling async
                    // tasks, which then itself also fires Threadpool events on FrameworkEventSource at unexpected locations, and trigger deadlocks.
                    // Hence, we like special case this and mask out Threadpool events.
                    EventKeywords keywords = listeningRequest.Keywords;
                    if (listeningRequest.Name == "System.Diagnostics.Eventing.FrameworkEventSource")
                    {
                        // Turn off the Threadpool | ThreadTransfer keyword. Definition is at http://referencesource.microsoft.com/#mscorlib/system/diagnostics/eventing/frameworkeventsource.cs
                        // However, if keywords was to begin with, then we need to set it to All first, which is 0xFFFFF....
                        if (keywords == 0)
                        {
                            keywords = EventKeywords.All;
                        }
                        keywords &= (EventKeywords)~0x12;
                    }

                    this.EnableEvents(eventSource, listeningRequest.Level, keywords);
                    this.enabledEventSources.Add(eventSource);
                }
            }
        }
    }
}
