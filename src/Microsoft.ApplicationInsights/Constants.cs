namespace Microsoft.ApplicationInsights
{
    internal class Constants
    {
        internal const string TelemetryServiceEndpoint = "https://dc.services.visualstudio.com/v2/track";

        internal const string ProfileQueryEndpoint = "https://dc.services.visualstudio.com/api/profiles/{0}/appId";

        internal const string TelemetryNamePrefix = "Microsoft.ApplicationInsights.";

        internal const string DevModeTelemetryNamePrefix = "Microsoft.ApplicationInsights.Dev.";

        internal const int MaxExceptionCountToSave = 10;
    }
}
