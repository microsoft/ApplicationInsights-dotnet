namespace Microsoft.ApplicationInsights.Web
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
    using System.Web;

    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ApplicationInsightsHttpModuleTests
    {
        private const long AllKeywords = -1;

        private PrivateObject module;
        private PrivateObject module2;

        [TestInitialize]
        public void Initialize()
        {
            this.module = HttpModuleHelper.CreateTestModule();
            this.module2 = HttpModuleHelper.CreateTestModule();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ((IHttpModule)this.module.Target).Dispose();
            ((IHttpModule)this.module2.Target).Dispose();
        }

#if NET40
        [TestMethod]
        public void OnEndAddsFlagInHttpContext()
        {
            var httpApplication = HttpModuleHelper.GetFakeHttpApplication();

            this.module.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { httpApplication, null }, CultureInfo.InvariantCulture);

            Assert.IsNotNull(httpApplication.Context.Items[RequestTrackingConstants.EndRequestCallFlag]);
        }
#endif
    }
}