#if NETSTANDARD2_0
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Core;

    internal class TokenCredentialEnvelope : CredentialEnvelope
    {
        private readonly TokenCredential tokenCredential;
        private readonly TokenRequestContext tokenRequestContext;

        public TokenCredentialEnvelope(TokenCredential tokenCredential)
        {
            this.tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
            this.tokenRequestContext = new TokenRequestContext(scopes: GetScopes());
        }

        /// <inheritdoc/>
        public override object Credential => this.tokenCredential;

        /// <inheritdoc/>
        public override string GetToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessToken = this.tokenCredential.GetToken(requestContext: this.tokenRequestContext, cancellationToken: cancellationToken);
            return accessToken.Token;
        }

        /// <inheritdoc/>
        public override async Task<string> GetTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessToken = await this.tokenCredential.GetTokenAsync(requestContext: this.tokenRequestContext, cancellationToken: cancellationToken).ConfigureAwait(false);
            return accessToken.Token;
        }
    }
}
#endif
