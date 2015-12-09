namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            
            string expectedSdkVersion = typeof(SdkVersionPropertyTelemetryInitializer).Assembly.GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
            Assert.Equal(item.Context.Internal.SdkVersion, expectedSdkVersion);
        }
    }
}
