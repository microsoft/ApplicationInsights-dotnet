namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that will update the User, Session and Operation contexts if request originates from a web test.
    /// </summary>
    internal class WebTestActivityProcessor : BaseProcessor<Activity>
    {
        private const string GsmSource = "Application Insights Availability Monitoring";
        private const string TestRunHeader = "SyntheticTest-RunId";
        private const string TestLocationHeader = "SyntheticTest-Location";

        /// <summary>
        /// Initializes a new instance of the <see cref="WebTestActivityProcessor"/> class.
        /// </summary>
        public WebTestActivityProcessor()
        {
        }

        /// <summary>
        /// Called when an activity ends. Sets web test specific tags if request is from availability monitoring.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            var context = HttpContext.Current;
            if (context == null)
            {
                return;
            }

            // Only process if synthetic source is not already set
            var existingSyntheticSource = activity.GetTagItem("ai.operation.syntheticSource");
            if (existingSyntheticSource == null || string.IsNullOrEmpty(existingSyntheticSource.ToString()))
            {
                var request = context.GetRequest();
                if (request != null)
                {
                    // Try Unvalidated first, fall back to regular Headers for test environments
                    var runIdHeader = request.UnvalidatedGetHeader(TestRunHeader);
                    if (string.IsNullOrEmpty(runIdHeader))
                    {
                        runIdHeader = request.Headers[TestRunHeader];
                    }

                    var locationHeader = request.UnvalidatedGetHeader(TestLocationHeader);
                    if (string.IsNullOrEmpty(locationHeader))
                    {
                        locationHeader = request.Headers[TestLocationHeader];
                    }

                    if (!string.IsNullOrEmpty(runIdHeader) && !string.IsNullOrEmpty(locationHeader))
                    {
                        activity.SetTag("ai.operation.syntheticSource", GsmSource);

                        // User id will be Pop location name and RunId (We cannot use just location because of sampling)
                        var userId = locationHeader + "_" + runIdHeader;
                        activity.SetTag("ai.user.id", userId);
                        activity.SetTag("session.id", runIdHeader);
                    }
                }
            }

            base.OnEnd(activity);
        }
    }
}
