namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;

    /// <summary>
    /// Enum for Azure Web App environment variables.
    /// </summary>
    [Flags]
    public enum AzureWebApEnvironmentVariables
    {
        /// <summary>
        /// For ASPNET.
        /// </summary>
        AspNet = 0,

        /// <summary>
        /// For Application.
        /// </summary>
        App = 1,

        /// <summary>
        /// For Common Language Runtime.
        /// </summary>
        CLR = 2,

        /// <summary>
        /// All of the above.
        /// </summary>
        All = 3
    }
}
