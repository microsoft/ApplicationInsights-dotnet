namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An envelope for an instance of Azure.Core.TokenCredential.
    /// </summary>
    public abstract class CredentialEnvelope
    {
        /// <summary>
        /// Source: 
        /// (https://docs.microsoft.com/azure/active-directory/develop/msal-acquire-cache-tokens#scopes-when-acquiring-tokens).
        /// (https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent#the-default-scope).
        /// </summary>
        private const string Scope = "https://monitor.azure.com//.default"; // TODO: THIS SCOPE IS UNVERIFIED. WAITING FOR SERVICES TEAM TO PROVIDE AN INT ENVIRONMENT FOR E2E TESTING.

        /// <summary>
        /// Gets the TokenCredential object held by this class.
        /// </summary>
        public abstract object Credential { get; }

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        public abstract string GetToken(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        public abstract Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get scopes for Azure Monitor as an array.
        /// </summary>
        /// <returns>An array of scopes.</returns>
        protected static string[] GetScopes() => new string[] { Scope };
    }
}
