namespace TestApp45.Aspx
{
    using System;
    using System.Threading;

    using Microsoft.ApplicationInsights;

    public partial class GenerateTelemetryItems : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                var telemetryClient = new TelemetryClient();

                telemetryClient.TrackRequest("RequestSuccess", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200",
                    true);
                telemetryClient.TrackRequest("RequestFailed", DateTimeOffset.Now, TimeSpan.FromSeconds(1), "500",
                    false);

                telemetryClient.TrackDependency("DependencySuccess", "DependencySuccess", DateTimeOffset.Now,
                    TimeSpan.FromMilliseconds(10), true);
                telemetryClient.TrackDependency("DependencyFailed", "DependencyFailed", DateTimeOffset.Now,
                    TimeSpan.FromSeconds(1), false);

                telemetryClient.TrackException(new ArgumentNullException());

                telemetryClient.TrackEvent("Event1");
                telemetryClient.TrackEvent("Event2");

                telemetryClient.TrackTrace("Trace1");
                telemetryClient.TrackTrace("Trace2");

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            finally
            {
                Response.Clear();
                Response.Write("GenerateTelemetryItems.aspx");
                Response.End();
            }
        }
    }
}