namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using OpenTelemetry.Resources;

    internal class AspNetCoreEnvironmentResourceDetector : IResourceDetector
    {
        private const string AspNetCoreEnvironmentPropertyName = "AspNetCoreEnvironment";
        private readonly IConfiguration configuration;

        public AspNetCoreEnvironmentResourceDetector(IConfiguration configuration)
        { 
            this.configuration = configuration;
        }

        public Resource Detect()
        {
            try
            {
                var aspNetCoreEnvironment = this.configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");

                return string.IsNullOrEmpty(aspNetCoreEnvironment)
                    ? Resource.Empty
                    : new Resource([new KeyValuePair<string, object>(AspNetCoreEnvironmentPropertyName, aspNetCoreEnvironment)]);
            }
            catch
            {
                return Resource.Empty;
            }
        }
    }
}
