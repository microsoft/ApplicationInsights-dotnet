namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using static Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup.ApplicationInsightsCorrelationIdProvider;

    [TestClass]
    public class ApplicationInsightsCorrelationIdProviderTests
    {
        public const string testIKey = nameof(testIKey);
        public const string testAppId = nameof(testAppId);
        public readonly string testCorrelationId = CorrelationIdHelper.FormatAppId(testAppId);

        /// <summary>
        /// Lookup is expected to fail on first call, this is how it invokes the Http request.
        /// </summary>
        [TestMethod]
        public void VerifyFailsOnFirstRequest()
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testAppId);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore));
        }

        /// <summary>
        /// Lookup is expected to succeed on the second call, after the Http request has completed.
        /// </summary>
        [TestMethod]
        public void VerifySucceedsOnSecondRequest()
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testAppId);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);
            
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore));
            Thread.Sleep(10); // wait for Async Tasks to resolve.
            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string actual));
            Assert.AreEqual(testCorrelationId, actual);
        }

        private ProfileServiceWrapper GenerateMockServiceWrapper(HttpStatusCode httpStatus, string testAppId)
        {
            var mock = new Mock<ProfileServiceWrapper>();
            mock.Setup(x => x.GetAsync(It.IsAny<string>()))
                .Returns(() =>
                {
                    return Task.FromResult(new HttpResponseMessage(httpStatus)
                    {
                        Content = new StringContent(testAppId)
                    });
                });
            return mock.Object;
        }
    }
}

