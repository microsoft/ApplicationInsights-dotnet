// This intentionally uses the same namespace as TelemetryConfiguration.
namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;

    /// <summary>
    /// Defines audience for Azure Monitor for the Azure Public Cloud and sovereign clouds.
    /// </summary>
    /// <remarks>
    /// See also: Azure.Identity.AzureAuthorityHosts <see href="https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/AzureAuthorityHosts.cs"/>.
    /// See also: Az.Accounts Get-AzEnvironment <see href="https://docs.microsoft.com/en-us/powershell/module/az.accounts/get-azenvironment"/>.
    /// </remarks>
    public static class AzureMonitorAudience
    {
        /// <summary>
        /// Maximum allowed length for audience string.
        /// </summary>
        /// <remarks>
        /// Setting an over-exaggerated max length to protect against malicious injections (2^9 = 512).
        /// </remarks>
        internal const int AudienceStringMaxLength = 512;

        /// <summary>
        /// The host of Azure Active Directory audience for Azure Public Cloud.
        /// </summary>
        public const string AzurePublicCloud = "https://monitor.azure.com/";

        /// <summary>
        /// The host of Azure Active Directory audience for Azure US Government Cloud.
        /// </summary>
        public const string AzureUSGovernment = "https://monitor.azure.us/";

        /// <summary>
        /// The host of Azure Active Directory audience for Azure China Cloud.
        /// </summary>
        public const string AzureChinaCloud = "https://monitor.azure.cn/";

        /// <summary>
        /// Combine a specified audience with the '.default' permission to create the array of scopes.
        /// </summary>
        /// <param name="audience">User provided input.</param>
        /// <returns>Array of scopes to be used to acquire an Azure Identity token.</returns>
        /// <remarks>
        /// We shouldn't punish a user for omitting the trailing slash character.
        /// </remarks>
        internal static string[] GetScopes(string audience)
        {
            string scope = audience + (audience.EndsWith("/", StringComparison.Ordinal) ? "/.default" : "//.default");
            return new string[] { scope };
        }
    }
}
