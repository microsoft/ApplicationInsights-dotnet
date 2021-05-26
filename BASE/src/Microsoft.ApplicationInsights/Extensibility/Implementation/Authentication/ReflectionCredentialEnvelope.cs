namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This is an envelope for an instance of Azure.Core.TokenCredential.
    /// This class uses reflection to interact with the Azure.Core library.
    /// </summary>
    /// <remarks>
    /// Our SDK currently targets net452, net46, and netstandard2.0.
    /// Azure.Core.TokenCredential is only available for netstandard2.0.
    /// </remarks>
    internal class ReflectionCredentialEnvelope
    {
        private readonly object tokenCredential;
        private readonly object tokenRequestContext;

        /// <summary>
        /// Create an instance of <see cref="ReflectionCredentialEnvelope"/>.
        /// </summary>
        /// <param name="tokenCredential">An instance of Azure.Core.TokenCredential.</param>
        public ReflectionCredentialEnvelope(object tokenCredential)
        {
            this.tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));

            if (tokenCredential.GetType().IsSubclassOf(Type.GetType("Azure.Core.TokenCredential, Azure.Core")))
            {
                this.tokenRequestContext = AzureCore.MakeTokenRequestContext(scopes: CredentialConstants.GetScopes());
            }
            else
            {
                throw new ArgumentException($"The provided {nameof(tokenCredential)} must inherit Azure.Core.TokenCredential", nameof(tokenCredential));
            }
        }

        /// <summary>
        /// Gets the TokenCredential object held by this class.
        /// </summary>
        public object Credential => this.tokenCredential;

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        public string GetToken(CancellationToken cancellationToken = default)
        {
            return AzureCore.InvokeGetToken(this.tokenCredential, this.tokenRequestContext, cancellationToken);
        }

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            return AzureCore.InvokeGetTokenAsync(this.tokenCredential, this.tokenRequestContext, cancellationToken);
        }

        /// <summary>
        /// This class provides Reflection based wrappers around types found in the Azure.Core library.
        /// Because of framework incompatibilities, we cannot take a direct reference on these types.
        /// 
        /// This class uses compiled Expression Trees. Read more here: 
        /// (https://docs.microsoft.com/dotnet/csharp/programming-guide/concepts/expression-trees/).
        /// (https://docs.microsoft.com/dotnet/csharp/expression-trees).
        /// </summary>
        internal static class AzureCore
        {
            private static readonly Delegate GetTokenValue;
            private static readonly Delegate GetTokenAsyncValue;
            private static readonly Delegate GetTokenProperty;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "For both optimization and readability, I'm building these objects in the same method.")]
            static AzureCore()
            {
                GetTokenValue = BuildDelegateGetToken();

                var asyncDelegates = BuildDelegateGetTokenAsync();
                GetTokenAsyncValue = asyncDelegates[0];
                GetTokenProperty = asyncDelegates[1];
            }

            internal static string InvokeGetToken(object tokenCredential, object tokenRequestContext, CancellationToken cancellationToken)
            {
                return (string)GetTokenValue.DynamicInvoke(tokenCredential, tokenRequestContext, cancellationToken);
            }

            internal static async Task<string> InvokeGetTokenAsync(object tokenCredential, object tokenRequestContext, CancellationToken cancellationToken)
            {
                var task = (Task)GetTokenAsyncValue.DynamicInvoke(tokenCredential, tokenRequestContext, cancellationToken);
                await task.ConfigureAwait(false);
                return (string)GetTokenProperty.DynamicInvoke(task);
            }

            /// <summary>
            /// This is a wrapper for the following constructor:
            /// <code>public TokenRequestContext (string[] scopes, string? parentRequestId = default, string? claims = default);</code>
            /// (https://docs.microsoft.com/dotnet/api/azure.core.tokenrequestcontext.-ctor).
            /// </summary>
            internal static object MakeTokenRequestContext(string[] scopes)
            {
                return Activator.CreateInstance(
                    type: Type.GetType("Azure.Core.TokenRequestContext, Azure.Core"),
                    args: new object[] { scopes, null, });
            }

            /// This creates a wrapper for the following method:
            /// <code>public abstract Azure.Core.AccessToken GetToken (Azure.Core.TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken).</code>
            /// (https://docs.microsoft.com/dotnet/api/azure.core.tokencredential.gettoken).
            private static Delegate BuildDelegateGetToken()
            {
                Type typeTokenCredential = Type.GetType("Azure.Core.TokenCredential, Azure.Core");
                Type typeTokenRequestContext = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");
                Type typeCancellationToken = typeof(CancellationToken);

                var parameterExpression_tokenCredential = Expression.Parameter(type: typeTokenCredential, name: "parameterExpression_TokenCredential");
                var parameterExpression_requestContext = Expression.Parameter(type: typeTokenRequestContext, name: "parameterExpression_RequestContext");
                var parameterExpression_cancellationToken = Expression.Parameter(type: typeCancellationToken, name: "parameterExpression_CancellationToken");

                var exprGetToken = Expression.Call(
                    instance: parameterExpression_tokenCredential,
                    method: typeTokenCredential.GetMethod(name: "GetToken", types: new Type[] { typeTokenRequestContext, typeCancellationToken }),
                    arg0: parameterExpression_requestContext,
                    arg1: parameterExpression_cancellationToken);

                var exprTokenProperty = Expression.Property(
                    expression: exprGetToken,
                    propertyName: "Token");

                return Expression.Lambda(
                    body: exprTokenProperty,
                    parameters: new ParameterExpression[]
                    {
                        parameterExpression_tokenCredential,
                        parameterExpression_requestContext,
                        parameterExpression_cancellationToken,
                    }).Compile();
            }

            /// <summary>
            /// This is a wrapper for the following method:
            /// <code>public abstract System.Threading.Tasks.ValueTask&lt;Azure.Core.AccessToken> GetTokenAsync (Azure.Core.TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken);</code>
            /// (https://docs.microsoft.com/dotnet/api/azure.core.tokencredential.gettokenasync).
            /// </summary>
            /// <returns>
            /// The Expression Tree library cannot handle async methods.
            /// As a workaround, this method returns two Delegates. 
            /// First;
            /// The first Delegate is a wrapper around GetTokenAsync which returns a ValueTask of AccessToken.
            /// Then calls ValueTask.GetTask to convert that to a Task which is a known type for older frameworks.
            /// This Task can be awaited. 
            /// Second;
            /// The second Delegate is a wrapper around Task.Result which returns the AccessToken.
            /// Then calls AccessToken.Token to get the string token.
            /// </returns>
            private static Delegate[] BuildDelegateGetTokenAsync()
            {
                Type typeTokenCredential = Type.GetType("Azure.Core.TokenCredential, Azure.Core");
                Type typeTokenRequestContext = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");
                Type typeCancellationToken = typeof(CancellationToken);

                var parameterExpression_TokenCredential = Expression.Parameter(type: typeTokenCredential, name: "parameterExpression_TokenCredential");
                var parameterExpression_RequestContext = Expression.Parameter(type: typeTokenRequestContext, name: "parameterExpression_RequestContext");
                var parameterExpression_CancellationToken = Expression.Parameter(type: typeCancellationToken, name: "parameterExpression_CancellationToken");

                // public abstract System.Threading.Tasks.ValueTask<Azure.Core.AccessToken> GetTokenAsync (Azure.Core.TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken);
                var methodInfo_GetTokenAsync = typeTokenCredential.GetMethod(name: "GetTokenAsync", types: new Type[] { typeTokenRequestContext, typeCancellationToken });

                var exprGetTokenAsync = Expression.Call(
                    instance: parameterExpression_TokenCredential,
                    method: methodInfo_GetTokenAsync,
                    arg0: parameterExpression_RequestContext,
                    arg1: parameterExpression_CancellationToken);

                var methodInfo_AsTask = methodInfo_GetTokenAsync.ReturnType.GetMethod("AsTask");

                var exprAsTask = Expression.Call(
                    instance: exprGetTokenAsync,
                    method: methodInfo_AsTask);

                var delegateGetTokenAsync = Expression.Lambda(
                    body: exprAsTask,
                    parameters: new ParameterExpression[]
                    {
                        parameterExpression_TokenCredential,
                        parameterExpression_RequestContext,
                        parameterExpression_CancellationToken,
                    }).Compile();

                var parameterExpression_Task = Expression.Parameter(type: methodInfo_AsTask.ReturnType, name: "parameterExpression_Task");

                var exprResultProperty = Expression.Property(
                    expression: parameterExpression_Task,
                    propertyName: "Result");

                var exprTokenProperty = Expression.Property(
                    expression: exprResultProperty,
                    propertyName: "Token");

                var delegateTokenProperty = Expression.Lambda(
                    body: exprTokenProperty,
                    parameters: new ParameterExpression[]
                    {
                        parameterExpression_Task,
                    }).Compile();

                return new[] { delegateGetTokenAsync, delegateTokenProperty };
            }
        }
    }
}
