namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
#if !NET40
    using System.Diagnostics.Tracing;
#endif

#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-PerformanceCollector-QuickPulse")]
    internal sealed class QuickPulseEventSource : EventSource
    {
        private static readonly QuickPulseEventSource Logger = new QuickPulseEventSource();

        private QuickPulseEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        public static QuickPulseEventSource Log
        {
            get
            {
                return Logger;
            }
        }

        public string ApplicationName { [NonEvent]get; [NonEvent]private set; }

        #region Infra init - success

        [Event(1, Level = EventLevel.Informational, Message = @"QuickPulse infrastructure is being initialized. {0}")]
        public void ModuleIsBeingInitializedEvent(
            string message,
            string applicationName = "dummy")
        {
            this.WriteEvent(1, message, this.ApplicationName);
        }

        [Event(3, Level = EventLevel.Informational, Message = @"Performance counter {0} has been successfully registered with QuickPulse performance collector.")]
        public void CounterRegisteredEvent(string counter, string applicationName = "dummy")
        {
            this.WriteEvent(3, counter, this.ApplicationName);
        }
        #endregion

        #region Infra init - failure
        [Event(5, Keywords = Keywords.UserActionable, Level = EventLevel.Warning, Message = @"Performance counter {1} has failed to register with QuickPulse performance collector. This might happen whenever an application is running on a platform that doesn't provide access to performance counters. Technical details: {0}")]
        public void CounterRegistrationFailedEvent(string e, string counter, string applicationName = "dummy")
        {
            this.WriteEvent(5, e, counter, this.ApplicationName);
        }

        [Event(6, Level = EventLevel.Warning, Message = @"Performance counter specified in QuickPulse as {1} was not parsed correctly. Technical details: {0}")]
        public void CounterParsingFailedEvent(string e, string counter, string applicationName = "dummy")
        {
            this.WriteEvent(6, e, counter, this.ApplicationName);
        }

        [Event(7, Keywords = Keywords.UserActionable, Level = EventLevel.Warning, Message = @"QuickPulseTelemetryModule could not locate a QuickPulseTelemetryProcessor in configuration. QuickPulse data will not be available.")]
        public void CouldNotObtainQuickPulseTelemetryProcessorEvent(string applicationName = "dummy")
        {
            this.WriteEvent(7, this.ApplicationName);
        }
        #endregion

        #region Data reading - success

        #endregion

        #region Data reading - failure
        [Event(11, Level = EventLevel.Warning, Message = @"Performance counter {1} has failed the reading operation in QuickPulse. Error message: {0}")]
        public void CounterReadingFailedEvent(string e, string counter, string applicationName = "dummy")
        {
            this.WriteEvent(11, e, counter, this.ApplicationName);
        }
        #endregion

        #region Data sending - success

        #endregion

        #region Data sending - failure
        [Event(12, Level = EventLevel.Verbose, Message = @"Failed communicate with the QuickPulse service. Error text: {0}")]
        public void ServiceCommunicationFailedEvent(string e, string applicationName = "dummy")
        {
            this.WriteEvent(12, e, this.ApplicationName);
        }
        #endregion

        #region Unknown errors

        [Event(13, Keywords = Keywords.UserActionable, Level = EventLevel.Warning, Message = @"Unknown error in QuickPulse infrastructure: {0}")]
        public void UnknownErrorEvent(string e, string applicationName = "dummy")
        {
            this.WriteEvent(13, e, this.ApplicationName);
        }

        #endregion

        #region Troubleshooting

        [Event(14, Message = "{0}", Level = EventLevel.Verbose)]
        public void TroubleshootingMessageEvent(string message, string applicationName = "dummy")
        {
            this.WriteEvent(14, message, this.ApplicationName);
        }

        #endregion

        [NonEvent]
        private string GetApplicationName()
        {
            string name;
            try
            {
                name = AppDomain.CurrentDomain.FriendlyName;
            }
            catch (Exception exp)
            {
                name = "Undefined " + exp.Message ?? exp.ToString();
            }

            return name;
        }

        public class Keywords
        {
            public const EventKeywords UserActionable = (EventKeywords)0x1;
        }
    }
}