using System.Web;

[assembly: PreApplicationStartMethod(
    typeof(Microsoft.ApplicationInsights.Extensibility.HostingStartup.WebRequestTrackingModuleRegister),
    "Register")]