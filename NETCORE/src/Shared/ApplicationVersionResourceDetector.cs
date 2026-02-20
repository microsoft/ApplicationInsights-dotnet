#if AI_ASPNETCORE_WEB
namespace Microsoft.ApplicationInsights.AspNetCore
#else
namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    using System.Collections.Generic;
    using OpenTelemetry.Resources;

    /// <summary>
    /// Resource detector that adds the application version as a service.version resource attribute.
    /// </summary>
    internal class ApplicationVersionResourceDetector : IResourceDetector
    {
        private readonly string applicationVersion;

        public ApplicationVersionResourceDetector(string applicationVersion)
        {
            this.applicationVersion = applicationVersion;
        }

        public Resource Detect()
        {
            return string.IsNullOrEmpty(this.applicationVersion)
                ? Resource.Empty
                : new Resource([new KeyValuePair<string, object>("service.version", this.applicationVersion)]);
        }
    }
}
