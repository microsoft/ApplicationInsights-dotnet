namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.Common;

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-Web")]
    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "appDomainName is required")]
    internal sealed class WebEventSource : EventSource
    {
        /// <summary>
        /// Instance of the PlatformEventSource class.
        /// </summary>
        public static readonly WebEventSource Log = new WebEventSource();
        private readonly ApplicationNameProvider applicationNameProvider = new ApplicationNameProvider();

        private WebEventSource()
        {
        }

        public static bool IsVerboseEnabled 
        { 
             [NonEvent] 
             get 
             { 
                 return Log.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1)); 
             } 
         }

        [Event(
            1,
            Message = "[msg=UserHostNotCollectedWarning];[exception={0}];",
            Level = EventLevel.Warning)]
        public void UserHostNotCollectedWarning(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                1,
                exception ?? string.Empty,
                this.applicationNameProvider.Name);
        }

        [Event(
            2,
            Message = "[msg=WebUriFormatException];",
            Level = EventLevel.Warning)]
        public void WebUriFormatException(string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, this.applicationNameProvider.Name);
        }

        [Event(
            3,
            Message = "[msg=NoHttpContext];",
            Level = EventLevel.Warning)]
        public void NoHttpContextWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                3,
                this.applicationNameProvider.Name);
        }

        /// <summary>
        /// Keywords for the PlatformEventSource. Those keywords should match keywords in Core.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Key word for user actionable events.
            /// </summary>
            public const EventKeywords UserActionable = (EventKeywords)0x1;

            /// <summary>
            /// Diagnostics tracing keyword.
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)0x2;

            /// <summary>
            /// Keyword for errors that trace at Verbose level.
            /// </summary>
            public const EventKeywords VerboseFailure = (EventKeywords)0x4;
        }
    }
}
