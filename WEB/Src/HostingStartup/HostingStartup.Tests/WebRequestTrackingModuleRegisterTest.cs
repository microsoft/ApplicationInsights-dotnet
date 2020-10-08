namespace Microsoft.ApplicationInsights.HostingStartup.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.HostingStartup;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebRequestTrackingModuleRegisterTest
    {
        [TestMethod]
        public void GetAIModuleTypeReturnsNullForNonExistingAssembly()
        {
            Assert.IsNull(WebRequestTrackingModuleRegister.GetModuleType("Non.Existing.Assembly", "Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule"));
        }

        [TestMethod]
        public void GetAIModuleTypeReturnsNullForNonExistingClass()
        {
            Assert.IsNull(WebRequestTrackingModuleRegister.GetModuleType(
                typeof(ClassWithFailingStaticConstructor).Assembly.FullName, "Non.Existing.Type"));
        }

        [TestMethod]
        public void GetAIModuleTypeReturnsNullForClassThatCannotBeInstantiated()
        {
            Assert.IsNull(WebRequestTrackingModuleRegister.GetModuleType(
                typeof(ClassWithFailingStaticConstructor).Assembly.GetName().Name, typeof(ClassWithFailingStaticConstructor).FullName));
        }

        [TestMethod]
        public void GetAIModuleTypeReturnsActualType()
        {
            Assert.AreEqual(typeof(WebRequestTrackingModuleRegisterTest), WebRequestTrackingModuleRegister.GetModuleType(typeof(WebRequestTrackingModuleRegisterTest).Assembly.GetName().Name, typeof(WebRequestTrackingModuleRegisterTest).FullName));
        }
    }
}
