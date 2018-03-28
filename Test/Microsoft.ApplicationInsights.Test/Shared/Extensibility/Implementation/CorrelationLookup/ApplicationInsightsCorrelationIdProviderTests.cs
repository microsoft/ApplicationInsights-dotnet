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
    public class ApplicationInsightsCorrelationIdProviderTests
    {
        const int testTimeoutMilliseconds = 20000; // 20 seconds
        const int taskWaitMilliseconds = 200;
        const int failedRequestRetryWaitTimeSeconds = 2;
        const string testIKey = nameof(testIKey);
        const string testAppId = nameof(testAppId);
        readonly string testCorrelationId = CorrelationIdHelper.FormatAppId(testAppId);

        /// <summary>
        /// Lookup is expected to fail on first call, this is how it invokes the Http request.
        /// </summary>
        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyFailsOnFirstRequest()
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testAppId);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore));
        }

        /// <summary>
        /// Lookup is expected to succeed on the second call, after the Http request has completed.
        /// </summary>
        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifySucceedsOnSecondRequest()
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testAppId);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            // first request expected to fail
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore));

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testIKey))
            {
                Console.WriteLine("wait");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string actual));
            Assert.AreEqual(testCorrelationId, actual);
        }

        /// <summary>
        /// Protect against injection attacks. Test that if an malicious value is returned, that value will be truncated.
        /// </summary>
        [TestMethod, Timeout(testTimeoutMilliseconds)]
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

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testIKey)) 
            {
                Console.WriteLine("wait");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string actual));
            Assert.AreEqual(testCorrelationId, actual);
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyWhenTaskInProgressNoNewTaskCreated()
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(() =>
            {
                Console.WriteLine($"sleep start: {DateTime.UtcNow}");
                Thread.Sleep(60000); // intentional long pause,  inspect tasks
                Console.WriteLine($"sleep stop: {DateTime.UtcNow}");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(testAppId)
                });
            });
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Console.WriteLine($"first request: {DateTime.UtcNow}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore1));
            Console.WriteLine($"second request: {DateTime.UtcNow}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore2));

            Assert.AreEqual(1, aiCorrelationIdProvider.FetchTasks.Count);
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyWhenRequestFailsWillWaitBeforeRetry() 
        {
            mockMethodFailOnceStateBool = false;
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(this.MockMethodFailOnce);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);
            var stopWatch = new Stopwatch();

            Console.WriteLine($"first request: {DateTime.UtcNow.ToString("HH:mm:ss:fffff")}");
            stopWatch.Start();
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore1)); // first request will fail

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testIKey))
            {
                Console.WriteLine("wait for task");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Console.WriteLine($"\nsecond request: {DateTime.UtcNow.ToString("HH:mm:ss:fffff")}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore2)); // first retry should fail, too soon

            while(!mockProfileServiceWrapper.FailedRequestsManager.CanRetry(testIKey))
            {
                Console.WriteLine("wait for retry");
                Thread.Sleep(taskWaitMilliseconds);
            }
            stopWatch.Stop();

            Console.WriteLine($"\nthird request: {DateTime.UtcNow.ToString("HH:mm:ss:fffff")}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore3)); // second retry should fail (because no matching value), but will create new request task

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testIKey))
            {
                Console.WriteLine("wait for task");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Console.WriteLine($"\nfourth request: {DateTime.UtcNow.ToString("HH:mm:ss:fffff")}");
            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string actual)); // third retry resolve

            Assert.AreEqual(testCorrelationId, actual);
            Assert.IsTrue(stopWatch.Elapsed >= TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds));
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyWhenRequestHardFailsWillNotRetry() 
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.NotFound);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Console.WriteLine("first request");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore1));

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testIKey))
            {
                Console.WriteLine("wait");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Console.WriteLine("second request");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore2)); // retry should fail, fatal error
            Thread.Sleep(15000); // wait for timeout to expire (15 seconds).

            Console.WriteLine("third request");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testIKey, out string ignore3)); // retry should still fail, fatal error
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

        private ProfileServiceWrapper GenerateMockServiceWrapper(Func<Task<HttpResponseMessage>> overrideGetAsync)
        {
            var mock = new Mock<ProfileServiceWrapper>(failedRequestRetryWaitTimeSeconds); 
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
            Console.WriteLine($"will method succeed: {mockMethodFailOnceStateBool}");
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

