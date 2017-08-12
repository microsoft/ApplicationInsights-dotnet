namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Globalization;
    using System.Net;

    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    

#if !NET40
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

            Assert.IsTrue(policiesApplied);
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
            Assert.IsNull(policy.MaxSenderCapacity);
        }

        [TestMethod]
        public void MaxSenderCapacityCanBeSetByPolicy()
        {
            var policy = new StubTransmissionPolicy();
            policy.MaxSenderCapacity = 42;
            Assert.AreEqual(42, policy.MaxSenderCapacity);
        }

        [TestMethod]
        public void MaxBufferCapacityIsNullByDefaultToIndicateThatPolicyIsNotApplicable()
        {
            var policy = new StubTransmissionPolicy();
            Assert.IsNull(policy.MaxBufferCapacity);
        }

        [TestMethod]
        public void MaxBufferCapacityCanBeSetByPolicy()
        {
            var policy = new StubTransmissionPolicy();
            policy.MaxBufferCapacity = 42;
            Assert.AreEqual(42, policy.MaxBufferCapacity);
        }

        [TestMethod]
        public void MaxStorageCapacityIsNullByDefaultToIndicateThatPolicyIsNotApplicable()
        {
            var policy = new StubTransmissionPolicy();
            Assert.IsNull(policy.MaxStorageCapacity);
        }

        [TestMethod]
        public void MaxStorageCapacityCanBeSetByPolicy()
        {
            var policy = new StubTransmissionPolicy();
            policy.MaxStorageCapacity = 42;
            Assert.AreEqual(42, policy.MaxStorageCapacity);
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