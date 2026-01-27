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
        /// Logs an event for when adding Application Insights telemetry fails.
        /// </summary>
        [Event(
            1,
            Keywords = Keywords.Diagnostics,
            Message = "Failed to add Application Insights telemetry. Error: '{0}'",
            Level = EventLevel.Error)]
        public void FailedToAddTelemetry(string errorMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, errorMessage, this.applicationNameProvider.Name);
        }

        /// <summary>
        /// Logs an event for when an invalid TracesPerSecond value is configured.
        /// </summary>
        [Event(
            2,
            Keywords = Keywords.Diagnostics,
            Message = "Invalid TracesPerSecond value '{0}'. Value must be at least 0. Using default value.",
            Level = EventLevel.Warning)]
        public void InvalidTracesPerSecondConfigured(double tracesPerSecond, string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, tracesPerSecond, this.applicationNameProvider.Name);
        }

        /// <summary>
        /// Logs an event for when an invalid SamplingRatio value is configured.
        /// </summary>
        [Event(
            3,
            Keywords = Keywords.Diagnostics,
            Message = "Invalid SamplingRatio value '{0}'. Value must be between 0.0 and 1.0. Using default value.",
            Level = EventLevel.Warning)]
        public void InvalidSamplingRatioConfigured(float samplingRatio, string appDomainName = "Incorrect")
        {
            this.WriteEvent(3, samplingRatio, this.applicationNameProvider.Name);
        }

        /// <summary>
        /// Logs an event for when telemetry configuration fails.
        /// </summary>
        [Event(
            4,
            Keywords = Keywords.Diagnostics,
            Message = "Failed to configure telemetry. Error: '{0}'",
            Level = EventLevel.Error)]
        public void TelemetryConfigurationFailure(string errorMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(4, errorMessage, this.applicationNameProvider.Name);
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
