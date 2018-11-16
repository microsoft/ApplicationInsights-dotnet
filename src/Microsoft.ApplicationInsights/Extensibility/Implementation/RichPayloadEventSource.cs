#if !NET45 // .Net 4.5 has a private implementation of this
namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Globalization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

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
                
                var telemetryItem = item as RequestTelemetry;
                // Sanitize, Copying global properties is to be done before calling .Data,
                // as Data returns a singleton instance, which won't be updated with changes made
                // after .Data is called.
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    RequestTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
                    Keywords.Requests);
            }
            else if (item is TraceTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Traces))
                {
                    return;
                }

                var telemetryItem = item as TraceTelemetry;
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    TraceTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
                    Keywords.Traces);
            }
            else if (item is EventTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Events))
                {
                    return;
                }

                var telemetryItem = item as EventTelemetry;
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    EventTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
                    Keywords.Events);
            }
            else if (item is DependencyTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Dependencies))
                {
                    return;
                }

                var telemetryItem = item as DependencyTelemetry;
                // Sanitize, Copying global properties is to be done before calling .InternalData,
                // as InternalData returns a singleton instance, which won't be updated with changes made
                // after .InternalData is called.
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    DependencyTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.InternalData,
                    telemetryItem.Context.Flags,
                    Keywords.Dependencies);
            }
            else if (item is MetricTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Metrics))
                {
                    return;
                }
                
                var telemetryItem = item as MetricTelemetry;
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    MetricTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
                    Keywords.Metrics);
            }
            else if (item is ExceptionTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Exceptions))
                {
                    return;
                }
                
                var telemetryItem = item as ExceptionTelemetry;
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    ExceptionTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data.Data,
                    telemetryItem.Context.Flags,
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
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    MetricTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
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
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    PageViewTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
                    Keywords.PageViews);
            }
            else if (item is PageViewPerformanceTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.PageViewPerformance))
                {
                    return;
                }
                
                var telemetryItem = item as PageViewPerformanceTelemetry;
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    PageViewPerformanceTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
                    Keywords.PageViewPerformance);
            }
#pragma warning disable 618
            else if (item is SessionStateTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Events))
                {
                    return;
                }
                
                var telemetryItem = (item as SessionStateTelemetry).Data;
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    EventTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
                    Keywords.Events);
            }
            else if (item is AvailabilityTelemetry)
            {
                if (!this.EventSourceInternal.IsEnabled(EventLevel.Verbose, Keywords.Availability))
                {
                    return;
                }
                
                var telemetryItem = item as AvailabilityTelemetry;
                CopyGlobalPropertiesIfRequired(item, telemetryItem.Properties);
                item.Sanitize();
                this.WriteEvent(
                    AvailabilityTelemetry.TelemetryName,
                    telemetryItem.Context.InstrumentationKey,
                    telemetryItem.Context.SanitizedTags,
                    telemetryItem.Data,
                    telemetryItem.Context.Flags,
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

        private static void CopyGlobalPropertiesIfRequired(ITelemetry telemetry, IDictionary<string, string> itemProperties)
        {
            // This check avoids accessing the public accessor GlobalProperties
            // unless needed, to avoid the penality of ConcurrentDictionary instantiation.
            if (telemetry.Context.GlobalPropertiesValue != null)
            {
                Utils.CopyDictionary(telemetry.Context.GlobalProperties, itemProperties);
            }
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

        private void WriteEvent<T>(string eventName, string instrumentationKey, IDictionary<string, string> tags, T data, long flags, EventKeywords keywords)
        {
            this.EventSourceInternal.Write(
                eventName,
                new EventSourceOptions() { Keywords = keywords },
                new { PartA_iKey = instrumentationKey, PartA_Tags = tags, _B = data, PartA_flags = flags });
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
#else
namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// RichPayload Event Source (.Net 4.5 version)
    /// It dynamically checks the runtime version and only emits the event if the runtime is .Net Framework 4.6 and above.
    /// As .Net 4.5 project doesn't support EventDataAttribute, the class uses the anonymous type (RichPayloadEventSource.TelemetryHandler.cs) for the corresponding telemetry data type.
    /// The anonymous type keeps the same properties and layout as the telemetry data type schema. 
    /// Once you update the data type, you should also update the anonymous type.
    /// </summary>
    internal sealed partial class RichPayloadEventSource : IDisposable
    {
        /// <summary>RichPayloadEventSource instance.</summary>
        public static readonly RichPayloadEventSource Log = new RichPayloadEventSource();

        /// <summary>Event source.</summary>
        internal readonly EventSource EventSourceInternal;

        /// <summary>Event provider name.</summary>
        private const string EventProviderName = "Microsoft-ApplicationInsights-Data";

        /// <summary>A dictionary mapping each telemetry item type to its handler.</summary>
        private readonly Dictionary<Type, Action<ITelemetry>> telemetryHandlers;

        /// <summary>Handler for <see cref="OperationTelemetry"/> start/stop operations.</summary>
        private readonly Action<OperationTelemetry, EventOpcode> operationStartStopHandler;

        /// <summary>
        /// Initializes a new instance of the RichPayloadEventSource class.
        /// </summary>
        public RichPayloadEventSource()
        {
            if (AppDomain.CurrentDomain.IsHomogenous && AppDomain.CurrentDomain.IsFullyTrusted)
            {
                var eventSourceType = typeof(EventSource);
                var eventSourceSettingsType = eventSourceType.Assembly.GetType("System.Diagnostics.Tracing.EventSourceSettings");

                if (eventSourceSettingsType != null)
                {
                    var etwSelfDescribingEventFormat = Enum.ToObject(eventSourceSettingsType, 8);
                    this.EventSourceInternal = (EventSource)Activator.CreateInstance(eventSourceType, EventProviderName, etwSelfDescribingEventFormat);

                    // CreateTelemetryHandlers is defined in RichPayloadEventSource.TelemetryHandler.cs
                    this.telemetryHandlers = this.CreateTelemetryHandlers(this.EventSourceInternal);

                    this.operationStartStopHandler = this.CreateOperationStartStopHandler(this.EventSourceInternal);
                }
            }
        }

        /// <summary>
        /// Process a collected telemetry item.
        /// </summary>
        /// <param name="item">A collected Telemetry item.</param>
        public void Process(ITelemetry item)
        {
            if (this.EventSourceInternal == null)
            {
                return;
            }

            Action<ITelemetry> handler = null;
            var itemType = item.GetType();
            if (!this.telemetryHandlers.TryGetValue(itemType, out handler))
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Unknown telemetry type: {0}", itemType.FullName);
                CoreEventSource.Log.LogVerbose(msg);

                return;
            }

            handler(item);
        }

        /// <summary>
        /// Record an operation start.
        /// </summary>
        /// <param name="operation">The operation which is about to start.</param>
        public void ProcessOperationStart(OperationTelemetry operation)
        {
            if (this.EventSourceInternal == null)
            {
                return;
            }

            if (this.EventSourceInternal.IsEnabled(EventLevel.Informational, Keywords.Operations))
            {
                this.operationStartStopHandler(operation, EventOpcode.Start);
            }
        }

        /// <summary>
        /// Record an operation stop.
        /// </summary>
        /// <param name="operation">The operation which has just stopped.</param>
        public void ProcessOperationStop(OperationTelemetry operation)
        {
            if (this.EventSourceInternal == null)
            {
                return;
            }

            if (this.EventSourceInternal.IsEnabled(EventLevel.Informational, Keywords.Operations))
            {
                this.operationStartStopHandler(operation, EventOpcode.Stop);
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
    }
}
#endif