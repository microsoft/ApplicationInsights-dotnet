namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Globalization;
    using System.Net;

    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class TransmissionPolicyTest
    {
        [TestMethod]
        public void ApplyInvokesApplyPoliciesAsyncOfTransmitter()
        {
            bool policiesApplied = false;
            var transmitter = new StubTransmitter
            {
                OnApplyPolicies = () => { policiesApplied = true; }
            };

            var policy = new StubTransmissionPolicy();
            policy.Initialize(transmitter);

            policy.Apply();

            Assert.True(policiesApplied);
        }

        [TestMethod]
        public void ApplyThrowsInvalidOperationExceptionWhenPolicyWasNotInitializedToPreventUsageErrors()
        {
            var policy = new StubTransmissionPolicy();
            Assert.Throws<InvalidOperationException>(() => policy.Apply());
        }

        [TestMethod]
        public void InitializeStoresTransmitterForUseByChildClasses()
        {
            var transmitter = new StubTransmitter();
            var policy = new TestableTransmissionPolicy();

            policy.Initialize(transmitter);

            Assert.Same(transmitter, policy.Transmitter);
        }

        [TestMethod]
        public void InitializeThrowsArgumentNullExceptionWhenTransmitterIsNullToPreventUsageErrors()
        {
            var policy = new StubTransmissionPolicy();
            Assert.Throws<ArgumentNullException>(() => policy.Initialize(null));
        }

        [TestMethod]
        public void MaxSenderCapacityIsNullByDefaultToIndicateThatPolicyIsNotApplicable()
        {
            var policy = new StubTransmissionPolicy();
            Assert.Null(policy.MaxSenderCapacity);
        }

        [TestMethod]
        public void MaxSenderCapacityCanBeSetByPolicy()
        {
            var policy = new StubTransmissionPolicy();
            policy.MaxSenderCapacity = 42;
            Assert.Equal(42, policy.MaxSenderCapacity);
        }

        [TestMethod]
        public void MaxBufferCapacityIsNullByDefaultToIndicateThatPolicyIsNotApplicable()
        {
            var policy = new StubTransmissionPolicy();
            Assert.Null(policy.MaxBufferCapacity);
        }

        [TestMethod]
        public void MaxBufferCapacityCanBeSetByPolicy()
        {
            var policy = new StubTransmissionPolicy();
            policy.MaxBufferCapacity = 42;
            Assert.Equal(42, policy.MaxBufferCapacity);
        }

        [TestMethod]
        public void MaxStorageCapacityIsNullByDefaultToIndicateThatPolicyIsNotApplicable()
        {
            var policy = new StubTransmissionPolicy();
            Assert.Null(policy.MaxStorageCapacity);
        }

        [TestMethod]
        public void MaxStorageCapacityCanBeSetByPolicy()
        {
            var policy = new StubTransmissionPolicy();
            policy.MaxStorageCapacity = 42;
            Assert.Equal(42, policy.MaxStorageCapacity);
        }

        [TestClass]
        public class GetBackOffTime : ErrorHandlingTransmissionPolicyTest
        {
            [TestMethod]
            public void NoErrorDelayIsSameAsSlotDelay()
            {
                var policy = new TestableTransmissionPolicy();
                TimeSpan delay = policy.GetBackOffTime(new WebHeaderCollection());
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void FirstErrorDelayIsSameAsSlotDelay()
            {
                var policy = new TestableTransmissionPolicy();
                policy.ConsecutiveErrors = 1;
                TimeSpan delay = policy.GetBackOffTime(new WebHeaderCollection());
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void UpperBoundOfDelayIsMaxDelay()
            {
                var policy = new TestableTransmissionPolicy();
                policy.ConsecutiveErrors = int.MaxValue;
                TimeSpan delay = policy.GetBackOffTime(new WebHeaderCollection());
                Assert.InRange(delay, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3600));
            }

            [TestMethod]
            public void RetryAfterFromHeadersHasMorePriorityThanExponentialRetry()
            {
                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", DateTimeOffset.UtcNow.AddSeconds(30).ToString("O"));

                var policy = new TestableTransmissionPolicy();
                policy.ConsecutiveErrors = 0;
                TimeSpan delay = policy.GetBackOffTime(headers);

                Assert.InRange(delay, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            }

            [TestMethod]
            public void AssertIfDateParseErrorCausesDefaultDelay()
            {
                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", "no one can parse me");

                var policy = new TestableTransmissionPolicy();
                policy.ConsecutiveErrors = 0;
                TimeSpan delay = policy.GetBackOffTime(headers);
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }

            [TestMethod]
            public void RetryAfterOlderThanNowCausesDefaultDelay()
            {
                // An old date
                string retryAfterDateString = DateTime.Now.AddMinutes(-1).ToString("R", CultureInfo.InvariantCulture);

                WebHeaderCollection headers = new WebHeaderCollection();
                headers.Add("Retry-After", retryAfterDateString);

                var policy = new TestableTransmissionPolicy();
                policy.ConsecutiveErrors = 0;
                TimeSpan delay = policy.GetBackOffTime(headers);
                Assert.Equal(TimeSpan.FromSeconds(10), delay);
            }
        }

        private class TestableTransmissionPolicy : TransmissionPolicy
        {
            public new Transmitter Transmitter
            {
                get { return base.Transmitter; }
            }
        }
    }
}