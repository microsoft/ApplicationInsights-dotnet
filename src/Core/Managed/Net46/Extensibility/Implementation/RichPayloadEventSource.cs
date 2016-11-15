namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
#if NET40
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.Diagnostics.Tracing;
#else
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Globalization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
#endif
    internal sealed class RichPayloadEventSource : IDisposable
    {
        /// <summary>RichPayloadEventSource instance.</summary>
        public static readonly RichPayloadEventSource Log = new RichPayloadEventSource();
        
        /// <summary>Event source.</summary>
        internal readonly EventSource EventSourceInternal;

        /// <summary>Event provider name.</summary>
        private const string EventProviderName = "Microsoft-ApplicationInsights-Data";

        /// <summary>
        /// Initializes a new instance of the RichPayloadEventSource class.
        /// </summary>
        public RichPayloadEventSource()
        {
            this.EventSourceInternal = new EventSource(
               EventProviderName,
               EventSourceSettings.EtwSelfDescribingEventFormat);
        }

        /// <summary>
        /// Process a collected telemetry item.
        /// </summary>
        /// <param name="item">A collected Telemetry item.</param>
        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Requests))
                {
                    return;
                }

                var telemetryItem = item as RequestTelemetry;
                this.WriteEvent(
                    RequestTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data, 
                    Keywords.Requests);
            }
            else if (item is TraceTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Traces))
                {
                    return;
                }

                var telemetryItem = item as TraceTelemetry;
                this.WriteEvent(
                    TraceTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Traces);
            }
            else if (item is EventTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Events))
                {
                    return;
                }

                var telemetryItem = item as EventTelemetry;
                this.WriteEvent(
                    EventTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Events);
            }
            else if (item is DependencyTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Dependencies))
                {
                    return;
                }

                var telemetryItem = item as DependencyTelemetry;
                this.WriteEvent(
                    DependencyTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.InternalData,
                    Keywords.Dependencies);
            }
#pragma warning disable CS0618
            else if (item is MetricTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Metrics))
                {
                    return;
                }

                var telemetryItem = item as MetricTelemetry;
                this.WriteEvent(
                    MetricTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Metrics);
            }
#pragma warning restore CS0618
            else if (item is ExceptionTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Exceptions))
                {
                    return;
                }

                var telemetryItem = item as ExceptionTelemetry;
                this.WriteEvent(
                    ExceptionTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Exceptions);
            }
#pragma warning disable 618
            else if (item is PerformanceCounterTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Metrics))
                {
                    return;
                }

                var telemetryItem = (item as PerformanceCounterTelemetry).Data;
                this.WriteEvent(
                    MetricTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Metrics);
            }
#pragma warning restore 618
            else if (item is PageViewTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.PageViews))
                {
                    return;
                }

                var telemetryItem = item as PageViewTelemetry;
                this.WriteEvent(
                    PageViewTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.PageViews);
            }
#pragma warning disable 618
            else if (item is SessionStateTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Events))
                {
                    return;
                }

                var telemetryItem = (item as SessionStateTelemetry).Data;
                this.WriteEvent(
                    EventTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Events);
            }
            else if (item is AvailabilityTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Availability))
                {
                    return;
                }

                var telemetryItem = item as AvailabilityTelemetry;
                this.WriteEvent(
                    AvailabilityTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Availability);
            }
            else
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Unknown telemetry type: {0}", item.GetType());
                CoreEventSource.Log.LogVerbose(msg);
            }
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.EventSourceInternal != null)
                {
                    this.EventSourceInternal.Dispose();
                }
            }
        }

        private void WriteEvent<T>(string eventName, string instrumentationKey, IDictionary<string, string> tags, T data, EventKeywords keywords)
        {
            this.EventSourceInternal.Write(
                eventName,
                new EventSourceOptions() { Keywords = keywords },
                new { PartA_iKey = instrumentationKey, PartA_Tags = tags, _B = data });
        }

        /// <summary>
        /// Keywords for the RichPayloadEventSource.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Keyword for requests.
            /// </summary>
            public const EventKeywords Requests = (EventKeywords)0x1;

            /// <summary>
            /// Keyword for traces.
            /// </summary>
            public const EventKeywords Traces = (EventKeywords)0x2;

            /// <summary>
            /// Keyword for events.
            /// </summary>
            public const EventKeywords Events = (EventKeywords)0x4;

            /// <summary>
            /// Keyword for exceptions.
            /// </summary>
            public const EventKeywords Exceptions = (EventKeywords)0x8;

            /// <summary>
            /// Keyword for dependencies.
            /// </summary>
            public const EventKeywords Dependencies = (EventKeywords)0x10;

            /// <summary>
            /// Keyword for metrics.
            /// </summary>
            public const EventKeywords Metrics = (EventKeywords)0x20;

            /// <summary>
            /// Keyword for page views.
            /// </summary>
            public const EventKeywords PageViews = (EventKeywords)0x40;

            /// <summary>
            /// Keyword for availability.
            /// </summary>
            public const EventKeywords Availability = (EventKeywords)0x200;
        }
    }
}
