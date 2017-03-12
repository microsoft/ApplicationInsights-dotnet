#if NET45
    // .Net 4.5 has a private implementation of this
#else
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

    /// <summary>
    /// Event Source exposes Application Insights telemetry information as ETW events.
    /// </summary>
    internal sealed partial class RichPayloadEventSource : IDisposable
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

                item.Sanitize();
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

                item.Sanitize();
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

                item.Sanitize();
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

                item.Sanitize();
                var telemetryItem = item as DependencyTelemetry;
                this.WriteEvent(
                    DependencyTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.InternalData,
                    Keywords.Dependencies);
            }
            else if (item is MetricTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Metrics))
                {
                    return;
                }

                item.Sanitize();
                var telemetryItem = item as MetricTelemetry;
                this.WriteEvent(
                    MetricTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.Tags,
                    telemetryItem.Data,
                    Keywords.Metrics);
            }
            else if (item is ExceptionTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Exceptions))
                {
                    return;
                }

                item.Sanitize();
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

                item.Sanitize();
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

                item.Sanitize();
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

                item.Sanitize();
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

                item.Sanitize();
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
        /// Record an operation start.
        /// </summary>
        /// <param name="operation">The operation.</param>
        public void ProcessOperationStart(OperationTelemetry operation)
        {
            if (this.EventSourceInternal.IsEnabled(EventLevel.Informational, Keywords.Operations))
            {
                this.WriteEvent(operation, EventOpcode.Start);
            }
        }

        /// <summary>
        /// Record an operation stop.
        /// </summary>
        /// <param name="operation">The operation.</param>
        public void ProcessOperationStop(OperationTelemetry operation)
        {
            if (this.EventSourceInternal.IsEnabled(EventLevel.Informational, Keywords.Operations))
            {
                this.WriteEvent(operation, EventOpcode.Stop);
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

        private void WriteEvent(OperationTelemetry item, EventOpcode eventOpCode)
        {
            var payload = new { IKey = item.Context.InstrumentationKey, Id = item.Id, Name = item.Name, RootId = item.Context.Operation.Id };

            if (item is RequestTelemetry)
            {
                this.EventSourceInternal.Write(
                    RequestTelemetry.TelemetryName,
                    new EventSourceOptions { Keywords = Keywords.Operations, Opcode = eventOpCode, Level = EventLevel.Informational },
                    payload);
            }
            else
            {
                this.EventSourceInternal.Write(
                    OperationTelemetry.TelemetryName,
                    new EventSourceOptions { ActivityOptions = EventActivityOptions.Recursive, Keywords = Keywords.Operations, Opcode = eventOpCode, Level = EventLevel.Informational },
                    payload);
            }
        }
    }
}
#endif