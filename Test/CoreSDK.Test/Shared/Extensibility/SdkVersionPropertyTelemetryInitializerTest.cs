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
    public class SdkVersionPropertyTelemetryInitializerTest
    {
        [TestMethod]
        public void ClassIsInternalAndNotMeantToBeUsedByCustomers()
        {
            Assert.False(typeof(SdkVersionPropertyTelemetryInitializer).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void ClassImplementsITelemetryInitializerToSupportTelemetryContext()
        {
            Assert.True(typeof(ITelemetryInitializer).GetTypeInfo().IsAssignableFrom(typeof(SdkVersionPropertyTelemetryInitializer).GetTypeInfo()));
        }

        [TestMethod]
        public void InitializeSetsSdkVersionPropertyOfGivenTelemetry()
        {
            var initializer = new SdkVersionPropertyTelemetryInitializer();
            var item = new RequestTelemetry();
            initializer.Initialize(item);

            Assert.NotNull(item.Context.Internal.SdkVersion);
        }

        [TestMethod]
        public void InitializeSetsSdkVersionValueAsAssemblyVersion()
        {
            var initializer = new SdkVersionPropertyTelemetryInitializer();
            var item = new RequestTelemetry();
            initializer.Initialize(item);
            
            string expectedSdkVersion;
#if !WINRT
            expectedSdkVersion = typeof(SdkVersionPropertyTelemetryInitializer).Assembly.GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#else
            expectedSdkVersion = typeof(SdkVersionPropertyTelemetryInitializer).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#endif
            Assert.Equal(item.Context.Internal.SdkVersion, expectedSdkVersion);
        }
    }
}
