namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;

#if NET40 || NET35
    using Microsoft.Diagnostics.Tracing;
#endif
#if CORE_PCL || NET45 || WINRT || UWP || NET46
    using System.Diagnostics.Tracing;
#endif

    [EventSource(Name = "Microsoft-ApplicationInsights-Core")]
    internal sealed class CoreEventSource : EventSource
    {
        public static readonly CoreEventSource Log = new CoreEventSource();

        private readonly ApplicationNameProvider nameProvider = new ApplicationNameProvider();

        /// <summary>
        /// Logs the information when there operation to track is null.
        /// </summary>
        [Event(1, Message = "Operation object is null.", Level = EventLevel.Warning)]
        public void OperationIsNullWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, this.nameProvider.Name);
        }

        /// <summary>
        /// Logs the information when there operation to stop does not match the current operation.
        /// </summary>
        [Event(2, Message = "Operation to stop does not match the current operation.", Level = EventLevel.Error)]
        public void InvalidOperationToStopError(string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, this.nameProvider.Name);
        }

        [Event(
            10,
            Keywords = Keywords.VerboseFailure,
            Message = "[msg=Log verbose];[msg={0}]",
            Level = EventLevel.Verbose)]
        public void LogVerbose(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                10,
                msg ?? string.Empty,
                this.nameProvider.Name);
        }
        
        [Event(
            20,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable,
            Message = "Diagnostics event throttling has been started for the event {0}",
            Level = EventLevel.Informational)]
        public void DiagnosticsEventThrottlingHasBeenStartedForTheEvent(
            int eventId,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(20, eventId, this.nameProvider.Name);
        }

        [Event(
            30,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable,
            Message = "Diagnostics event throttling has been reset for the event {0}, event was fired {1} times during last interval",
            Level = EventLevel.Informational)]
        public void DiagnosticsEventThrottlingHasBeenResetForTheEvent(
            int eventId,
            int executionCount,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(30, eventId, executionCount, this.nameProvider.Name);
        }

        [Event(
            40,
            Keywords = Keywords.Diagnostics,
            Message = "Scheduler timer dispose failure: {0}",
            Level = EventLevel.Warning)]
        public void DiagnoisticsEventThrottlingSchedulerDisposeTimerFailure(
            string exception,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                40, 
                exception ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            50,
            Keywords = Keywords.Diagnostics,
            Message = "A scheduler timer was created for the interval: {0}",
            Level = EventLevel.Verbose)]
        public void DiagnoisticsEventThrottlingSchedulerTimerWasCreated(
            int intervalInMilliseconds,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(50, intervalInMilliseconds, this.nameProvider.Name);
        }

        [Event(
            60,
            Keywords = Keywords.Diagnostics,
            Message = "A scheduler timer was removed",
            Level = EventLevel.Verbose)]
        public void DiagnoisticsEventThrottlingSchedulerTimerWasRemoved(string appDomainName = "Incorrect")
        {
            this.WriteEvent(60, this.nameProvider.Name);
        }
        
        [Event(
            70,
            Message = "No Telemetry Configuration provided. Using the default TelemetryConfiguration.Active.",
            Level = EventLevel.Warning)]
        public void TelemetryClientConstructorWithNoTelemetryConfiguration(string appDomainName = "Incorrect")
        {
            this.WriteEvent(70, this.nameProvider.Name);
        }

        [Event(
            71,
            Message = "Value for property '{0}' of {1} was not found. Populating it by default.",
            Level = EventLevel.Verbose)]
        public void PopulateRequiredStringWithValue(string parameterName, string telemetryType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                71, 
                parameterName ?? string.Empty, 
                telemetryType ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            72,
            Message = "Invalid duration for Request Telemetry. Setting it to '00:00:00'.",
            Level = EventLevel.Warning)]
        public void RequestTelemetryIncorrectDuration(string appDomainName = "Incorrect")
        {
            this.WriteEvent(72, this.nameProvider.Name);
        }

        [Event(
           80,
           Message = "Telemetry tracking was disabled. Message is dropped.",
           Level = EventLevel.Verbose)]
        public void TrackingWasDisabled(string appDomainName = "Incorrect")
        {
            this.WriteEvent(80, this.nameProvider.Name);
        }

        [Event(
           81,
           Message = "Telemetry tracking was enabled. Messages are being logged.",
           Level = EventLevel.Verbose)]
        public void TrackingWasEnabled(string appDomainName = "Incorrect")
        {
            this.WriteEvent(81, this.nameProvider.Name);
        }

        [Event(
            90,
            Keywords = Keywords.ErrorFailure,
            Message = "[msg=Log Error];[msg={0}]",
            Level = EventLevel.Error)]
        public void LogError(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                90, 
                msg ?? string.Empty,
                this.nameProvider.Name);
        }        

        /// <summary>
        /// Keywords for the PlatformEventSource.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Key word for user actionable events.
            /// </summary>
            public const EventKeywords UserActionable = (EventKeywords)EventSourceKeywords.UserActionable;

            /// <summary>
            /// Keyword for errors that trace at Verbose level.
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)EventSourceKeywords.Diagnostics;

            /// <summary>
            /// Keyword for errors that trace at Verbose level.
            /// </summary>
            public const EventKeywords VerboseFailure = (EventKeywords)EventSourceKeywords.VerboseFailure;

            /// <summary>
            /// Keyword for errors that trace at Error level.
            /// </summary>
            public const EventKeywords ErrorFailure = (EventKeywords)EventSourceKeywords.ErrorFailure;
        }
    }
}
