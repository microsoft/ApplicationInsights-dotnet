#if NET461 || NETCOREAPP2_1 || NETCOREAPP3_1 || NET5_0
namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Core;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The <see cref="ReflectionCredentialEnvelope"/> cannot take a dependency on <see cref="Azure.Core.TokenCredential"/>.
    /// We must use reflection to interact with this class.
    /// These tests are to confirm that we can correctly identity classes that implement TokenCredential and address it's methods.
    /// </summary>
    [TestClass]
    [TestCategory("AAD")]
    public class ReflectionCredentialEnvelopeTests
    {
        [TestMethod]
        public void VerifyCanIdentifyValidClass()
        {
            var testClass2 = new TestClass2();
            _ = new ReflectionCredentialEnvelope(testClass2);
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

        //[TestMethod]
        //public void VerifyGetTokenAsExpression_UsingCompileTimeTypes()
        //{
        //    var mockCredential = new MockCredential();
        //    var requestContext = new TokenRequestContext(new string[] { "test/scope" });

        //    var expression = ReflectionCredentialEnvelope.GetTokenAsExpression(mockCredential, requestContext).Compile();

        //    var testResult = expression(mockCredential, requestContext, CancellationToken.None);
        //    Assert.AreEqual("TEST TOKEN test/scope", testResult);
        //}

        ///// <summary>
        ///// This more closely represents how this would be used in a production environment.
        ///// </summary>
        //[TestMethod]
        //public void VerifyGetTokenAsExpression_UsingDynamicTypes()
        //{
        //    // This currently throws ArgumentExceptions:
        //    // ParameterExpression of type 'Microsoft.ApplicationInsights.Authentication.Tests.MockCredential' cannot be used for delegate parameter of type 'System.Object'
        //    // ParameterExpression of type 'Azure.Core.TokenRequestContext' cannot be used for delegate parameter of type 'System.Object'


        //    var mockCredential = (object)new MockCredential();
        //    var requestContext = ReflectionCredentialEnvelope.MakeTokenRequestContext(new[] { "test/scope" });

        //    var expression = ReflectionCredentialEnvelope.GetTokenAsExpression(mockCredential, requestContext).Compile();

        //    var testResult = expression(mockCredential, requestContext, CancellationToken.None);
        //    Assert.AreEqual("TEST TOKEN test/scope", testResult);
        //}

        [TestMethod]
        public void VerifyCanMakeTokenRequestContext()
        {
            var testScope = new string[] { "test/scope" };

            var requestContext = new TokenRequestContext(testScope);

            var tokenRequestContext = ReflectionCredentialEnvelope.AzureCore.MakeTokenRequestContext(testScope);
            Assert.IsInstanceOfType(tokenRequestContext, typeof(TokenRequestContext));
        }


        [TestMethod]
        public void VerifyGetToken_AsLambdaExpression_UsingCompileTimeTypes()
        {
            var mockCredential = new MockCredential();
            var requestContext = new TokenRequestContext(new string[] { "test/scope" });

            var testResult = ReflectionCredentialEnvelope.AzureCore.InvokeGetToken(mockCredential, requestContext, CancellationToken.None);

            Assert.AreEqual("TEST TOKEN test/scope", testResult);
        }

        /// <summary>
        /// This more closely represents how this would be used in a production environment.
        /// </summary>
        [TestMethod]
        public void VerifyGetToken_AsLambdaExpression_UsingDynamicTypes()
        {
            var mockCredential = (object)new MockCredential();
            var requestContext = ReflectionCredentialEnvelope.AzureCore.MakeTokenRequestContext(new[] { "test/scope" });

            var testResult = ReflectionCredentialEnvelope.AzureCore.InvokeGetToken(mockCredential, requestContext, CancellationToken.None);

            Assert.AreEqual("TEST TOKEN test/scope", testResult);
        }


        [TestMethod]
        public async Task VerifyGetTokenAsync_AsLambdaExpression_UsingCompileTimeTypes()
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
        public async Task VerifyGetTokenAsync_AsLambdaExpression_UsingDynamicTypes()
        {
            var mockCredential = (object)new MockCredential();
            var requestContext = ReflectionCredentialEnvelope.AzureCore.MakeTokenRequestContext(new[] { "test/scope" });

            var testResult = await ReflectionCredentialEnvelope.AzureCore.InvokeGetTokenAsync(mockCredential, requestContext, CancellationToken.None);

            Assert.AreEqual("TEST TOKEN test/scope", testResult);
        }

#region TestClasses
        private class TestClass1 : Azure.Core.TokenCredential
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

        private class TestClass2 : TestClass1 { }

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