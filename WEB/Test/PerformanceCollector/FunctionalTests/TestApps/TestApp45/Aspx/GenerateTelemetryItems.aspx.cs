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

                telemetryClient.TrackDependency(dependencyTypeName: "TestDependency", dependencyName: "DependencySuccess",data: "DependencySuccess", startTime: DateTimeOffset.Now,
                    duration: TimeSpan.FromMilliseconds(10), success: true);
                telemetryClient.TrackDependency(dependencyTypeName: "TestDependency", dependencyName: "DependencyFailed", data: "DependencyFailed", startTime: DateTimeOffset.Now,
                    duration: TimeSpan.FromSeconds(1), success: false);

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