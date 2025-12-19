using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace ClassicAspNetWebApp
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            // BundleConfig.RegisterBundles(BundleTable.Bundles);
            
            // Example: Configure Azure Active Directory (AAD) authentication for Application Insights
            // Requires: Install-Package Azure.Identity
            // var telemetryConfig = TelemetryConfiguration.CreateDefault();
            // telemetryConfig.ConnectionString = "InstrumentationKey=YOUR_IKEY;IngestionEndpoint=https://ingestion-endpoint.applicationinsights.azure.com/";
            // telemetryConfig.SetAzureTokenCredential(new Azure.Identity.DefaultAzureCredential());
        }
    }
}