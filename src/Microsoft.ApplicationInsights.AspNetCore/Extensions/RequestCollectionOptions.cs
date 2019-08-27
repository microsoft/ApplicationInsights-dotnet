namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
{
    /// <summary>
    /// Request collection options define the custom behavior or non-default features of request collection.
    /// </summary>
    public class RequestCollectionOptions
    {
        /// <summary>
        /// Creates new instance of <see cref="RequestCollectionOptions"/> class and fills default values.
        /// </summary>
        public RequestCollectionOptions()
        {
            this.InjectResponseHeaders = true;

            // In NetStandard20, ApplicationInsightsLoggerProvider is enabled by default,
            // which captures Exceptions. Disabling it in RequestCollectionModule to avoid duplication.
#if NETSTANDARD2_0
            this.TrackExceptions = false;
#else
            this.TrackExceptions = true;
#endif
            this.EnableW3CDistributedTracing = false;
        }

        /// <summary>
        /// Get or sets value indicating whether Request-Context header is injected into the response.
        /// </summary>
        public bool InjectResponseHeaders { get; set; }

        /// <summary>
        /// Get or sets value indicating whether exceptions are be tracked.
        /// </summary>
        public bool TrackExceptions { get; set; }

        /// <summary>
        /// Get or sets value indicating whether W3C distributed tracing standard is enabled.
        /// </summary>
        public bool EnableW3CDistributedTracing { get; set; }
    }
}
