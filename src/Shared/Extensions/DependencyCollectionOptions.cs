#if AI_ASPNETCORE_WEB
    namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
#else
namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    /// <summary>
    /// Default collection options define the custom behavior or non-default features of dependency collection.
    /// </summary>
    public class DependencyCollectionOptions
    {
        /// <summary>
        /// Creates new instance of <see cref="DependencyCollectionOptions"/> class and fills default values.
        /// </summary>
        public DependencyCollectionOptions()
        {
            EnableLegacyCorrelationHeadersInjection = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable legacy (x-ms*) correlation headers injection.
        /// </summary>
        public bool EnableLegacyCorrelationHeadersInjection { get; set; }
    }
}
