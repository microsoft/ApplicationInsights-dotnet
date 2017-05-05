namespace Microsoft.ApplicationInsights.DependencyCollector
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
                var tcs = new TaskCompletionSource<string>();
                tcs.SetResult(ikey);
                return tcs.Task;
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
    }
}
