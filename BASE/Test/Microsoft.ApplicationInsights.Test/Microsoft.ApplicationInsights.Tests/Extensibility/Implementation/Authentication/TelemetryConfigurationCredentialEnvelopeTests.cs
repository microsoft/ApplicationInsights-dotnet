#if !NET452 && !NET46
namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// These tests verify that <see cref="TelemetryConfiguration"/> can receive and store an instance of <see cref="Azure.Core.TokenCredential"/>.
    /// </summary>
    /// <remarks>
    /// These tests do not run in NET452 OR NET46. 
    /// In these cases, the test runner is NET452 or NET46 and Azure.Core.TokenCredential is NOT SUPPORTED in these frameworks.
    /// This does not affect the end user because we REQUIRE the end user to create their own instance of TokenCredential.
    /// This ensures that the end user is consuming the AI SDK in one of the newer frameworks.
    /// </remarks>
    [TestClass]
    [TestCategory("AAD")]
    public class TelemetryConfigurationCredentialEnvelopeTests
    {
        /// <summary>
        /// This tests verifies that each supported language can create and set a Credential.
        /// </summary>
        [TestMethod]
        public void VerifyCanSetCredential()
        {
            var mockCredential = new MockCredential();

            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.SetCredential(mockCredential);

            Assert.IsInstanceOfType(telemetryConfiguration.CredentialEnvelope, typeof(ReflectionCredentialEnvelope));
            Assert.AreEqual(mockCredential, telemetryConfiguration.CredentialEnvelope.Credential, "Credential should be the same instance that we pass in.");
        }

        /// <summary>
        /// TelemetryConfiguration accepts an <see cref="Object"/> parameter, and uses reflection to verify the type at runtime 
        /// This test is to verify that we cannot set invalid types.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void VerifyCannotSetInvalidObjectOnTelemetryConfiguration()
        {
            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.SetCredential(Guid.Empty);
        }
    }
}
#endif
