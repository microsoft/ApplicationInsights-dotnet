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

            // first request expected to fail
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore));
            Thread.Sleep(10); // wait for Async Tasks to resolve.
            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string actual));
            Assert.AreEqual(testCorrelationId, actual);
        }

        /// <summary>
        /// Protect against injection attacks. Test that if an malicious value is returned, that value will be truncated.
        /// </summary>
        [TestMethod]
        public void VerifyMaliciousAppIdIsTruncated() 
        {
            // 50 character string.
            var testAppId = "a123456789b123546789c123456789d123456798e123456789";

            // An arbitrary string that is expected to be truncated.
            var malicious = "00000000000000000000000000000000000000000000000000000000000";

            var testCorrelationId = CorrelationIdHelper.FormatAppId(testAppId);

            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testAppId + malicious);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            // first request expected to fail
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore));
            Thread.Sleep(10); // wait for Async Tasks to resolve.
            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string actual));
            Assert.AreEqual(testCorrelationId, actual);
        }

        [TestMethod]
        public void VerifyWhenTaskInProgressNoNewTaskCreated() //TODO: THIS TEST IS BROKEN
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(() =>
            {
                Thread.Sleep(2000); //long pause
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(testAppId)
                });
            });
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            // first request expected to fail
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore1));
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore2));

            Assert.AreEqual(1, mockProfileServiceWrapper.FetchTasks.Count);
        }

        [TestMethod]
        public void VerifyWhenRequestFailsWillWaitBeforeRetry()
        {
            mockMethodFailOnceStateBool = false;
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(MockMethodFailOnce);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore1));
            Thread.Sleep(10); // wait for Async Tasks to resolve.
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore2)); // first retry should fail, too soon
            Thread.Sleep(15000); // wait for timeout to expire (15 seconds).
            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string actual)); // second retry should succeed, longer wait
            Assert.AreEqual(testCorrelationId, actual);
        }

        [TestMethod]
        public void VerifyWhenRequestHardFailsWillNotRetry() //TODO: TAKE ARRPOACH OF TASK TEST, VERIFY THAT NEW TASK NOT CREATED
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.NotFound);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore1));
            Thread.Sleep(10); // wait for Async Tasks to resolve.
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore2)); // retry should fail, fatal error
            Thread.Sleep(15000); // wait for timeout to expire (15 seconds).
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore3)); // retry should still fail, fatal error
        }

        private ProfileServiceWrapper GenerateMockServiceWrapper(HttpStatusCode httpStatus, string testAppId = null)
        {
            var mock = new Mock<ProfileServiceWrapper>((int)10); //10 seconds default request retry
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

        private ProfileServiceWrapper GenerateMockServiceWrapper(Func<Task<HttpResponseMessage>> overrideGetAsync)
        {
            var mock = new Mock<ProfileServiceWrapper>((int)10); //10 seconds default request retry
            mock.Setup(x => x.GetAsync(It.IsAny<string>()))
                .Returns(overrideGetAsync);
            return mock.Object;
        }

        /// <summary>
        /// This bool is external to the method so it will save state between runs.
        /// </summary>
        bool mockMethodFailOnceStateBool;
        private Task<HttpResponseMessage> MockMethodFailOnce()
        {
            // Simulate a retry scenario: On first run fail, on second run pass. 
            Console.WriteLine($"test {mockMethodFailOnceStateBool}");
            if (mockMethodFailOnceStateBool)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(testAppId)
                });
            }
            else
            {
                mockMethodFailOnceStateBool = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
        }
    }
}

