namespace Microsoft.ApplicationInsights.Common
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-Common")]
    internal sealed class CrossComponentCorrelationEventSource : EventSource
    {
        public static readonly CrossComponentCorrelationEventSource Log = new CrossComponentCorrelationEventSource();

        private CrossComponentCorrelationEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        public string ApplicationName { [NonEvent]get; [NonEvent]private set; }

        [Event(
            1,
            Keywords = Keywords.UserActionable,
            Message = "Failed to retrieve App ID for the current application insights resource. Make sure the configured instrumentation key is valid. Error: {0}",
            Level = EventLevel.Warning)]
        public void FetchAppIdFailed(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, exception, this.ApplicationName);
        }

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
                name = "Undefined " + exp;
            }

            return name;
        }

        /// <summary>
        /// Keywords for the <see cref="CrossComponentCorrelationEventSource"/>.
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
        }
    }
}