namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
#if NETCORE || NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
#if NETCORE
    using System.Reflection;
#endif
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-DependencyCollector")]
    internal sealed class DependencyCollectorEventSource : EventSource
    {
        public static readonly DependencyCollectorEventSource Log = new DependencyCollectorEventSource();

        private DependencyCollectorEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        public string ApplicationName { [NonEvent]get; [NonEvent]private set; }

        [Event(
            1,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=RemoteDependencyModule failed];[msg={0}];[fwv={1}];",
            Level = EventLevel.Error)]
        public void RemoteDependencyModuleError(string msg, string frameworkVersion, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, msg ?? string.Empty, frameworkVersion ?? string.Empty, this.ApplicationName);
        }

        [Event(
            2,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=DependencyOperationTelemetryInitializerFailed];[msg={0}]",
            Level = EventLevel.Error)]
        public void DependencyOperationTelemetryInitializerError(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, msg, this.ApplicationName);
        }

        [Event(
            3,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=DependencyOperationNameNull];",
            Level = EventLevel.Warning)]
        public void DependencyOperationNameNullWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(3, this.ApplicationName);
        }

        [Event(
            4,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=Runtime Dependency monitoring is turned off because APMC monitoring is enabled for the process. To enable AIC Runtime Dependency monitoring, turned off APMC monitoring for the current process.];",
            Level = EventLevel.Verbose)]
        public void RemoteDependencyDisabledApmcEnabled(string appDomainName = "Incorrect")
        {
            this.WriteEvent(4, this.ApplicationName);
        }

        [Event(
            5,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=RemoteDependencyModule warning.];[msg={0}]",
            Level = EventLevel.Warning)]
        public void RemoteDependencyModuleWarning(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(5, msg ?? string.Empty, this.ApplicationName);
        }

        [Event(
            6,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=RemoteDependencyModule verbose.];[msg={0}]",
            Level = EventLevel.Verbose)]
        public void RemoteDependencyModuleVerbose(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(6, msg, this.ApplicationName);
        }

        [Event(
            7,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=RemoteDependencyTelemetryCollected.];",
            Level = EventLevel.Verbose)]
        public void RemoteDependencyTelemetryCollected(string appDomainName = "Incorrect")
        {
            this.WriteEvent(7, this.ApplicationName);
        }

        [Event(
            8,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=RemoteDependencyModule information.];[msg={0}]",
            Level = EventLevel.Informational)]
        public void RemoteDependencyModuleInformation(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(8, msg ?? string.Empty, this.ApplicationName);
        }

        [Event(
            9,
            Keywords = Keywords.RddEventKeywords,
            Message = "[msg=Runtime instrumentation agent is not attached. To enable runtime instrumentation agent monitoring, install Application Insights Agent.]",
            Level = EventLevel.Verbose)]
        public void RemoteDependencyModuleProfilerNotAttached(string appDomainName = "Incorrect")
        {
            this.WriteEvent(9, this.ApplicationName);
        }

        [Event(
            10,
            Keywords = Keywords.RddEventKeywords,
            Message = "Begin callback called for id = '{0}', name= '{1}'",
            Level = EventLevel.Verbose)]
        public void BeginCallbackCalled(long id, string name, string appDomainName = "Incorrect")
        {
            this.WriteEvent(10, id, name ?? string.Empty, this.ApplicationName);
        }

        [Event(
            11,
            Keywords = Keywords.RddEventKeywords,
            Message = "End callback called for id = '{0}'",
            Level = EventLevel.Verbose)]
        public void EndCallbackCalled(string id, string appDomainName = "Incorrect")
        {
            this.WriteEvent(11, id, this.ApplicationName);
        }

        [Event(
            12,
            Keywords = Keywords.RddEventKeywords,
            Message = "End callback - cannot find start of operation for id = '{0}'",
            Level = EventLevel.Warning)]
        public void EndCallbackWithNoBegin(string id, string appDomainName = "Incorrect")
        {
            this.WriteEvent(12, id, this.ApplicationName);
        }

        [NonEvent]
        public void CallbackError(long id, string callbackName, Exception exception)
        {
            if (this.IsEnabled(EventLevel.Error, Keywords.RddEventKeywords))
            {
                this.CallbackError(id.ToString(CultureInfo.InvariantCulture), callbackName ?? string.Empty, exception == null ? "null" : exception.ToInvariantString());
            }
        }

        [Event(
            13,
            Keywords = Keywords.RddEventKeywords,
            Message = "Callback '{1}' failed for id = '{0}'. Exception: {2}",
            Level = EventLevel.Error)]
        public void CallbackError(string id, string callbackName, string exceptionString, string appDomainName = "Incorrect")
        {
            this.WriteEvent(13, id, callbackName ?? string.Empty, exceptionString ?? string.Empty, this.ApplicationName);
        }

        [Event(
            14,
            Keywords = Keywords.RddEventKeywords,
            Message = "Callback '{1}' will not run for id = '{0}'. Reason: {2}",
            Level = EventLevel.Warning)]
        public void NotExpectedCallback(long id, string callbackName, string reason, string appDomainName = "Incorrect")
        {
            this.WriteEvent(14, id, callbackName ?? string.Empty, reason ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs the information when the DependencyTelemetry item is null as warning.
        /// </summary>
        [Event(15, Message = "Dependency telemetry item is null.", Level = EventLevel.Warning)]
        public void DependencyTelemetryItemIsNullWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(15, this.ApplicationName);
        }

        /// <summary>
        /// Logs the information when the HttpWebRequest is null as warning.
        /// </summary>
        [Event(16, Message = "WebRequest is null.", Level = EventLevel.Warning)]
        public void WebRequestIsNullWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(16, this.ApplicationName);
        }

        /// <summary>
        /// Logs the information when a telemetry item that is already existing in the tables (that is currently being tracked) is tracked again.
        /// </summary>
        [Event(17, Message = "Tracking an already existing item in the table.", Level = EventLevel.Verbose)]
        public void TrackingAnExistingTelemetryItemVerbose(string appDomainName = "Incorrect")
        {
            this.WriteEvent(17, this.ApplicationName);
        }

        /// <summary>
        /// Logs the information when the telemetry item to track is null.
        /// </summary>
        [Event(18, Message = "Telemetry to track is null.", Level = EventLevel.Warning)]
        public void TelemetryToTrackIsNullWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(18, this.ApplicationName);
        }

        [Event(19, 
            Message = "RemoteDependency profiler failed to attach. Collection will default to EventSource implementation. Error details: {0}",
            Level = EventLevel.Error)]
        public void ProfilerFailedToAttachError(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(19, error ?? string.Empty, this.ApplicationName);
        }

        [Event(
            20,
            Keywords = Keywords.RddEventKeywords,
            Message = "UnexpectedCallbackParameter. Expected type: {0}.",
            Level = EventLevel.Warning)]
        public void UnexpectedCallbackParameter(string expectedType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(20, expectedType ?? string.Empty, this.ApplicationName);
        }

        [Event(
            21,
            Keywords = Keywords.RddEventKeywords,
            Message = "End async callback called for id = '{0}'",
            Level = EventLevel.Verbose)]
        public void EndAsyncCallbackCalled(string id, string appDomainName = "Incorrect")
        {
            this.WriteEvent(21, id, this.ApplicationName);
        }

        [Event(
           22,
           Keywords = Keywords.RddEventKeywords,
           Message = "End async exception callback called for id = '{0}'",
           Level = EventLevel.Verbose)]
        public void EndAsyncExceptionCallbackCalled(string id, string appDomainName = "Incorrect")
        {
            this.WriteEvent(22, id, this.ApplicationName);
        }

        [NonEvent]
        private string GetApplicationName()
        {
            string name;
            try
            {
#if NETCORE
                name = Assembly.GetEntryAssembly().FullName;
#else
                name = AppDomain.CurrentDomain.FriendlyName;
#endif
            }
            catch (Exception exp)
            {
                name = "Undefined " + exp;
            }

            return name;
        }

        /// <summary>
        /// Keywords for the <see cref="DependencyCollectorEventSource"/>.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Key word for user actionable events.
            /// </summary>
            public const EventKeywords UserActionable = (EventKeywords)0x1;

            /*  Reserve first 3 for other service keywords
             *  public const EventKeywords Service1 = (EventKeywords)0x2;
             *  public const EventKeywords Service2 = (EventKeywords)0x4;
             *  public const EventKeywords Service3 = (EventKeywords)0x8;
             */

            /// <summary>
            /// Key word for resource discovery module failures.
            /// </summary>
            public const EventKeywords RddEventKeywords = (EventKeywords)0x400;
        }
    }
}