namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class ApplicationInsightsCorrelationIdProviderTests : CorrelationLookupTestBase
    {
        /// <summary>
        /// Lookup is expected to fail on first call, this is how it invokes the Http request.
        /// </summary>
        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyFailsOnFirstRequest()
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testApplicationId);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore));
        }

        /// <summary>
        /// Lookup is expected to succeed on the second call, after the Http request has completed.
        /// </summary>
        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifySucceedsOnSecondRequest()
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testApplicationId);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            // first request expected to fail
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore));

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testInstrumentationKey))
            {
                Console.WriteLine("wait");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string actual));
            Assert.AreEqual(testCorrelationId, actual);
        }

        /// <summary>
        /// Protect against injection attacks. Test that if an malicious value is returned, that value will be truncated.
        /// </summary>
        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyMaliciousApplicationIdIsTruncated() 
        {
            // 50 character string.
            var testApplicationId = "a123456789b123546789c123456789d123456798e123456789";

            // An arbitrary string that is expected to be truncated.
            var malicious = "00000000000000000000000000000000000000000000000000000000000";

            var testCorrelationId = CorrelationIdHelper.FormatApplicationId(testApplicationId);

            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.OK, testApplicationId + malicious);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            // first request expected to fail
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore));

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testInstrumentationKey)) 
            {
                Console.WriteLine("wait");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string actual));
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
                    Content = new StringContent(testApplicationId)
                });
            });
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Console.WriteLine($"first request: {DateTime.UtcNow}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore1));
            Console.WriteLine($"second request: {DateTime.UtcNow}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore2));

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
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore1)); // first request will fail, will create internal failure-timeout 

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testInstrumentationKey))
            {
                Console.WriteLine("wait for task");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Console.WriteLine($"\nsecond request: {DateTime.UtcNow.ToString("HH:mm:ss:fffff")}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore2)); // first retry should fail because timeout (too soon)

            while(!mockProfileServiceWrapper.FailedRequestsManager.CanRetry(testInstrumentationKey))
            {
                Console.WriteLine("wait for retry");
                Thread.Sleep(taskWaitMilliseconds);
            }
            stopWatch.Stop();
            Assert.IsTrue(stopWatch.Elapsed >= failedRequestRetryWaitTime, "too fast, did not wait timeout");

            Console.WriteLine($"\nthird request: {DateTime.UtcNow.ToString("HH:mm:ss:fffff")}");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore3)); // second retry should fail (because no matching value), but will create new request task

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testInstrumentationKey))
            {
                Console.WriteLine("wait for task");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Console.WriteLine($"\nfourth request: {DateTime.UtcNow.ToString("HH:mm:ss:fffff")}");
            Assert.IsTrue(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string actual)); // third retry should resolve

            Assert.AreEqual(testCorrelationId, actual);
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyWhenRequestHardFailsWillNotRetry() 
        {
            var mockProfileServiceWrapper = GenerateMockServiceWrapper(HttpStatusCode.NotFound);
            var aiCorrelationIdProvider = new ApplicationInsightsCorrelationIdProvider(mockProfileServiceWrapper);

            Console.WriteLine("first request");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore1));

            // wait for async tasks to complete
            while (aiCorrelationIdProvider.IsFetchAppInProgress(testInstrumentationKey))
            {
                Console.WriteLine("wait");
                Thread.Sleep(taskWaitMilliseconds);
            }

            Console.WriteLine("second request");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore2)); // retry should fail, fatal error
            Thread.Sleep(failedRequestRetryWaitTime + failedRequestRetryWaitTime); // wait for timeout to expire (2x timeout).

            Console.WriteLine("third request");
            Assert.IsFalse(aiCorrelationIdProvider.TryGetCorrelationId(testInstrumentationKey, out string ignore3)); // retry should still fail, fatal error
        }
    }
}

