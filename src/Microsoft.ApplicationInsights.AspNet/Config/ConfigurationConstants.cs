namespace Microsoft.ApplicationInsights.AspNet.Config
{
    internal static class ConfigurationConstants
    {
        public const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        public const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        public const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        public const string InstrumentationKeyForWebSites = "APPINSIGHTS_INSTRUMENTATIONKEY";
        public const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        public const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";
    }
}
