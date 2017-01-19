namespace Microsoft.ApplicationInsights.AspNetCore.Logging
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class to control default debug logger and disable it if logger was added to <see cref="ILoggerFactory"/> explicetely.
    /// </summary>
    internal class DebugLoggerControl
    {
        public DebugLoggerControl()
        {
            EnableDebugLogger = true;
        }

        /// <summary>
        /// This property gets set to <code>false</code> by <see cref="ApplicationInsightsLoggerFactoryExtensions.AddApplicationInsights"/>.
        /// </summary>
        public bool EnableDebugLogger { get; set; }
    }
}
