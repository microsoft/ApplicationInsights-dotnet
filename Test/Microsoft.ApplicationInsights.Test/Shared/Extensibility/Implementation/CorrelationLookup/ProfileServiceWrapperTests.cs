namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class ProfileServiceWrapperTests
    {
        const int testTimeoutMilliseconds = 20000; // 20 seconds
        const int failedRequestRetryWaitTimeSeconds = 2;
        const string testIkey = nameof(testIkey);
        const string testAppId = nameof(testAppId);

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public async Task VerifyHappyPath()
        {
            var profileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testAppId);

            var actualAppId = await profileServiceWrapper.FetchAppIdAsync(testIkey);
            Assert.AreEqual(testAppId, actualAppId);
        }

        private ProfileServiceWrapper GenerateMockServiceWrapper(HttpStatusCode httpStatus, string testAppId = null)
        {
            var mock = new Mock<ProfileServiceWrapper>(failedRequestRetryWaitTimeSeconds);
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

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public async Task VerifyCanRetryHttp500ErrorAfterTimeout()
        {
            var profileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.InternalServerError, testAppId);

            var fetch1 = await profileServiceWrapper.FetchAppIdAsync(testIkey);
            Assert.IsNull(fetch1);

            Assert.IsFalse(profileServiceWrapper.FailedRequestsManager.CanRetry(testIkey)); //TODO: SHOULD CHECK WITH A LOOP

            Thread.Sleep(TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds + 1));

            Assert.IsTrue(profileServiceWrapper.FailedRequestsManager.CanRetry(testIkey));
        }

        [TestMethod]///, Timeout(testTimeoutMilliseconds)]
        public async Task VerifyCanNotRetryHttp400Error()
        {
            var profileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.NotFound, testAppId);

            var fetch1 = await profileServiceWrapper.FetchAppIdAsync(testIkey);
            Assert.IsNull(fetch1);

            Assert.IsFalse(profileServiceWrapper.FailedRequestsManager.CanRetry(testIkey));

            Thread.Sleep(TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds + 1));

            Assert.IsFalse(profileServiceWrapper.FailedRequestsManager.CanRetry(testIkey));
        }

    }
}
