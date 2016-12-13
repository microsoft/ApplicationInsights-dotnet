namespace Wa40Aspx
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using System;

    public partial class TestMetricForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var telemetry = new TelemetryClient(TelemetryConfiguration.Active);
            telemetry.TrackMetric(new MetricTelemetry() { Name = "MetricTracker", Count = 1, Sum = 0.01 });
        }
    }
}