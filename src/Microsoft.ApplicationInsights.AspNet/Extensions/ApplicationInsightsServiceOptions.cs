namespace Microsoft.ApplicationInsights.AspNet.Extensions
{
    /// <summary>
    /// Application Insights service options defines the custom behavior of the features to add, as opposed to the default selection of featuers obtained from Application Insights.
    /// </summary>
    public class ApplicationInsightsServiceOptions
    {
        private bool disableDefaultSampling = false;

        /// <summary>
        /// Setting DisableDefaultSampling to true, will disable the default adaptive sampling feature. As a result, no telemetry processor 
        /// that controls sampling is added to the service.
        /// </summary>
        public bool DisableDefaultSampling
        {
            get
            {
                return disableDefaultSampling;
            }
            set
            {
                disableDefaultSampling = value;
            }
        }
    }
}
