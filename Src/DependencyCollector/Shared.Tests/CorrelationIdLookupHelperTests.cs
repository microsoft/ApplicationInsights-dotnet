namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Correlation Id Lookup helper tests.
    /// </summary>
    [TestClass]
    public sealed class CorrelationIdLookupHelperTests
    {
        /// <summary>
        /// Makes sure that the first call to get app id returns false, because it hasn't been fetched yet.
        /// But the second call is able to get it from the dictionary.
        /// </summary>
        [TestMethod]
        public void CorrelationIdLookupHelperReturnsAppIdOnSecondCall()
        {
            var correlationIdLookupHelper = new CorrelationIdLookupHelper((ikey) =>
            {
                // Pretend App Id is the same as Ikey
                return Task.FromResult(ikey);
            });

            string instrumenationKey = Guid.NewGuid().ToString();
            string cid;

            // First call returns false;
            Assert.IsFalse(correlationIdLookupHelper.TryGetXComponentCorrelationId(instrumenationKey, out cid));

            // Let's wait for the task to complete. It should be really quick (based on the test setup) but not immediate.
            while (correlationIdLookupHelper.IsFetchAppInProgress(instrumenationKey))
            {
                Thread.Sleep(10); // wait 10 ms.
            }

            // Once fetch is complete, subsequent calls should return correlation id.
            Assert.IsTrue(correlationIdLookupHelper.TryGetXComponentCorrelationId(instrumenationKey, out cid));
        }

        /// <summary>
        /// Test that if an malicious value is returned, that value will be truncated.
        /// </summary>
        [TestMethod]
        public void CorrelationIdLookupHelperTruncatesMaliciousValue()
        {
            // 50 character string.
            var value = "a123456789b123546789c123456789d123456798e123456789";

            // An arbitrary string that is expected to be truncated.
            var malicious = "00000000000000000000000000000000000000000000000000000000000";

            var cidPrefix = "cid-v1:";

            var correlationIdLookupHelper = new CorrelationIdLookupHelper((ikey) =>
            {
                return Task.FromResult(value + malicious);
            });

            string instrumenationKey = Guid.NewGuid().ToString();

            // first request fails because this will create the fetch task.
            Assert.IsFalse(correlationIdLookupHelper.TryGetXComponentCorrelationId(instrumenationKey, out string ignore));

            // Let's wait for the task to complete. It should be really quick (based on the test setup) but not immediate.
            while (correlationIdLookupHelper.IsFetchAppInProgress(instrumenationKey))
            {
                Thread.Sleep(10); // wait 10 ms.
            }

            // Once fetch is complete, subsequent calls should return correlation id.
            Assert.IsTrue(correlationIdLookupHelper.TryGetXComponentCorrelationId(instrumenationKey, out string cid));
            Assert.AreEqual(cidPrefix + value, cid);
        }
    }
}
