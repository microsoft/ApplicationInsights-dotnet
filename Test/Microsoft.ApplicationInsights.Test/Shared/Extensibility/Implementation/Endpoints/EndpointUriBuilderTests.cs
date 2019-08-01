namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Endpoints")]
    public class EndpointUriBuilderTests
    {
        /// <summary>
        /// Location and Endpoint are user input fields (via connection string).
        /// Need to ensure that if the user inputs extra periods, that we don't crash.
        /// </summary>
        [TestMethod]
        public void VerifyCanHandleExtraPeriods()
        {
            var test = new EndpointUriBuilder
            {
                Location = "westus2.",
                Prefix = "dc",
                Host = ".applicationinsights.azure.cn",
            };

            var uri = test.ToUri();
            Assert.AreEqual("https://westus2.dc.applicationinsights.azure.cn/", uri.AbsoluteUri);
        }

        [TestMethod]
        public void VerifyGoodAddress_WithLocation()
        {
            var test = new EndpointUriBuilder
            {
                Location = "westus2",
                Prefix = "dc",
                Host = "applicationinsights.azure.cn",
            };

            var uri = test.ToUri();
            Assert.AreEqual("https://westus2.dc.applicationinsights.azure.cn/", uri.AbsoluteUri);
        }

        [TestMethod]
        public void VerifyGoodAddress_WithoutLocation()
        {
            var test = new EndpointUriBuilder
            {
                Location = null,
                Prefix = "dc",
                Host = "applicationinsights.azure.cn",
            };

            var uri = test.ToUri();
            Assert.AreEqual("https://dc.applicationinsights.azure.cn/", uri.AbsoluteUri);
        }
    }
}
