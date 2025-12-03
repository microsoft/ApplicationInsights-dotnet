namespace Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing
{
#if AI_ASPNETCORE_WEB
    using Microsoft.ApplicationInsights.Shared.Internals;
#endif
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event source for Application Insights ASP.NET Core SDK.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-AspNetCore")]
    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "appDomainName is required")]
    [SuppressMessage("", "SA1611:ElementParametersMustBeDocumented", Justification = "Internal only class.")]
    internal sealed class AspNetCoreEventSource : EventSource
    {
        /// <summary>
        /// The singleton instance of this event source.
        /// Due to how EventSource initialization works this has to be a public field and not
        /// a property otherwise the internal state of the event source will not be enabled.
        /// </summary>
        public static readonly AspNetCoreEventSource Instance = new AspNetCoreEventSource();
        private readonly ApplicationNameProvider applicationNameProvider = new ApplicationNameProvider();

        /// <summary>
        /// Prevents a default instance of the <see cref="AspNetCoreEventSource"/> class from being created.
        /// </summary>
        private AspNetCoreEventSource()
            : base()
        {
        }

        /// <summary>
        /// Logs an event for when generic error occur within the SDK.
        /// </summary>
        [Event(
            1,
            Keywords = Keywords.Diagnostics,
            Message = "An error has occurred which may prevent application insights from functioning. Error message: '{0}' ",
            Level = EventLevel.Error)]
        public void LogError(string errorMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, errorMessage, this.applicationNameProvider.Name);
        }

        /// <summary>
        /// Keywords for the AspNetEventSource.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Keyword for errors that trace at Verbose level.
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)0x1;
        }
    }
}
