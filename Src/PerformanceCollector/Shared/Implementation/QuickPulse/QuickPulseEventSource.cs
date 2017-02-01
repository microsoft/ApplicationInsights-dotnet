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

        [Event(1, Level = EventLevel.Informational, Message = @"QuickPulse infrastructure is being initialized. QuickPulseServiceEndpoint: '{0}', DisableFullTelemetryItems: '{1}', DisableTopCpuProcesses: '{2}' ")]
        public void ModuleIsBeingInitializedEvent(
            string serviceEndpoint,
            bool disableFullTelemetryItems,
            bool disableTopCpuProcesses,
            string applicationName = "dummy")
        {
            this.WriteEvent(1, serviceEndpoint ?? string.Empty, disableFullTelemetryItems, disableTopCpuProcesses, this.ApplicationName);
        }

        [Event(3, Level = EventLevel.Informational, Message = @"Performance counter {0} has been successfully registered with QuickPulse performance collector.")]
        public void CounterRegisteredEvent(string counter, string applicationName = "dummy")
        {
            this.WriteEvent(3, counter ?? string.Empty, this.ApplicationName);
        }
        #endregion

        #region Infra init - failure
        [Event(5, Keywords = Keywords.UserActionable, Level = EventLevel.Error, Message = @"Performance counter {1} has failed to register with QuickPulse performance collector. This might happen whenever an application is running on a platform that doesn't provide access to performance counters. Technical details: {0}")]
        public void CounterRegistrationFailedEvent(string e, string counter, string applicationName = "dummy")
        {
            this.WriteEvent(5, e ?? string.Empty, counter ?? string.Empty, this.ApplicationName);
        }

        [Event(6, Level = EventLevel.Warning, Message = @"Performance counter specified in QuickPulse as {1} was not parsed correctly. Technical details: {0}")]
        public void CounterParsingFailedEvent(string e, string counter, string applicationName = "dummy")
        {
            this.WriteEvent(6, e ?? string.Empty, counter ?? string.Empty, this.ApplicationName);
        }
        #endregion

        #region Data reading - success

        #endregion

        #region Data reading - failure
        [Event(11, Level = EventLevel.Warning, Message = @"Performance counter {1} has failed the reading operation in QuickPulse. Error message: {0}")]
        public void CounterReadingFailedEvent(string e, string counter, string applicationName = "dummy")
        {
            this.WriteEvent(11, e ?? string.Empty, counter ?? string.Empty, this.ApplicationName);
        }

        [Event(20, Level = EventLevel.Verbose, Message = @"QuickPulse has failed to read process information. Error message: {0}")]
        public void ProcessesReadingFailedEvent(string e, string applicationName = "dummy")
        {
            this.WriteEvent(20, e ?? string.Empty, this.ApplicationName);
        }
        #endregion

        #region Data sending - success

        #endregion

        #region Data sending - failure
        [Event(12, Level = EventLevel.Verbose, Message = @"Failed to communicate with the QuickPulse service. Error text: {0}")]
        public void ServiceCommunicationFailedEvent(string e, string applicationName = "dummy")
        {
            this.WriteEvent(12, e ?? string.Empty, this.ApplicationName);
        }
        #endregion

        #region Unknown errors

        [Event(13, Level = EventLevel.Error, Message = @"Unexpected error in QuickPulse infrastructure: {0}. QuickPulse data will not be available.")]
        public void UnknownErrorEvent(string e, string applicationName = "dummy")
        {
            this.WriteEvent(13, e ?? string.Empty, this.ApplicationName);
        }

        #endregion

        #region Troubleshooting

        [Event(14, Message = "{0}", Level = EventLevel.Verbose)]
        public void TroubleshootingMessageEvent(string message, string applicationName = "dummy")
        {
            this.WriteEvent(14, message ?? string.Empty, this.ApplicationName);
        }

        [Event(15, Message = "Sample submitted. Response: '{0}'", Level = EventLevel.Verbose)]
        public void SampleSubmittedEvent(string response, string applicationName = "dummy")
        {
            this.WriteEvent(15, response ?? string.Empty, this.ApplicationName);
        }

        [Event(16, Message = "Ping sent. Response: '{0}'", Level = EventLevel.Verbose)]
        public void PingSentEvent(string response, string applicationName = "dummy")
        {
            this.WriteEvent(16, response ?? string.Empty, this.ApplicationName);
        }

        [Event(17, Message = "State timer tick finished: {0} ms", Level = EventLevel.Verbose)]
        public void StateTimerTickFinishedEvent(long elapsedMs, string applicationName = "dummy")
        {
            this.WriteEvent(17, elapsedMs, this.ApplicationName);
        }

        [Event(18, Message = "Collection timer tick finished: {0} ms", Level = EventLevel.Verbose)]
        public void CollectionTimerTickFinishedEvent(long elapsedMs, string applicationName = "dummy")
        {
            this.WriteEvent(18, elapsedMs, this.ApplicationName);
        }

        [Event(19, Message = "Sample stored. Buffer length: {0}", Level = EventLevel.Verbose)]
        public void SampleStoredEvent(int bufferLength, string applicationName = "dummy")
        {
            this.WriteEvent(19, bufferLength, this.ApplicationName);
        }

        [Event(7, Level = EventLevel.Verbose, Message = @"QuickPulseTelemetryModule has received a registration request from a QuickPulseTelemetryProcessor.")]
        public void ProcessorRegistered(string count, string applicationName = "dummy")
        {
            this.WriteEvent(7, count, this.ApplicationName);
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