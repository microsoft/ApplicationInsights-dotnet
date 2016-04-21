namespace Microsoft.ApplicationInsights.AspNet.Extensions
{
    /// <summary>
    /// Extensions for Application Insights Service Options that returns specific set of properties if serviceOptions is enabled. If not, it returns the default initial value.
    /// </summary>
    internal static class ApplicationInsightsServiceOptionsExtensions
    {
        private static ApplicationInsightsServiceOptions _serviceOptions = new ApplicationInsightsServiceOptions();

        /// <summary>
        /// Checks if sampling is disabled through service options.
        /// </summary>
        internal static bool GetDisableDefaultSampling(this ApplicationInsightsServiceOptions serviceOptions)
        {
            if (serviceOptions != null)
            {
                return serviceOptions.DisableDefaultSampling;
            }

            return _serviceOptions.DisableDefaultSampling;
        }
    }
}
