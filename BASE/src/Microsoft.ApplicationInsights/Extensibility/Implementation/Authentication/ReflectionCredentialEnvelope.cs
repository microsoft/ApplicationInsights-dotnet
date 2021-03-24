namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Our SDK currently targets net452, net46, and netstandard2.0.
    /// Azure.Core.TokenCredential is only available for netstandard2.0.
    /// I'm introducing this class as a wrapper so we can receive an instance of this class and pass it around within our SDK.
    /// </remarks>
    internal class ReflectionCredentialEnvelope : ICredentialEnvelope
    {
        private readonly object tokenRequestContext;
        private readonly MethodInfo getTokenAsyncMethod;
        private readonly MethodInfo getTokenMethod;
        //private readonly Type accessTokenType;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Must use reflection to verify that this object is an instance of Azure.Core.TokenCredential.
        /// </remarks>
        /// <param name="tokenCredential"></param>
        public ReflectionCredentialEnvelope(object tokenCredential)
        {
            if (this.IsTokenCredential(tokenCredential.GetType()))
            {
                this.Credential = tokenCredential;

                // (https://docs.microsoft.com/en-us/dotnet/api/azure.core.tokenrequestcontext.-ctor?view=azure-dotnet).
                // Invoking this constructor: Azure.Core.TokenRequestContext(String[], String, String).
                var tokenRequestContextType = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");
                var paramArray = new object[] { AuthConstants.GetScopes(), null, null };
                this.tokenRequestContext = Activator.CreateInstance(tokenRequestContextType, args: paramArray);

                // (https://docs.microsoft.com/en-us/dotnet/api/azure.core.tokencredential.gettokenasync?view=azure-dotnet).
                // Getting a handle for this method: Azure.Core.TokenCredential.GetTokenAsync().
                this.getTokenAsyncMethod = this.Credential.GetType().GetMethod("GetTokenAsync");
                this.getTokenMethod = this.Credential.GetType().GetMethod("GetToken");

                // this.accessTokenType = Type.GetType("Azure.Core.AccessToken, Azure.Core");
            }
            else
            {
                throw new ArgumentException($"The provided {nameof(tokenCredential)} must inherit Azure.Core.TokenCredential", nameof(tokenCredential));
            }
        }

        public object Credential { get; private set; }

        public string GetToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessToken = this.getTokenMethod.Invoke(this.Credential, new object[] { this.tokenRequestContext, cancellationToken });
            var tokenProperty = accessToken.GetType().GetProperty("Token");
            return (string)tokenProperty.GetValue(accessToken);
        }

        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() => this.GetToken(cancellationToken));

            // TODO: THIS DOESN'T WORK. CAN CIRCLE BACK AND INVESTIGATE THIS LATER.
            // 'Unable to cast object of type 'System.Threading.Tasks.ValueTask`1[Azure.Core.AccessToken]' to type 'System.Threading.Tasks.Task'.'
            // (https://stackoverflow.com/questions/39674988/how-to-call-a-generic-async-method-using-reflection/39679855).
            //var task = (Task)this.getTokenAsyncMethod.Invoke(this.Credential, new object[] { this.tokenRequestContext, cancellationToken });
            //await task.ConfigureAwait(false);
            //var resultProperty = task.GetType().GetProperty("Result");
            //var accessToken = resultProperty.GetValue(task);
            //var tokenProperty = accessToken.GetType().GetProperty("Token");
            //return (string)tokenProperty.GetValue(accessToken);
        }

        /// <summary>
        /// Checks if the input inherits <see cref="Azure.Core.TokenCredential"/>
        /// </summary>
        private bool IsTokenCredential(Type inputType) => inputType.IsSubclassOf(Type.GetType("Azure.Core.TokenCredential, Azure.Core"));
    }
}
