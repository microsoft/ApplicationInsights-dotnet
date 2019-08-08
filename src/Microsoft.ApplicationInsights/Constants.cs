namespace Microsoft.ApplicationInsights
{
    internal class Constants
    {
        internal const string TelemetryServiceEndpoint = "https://dc.services.visualstudio.com/v2/track"; // TODO: REMOVE

        internal const string ProfileQueryEndpoint = "https://dc.services.visualstudio.com/api/profiles/{0}/appId"; // TODO: REMOVE

        internal const string TelemetryNamePrefix = "Microsoft.ApplicationInsights.";

        internal const string DevModeTelemetryNamePrefix = "Microsoft.ApplicationInsights.Dev.";

        internal const string EventNameForUnknownTelemetry = "ConvertedTelemetry";

        internal const int MaxExceptionCountToSave = 10;
    }
}
