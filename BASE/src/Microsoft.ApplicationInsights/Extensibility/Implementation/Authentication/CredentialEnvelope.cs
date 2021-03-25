namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class CredentialEnvelope
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-acquire-cache-tokens#scopes-when-acquiring-tokens
        /// 
        /// Other APIs might require that no scheme or host is included in the scope value, and expect only the app ID (a GUID) and the scope name, for example: 11111111-1111-1111-1111-111111111111/api.read
        /// </summary>
        private const string Scope = "https://storage.azure.com/.default"; // example from Blob Storage. TODO: NEED OUR OWN SCOPE

        protected static string[] GetScopes() => new string[] { Scope };

        public abstract object Credential { get;}

        public abstract string GetToken(CancellationToken cancellationToken);

        public abstract Task<string> GetTokenAsync(CancellationToken cancellationToken);
    }
}
