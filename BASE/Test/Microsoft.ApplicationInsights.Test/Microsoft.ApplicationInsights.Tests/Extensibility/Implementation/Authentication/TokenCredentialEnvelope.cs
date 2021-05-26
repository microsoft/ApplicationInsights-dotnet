#if !NET452 && !NET46
namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Core;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;

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
        public override string GetToken(CancellationToken cancellationToken = default)
        {
            var accessToken = this.tokenCredential.GetToken(requestContext: this.tokenRequestContext, cancellationToken: cancellationToken);
            return accessToken.Token;
        }

        /// <inheritdoc/>
        public override async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            var accessToken = await this.tokenCredential.GetTokenAsync(requestContext: this.tokenRequestContext, cancellationToken: cancellationToken).ConfigureAwait(false);
            return accessToken.Token;
        }
    }
}
#endif
