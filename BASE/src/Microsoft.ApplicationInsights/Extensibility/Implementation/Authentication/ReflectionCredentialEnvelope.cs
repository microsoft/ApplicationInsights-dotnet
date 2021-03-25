namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Linq.Expressions;
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
    internal class ReflectionCredentialEnvelope : CredentialEnvelope
    {
        private readonly Type tokenCredentialType = Type.GetType("Azure.Core.TokenCredential, Azure.Core");
        private readonly Type tokenRequestContextType = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");

        private readonly object tokenCredential;
        private readonly object tokenRequestContext;
        //private readonly MethodInfo getTokenAsyncMethod;
        //private readonly MethodInfo getTokenMethod;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCredential"></param>
        public ReflectionCredentialEnvelope(object tokenCredential)
        {
            this.tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));

            if (tokenCredential.GetType().IsSubclassOf(tokenCredentialType))
            {
                // (https://docs.microsoft.com/en-us/dotnet/api/azure.core.tokenrequestcontext.-ctor?view=azure-dotnet).
                // Invoking this constructor: Azure.Core.TokenRequestContext(String[], String, String).
                var paramArray = new object[] { GetScopes(), null, null };
                this.tokenRequestContext = Activator.CreateInstance(tokenRequestContextType, args: paramArray);
            }
            else
            {
                throw new ArgumentException($"The provided {nameof(tokenCredential)} must inherit Azure.Core.TokenCredential", nameof(tokenCredential));
            }
        }

        public override object Credential => this.tokenCredential;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This is a wrapper for the following method:
        /// <code>public abstract Azure.Core.AccessToken GetToken (Azure.Core.TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken);</code>.
        /// (https://docs.microsoft.com/dotnet/api/azure.core.tokencredential.gettoken).
        /// </remarks>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override string GetToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tokenCredentialType = Type.GetType("Azure.Core.TokenCredential, Azure.Core");
            var tokenRequestContextType = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");
            var accessTokenType = Type.GetType("Azure.Core.AccessToken, Azure.Core");

            var parameterExpression_requestContext = Expression.Parameter(type: tokenRequestContextType, name: "parameterExpression_requestContext");
            var parameterExpression_cancellationToken = Expression.Parameter(type: typeof(CancellationToken), name: "parameterExpression_cancellationToken");

            Expression callExpr = Expression.Call(
                instance: Expression.New(this.Credential.GetType()),
                method: tokenCredentialType.GetMethod(name: "GetToken", types: new Type[] { tokenRequestContextType, typeof(CancellationToken) }),
                arg0: parameterExpression_requestContext,
                arg1: parameterExpression_cancellationToken
                );

            var lambdaTest = Expression.Lambda(
                body: callExpr,
                parameters: new ParameterExpression[]
                {
                    parameterExpression_requestContext,
                    parameterExpression_cancellationToken
                });

            var compileTest = lambdaTest.Compile(); // TODO: THIS NEEDS TO BE STORED AS A PRIVATE FIELD SO IT CAN BE REUSED.
            var accessToken = compileTest.DynamicInvoke(this.tokenRequestContext, cancellationToken);

            var tokenProperty = accessTokenType.GetProperty("Token");
            return (string)tokenProperty.GetValue(accessToken);
        }

        public override async Task<string> GetTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tokenCredentialType = Type.GetType("Azure.Core.TokenCredential, Azure.Core");
            var tokenRequestContextType = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");
            var accessTokenType = Type.GetType("Azure.Core.AccessToken, Azure.Core");

            var parameterExpression_requestContext = Expression.Parameter(type: tokenRequestContextType, name: "parameterExpression_requestContext");
            var parameterExpression_cancellationToken = Expression.Parameter(type: typeof(CancellationToken), name: "parameterExpression_cancellationToken");

            Expression callExpr = Expression.Call(
                instance: Expression.New(this.Credential.GetType()),
                method: tokenCredentialType.GetMethod(name: "GetTokenAsync", types: new Type[] { tokenRequestContextType, typeof(CancellationToken) }),
                arg0: parameterExpression_requestContext,
                arg1: parameterExpression_cancellationToken
                );

            var lambdaTest = Expression.Lambda(
                body: callExpr,
                parameters: new ParameterExpression[]
                {
                    parameterExpression_requestContext,
                    parameterExpression_cancellationToken
                });

            var compileTest = lambdaTest.Compile(); // TODO: THIS NEEDS TO BE STORED AS A PRIVATE FIELD SO IT CAN BE REUSED.
            var valueTaskAccessToken = compileTest.DynamicInvoke(this.tokenRequestContext, cancellationToken);

            var asTaskMethod = valueTaskAccessToken.GetType().GetMethod("AsTask");

            var task = (Task)asTaskMethod.Invoke(valueTaskAccessToken, null);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            var accessToken = resultProperty.GetValue(task);

            var tokenProperty = accessTokenType.GetProperty("Token");
            return (string)tokenProperty.GetValue(accessToken);
        }
    }
}
