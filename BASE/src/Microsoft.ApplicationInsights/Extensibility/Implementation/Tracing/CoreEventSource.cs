namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;

    [EventSource(Name = "Microsoft-ApplicationInsights-Core")]
    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "appDomainName is required")]
    internal sealed class CoreEventSource : EventSource
    {
        public static readonly CoreEventSource Log = new CoreEventSource();

        private readonly ApplicationNameProvider nameProvider = new ApplicationNameProvider();

        public static bool IsVerboseEnabled
        {
            [NonEvent]
            get
            {
                return Log.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
            }
        }

        /// <summary>
        /// Logs the information when there operation to track is null.
        /// </summary>
        [Event(1, Message = "Operation object is null.", Level = EventLevel.Warning)]
        public void OperationIsNullWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, this.nameProvider.Name);
        }

        [Event(
            2,
            Message = "Value for property '{0}' of {1} was not found. Populating it by default.",
            Level = EventLevel.Verbose)]
        public void PopulateRequiredStringWithValue(string parameterName, string telemetryType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                2, 
                parameterName ?? string.Empty, 
                telemetryType ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            3,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' was not found. Type loading was skipped. Monitoring will continue.",
            Level = EventLevel.Error)]
        public void TypeWasNotFoundConfigurationError(string type, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                3,
                type ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            4,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' does not implement '{1}'. Type loading was skipped. Monitoring will continue.",
            Level = EventLevel.Error)]
        public void IncorrectTypeConfigurationError(string type, string expectedType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                4,
                type ?? string.Empty,
                expectedType ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            5,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' will not be create. Error: '{1}'. Monitoring will continue if you set InstrumentationKey programmatically.",
            Level = EventLevel.Error)]
        public void MissingMethodExceptionConfigurationError(string type, string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                5,
                type ?? string.Empty,
                error ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            6,
            Message = "Failed to get environment variables due to security exception; code is likely running in partial trust. Exception: {0}.",
            Level = EventLevel.Warning)]
        public void FailedToLoadEnvironmentVariables(string ex, string appDomainName = "Incorrect")
        {
            this.WriteEvent(6, ex, this.nameProvider.Name);
        }

        [Event(7,
            Message = "Initialization is skipped for the sampled item.",
            Level = EventLevel.Informational)]
        public void InitializationIsSkippedForSampledItem(string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                7,
                this.nameProvider.Name);
        }

        [Event(8, Message = "TelemetryClient.Flush was invoked.", Level = EventLevel.Verbose)]
        public void TelemetlyClientFlush(string appDomainName = "Incorrect") => this.WriteEvent(8, this.nameProvider.Name);

        [Event(9, Message = "Failed to start hosted services. Exception: {0}", Level = EventLevel.Warning, Keywords = Keywords.UserActionable)]
        public void FailedToStartHostedServices(string exception, string appDomainName = "Incorrect") => this.WriteEvent(9, exception ?? string.Empty, this.nameProvider.Name);

        [Event(10, Message = "TrackEvent was called with a null or empty event name. The event will not be tracked properly.", Level = EventLevel.Warning, Keywords = Keywords.UserActionable)]
        public void TrackEventInvalidName(string appDomainName = "Incorrect") => this.WriteEvent(10, this.nameProvider.Name);

        [Event(11, Message = "TrackEvent was called with a null EventTelemetry object. The event will be ignored.", Level = EventLevel.Warning, Keywords = Keywords.UserActionable)]
        public void TrackEventTelemetryIsNull(string appDomainName = "Incorrect") => this.WriteEvent(11, this.nameProvider.Name);

        [Event(12, Message = "Track was called with an unsupported telemetry type: {0}. Only RequestTelemetry, DependencyTelemetry, TraceTelemetry, EventTelemetry, and ExceptionTelemetry are supported.", Level = EventLevel.Warning, Keywords = Keywords.UserActionable)]
        public void UnsupportedTelemetryType(string telemetryType, string appDomainName = "Incorrect") => this.WriteEvent(12, telemetryType ?? "Unknown", this.nameProvider.Name);

        [Event(13, Message = "FlushAsync failed with exception: {0}", Level = EventLevel.Warning, Keywords = Keywords.UserActionable)]
        public void FlushAsyncFailed(string exception, string appDomainName = "Incorrect") => this.WriteEvent(13, exception ?? string.Empty, this.nameProvider.Name);

        [Event(14, Message = "TelemetryClient.FlushAsync was invoked.", Level = EventLevel.Verbose)]
        public void TelemetryClientFlushAsync(string appDomainName = "Incorrect") => this.WriteEvent(14, this.nameProvider.Name);

        [Event(15, Message = "FlushAsync was cancelled by the caller.", Level = EventLevel.Informational)]
        public void FlushAsyncCancelled(string appDomainName = "Incorrect") => this.WriteEvent(15, this.nameProvider.Name);

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