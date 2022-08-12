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
    [TestCategory("AAD")]
    public class AzureHelperTests
    {
        [TestMethod]
        public void VerifyGetScope()
        {
            Assert.AreEqual(expected: "https://monitor.azure.com//.default", AuthHelper.GetScope(audience: "https://monitor.azure.com"));
            Assert.AreEqual(expected: "https://monitor.azure.com//.default", AuthHelper.GetScope(audience: "https://monitor.azure.com/"));

            Assert.ThrowsException<ArgumentNullException>(() => AuthHelper.GetScope(null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => AuthHelper.GetScope(new string('X', AuthConstants.AudienceStringMaxLength + 1)));
        }
    }
}
