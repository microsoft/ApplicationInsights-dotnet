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

            // ============================================================================
            // EXAMPLE: Configure Application Insights with Azure Monitor Exporter Options
            // ============================================================================
            // 
            var telemetryConfig = TelemetryConfiguration.CreateDefault();
            telemetryConfig.ConnectionString = "";
            //
            // // SAMPLING: Choose ONE approach (not both)
            // // SamplingRatio: Percentage of telemetry to keep (0.0 to 1.0). Default: 1.0 (100%)
            // telemetryConfig.SamplingRatio = 0.5f;           // Keep 50% of telemetry
            // // TracesPerSecond: Rate-limited sampling. Default: null (disabled)
            // // telemetryConfig.TracesPerSecond = 5.0;       // OR: Keep max 5 traces/second
            //
            // // OFFLINE STORAGE: Persists telemetry when network is unavailable
            // // StorageDirectory: Custom path for offline storage.
            // //   Default: %LOCALAPPDATA%\Microsoft\AzureMonitor (Windows)
            // //            $TMPDIR/Microsoft/AzureMonitor (Linux/macOS)
            // // telemetryConfig.StorageDirectory = @"C:\AppInsightsStorage";
            // // DisableOfflineStorage: Set to true to disable offline storage. Default: false
            // telemetryConfig.DisableOfflineStorage = false;
            //
            // // FEATURES
            // // EnableLiveMetrics: Enable real-time metrics streaming. Default: true
            // telemetryConfig.EnableLiveMetrics = true;
            // // EnableTraceBasedLogsSampler: Apply trace-based sampling to logs. Default: true
            // telemetryConfig.EnableTraceBasedLogsSampler = true;
            //
            // // AUTHENTICATION: Azure AD (Entra ID) token-based auth
            // // Requires: Install-Package Azure.Identity
            // // telemetryConfig.SetAzureTokenCredential(new Azure.Identity.DefaultAzureCredential());
            //
            var telemetryClient = new TelemetryClient(telemetryConfig);
            telemetryClient.Context.Cloud.RoleName = "MyWebApp";
        }
    }
}