namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.Linq;
    using System.Web;

    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
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
    }
}