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
            /// Keyword for performance counters.
            /// </summary>
            public const EventKeywords PerformanceCounters = (EventKeywords)0x80;

            /// <summary>
            /// Keyword for operations (Start/Stop).
            /// </summary>
            public const EventKeywords Operations = (EventKeywords)0x80;

            /// <summary>
            /// Keyword for session state.
            /// </summary>
            public const EventKeywords SessionState = (EventKeywords)0x100;
        }
    }
}
