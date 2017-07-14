namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
#if NET40
    using Microsoft.Diagnostics.Tracing;
#else
    using System.Diagnostics.Tracing;
#endif

    [EventSource(Name = "Microsoft-ApplicationInsights-Core")]
    internal sealed class CoreEventSource : EventSource
    {
        public static readonly CoreEventSource Log = new CoreEventSource();

        private readonly ApplicationNameProvider nameProvider = new ApplicationNameProvider();

        public bool IsVerboseEnabled
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

        /// <summary>
        /// Logs the information when there operation to stop does not match the current operation.
        /// </summary>
        [Event(2, Message = "Operation to stop does not match the current operation.", Level = EventLevel.Error)]
        public void InvalidOperationToStopError(string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, this.nameProvider.Name);
        }

        [Event(
            3,
            Keywords = Keywords.VerboseFailure,
            Message = "[msg=Log verbose];[msg={0}]",
            Level = EventLevel.Verbose)]
        public void LogVerbose(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                3,
                msg ?? string.Empty,
                this.nameProvider.Name);
        }
        
        [Event(
            4,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable,
            Message = "Diagnostics event throttling has been started for the event {0}",
            Level = EventLevel.Informational)]
        public void DiagnosticsEventThrottlingHasBeenStartedForTheEvent(
            string eventId,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(4, eventId ?? "NULL", this.nameProvider.Name);
        }

        [Event(
            5,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable,
            Message = "Diagnostics event throttling has been reset for the event {0}, event was fired {1} times during last interval",
            Level = EventLevel.Informational)]
        public void DiagnosticsEventThrottlingHasBeenResetForTheEvent(
            int eventId,
            int executionCount,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(5, eventId, executionCount, this.nameProvider.Name);
        }

        [Event(
            6,
            Keywords = Keywords.Diagnostics,
            Message = "Scheduler timer dispose failure: {0}",
            Level = EventLevel.Warning)]
        public void DiagnoisticsEventThrottlingSchedulerDisposeTimerFailure(
            string exception,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                6, 
                exception ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            7,
            Keywords = Keywords.Diagnostics,
            Message = "A scheduler timer was created for the interval: {0}",
            Level = EventLevel.Verbose)]
        public void DiagnoisticsEventThrottlingSchedulerTimerWasCreated(
            string intervalInMilliseconds,
            string appDomainName = "Incorrect")
        {
            this.WriteEvent(7, intervalInMilliseconds ?? "NULL", this.nameProvider.Name);
        }

        [Event(
            8,
            Keywords = Keywords.Diagnostics,
            Message = "A scheduler timer was removed",
            Level = EventLevel.Verbose)]
        public void DiagnoisticsEventThrottlingSchedulerTimerWasRemoved(string appDomainName = "Incorrect")
        {
            this.WriteEvent(8, this.nameProvider.Name);
        }
        
        [Event(
            9,
            Message = "No Telemetry Configuration provided. Using the default TelemetryConfiguration.Active.",
            Level = EventLevel.Warning)]
        public void TelemetryClientConstructorWithNoTelemetryConfiguration(string appDomainName = "Incorrect")
        {
            this.WriteEvent(9, this.nameProvider.Name);
        }

        [Event(
            10,
            Message = "Value for property '{0}' of {1} was not found. Populating it by default.",
            Level = EventLevel.Verbose)]
        public void PopulateRequiredStringWithValue(string parameterName, string telemetryType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                10, 
                parameterName ?? string.Empty, 
                telemetryType ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            11,
            Message = "Invalid duration for Telemetry. Setting it to '00:00:00'.",
            Level = EventLevel.Warning)]
        public void TelemetryIncorrectDuration(string appDomainName = "Incorrect")
        {
            this.WriteEvent(11, this.nameProvider.Name);
        }

        [Event(
           12,
           Message = "Telemetry tracking was disabled. Message is dropped.",
           Level = EventLevel.Verbose)]
        public void TrackingWasDisabled(string appDomainName = "Incorrect")
        {
            this.WriteEvent(12, this.nameProvider.Name);
        }

        [Event(
           13,
           Message = "Telemetry tracking was enabled. Messages are being logged.",
           Level = EventLevel.Verbose)]
        public void TrackingWasEnabled(string appDomainName = "Incorrect")
        {
            this.WriteEvent(13, this.nameProvider.Name);
        }

        [Event(
            14,
            Keywords = Keywords.ErrorFailure,
            Message = "[msg=Log Error];[msg={0}]",
            Level = EventLevel.Error)]
        public void LogError(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                14, 
                msg ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            15,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' was not found. Type loading was skipped. Monitoring will continue.",
            Level = EventLevel.Error)]
        public void TypeWasNotFoundConfigurationError(string type, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                15,
                type ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            16,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' does not implement '{1}'. Type loading was skipped. Monitoring will continue.",
            Level = EventLevel.Error)]
        public void IncorrectTypeConfigurationError(string type, string expectedType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                16,
                type ?? string.Empty,
                expectedType ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            17,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' does not have property '{1}'. Property initialization was skipped. Monitoring will continue.",
            Level = EventLevel.Error)]
        public void IncorrectPropertyConfigurationError(string type, string property, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                17,
                type ?? string.Empty,
                property ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            18,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Element '{0}' element does not have a Type attribute, does not specify a value and is not a valid collection type. Type initialization was skipped. Monitoring will continue.",
            Level = EventLevel.Error)]
        public void IncorrectInstanceAtributesConfigurationError(string definition, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                18,
                definition ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            19,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. '{0}' element has unexpected contents: '{1}': '{2}'. Type initialization was skipped. Monitoring will continue.",
            Level = EventLevel.Error)]
        public void LoadInstanceFromValueConfigurationError(string element, string contents, string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                19,
                element ?? string.Empty,
                contents ?? string.Empty,
                error ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            20,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Exception: '{0}'. Monitoring will continue if you set InstrumentationKey programmatically.",
            Level = EventLevel.Error)]
        public void ConfigurationFileCouldNotBeParsedError(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                20,
                error ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            21,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' will not be create. Error: '{1}'. Monitoring will continue if you set InstrumentationKey programmatically.",
            Level = EventLevel.Error)]
        public void MissingMethodExceptionConfigurationError(string type, string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                21,
                type ?? string.Empty,
                error ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            22,
            Keywords = Keywords.UserActionable,
            Message = "ApplicationInsights configuration file loading failed. Type '{0}' will not be initialized. Error: '{1}'. Monitoring will continue if you set InstrumentationKey programmatically.",
            Level = EventLevel.Error)]
        public void ComponentInitializationConfigurationError(string type, string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                22,
                type ?? string.Empty,
                error ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            23,
            Message = "ApplicationInsights configuration file '{0}' was not found.",
            Level = EventLevel.Warning)]
        public void ApplicationInsightsConfigNotFoundWarning(string file, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                23,
                file ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            24,
            Message = "Failed to send: {0}.",
            Level = EventLevel.Warning)]
        public void FailedToSend(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                24,
                msg ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
           25,
           Message = "Exception happened during getting the machine name: '{0}'.",
           Level = EventLevel.Error)]
        public void FailedToGetMachineName(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                25,
                error ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            26,
            Message = "Failed to flush aggregated metrics. Exception: {0}.",
            Level = EventLevel.Error)]
        public void FailedToFlushMetricAggregators(string ex, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                26,
                ex ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            27,
            Message = "Failed to snapshot aggregated metrics. Exception: {0}.",
            Level = EventLevel.Error)]
        public void FailedToSnapshotMetricAggregators(string ex, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                27,
                ex ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            28,
            Message = "Failed to invoke metric processor '{0}'. If the issue persists, remove the processor. Exception: {1}.",
            Level = EventLevel.Error)]
        public void FailedToRunMetricProcessor(string processorName, string ex, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                28,
                processorName ?? string.Empty,
                ex ?? string.Empty,
                this.nameProvider.Name);
        }

        [Event(
            29,
            Message = "The backlog of unsent items has reached maximum size of {0}. Items will be dropped until the backlog is cleared.",
            Level = EventLevel.Error)]
        public void ItemDroppedAsMaximumUnsentBacklogSizeReached(int maxBacklogSize, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                29,
                maxBacklogSize,               
                this.nameProvider.Name);
        }

        [Event(
            30,
            Message = "Flush was called on the telemetry channel (InMemoryChannel) after it was disposed.",
            Level = EventLevel.Warning)]
        public void InMemoryChannelFlushedAfterBeingDisposed(string appDomainName = "Incorrect")
        {
            this.WriteEvent(30, this.nameProvider.Name);
        }

        [Event(
            31,
            Message = "Send was called on the telemetry channel (InMemoryChannel) after it was disposed, the telemetry data was dropped.",
            Level = EventLevel.Warning)]
        public void InMemoryChannelSendCalledAfterBeingDisposed(string appDomainName = "Incorrect")
        {
            this.WriteEvent(31, this.nameProvider.Name);
        }

        [Event(
            32,
            Message = "Failed to get environment variables due to security exception; code is likely running in partial trust. Exception: {0}.",
            Level = EventLevel.Warning)]
        public void FailedToLoadEnvironmentVariables(string ex, string appDomainName = "Incorrect")
        {
            this.WriteEvent(32, ex, this.nameProvider.Name);
        }

        // Verbosity is Error - so it is always sent to portal; Keyword is Diagnostics so throttling is not applied.
        [Event(33,
            Message = "A Metric Extractor detected a telemetry item with SamplingPercentage < 100. Metrics Extractors should be used before Sampling Processors or any other Telemetry Processors that might filter out Telemetry Items. Otherwise, extracted metrics may be incorrect.",
            Level = EventLevel.Error,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable)]
        public void MetricExtractorAfterSamplingError(string appDomainName = "Incorrect")
        {
            this.WriteEvent(33, this.nameProvider.Name);
        }

        // Verbosity is Verbose - targeted at support personnel; Keyword is Diagnostics so throttling is not applied.
        [Event(34,
            Message = "A Metric Extractor detected a telemetry item with SamplingPercentage < 100. Metrics Extractors Extractor should be used before Sampling Processors or any other Telemetry Processors that might filter out Telemetry Items. Otherwise, extracted metrics may be incorrect.",
            Level = EventLevel.Verbose,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable)]
        public void MetricExtractorAfterSamplingVerbose(string appDomainName = "Incorrect")
        {
            this.WriteEvent(34, this.nameProvider.Name);
        }

        [Event(35,
            Message = "At least one telemetry channel cannot consume incoming telemetry fast enough. Some telemetry was dropped to prevent out of memory condition.",
            Level = EventLevel.Warning)]
        public void TelemetryDroppedToPreventQueueOverflow(string appDomainName = "Incorrect")
        {
            this.WriteEvent(35, this.nameProvider.Name);
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
