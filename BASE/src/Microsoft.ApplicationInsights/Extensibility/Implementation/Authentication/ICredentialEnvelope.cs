namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class CredentialEnvelope
    {
        public abstract object Credential { get;}

        public abstract string GetToken(CancellationToken cancellationToken);

        public abstract Task<string> GetTokenAsync(CancellationToken cancellationToken);
    }
}
