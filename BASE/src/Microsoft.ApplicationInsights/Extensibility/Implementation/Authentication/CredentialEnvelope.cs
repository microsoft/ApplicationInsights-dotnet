namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class CredentialEnvelope
    {
        /// <summary>
        /// The default scope used for token authentication.
        /// </summary>
        private const string Scope = "https://storage.azure.com/.default"; // example from Blob Storage. TODO: NEED OUR OWN SCOPE

        protected string[] Scopes => new string[] { Scope };

        public abstract object Credential { get; }

        public abstract string GetToken();

        public abstract Task<string> GetTokenAsync(CancellationToken cancellationToken);
    }
}
