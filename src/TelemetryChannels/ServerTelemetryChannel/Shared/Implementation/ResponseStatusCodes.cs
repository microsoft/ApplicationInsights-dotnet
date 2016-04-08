namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    internal class ResponseStatusCodes
    {
        public const int RequestTimeout = 408;
        public const int ResponseCodeTooManyRequests = 429;
        public const int ResponseCodeTooManyRequestsOverExtendedTime = 439;
        public const int InternalServerError = 500;
        public const int ServiceUnavailable = 503;
    }
}
