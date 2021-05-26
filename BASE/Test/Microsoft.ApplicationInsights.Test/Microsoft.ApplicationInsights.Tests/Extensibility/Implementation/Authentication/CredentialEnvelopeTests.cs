namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("AAD")]
    public class CredentialEnvelopeTests
    {
        /// <summary>
        /// This tests verifies that each supported language can create and set a Credential.
        /// </summary>
        [TestMethod]
        public void VerifyCanSetCredential()
        {
#if NET452 || NET46
            // *THIS IS COMPLICATED*
            // In this case, the test runner is NET452 or NET46.
            // The Azure.Core.TokenCredential is NOT SUPPORTED in these frameworks, so we cannot run this test.
            // This does not affect the end user because we REQUIRE the end user to create their own instance of TokenCredential.
            // This ensures that the end user is consuming the AI SDK in one of the newer frameworks.
#elif NET461 || NETCOREAPP2_1 || NETCOREAPP3_1 || NET5_0
            var mockCredential = new MockCredential();

            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.SetCredential(mockCredential);

            Assert.IsInstanceOfType(telemetryConfiguration.CredentialEnvelope, typeof(ReflectionCredentialEnvelope));
            Assert.AreEqual(mockCredential, telemetryConfiguration.CredentialEnvelope.Credential);
#else
#error This framework is a testing gap.
#endif
        }

#if NET461
        /// <summary>
        /// For older frameworks, TelemetryConfiguration accepts an <see cref="Object"/> parameter.
        /// This method will use reflection to verify the type at runtime.
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

#if NETCOREAPP2_1 || NETCOREAPP3_1 || NET5_0
        /// <summary>
        /// This test verifies that both <see cref="TokenCredentialEnvelope"/> and <see cref="ReflectionCredentialEnvelope"/> return identical tokens.
        /// This test can only run in frameworks that support both types.
        /// </summary>
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

        /// <summary>
        /// This test verifies that both <see cref="TokenCredentialEnvelope"/> and <see cref="ReflectionCredentialEnvelope"/> return identical tokens.
        /// This test can only run in frameworks that support both types.
        /// </summary>
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
