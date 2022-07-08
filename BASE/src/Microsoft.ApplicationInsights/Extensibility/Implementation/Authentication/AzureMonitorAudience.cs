// This intentionally uses the same namespace as TelemetryConfiguration.
namespace Microsoft.ApplicationInsights.Extensibility
{
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
    }
}
