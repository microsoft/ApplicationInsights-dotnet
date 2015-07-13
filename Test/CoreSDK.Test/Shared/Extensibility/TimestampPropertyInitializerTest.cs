namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Reflection;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.TestFramework;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class TimestampPropertyInitializerTest
    {
        [TestMethod]
        public void ClassIsPublicToAllowUsersCreateIt()
        {
            Assert.True(typeof(TimestampPropertyInitializer).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void ClassImplementsITelemetryInitializerToSupportTelemetryContext()
        {
            Assert.True(typeof(ITelemetryInitializer).GetTypeInfo().IsAssignableFrom(typeof(TimestampPropertyInitializer).GetTypeInfo()));
        }

        [TestMethod]
        public void InitializeSetsTimestampPropertyOfGivenTelemetry()
        {
            var initializer = new TimestampPropertyInitializer();
            var telemetry = new StubTelemetry();
            initializer.Initialize(telemetry);
            Assert.True(DateTimeOffset.Now.Subtract(telemetry.Timestamp) < TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public void InitializeDoesNotOverrideTimestampSpecifiedExplicitly()
        {
            var initializer = new TimestampPropertyInitializer();
            var expected = new DateTimeOffset(new DateTime(42));
            var telemetry = new StubTelemetry { Timestamp = expected };
            initializer.Initialize(telemetry);
            Assert.Equal(expected, telemetry.Timestamp);
        }
    }
}
