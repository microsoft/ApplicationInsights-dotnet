namespace Microsoft.ApplicationInsights.Authentication.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Core;
    using Azure.Core.Pipeline;
    using Azure.Identity;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CredentialEnvelopeTests
    {
        [TestMethod]
        public void VerifyCanSetCredential()
        {
            var defaultTokenCredential = new MockCredential();

            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.SetCredential(defaultTokenCredential);


            var credentialEnvelope = telemetryConfiguration.CredentialEnvelope;
#if NET461
            Assert.IsInstanceOfType(credentialEnvelope, typeof(ReflectionCredentialEnvelope));
#elif NET5_0
            Assert.IsInstanceOfType(credentialEnvelope, typeof(TokenCredentialEnvelope));
#else
            throw new System.Exception("this is a testing gap.");
#endif

            Assert.AreEqual(defaultTokenCredential, telemetryConfiguration.CredentialEnvelope.Credential);
        }

#if NET461
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyCannotSetInvalidType()
        {
            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.SetCredential(Guid.Empty);
        }
#endif

#if NET5_0
        [TestMethod]
        public void VerifyCanGetTokenString()
        {
            var mockCredential = new MockCredential();

            var tokenCredentialEnvelope = new TokenCredentialEnvelope(mockCredential);
            var token = tokenCredentialEnvelope.GetToken();
            Assert.IsNotNull(token);
            
            var reflectionCredentialEnvelope = new ReflectionCredentialEnvelope(mockCredential);
            var tokenFromReflection = reflectionCredentialEnvelope.GetToken();
            Assert.IsNotNull(tokenFromReflection);

            Assert.AreEqual(token, tokenFromReflection);
        }
#endif
        /// <remarks>
        /// Copied from (https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/core/Azure.Core.TestFramework/src/MockCredential.cs).
        /// </remarks>
        private class MockCredential : TokenCredential
        {
            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
            }

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                return new AccessToken("TEST TOKEN " + string.Join(" ", requestContext.Scopes), DateTimeOffset.MaxValue);
            }
        }
    }
}
