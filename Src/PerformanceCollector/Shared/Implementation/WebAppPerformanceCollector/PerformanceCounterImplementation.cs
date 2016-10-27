namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;
    using System.Collections.Generic;

    internal class PerformanceCounterImplementation
    {
        /// <summary>
        /// Available Environment Variables in Azure Web Apps
        /// </summary>
        private readonly List<string> listEnvironmentVariables = new List<string>
        {
            "WEBSITE_COUNTERS_ASPNET",
            "WEBSITE_COUNTERS_APP",
            "WEBSITE_COUNTERS_CLR",
            "WEBSITE_COUNTERS_ALL"
        };

        /// <summary>
        /// Retrieves counter data from Azure Web App Environment Variables.
        /// </summary>
        /// <param name="environmentVariable">Name of environment variable</param>
        /// <returns>Raw Json with</returns>
        public string GetAzureWebAppEnvironmentVariables(AzureWebApEnvironmentVariables environmentVariable)
        {
            return Environment.GetEnvironmentVariable(this.listEnvironmentVariables[(int)environmentVariable]);
        }
    }
}
