namespace TestApp40.Aspx
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.ApplicationInsights;

    public partial class GenerateTelemetryItems : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));

            new TelemetryClient().TrackRequest("RequestSuccess", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", true);
            new TelemetryClient().TrackRequest("RequestFailed", DateTimeOffset.Now, TimeSpan.FromSeconds(1), "500", false);

            new TelemetryClient().TrackDependency("DependencySuccess", "DependencySuccess", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), true);
            new TelemetryClient().TrackDependency("DependencyFailed", "DependencyFailed", DateTimeOffset.Now, TimeSpan.FromSeconds(1), false);

            new TelemetryClient().TrackException(new ArgumentNullException());

            new TelemetryClient().TrackEvent("Event1");
            new TelemetryClient().TrackEvent("Event2");

            new TelemetryClient().TrackTrace("Trace1");
            new TelemetryClient().TrackTrace("Trace2");

            Response.Clear();
            Response.Write("GenerateTelemetryItems.aspx");
            Response.End();
        }
    }
}