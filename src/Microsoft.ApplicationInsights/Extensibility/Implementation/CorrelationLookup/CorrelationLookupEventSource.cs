namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    //// [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-CorrelationLookup")] - EVERY COMPONENT SHOULD DEFINE IT"S OWN NAME
    internal sealed class CorrelationLookupEventSource : EventSource
    {
        public static readonly CorrelationLookupEventSource Log = new CorrelationLookupEventSource();

        private CorrelationLookupEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        public string ApplicationName { [NonEvent]get; [NonEvent]private set; }

        [Event(
            1,
            Keywords = Keywords.UserActionable,
            Message = "Failed to retrieve App Id for the current application insights resource. Make sure the configured instrumentation key is valid. Error: {0}",
            Level = EventLevel.Warning)]
        public void FetchAppIdFailed(string exception)
        {
            this.WriteEvent(1, exception, this.ApplicationName);
        }

        [Event(
            2,
            Keywords = Keywords.UserActionable,
            Message = "Failed to retrieve App Id for the current application insights resource. Endpoint returned HttpStatusCode: {0}",
            Level = EventLevel.Warning)]
        public void FetchAppIdFailedWithResponseCode(string httpStatusCode)
        {
            this.WriteEvent(6, httpStatusCode, this.ApplicationName);
        }

        [NonEvent]
        private string GetApplicationName()
        {
            string name;
            try
            {
#if NETSTANDARD1_6
                name = System.Reflection.Assembly.GetEntryAssembly().FullName;
#elif NETSTANDARD1_3
                name = string.Empty;
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
        /// Keywords for the <see cref="CorrelationLookupEventSource"/>.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Key word for user actionable events.
            /// </summary>
            public const EventKeywords UserActionable = (EventKeywords)0x1;

            /// <summary>
            /// Key word for diagnostics events.
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)0x2;
        }
    }
}
