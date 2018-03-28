namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class ProfileServiceWrapperTests
    {
        const int testTimeoutMilliseconds = 20000; // 20 seconds
        const int failedRequestRetryWaitTimeSeconds = 1;
        const string testInstrumentationKey = nameof(testInstrumentationKey);
        const string testApplicationId = nameof(testApplicationId);

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public async Task VerifyHappyPath()
        {
            var profileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testApplicationId);

            var actualApplicationId = await profileServiceWrapper.FetchApplicationIdAsync(testInstrumentationKey);
            Assert.AreEqual(testApplicationId, actualApplicationId);
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public async Task VerifyCanRetryHttp500ErrorAfterTimeout()
        {
            var profileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.InternalServerError, testApplicationId);

            var fetch1 = await profileServiceWrapper.FetchApplicationIdAsync(testInstrumentationKey);
            Assert.IsNull(fetch1);

            Assert.IsFalse(profileServiceWrapper.FailedRequestsManager.CanRetry(testInstrumentationKey)); //TODO: SHOULD CHECK WITH A LOOP

            Thread.Sleep(TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds + 1));

            Assert.IsTrue(profileServiceWrapper.FailedRequestsManager.CanRetry(testInstrumentationKey));
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public async Task VerifyCanNotRetryHttp400Error()
        {
            var profileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.NotFound, testApplicationId);

            var fetch1 = await profileServiceWrapper.FetchApplicationIdAsync(testInstrumentationKey);
            Assert.IsNull(fetch1);

            Assert.IsFalse(profileServiceWrapper.FailedRequestsManager.CanRetry(testInstrumentationKey));

            Thread.Sleep(TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds + 1));

            Assert.IsFalse(profileServiceWrapper.FailedRequestsManager.CanRetry(testInstrumentationKey));
        }

        private ProfileServiceWrapper GenerateMockServiceWrapper(HttpStatusCode httpStatus, string testApplicationId = null)
        {
            var mock = new Mock<ProfileServiceWrapper>(failedRequestRetryWaitTimeSeconds);
            mock.Setup(x => x.GetAsync(It.IsAny<string>()))
                .Returns(() =>
                {
                    return Task.FromResult(new HttpResponseMessage(httpStatus)
                    {
                        Content = new StringContent(testApplicationId)
                    });
                });
            return mock.Object;
        }
    }
}
