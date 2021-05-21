namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CredentialEnvelopeTests
    {
        [TestMethod]
        public void VerifyCanSetCredential()
        {
            var mockCredential = new MockCredential();

            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.SetCredential(mockCredential);


            var credentialEnvelope = telemetryConfiguration.CredentialEnvelope;
#if NET461
            Assert.IsInstanceOfType(credentialEnvelope, typeof(ReflectionCredentialEnvelope));
#elif NET5_0
            Assert.IsInstanceOfType(credentialEnvelope, typeof(TokenCredentialEnvelope));
#else
            throw new System.Exception("this is a testing gap.");
#endif

            Assert.AreEqual(mockCredential, telemetryConfiguration.CredentialEnvelope.Credential);
        }

#if NET461
        /// <summary>
        /// For older frameworks, TelemetryConfiguration accepts an <see cref="Object"/> parameter.
        /// This test is to verify that we cannot set invalid types.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyCannotSetInvalidObjectOnTelemetryConfiguration()
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

        [TestMethod]
        public async Task VerifyCanGetTokenStringAsync()
        {
            var mockCredential = new MockCredential();

            var tokenCredentialEnvelope = new TokenCredentialEnvelope(mockCredential);
            var token = await tokenCredentialEnvelope.GetTokenAsync();
            Assert.IsNotNull(token);
            
            var reflectionCredentialEnvelope = new ReflectionCredentialEnvelope(mockCredential);
            var tokenFromReflection = await reflectionCredentialEnvelope.GetTokenAsync();
            Assert.IsNotNull(tokenFromReflection);

            Assert.AreEqual(token, tokenFromReflection);
        }
#endif
    }
}
