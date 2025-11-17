namespace Microsoft.ApplicationInsights.WorkerService.Implementation.Tracing
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.Shared.Internals;

    /// <summary>
    /// Event source for Application Insights Worker Service SDK.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-WorkerService")]
    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "appDomainName is required")]
    [SuppressMessage("", "SA1611:ElementParametersMustBeDocumented", Justification = "Internal only class.")]
    internal sealed class WorkerServiceEventSource : EventSource
    {
        /// <summary>
        /// The singleton instance of this event source.
        /// Due to how EventSource initialization works this has to be a public field and not
        /// a property otherwise the internal state of the event source will not be enabled.
        /// </summary>
        public static readonly WorkerServiceEventSource Instance = new WorkerServiceEventSource();
        private readonly ApplicationNameProvider applicationNameProvider = new ApplicationNameProvider();

        /// <summary>
        /// Prevents a default instance of the <see cref="WorkerServiceEventSource"/> class from being created.
        /// </summary>
        private WorkerServiceEventSource()
            : base()
        {
        }

        /// <summary>
        /// Logs error message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(1, Message = "An error has occurred which may prevent application insights from functioning. Error message: '{0}'", Level = EventLevel.Error)]
        public void LogError(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, message, this.applicationNameProvider.Name);
        }

        /// <summary>
        /// Keywords for the AspNetEventSource.
        /// </summary>
        public static class Keywords
        {
            /// <summary>
            /// Keyword for errors that trace at Verbose level.
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)0x1;
        }
    }
}
