namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
#if WINDOWS_PHONE || WINDOWS_PHONE_APP || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class SdkVersionPropertyContextInitializerTest
    {
        [TestMethod]
        public void ClassIsInternalAndNotMeantToBeUsedByCustomers()
        {
            Assert.False(typeof(SdkVersionPropertyContextInitializer).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void ClassImplementsIContextInitializerToSupportTelemetryContext()
        {
            Assert.True(typeof(IContextInitializer).GetTypeInfo().IsAssignableFrom(typeof(SdkVersionPropertyContextInitializer).GetTypeInfo()));
        }

        [TestMethod]
        public void InitializeSetsSdkVersionPropertyOfGivenTelemetry()
        {
            var initializer = new SdkVersionPropertyContextInitializer();
            var telemetryContext = new TelemetryContext();
            initializer.Initialize(telemetryContext);

            Assert.NotNull(telemetryContext.Internal.SdkVersion);
        }

        [TestMethod]
        public void InitializeSetsSdkVersionValueAsAssemblyVersion()
        {
            var initializer = new SdkVersionPropertyContextInitializer();
            var telemetryContext = new TelemetryContext();
            initializer.Initialize(telemetryContext);
            
            string expectedSdkVersion;
#if !WINRT
            expectedSdkVersion = typeof(SdkVersionPropertyContextInitializer).Assembly.GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#else
            expectedSdkVersion = typeof(SdkVersionPropertyContextInitializer).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#endif
            Assert.Equal(telemetryContext.Internal.SdkVersion, expectedSdkVersion);
        }
    }
}
