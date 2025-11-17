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

        [Event(
            4,
            Message = "ApplicationInsights.config not found at path: {0}",
            Level = EventLevel.Informational)]
        public void ApplicationInsightsConfigNotFound(string path, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                4,
                path ?? "NULL",
                this.applicationNameProvider.Name);
        }

        [Event(
            5,
            Message = "ApplicationInsights.config loaded successfully from: {0}",
            Level = EventLevel.Informational)]
        public void ApplicationInsightsConfigLoaded(string path, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                5,
                path ?? "NULL",
                this.applicationNameProvider.Name);
        }

        [Event(
            6,
            Message = "Failed to read ApplicationInsights.config. Error: {0}",
            Level = EventLevel.Warning)]
        public void ApplicationInsightsConfigReadError(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                6,
                error ?? "NULL",
                this.applicationNameProvider.Name);
        }

        [Event(
            7,
            Message = "ConnectionString not found in ApplicationInsights.config at path: {0}",
            Level = EventLevel.Warning)]
        public void ApplicationInsightsConfigConnectionStringNotFound(string path, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                7,
                path ?? "NULL",
                this.applicationNameProvider.Name);
        }

        [Event(
            8,
            Message = "[msg=AuthIdTrackingCookieNotAvailable];",
            Level = EventLevel.Verbose)]
        public void AuthIdTrackingCookieNotAvailable(string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                8,
                this.applicationNameProvider.Name);
        }

        [Event(
            9,
            Message = "[msg=WebSessionTrackingSessionCookieIsNotSpecifiedInRequest];",
            Level = EventLevel.Verbose)]
        public void WebSessionTrackingSessionCookieIsNotSpecifiedInRequest(string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                9,
                this.applicationNameProvider.Name);
        }

        [Event(
            10,
            Message = "[msg=WebUserTrackingUserCookieIsIncomplete];[cookieValue={0}];",
            Level = EventLevel.Verbose)]
        public void WebUserTrackingUserCookieIsIncomplete(string cookieValue, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                10,
                cookieValue ?? string.Empty,
                this.applicationNameProvider.Name);
        }

        [Event(
            11,
            Message = "[msg=WebUserTrackingUserCookieNotAvailable];",
            Level = EventLevel.Verbose)]
        public void WebUserTrackingUserCookieNotAvailable(string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                11,
                this.applicationNameProvider.Name);
        }

        [Event(
            12,
            Message = "[msg=WebLocationIdHeaderFound];[headerName={0}];",
            Level = EventLevel.Verbose)]
        public void WebLocationIdHeaderFound(string headerName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                12,
                headerName ?? string.Empty,
                this.applicationNameProvider.Name);
        }

        [Event(
            13,
            Message = "[msg=WebLocationIdSet];[locationId={0}];",
            Level = EventLevel.Verbose)]
        public void WebLocationIdSet(string locationId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                13,
                locationId ?? string.Empty,
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
