namespace Microsoft.ApplicationInsights
{
    internal class Constants
    {
        internal const string ProfileQueryEndpoint = "https://dc.services.visualstudio.com/api/profiles/{0}/appId"; // TODO: REMOVE CONST

        internal const string TelemetryNamePrefix = "Microsoft.ApplicationInsights.";

        internal const string DevModeTelemetryNamePrefix = "Microsoft.ApplicationInsights.Dev.";

        internal const string EventNameForUnknownTelemetry = "ConvertedTelemetry";

        internal const int MaxExceptionCountToSave = 10;
    }
}
