namespace Microsoft.ApplicationInsights.Authentication.Tests
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
