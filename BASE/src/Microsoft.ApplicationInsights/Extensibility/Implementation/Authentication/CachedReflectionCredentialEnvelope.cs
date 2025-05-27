namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This is a version of <see cref="ReflectionCredentialEnvelope"/> that caches and reuses
    /// the auth token for most of its lifetime, thereby preventing repeated calls to fetch
    /// new tokens.
    /// </summary>
    internal sealed class CachedReflectionCredentialEnvelope : ReflectionCredentialEnvelope
    {
        /// <summary>
        /// The token refresh interval. This is the minimum lifetime required
        /// for the auth token. A cached token can be re-used if its remaining
        /// lifetime is at least as long as this interval.
        /// </summary>
        private readonly TimeSpan tokenRefreshOffset;

        /// <summary>
        /// The cached token, if any.
        /// </summary>
        private AuthToken? cachedToken;

        /// <summary>
        /// Create an instance of <see cref="CachedReflectionCredentialEnvelope"/>.
        /// </summary>
        /// <param name="tokenCredential">An instance of Azure.Core.TokenCredential.</param>
        /// <remarks>
        /// The default 5 minute token refresh interval matches the default
        /// tokenRefreshOffset in Azure.Core's BearerTokenAuthenticationPolicy. It's
        /// reasonable to assume that callers will use the auth token to make an
        /// authenticated call well within 5 minutes of calling GetToken.
        /// </remarks>
        public CachedReflectionCredentialEnvelope(object tokenCredential) : this(tokenCredential, TimeSpan.FromMinutes(5))
        {
        }

        /// <summary>
        /// Create an instance of <see cref="CachedReflectionCredentialEnvelope"/>.
        /// </summary>
        /// <param name="tokenCredential">An instance of Azure.Core.TokenCredential.</param>
        /// <param name="tokenRefreshOffset">The remaining lifetime allowed before a cached token must be refreshed.</param>
        public CachedReflectionCredentialEnvelope(object tokenCredential, TimeSpan tokenRefreshOffset) : base(tokenCredential)
        {
            this.tokenRefreshOffset = tokenRefreshOffset;
        }

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <remarks>
        /// Whomever uses this MUST verify that it's called within <see cref="SdkInternalOperationsMonitor.Enter"/> otherwise dependency calls will be tracked.
        /// </remarks>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        public override AuthToken GetToken(CancellationToken cancellationToken = default)
        {
            if (this.TryUseCachedToken(out AuthToken authToken))
            {
                return authToken;
            }

            return (this.cachedToken = base.GetToken(cancellationToken)).Value;
        }

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <remarks>
        /// Whomever uses this MUST verify that it's called within <see cref="SdkInternalOperationsMonitor.Enter"/> otherwise dependency calls will be tracked.
        /// </remarks>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        public override async Task<AuthToken> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (this.TryUseCachedToken(out AuthToken authToken))
            {
                return authToken;
            }

            return (this.cachedToken = await base.GetTokenAsync(cancellationToken).ConfigureAwait(false)).Value;
        }

        /// <summary>
        /// Check whether we can use the cached authentication token because its remaining
        /// lifetime is longer than the minimum required lifetime (5 minutes).
        /// </summary>
        /// <param name="cachedToken">On success, the value of the cached token.</param>
        /// <returns>True if the cached token can be used. False otherwise.</returns>
        private bool TryUseCachedToken(out AuthToken cachedToken)
        {
            AuthToken? token = this.cachedToken;
            if (token.HasValue)
            {
                TimeSpan timeRemaining = token.Value.ExpiresOn - DateTimeOffset.UtcNow;
                if (timeRemaining >= this.tokenRefreshOffset)
                {
                    cachedToken = token.Value;
                    return true;
                }

                // Cached token must be refreshed.
            }

            cachedToken = default;
            return false;
        }
    }
}
