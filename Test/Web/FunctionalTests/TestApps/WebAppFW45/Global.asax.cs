namespace WebAppFW45
{
    using System.Configuration;
    using System.Web.Http;
    using Microsoft.ApplicationInsights;

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var setting = ConfigurationManager.AppSettings["TestApp.SendTelemetyIntemOnAppStart"];
            if (false == string.IsNullOrWhiteSpace(setting) && true == bool.Parse(setting))
            {
                new TelemetryClient().TrackTrace("Application_Start");
            }

            GlobalConfiguration.Configure(WebApiConfig.Register);
            // To remove 1 minute wait for items to apprear we can:
            // - set MaxNumberOfItemsPerTransmission to 1 so each item is delivered immidiately
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
