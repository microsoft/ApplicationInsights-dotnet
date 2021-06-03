#if !NET452 && !NET46
namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Core;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// The <see cref="ReflectionCredentialEnvelope"/> cannot take a dependency on <see cref="Azure.Core.TokenCredential"/>.
    /// We must use reflection to interact with this class.
    /// These tests are to confirm that we can correctly identity classes that implement TokenCredential and address it's methods.
    /// </summary>
    /// <remarks>
    /// These tests do not run in NET452 OR NET46. 
    /// In these cases, the test runner is NET452 or NET46 and Azure.Core.TokenCredential is NOT SUPPORTED in these frameworks.
    /// This does not affect the end user because we REQUIRE the end user to create their own instance of TokenCredential.
    /// This ensures that the end user is consuming the AI SDK in one of the newer frameworks.
    /// </remarks>
    [TestClass]
    [TestCategory("AAD")]
    public class ReflectionCredentialEnvelopeTests
    {
        [TestMethod]
        public void VerifyCanIdentifyValidClass()
        {
            var testClass2 = new TestClassInheritsTokenCredential();
            _ = new ReflectionCredentialEnvelope(testClass2);
            // NO ASSERT. This test is valid if no exception is thrown. :)
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyCanIdentityInvalidClass()
        {
            var notTokenCredential2 = new NotTokenCredential2();
            _ = new ReflectionCredentialEnvelope(notTokenCredential2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyCannotSetInvalidType()
        {
            _ = new ReflectionCredentialEnvelope(Guid.Empty);
        }

        [TestMethod]
        public void VerifyCanMakeTokenRequestContext()
        {
            var testScope = new string[] { "test/scope" };

            var requestContext = new TokenRequestContext(testScope);

            var tokenRequestContextViaReflection = ReflectionCredentialEnvelope.AzureCore.MakeTokenRequestContext(testScope);
            Assert.IsInstanceOfType(tokenRequestContextViaReflection, typeof(TokenRequestContext));
            Assert.AreEqual(requestContext, tokenRequestContextViaReflection);
        }

        [TestMethod]
        public void VerifyGetToken_UsingCompileTimeTypes()
        {
            var mockCredential = new MockCredential();
            var requestContext = new TokenRequestContext(new string[] { "test/scope" });

            var testResult = ReflectionCredentialEnvelope.AzureCore.InvokeGetToken(mockCredential, requestContext, CancellationToken.None);

            Assert.AreEqual("TEST TOKEN test/scope", testResult);
        }

        [TestMethod]
        public async Task VerifyGetTokenAsync_UsingCompileTimeTypes()
        {
            var mockCredential = new MockCredential();
            var requestContext = new TokenRequestContext(new string[] { "test/scope" });

            var testResult = await ReflectionCredentialEnvelope.AzureCore.InvokeGetTokenAsync(mockCredential, requestContext, CancellationToken.None);

            Assert.AreEqual("TEST TOKEN test/scope", testResult);
        }

        /// <summary>
        /// This more closely represents how this would be used in a production environment.
        /// </summary>
        [TestMethod]
        public void VerifyGetToken_UsingDynamicTypes()
        {
            var mockCredential = (object)new MockCredential();
            var requestContext = ReflectionCredentialEnvelope.AzureCore.MakeTokenRequestContext(new[] { "test/scope" });

            var testResult = ReflectionCredentialEnvelope.AzureCore.InvokeGetToken(mockCredential, requestContext, CancellationToken.None);

            Assert.AreEqual("TEST TOKEN test/scope", testResult);
        }

        /// <summary>
        /// This more closely represents how this would be used in a production environment.
        /// </summary>
        [TestMethod]
        public async Task VerifyGetTokenAsync_UsingDynamicTypes()
        {
            var mockCredential = (object)new MockCredential();
            var requestContext = ReflectionCredentialEnvelope.AzureCore.MakeTokenRequestContext(new[] { "test/scope" });

            var testResult = await ReflectionCredentialEnvelope.AzureCore.InvokeGetTokenAsync(mockCredential, requestContext, CancellationToken.None);

            Assert.AreEqual("TEST TOKEN test/scope", testResult);
        }

        /// <summary>
        /// This test verifies that both <see cref="Azure.Core"/> and <see cref="ReflectionCredentialEnvelope"/> return identical tokens.
        /// </summary>
        [TestMethod]
        public void VerifyGetToken_ReturnsValidToken()
        {
            var requestContext = new TokenRequestContext(scopes: AuthConstants.GetScopes());
            var mockCredential = new MockCredential();
            var tokenUsingTypes = mockCredential.GetToken(requestContext, CancellationToken.None);

            var reflectionCredentialEnvelope = new ReflectionCredentialEnvelope(mockCredential);
            var tokenUsingReflection = reflectionCredentialEnvelope.GetToken();

            Assert.AreEqual(tokenUsingTypes.Token, tokenUsingReflection);
        }

        /// <summary>
        /// This test verifies that both <see cref="Azure.Core"/> and <see cref="ReflectionCredentialEnvelope"/> return identical tokens.
        /// </summary>
        [TestMethod]
        public async Task VerifyGetTokenAsync_ReturnsValidToken()
        {
            var requestContext = new TokenRequestContext(scopes: AuthConstants.GetScopes());
            var mockCredential = new MockCredential();
            var tokenUsingTypes = await mockCredential.GetTokenAsync(requestContext, CancellationToken.None);

            var reflectionCredentialEnvelope = new ReflectionCredentialEnvelope(mockCredential);
            var tokenUsingReflection = await reflectionCredentialEnvelope.GetTokenAsync();

            Assert.AreEqual(tokenUsingTypes.Token, tokenUsingReflection);
        }

        [TestMethod]
        public void VerifyGetToken_IfCredentialThrowsException_EnvelopeReturnsNull()
        {
            Mock<TokenCredential> mockTokenCredential = new Mock<TokenCredential>();
            mockTokenCredential.Setup(x => x.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>())).Throws(new NotImplementedException());
            var mockCredential = mockTokenCredential.Object;

            var reflectionCredentialEnvelope = new ReflectionCredentialEnvelope(mockCredential);
            var token = reflectionCredentialEnvelope.GetToken();
            Assert.IsNull(token);
        }
        
        [TestMethod]
        public async Task VerifyGetTokenAsync_IfCredentialThrowsException_EnvelopeReturnsNull()
        {
            Mock<TokenCredential> mockTokenCredential = new Mock<TokenCredential>();
            mockTokenCredential.Setup(x => x.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>())).Throws(new NotImplementedException());
            var mockCredential = mockTokenCredential.Object;

            var reflectionCredentialEnvelope = new ReflectionCredentialEnvelope(mockCredential);
            var token = await reflectionCredentialEnvelope.GetTokenAsync();
            Assert.IsNull(token);
        }

#region TestClasses
        
        /// <summary>
        /// This class inherits <see cref="MockCredential"/> which inherits <see cref="Azure.Core.TokenCredential"/>.
        /// This class is used to verify that the <see cref="ReflectionCredentialEnvelope"/> can correctly identify tests that inherit <see cref="Azure.Core.TokenCredential"/>.
        /// </summary>
        private class TestClassInheritsTokenCredential : MockCredential { }

        private abstract class NotTokenCredential
        {
            public abstract AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken);

            public abstract ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken);
        }

        private class NotTokenCredential1 : NotTokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class NotTokenCredential2 : NotTokenCredential1 { }
#endregion
    }
}
#endif
