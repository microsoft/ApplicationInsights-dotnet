#if NETSTANDARD2_0
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Azure.Core;

    public class TokenCredentialEnvelope : ICredentialEnvelope
    {
        private readonly TokenCredential tokenCredential;

        public TokenCredentialEnvelope(TokenCredential tokenCredential) => this.tokenCredential = tokenCredential;

        public object Credential => this.tokenCredential;

        public string GetToken() => null;//this.Credential.GetToken().Token;

        public async Task<string> GetTokenAsync() => await Task.FromResult<string>(null);//await this.Credential.GetTokenAsync().Token;
    }
}
#endif