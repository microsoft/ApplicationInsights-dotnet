using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace E2ETestWebApi
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        public static string EndPointAddressFormat = "http://{0}/api/Data/PushItem";

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var ingestionhostname = Microsoft.Azure.CloudConfigurationManager.GetSetting("ingestionhostname");
            TelemetryConfiguration.Active.TelemetryChannel.EndpointAddress = string.Format(EndPointAddressFormat, ingestionhostname);
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
        }
    }
}
