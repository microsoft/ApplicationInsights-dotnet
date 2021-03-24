#if NETSTANDARD2_0
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Core;

    internal class TokenCredentialEnvelope : ICredentialEnvelope
    {
        private readonly TokenCredential tokenCredential;
        private readonly TokenRequestContext tokenRequestContext;

        public TokenCredentialEnvelope(TokenCredential tokenCredential)
        {
            this.tokenCredential = tokenCredential;
            this.tokenRequestContext = new TokenRequestContext(scopes: AuthConstants.GetScopes());
        }

        public object Credential => this.tokenCredential;

        public string GetToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessToken = this.tokenCredential.GetToken(requestContext: this.tokenRequestContext, cancellationToken: cancellationToken);
            return accessToken.Token;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// You can also use the C# default(CancellationToken) statement to create an empty cancellation token.
        /// Source: (https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken.none).
        /// </remarks>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessToken = await this.tokenCredential.GetTokenAsync(requestContext: this.tokenRequestContext, cancellationToken: cancellationToken);
            return accessToken.Token;
        }
    }
}
#endif