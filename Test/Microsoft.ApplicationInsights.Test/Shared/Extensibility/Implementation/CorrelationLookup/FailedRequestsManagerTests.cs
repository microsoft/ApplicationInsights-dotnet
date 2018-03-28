namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;

    [TestClass]
    public class FailedRequestsManagerTests
    {
        const int testTimeoutMilliseconds = 20000; // 20 seconds
        const int failedRequestRetryWaitTimeSeconds = 1;
        const string testInstrumentationKey = nameof(testInstrumentationKey);

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyRetryTimeout()
        {
            var stopWatch = new Stopwatch();
            var failedRequestsManager = new FailedRequestsManager(failedRequestRetryWaitTimeSeconds);
            
            stopWatch.Start();
            failedRequestsManager.RegisterFetchFailure(testInstrumentationKey, new Exception());

            while(!failedRequestsManager.CanRetry(testInstrumentationKey))
            {
                Thread.Sleep(100);
            }

            stopWatch.Stop();
            Assert.IsTrue(stopWatch.Elapsed >= TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds));
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyCanRetryHttp500ErrorAfterTimeout()
        {
            var failedRequestsManager = new FailedRequestsManager(failedRequestRetryWaitTimeSeconds);

            failedRequestsManager.RegisterFetchFailure(testInstrumentationKey, HttpStatusCode.InternalServerError);

            Assert.IsFalse(failedRequestsManager.CanRetry(testInstrumentationKey)); //TODO: SHOULD CHECK WITH A LOOP

            Thread.Sleep(TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds + 1));

            Assert.IsTrue(failedRequestsManager.CanRetry(testInstrumentationKey));
        }

        [TestMethod, Timeout(testTimeoutMilliseconds)]
        public void VerifyCanNotRetryHttp400Error()
        {
            var failedRequestsManager = new FailedRequestsManager(failedRequestRetryWaitTimeSeconds);

            failedRequestsManager.RegisterFetchFailure(testInstrumentationKey, HttpStatusCode.NotFound);

            Assert.IsFalse(failedRequestsManager.CanRetry(testInstrumentationKey));

            Thread.Sleep(TimeSpan.FromSeconds(failedRequestRetryWaitTimeSeconds + 1));

            Assert.IsFalse(failedRequestsManager.CanRetry(testInstrumentationKey));
        }
    }
}