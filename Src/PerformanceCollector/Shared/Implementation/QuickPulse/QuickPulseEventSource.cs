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
    internal class QuickPulseEventSource : EventSource
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
            string dummy = "dummy",
            string applicationName = "dummy")
        {
            this.WriteEvent(1, message, this.ApplicationName);
        }

       
        #endregion

        #region Infra init - failure

     

        #endregion

        #region Data reading - success

       
        #endregion

        #region Data reading - failure

     
        #endregion

        #region Data sending - success

        #endregion

        #region Data sending - failure

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