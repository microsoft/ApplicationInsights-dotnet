using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace WebAppFW45
{
    using Microsoft.ApplicationInsights;

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var setting = ConfigurationManager.AppSettings["TestApp.SendTelemetryItemOnAppStart"];
            if (false == string.IsNullOrWhiteSpace(setting) && true == bool.Parse(setting))
            {
                new TelemetryClient().TrackTrace("Application_Start");
            }

            GlobalConfiguration.Configure(WebApiConfig.Register);

            // To remove 1 minute wait for items to appear we can:
            // - set MaxNumberOfItemsPerTransmission to 1 so each item is delivered immediately
            // - call telemetryQueue.Flush each X ms
            // - set MaxNumberOfItemsPerTransmission as a property under Queue in AI.config
            // Example : 
            //var configuration = TelemetryConfiguration.Default;
            //var telemetryChannel = (TelemetryChannel)configuration.TelemetryChannel;
            //var telemetryQueue = (TelemetryQueue)telemetryChannel.TelemetryQueue;
            //telemetryQueue.MaxNumberOfItemsPerTransmission = 1;
        }
    }
}
