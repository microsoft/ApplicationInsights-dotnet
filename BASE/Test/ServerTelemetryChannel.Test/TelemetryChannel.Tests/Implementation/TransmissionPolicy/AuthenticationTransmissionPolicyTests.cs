namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation.TransmissionPolicy
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("TransmissionPolicy")]
    public class AuthenticationTransmissionPolicyTests
    {
        [TestMethod]
        public void Verify400TriggersThrottling() => this.EvaluateIfStatusCodeTriggersThrottling(ResponseStatusCodes.BadRequest);

        [TestMethod]
        public void Verify401TriggersThrottling() => this.EvaluateIfStatusCodeTriggersThrottling(ResponseStatusCodes.Unauthorized);

        [TestMethod]
        public void Verify403TriggersThrottling() => this.EvaluateIfStatusCodeTriggersThrottling(ResponseStatusCodes.Forbidden);

        [TestMethod]
        public void Verify200DoesNotTriggerThrottling() => this.EvaluateIfStatusCodeIgnored(ResponseStatusCodes.Success);

        [TestMethod]
        public void VerifyOtherDoesNotTriggerThrottling() => this.EvaluateIfStatusCodeIgnored(000);

        private void EvaluateIfStatusCodeTriggersThrottling(int statusCode)
        {
            var retryAfterSeconds = BackoffLogicManager.SlotDelayInSeconds;
            var waitForTheFirstApplyAsync = TimeSpan.FromMilliseconds(100);
            var waitForTheSecondApplyAsync = TimeSpan.FromMilliseconds(retryAfterSeconds * 1000 + 500);

            var policyApplied = new AutoResetEvent(false);
            var transmitter = new StubTransmitter
            {
                OnApplyPolicies = () => policyApplied.Set(),
            };

            var policy = new AuthenticationTransmissionPolicy()
            {
                Enabled = true,
            };
            policy.Initialize(transmitter);

            transmitter.OnTransmissionSent(
                new TransmissionProcessedEventArgs(
                    transmission: new StubTransmission(),
                    exception: null,
                    response: new HttpWebResponseWrapper()
                    {
                        StatusCode = statusCode,
                        StatusDescription = null,
                    }));

            // Assert: First Handle will trigger Throttle and delay.
            Assert.IsTrue(policyApplied.WaitOne(waitForTheFirstApplyAsync));

            Assert.AreEqual(0, policy.MaxSenderCapacity);
            Assert.IsNull(policy.MaxBufferCapacity);
            Assert.IsNull(policy.MaxStorageCapacity);

            // Assert: Throttle expires and policy will be reset.
            Assert.IsTrue(policyApplied.WaitOne(waitForTheSecondApplyAsync));

            Assert.IsNull(policy.MaxSenderCapacity);
            Assert.IsNull(policy.MaxBufferCapacity);
            Assert.IsNull(policy.MaxStorageCapacity);
        }

        private void EvaluateIfStatusCodeIgnored(int statusCode)
        {
            var waitForTheFirstApplyAsync = TimeSpan.FromMilliseconds(100);

            var policyApplied = new AutoResetEvent(false);
            var transmitter = new StubTransmitter
            {
                OnApplyPolicies = () => policyApplied.Set(),
            };

            var policy = new AuthenticationTransmissionPolicy()
            {
                Enabled = true,
            };
            policy.Initialize(transmitter);

            transmitter.OnTransmissionSent(
                new TransmissionProcessedEventArgs(
                    transmission: new StubTransmission(),
                    exception: null,
                    response: new HttpWebResponseWrapper()
                    {
                        StatusCode = statusCode,
                        StatusDescription = null,
                    }));

            // Assert: The Apply event handler should not be called.
            Assert.IsFalse(policyApplied.WaitOne(waitForTheFirstApplyAsync));

            // Assert: Capacities should have default values.
            Assert.IsNull(policy.MaxSenderCapacity);
            Assert.IsNull(policy.MaxBufferCapacity);
            Assert.IsNull(policy.MaxStorageCapacity);
        }
    }
}
