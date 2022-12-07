#if !NET452 && !NET46
namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using System.Threading;

    using Azure.Core;

    /// <summary>
    /// A <see cref="MockCredential"/> that counts the number of calls to <see cref="GetToken(Azure.Core.TokenRequestContext, System.Threading.CancellationToken)"/>.
    /// </summary>
    public class CountingMockCredential : MockCredential
    {
        private int getTokenCallCount;

        /// <summary>
        /// Gets or sets the call count.
        /// </summary>
        public int GetTokenCallCount { get => getTokenCallCount; private set => getTokenCallCount = value; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref getTokenCallCount);
            return base.GetToken(requestContext, cancellationToken);
        }
    }
}
#endif
