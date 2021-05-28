namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    internal static class AuthConstants
    {
        /// <summary>
        /// Source: 
        /// (https://docs.microsoft.com/azure/active-directory/develop/msal-acquire-cache-tokens#scopes-when-acquiring-tokens).
        /// (https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent#the-default-scope).
        /// </summary>
        private const string AzureMonitorScope = "https://monitor.azure.com//.default"; // TODO: THIS SCOPE IS UNVERIFIED. WAITING FOR SERVICES TEAM TO PROVIDE AN INT ENVIRONMENT FOR E2E TESTING.

        /// <summary>
        /// Get scopes for Azure Monitor as an array.
        /// </summary>
        /// <returns>An array of scopes.</returns>
        public static string[] GetScopes() => new string[] { AzureMonitorScope };
    }
}
