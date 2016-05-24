 namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class TransmissionPolicyHelpersTest
    {
        [TestClass]
        public class GetBackOffTime
        {
            [TestMethod]
            public void NoErrorDelayIsSameAsSlotDelay()
            {
                TimeSpan delay = TransmissionPolicyHelpers.GetBackOffTime(0, new WebHeaderCollection());
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void FirstErrorDelayIsSameAsSlotDelay()
            {
                TimeSpan delay = TransmissionPolicyHelpers.GetBackOffTime(1, new WebHeaderCollection());
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void UpperBoundOfDelayIsMaxDelay()
            {
                TimeSpan delay = TransmissionPolicyHelpers.GetBackOffTime(int.MaxValue, new WebHeaderCollection());
                Assert.InRange(delay, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3600));
            }

            [TestMethod]
            public void RetryAfterFromHeadersHasMorePriorityThanExponentialRetry()
            {
                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", DateTimeOffset.UtcNow.AddSeconds(30).ToString("O"));

                TimeSpan delay = TransmissionPolicyHelpers.GetBackOffTime(0, headers);

                Xunit.Assert.InRange(delay, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            }

            [TestMethod]
            public void AssertIfDateParseErrorCausesDefaultDelay()
            {
                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", "no one can parse me");

                TimeSpan delay = TransmissionPolicyHelpers.GetBackOffTime(0, headers);
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void RetryAfterOlderThanNowCausesDefaultDelay()
            {
                // An old date
                string retryAfterDateString = DateTime.Now.AddMinutes(-1).ToString("R", CultureInfo.InvariantCulture);

                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", retryAfterDateString);

                TimeSpan delay = TransmissionPolicyHelpers.GetBackOffTime(0, headers);
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }
        }
    }
}
