namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
{
    /// <summary>
    /// Application Insights service options defines the custom behavior of the features to add, as opposed to the default selection of featuers obtained from Application Insights.
    /// </summary>
    public class ApplicationInsightsServiceOptions
    {
        private bool enableAdaptiveSampling = true;

        /// <summary>
        /// Setting EnableAdaptiveSampling to false, will disable the default adaptive sampling feature. As a result, no telemetry processor 
        /// that controls sampling is added to the service by default.
        /// </summary>
        public bool EnableAdaptiveSampling
        {
            get
            {
                return enableAdaptiveSampling;
            }
            set
            {
                enableAdaptiveSampling = value;
            }
        }
    }
}
