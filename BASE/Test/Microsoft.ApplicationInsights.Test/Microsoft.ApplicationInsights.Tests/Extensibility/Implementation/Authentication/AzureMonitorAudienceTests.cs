namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Authentication
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [TestClass]
    public class AzureMonitorAudienceTests
    {
        [TestMethod]
        public void VerifyScope()
        {
            RunTest(input: "https://monitor.azure.com", expected: "https://monitor.azure.com//.default");
            RunTest(input: "https://monitor.azure.com/", expected: "https://monitor.azure.com//.default");
            RunTest(input: AzureMonitorAudience.AzurePublicCloud, expected: "https://monitor.azure.com//.default");
            RunTest(input: AzureMonitorAudience.AzureChinaCloud, expected: "https://monitor.azure.cn//.default");
            RunTest(input: AzureMonitorAudience.AzureUSGovernment, expected: "https://monitor.azure.us//.default");
        }

        private void RunTest(string input, string expected)
        {
            Assert.AreEqual(expected, AzureMonitorAudience.GetScopes(input).Single());
        }
    }
}
