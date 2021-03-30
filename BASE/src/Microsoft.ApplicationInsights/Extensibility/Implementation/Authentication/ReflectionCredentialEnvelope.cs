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
    /// This class uses compiled Expression Trees.
    /// Read more here: 
    /// (https://docs.microsoft.com/dotnet/csharp/programming-guide/concepts/expression-trees/).
    /// (https://docs.microsoft.com/dotnet/csharp/expression-trees).
    /// </remarks>
    internal class ReflectionCredentialEnvelope : CredentialEnvelope
    {
        private readonly Type tokenCredentialType = Type.GetType("Azure.Core.TokenCredential, Azure.Core");
        private readonly Type tokenRequestContextType = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");
        private readonly Type accessTokenType = Type.GetType("Azure.Core.AccessToken, Azure.Core");

        private readonly object tokenCredential;
        private readonly object tokenRequestContext;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCredential"></param>
        public ReflectionCredentialEnvelope(object tokenCredential)
        {
            this.tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));

            if (tokenCredential.GetType().IsSubclassOf(tokenCredentialType))
            {
                this.tokenRequestContext = MakeTokenRequestContext(scopes: GetScopes());
            }
            else
            {
                throw new ArgumentException($"The provided {nameof(tokenCredential)} must inherit Azure.Core.TokenCredential", nameof(tokenCredential));
            }
        }

        public override object Credential => this.tokenCredential;

        /// <summary>
        /// This is a wrapper for the following constructor:
        /// <code>public TokenRequestContext (string[] scopes, string? parentRequestId = default, string? claims = default);</code>
        /// (https://docs.microsoft.com/dotnet/api/azure.core.tokenrequestcontext.-ctor).
        /// </summary>
        internal static object MakeTokenRequestContext(string[] scopes)
        {
            return Activator.CreateInstance(
                type: Type.GetType("Azure.Core.TokenRequestContext, Azure.Core"),
                args: new object[] { scopes, null, null });
        }

        public override string GetToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            var expression = GetTokenAsLambdaExpression();
            var @delegate = expression.Compile(); // TODO: THIS NEEDS TO BE STORED AS A PRIVATE FIELD SO IT CAN BE REUSED.
            return (string)@delegate.DynamicInvoke(this.tokenCredential, this.tokenRequestContext, cancellationToken);
        }

        ///// <summary>
        ///// This is a wrapper for the following method:
        ///// <code>public abstract Azure.Core.AccessToken GetToken (Azure.Core.TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken);</code>
        ///// (https://docs.microsoft.com/dotnet/api/azure.core.tokencredential.gettoken).
        ///// </summary>
        ///// <typeparam name="T1"></typeparam>
        ///// <typeparam name="T2"></typeparam>
        ///// <param name="T_TokenCredential"></param>
        ///// <param name="T_TokenRequestContext"></param>
        ///// <returns></returns>
        //internal static Expression<Func<T1, T2, CancellationToken, string>> GetTokenAsExpression<T1, T2>(T1 T_TokenCredential, T2 T_TokenRequestContext)
        //{
        //    Type typeTokenCredential = Type.GetType("Azure.Core.TokenCredential, Azure.Core");
        //    Type typeTokenRequestContext = Type.GetType("Azure.Core.TokenRequestContext, Azure.Core");
        //    Type typeCancellationToken = typeof(CancellationToken);

        //    if (!T_TokenCredential.GetType().IsSubclassOf(typeTokenCredential))
        //    {
        //        throw new ArgumentException("Must be an instance of Azure.Core.TokenCredential", nameof(T_TokenCredential));
        //    }

        //    if (!T_TokenRequestContext.GetType().IsEquivalentTo(typeTokenRequestContext))
        //    {
        //        throw new ArgumentException("Must be an instance of Azure.Core.TokenRequestContext", nameof(T_TokenRequestContext));
        //    }

        //    var parameterExpression_tokenCredential = Expression.Parameter(type: typeTokenCredential, name: "parameterExpression_TokenCredential");
        //    var parameterExpression_requestContext = Expression.Parameter(type: typeTokenRequestContext, name: "parameterExpression_RequestContext");
        //    var parameterExpression_cancellationToken = Expression.Parameter(type: typeCancellationToken, name: "parameterExpression_CancellationToken");

        //    var exprGetToken = Expression.Call(
        //        instance: parameterExpression_tokenCredential,
        //        method: typeTokenCredential.GetMethod(name: "GetToken", types: new Type[] { typeTokenRequestContext, typeCancellationToken }),
        //        arg0: parameterExpression_requestContext,
        //        arg1: parameterExpression_cancellationToken
        //        );

        //    var exprTokenProperty = Expression.Property(
        //        expression: exprGetToken,
        //        propertyName: "Token"
        //        );


        //    var test = Expression.Lambda(
        //        body: exprTokenProperty,
        //        parameters: new ParameterExpression[]
        //        {
        //            parameterExpression_tokenCredential,
        //            parameterExpression_requestContext,
        //            parameterExpression_cancellationToken
        //        });
        //    var test2 = test.Compile();
        //    var result = test2.DynamicInvoke(T_TokenCredential, T_TokenRequestContext, CancellationToken.None);


        //    return Expression.Lambda<Func<T1, T2, CancellationToken, string>>(
        //        body: exprTokenProperty,
        //        parameters: new ParameterExpression[]
        //        {
        //            parameterExpression_tokenCredential,
        //            parameterExpression_requestContext,
        //            parameterExpression_cancellationToken
        //        });
        //}

        /// <summary>
        /// This creates a wrapper for the following method:
        /// <code>public abstract Azure.Core.AccessToken GetToken (Azure.Core.TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken);</code>
        /// (https://docs.microsoft.com/dotnet/api/azure.core.tokencredential.gettoken).
        /// </summary>
        internal static LambdaExpression GetTokenAsLambdaExpression()
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
                arg1: parameterExpression_cancellationToken
                );

            var exprTokenProperty = Expression.Property(
                expression: exprGetToken,
                propertyName: "Token"
                );

            return Expression.Lambda(
                body: exprTokenProperty,
                parameters: new ParameterExpression[]
                {
                    parameterExpression_tokenCredential,
                    parameterExpression_requestContext,
                    parameterExpression_cancellationToken
                });
        }


        /// <summary>
        /// This is a wrapper for the following method:
        /// <code>public abstract System.Threading.Tasks.ValueTask&lt;Azure.Core.AccessToken> GetTokenAsync (Azure.Core.TokenRequestContext requestContext, System.Threading.CancellationToken cancellationToken);</code>
        /// (https://docs.microsoft.com/dotnet/api/azure.core.tokencredential.gettokenasync).
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// TODO: CONVERT THIS INTO A PARAMETER-LESS METHOD THAT RETURNS THE EXPRESSION
        public static async Task<string> GetTokenAsyncAsExpression(object tokenCredential, object tokenRequestContext, CancellationToken cancellationToken)
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
                arg1: parameterExpression_CancellationToken
                );

            var methodInfo_AsTask = methodInfo_GetTokenAsync.ReturnType.GetMethod("AsTask");

            var exprAsTask = Expression.Call(
                instance: exprGetTokenAsync,
                method: methodInfo_AsTask
                );

            var delegateGetTokenAsyncAsTask = Expression.Lambda(
                body: exprAsTask,
                parameters: new ParameterExpression[]
                {
                    parameterExpression_TokenCredential,
                    parameterExpression_RequestContext,
                    parameterExpression_CancellationToken
                }).Compile();

            var task = (Task)delegateGetTokenAsyncAsTask.DynamicInvoke(tokenCredential, tokenRequestContext, cancellationToken);
            await task.ConfigureAwait(false);

            var parameterExpression_Task = Expression.Parameter(type: methodInfo_AsTask.ReturnType, name: "parameterExpression_Task");

            var exprResultProperty = Expression.Property(
                expression: parameterExpression_Task,
                propertyName: "Result"
                );

            var exprTokenProperty = Expression.Property(
                expression: exprResultProperty,
                propertyName: "Token"
                );

            var delegateToken = Expression.Lambda(
                body: exprTokenProperty,
                parameters: new ParameterExpression[]
                {
                    parameterExpression_Task,
                }).Compile();

            return (string)delegateToken.DynamicInvoke(task);
        }
    }
}
