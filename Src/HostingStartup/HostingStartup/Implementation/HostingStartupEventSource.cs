namespace Microsoft.ApplicationInsights.Extensibility.HostingStartup
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.Common;

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-HostingStartup")]
    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "appDomainName is required")]
    internal sealed class HostingStartupEventSource : EventSource
    {
        /// <summary>
        /// Instance of the PlatformEventSource class.
        /// </summary>
        public static readonly HostingStartupEventSource Log = new HostingStartupEventSource();
        private readonly ApplicationNameProvider applicationNameProvider = new ApplicationNameProvider();

        private HostingStartupEventSource()
        {
        }

        [Event(1, Message = "Logs file name: {0}.", Level = EventLevel.Verbose)]
        public void LogsFileName(string fileName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, fileName ?? string.Empty, this.applicationNameProvider.Name);
        }

        [Event(
            2,
            Keywords = Keywords.UserActionable,
            Message = "Access to the logs folder was denied (User: {1}). Error message: {0}.",
            Level = EventLevel.Error)]
        public void LogStorageAccessDeniedError(string error, string user, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                2,
                error ?? string.Empty,
                user ?? string.Empty,
                this.applicationNameProvider.Name);
        }

        [Event(
           3,
           Message = "Trying to load http module type from assembly: {0}, type name: {1}.",
           Level = EventLevel.Verbose)]
        public void HttpModuleLoadingStart(string assemblyName, string moduleName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                3,
                assemblyName ?? string.Empty,
                moduleName ?? string.Empty,
                this.applicationNameProvider.Name);
        }

        [Event(
           4,
           Message = "Http module type from assembly: {0}, type name: {1} loaded successfully",
           Level = EventLevel.Verbose)]
        public void HttpModuleLoadingEnd(string assemblyName, string moduleName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                4,
                assemblyName ?? string.Empty,
                moduleName ?? string.Empty,
                this.applicationNameProvider.Name);
        }

        [Event(
           5,
           Keywords = Keywords.UserActionable,
           Message = "Error loading http module type from assembly {0}, type name {1}, exception: {2}.",
           Level = EventLevel.Error)]
        public void HttpModuleLoadingError(string assemblyName, string moduleName, string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                5,
                assemblyName ?? string.Empty,
                moduleName ?? string.Empty,
                exception ?? string.Empty,
                this.applicationNameProvider.Name);
        }

        [Event(
           6,
           Message = "Call to WindowsIdentity.Current failed with the exception: {0}.",
           Level = EventLevel.Warning)]
        public void LogWindowsIdentityAccessSecurityException(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                6,
                error ?? string.Empty,
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
