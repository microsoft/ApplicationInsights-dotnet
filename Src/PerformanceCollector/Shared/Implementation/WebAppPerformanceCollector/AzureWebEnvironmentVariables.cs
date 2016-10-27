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
        /// ASP.NET.
        /// </summary>
        AspNet = 0,

        /// <summary>
        /// Application.
        /// </summary>
        App = 1,

        /// <summary>
        /// Common Language Runtime.
        /// </summary>
        CLR = 2,

        /// <summary>
        /// All of the above.
        /// </summary>
        All = 3
    };
}
