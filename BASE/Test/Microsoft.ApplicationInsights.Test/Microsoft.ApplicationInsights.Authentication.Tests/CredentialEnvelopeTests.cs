namespace Microsoft.ApplicationInsights.Authentication.Tests
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CredentialEnvelopeTests
    {
        [TestMethod]
        public void VerifyCanSetCredential()
        {
            var defaultTokenCredential = new Azure.Identity.DefaultAzureCredential();

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
    }

}
