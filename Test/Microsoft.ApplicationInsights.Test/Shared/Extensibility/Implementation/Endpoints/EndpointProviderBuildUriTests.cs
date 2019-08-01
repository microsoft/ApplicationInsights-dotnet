namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Endpoints")]
    public class EndpointProviderBuildUriTests
    {
        /// <summary>
        /// Location and Endpoint are user input fields (via connection string).
        /// Need to ensure that if the user inputs extra periods, that we don't crash.
        /// </summary>
        [TestMethod]
        public void VerifyCanHandleExtraPeriods()
        {
            var uri = EndpointProvider.BuildUri(
                location: "westus2.",
                prefix: "dc",
                suffix: ".applicationinsights.azure.cn");

            Assert.AreEqual("https://westus2.dc.applicationinsights.azure.cn/", uri.AbsoluteUri);
        }

        [TestMethod]
        public void VerifyGoodAddress_WithLocation()
        {
            var uri = EndpointProvider.BuildUri(
                location: "westus2",
                prefix: "dc",
                suffix: "applicationinsights.azure.cn");

            Assert.AreEqual("https://westus2.dc.applicationinsights.azure.cn/", uri.AbsoluteUri);
        }

        [TestMethod]
        public void VerifyGoodAddress_WithoutLocation()
        {
            var uri = EndpointProvider.BuildUri(
                location: null,
                prefix: "dc",
                suffix: "applicationinsights.azure.cn");

            Assert.AreEqual("https://dc.applicationinsights.azure.cn/", uri.AbsoluteUri);
        }
    }
}
