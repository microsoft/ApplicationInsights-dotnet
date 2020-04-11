namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class FlushAsyncTransmissionPolicyTest
    {
        [TestClass]
        public class HandleTransmissionSentEvent : FlushAsyncTransmissionPolicyTest
        {
            private const int ResponseCodeSuccess = 200;

            [TestMethod]
            public void SuccessfullyMovesItemsFromBufferToStorageWithFlushAsyncTask()
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () => { policyApplied.Set(); };

                var policy = new FlushAsyncTransmissionPolicy();
                policy.Initialize(transmitter);

                var transmission = new StubTransmission();
                var task = transmission.GetFlushTask(default);
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission,
                    null,
                    new HttpWebResponseWrapper() { StatusCode = ResponseCodeSuccess }));

                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.AreEqual(0, policy.MaxSenderCapacity);
                Assert.AreEqual(0, policy.MaxBufferCapacity);
                // Pauses other transmission for 3 seconds to ensure items moved to storage are not picked by sender immediately.
                Assert.IsTrue(policyApplied.WaitOne(3100));
                Assert.IsNull(policy.MaxSenderCapacity);
                Assert.IsNull(policy.MaxBufferCapacity);
                Assert.IsTrue(task.Result);
            }

            [TestMethod]
            public void SuccessfullyMovesCurrentTransmissionAndBufferToStorageWithFlushAsyncTask()
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () => { policyApplied.Set(); };

                var policy = new FlushAsyncTransmissionPolicy();
                policy.Initialize(transmitter);

                var transmission = new StubTransmission();
                var task = transmission.GetFlushTask(default);
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission,
                    null,
                    new HttpWebResponseWrapper() { StatusCode = ResponseCodeSuccess, StatusDescription = "SendToDisk" }));

                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.IsTrue(policyApplied.WaitOne(3100));
                Assert.IsTrue(task.Result);
            }

            [TestMethod]
            public void FailsToMoveItemsToStorageWithFlushAsyncTask()
            {
                var policyApplied = new AutoResetEvent(false);
                var transmitter = new StubTransmitter();
                transmitter.OnApplyPolicies = () => { policyApplied.Set(); };
                // IsEnqueueSuccess is set to false on storage issue.
                transmitter.Storage.IsEnqueueSuccess = false;

                var policy = new FlushAsyncTransmissionPolicy();
                policy.Initialize(transmitter);

                var transmission = new StubTransmission();
                var task = transmission.GetFlushTask(default);
                transmitter.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission,
                    null,
                    new HttpWebResponseWrapper() { StatusCode = ResponseCodeSuccess }));

                Assert.IsTrue(policyApplied.WaitOne(100));
                Assert.IsTrue(policyApplied.WaitOne(3100));
                Assert.IsFalse(task.Result);
            }
        }
    }
}