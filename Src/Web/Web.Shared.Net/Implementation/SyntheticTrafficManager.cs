namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System.Web;

    /// <summary>
    /// Helper class that validates requests and gets information about the request source.
    /// </summary>
    public class SyntheticTrafficManager
    {
        private const string GsmSyntheticSourceName = "Application Insights Availability Monitoring";
        private const string GsmSyntheticTestRunId = "SyntheticTest-RunId";
        private const string GsmSyntheticTestLocation = "SyntheticTest-Location";

        /// <summary>
        /// Determines if the request is from a synthetic source.
        /// </summary>
        /// <returns>True if request is synthetic.</returns>
        public bool IsSynthetic(HttpContext platformContext)
        {
            // Curretnly we mark only requests from GSM tests as synthetic 
            string header = this.GetSessionId(platformContext);
            return !string.IsNullOrWhiteSpace(header);
        }

        /// <summary>
        /// Returns synthetic session id.
        /// </summary>
        /// <returns>Synthetic session id.</returns>
        public string GetSessionId(HttpContext platformContext)
        {
            return platformContext.Request.UnvalidatedGetHeader(GsmSyntheticTestRunId);
        }

        /// <summary>
        /// The method returns synthetic user id.
        /// </summary>
        /// <returns>Returns synthetic user id.</returns>
        public string GetUserId(HttpContext platformContext)
        {
            return platformContext.Request.UnvalidatedGetHeader(GsmSyntheticTestLocation);
        }

        /// <summary>
        /// Returns request SyntheticSource or null if the request is not synthetic.
        /// </summary>
        /// <returns>Request SyntheticSource or null if the request is not synthetic.</returns>
        public string GetSyntheticSource(HttpContext platformContext)
        {
            if (this.IsSynthetic(platformContext))
            {
                return GsmSyntheticSourceName;
            }

            return null;
        }
    }
}
