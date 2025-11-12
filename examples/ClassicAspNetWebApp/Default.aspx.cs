using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ClassicAspNetWebApp
{
    public partial class _Default : Page
    {
        private TelemetryClient telemetryClient = new TelemetryClient(TelemetryConfiguration.CreateDefault());

        protected void Page_Load(object sender, EventArgs e)
        {
            telemetryClient.TrackTrace("Default.aspx Page_Load event fired.");
        }
    }
}