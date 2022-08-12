namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    internal static class AuthConstants
    {
        public const string AuthorizationHeaderName = "Authorization";

        public const string AuthorizationTokenPrefix = "Bearer ";

        /// <summary>
        /// Source: 
        /// (https://docs.microsoft.com/azure/active-directory/develop/msal-acquire-cache-tokens#scopes-when-acquiring-tokens).
        /// (https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent#the-default-scope).
        /// </summary>
        internal const string DefaultAzureMonitorScope = "https://monitor.azure.com//.default";

        internal const string DefaultAzureMonitorPermission = "/.default";

        /// <summary>
        /// Maximum allowed length for audience string.
        /// </summary>
        /// <remarks>
        /// Setting an over-exaggerated max length to protect against malicious injections (2^9 = 512).
        /// </remarks>
        internal const int AudienceStringMaxLength = 512;
    }
}
