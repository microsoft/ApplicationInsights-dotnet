namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    using Channel.Helpers;

#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    public class ApplicationLifecycleTransmissionPolicyTest
    {
        [TestClass]
        public class HandleApplicationStoppingEvent : ApplicationLifecycleTransmissionPolicyTest
        {
            [TestMethod]
            public void StopsTransmissionSendingAndBuffering()
            {
                var transmitter = new StubTransmitter();
                
                var applicationLifecycle = new StubApplicationLifecycle();
                var policy = new ApplicationLifecycleTransmissionPolicy(applicationLifecycle);
                policy.Initialize(transmitter);

                applicationLifecycle.OnStopping(ApplicationStoppingEventArgs.Empty);

                Assert.Equal(0, policy.MaxSenderCapacity);
                Assert.Equal(0, policy.MaxBufferCapacity);
            }

            [TestMethod]
            public void EventHandlerIsNotAssignedInConstructorToPreventRaceConditionWithInitialize()
            {
                var applicationLifecycle = new StubApplicationLifecycle();
                var policy = new ApplicationLifecycleTransmissionPolicy(applicationLifecycle);

                bool asyncMethodInvoked = false;
                Func<Func<Task>, Task> asyncMethodRunner = asyncMethod =>
                {
                    asyncMethodInvoked = true;
                    return asyncMethod();
                };
                applicationLifecycle.OnStopping(new ApplicationStoppingEventArgs(asyncMethodRunner));

                Assert.False(asyncMethodInvoked);
            }
        }

        private class TestableApplicationLifecycleTransmissionPolicy : ApplicationLifecycleTransmissionPolicy
        {
            public TestableApplicationLifecycleTransmissionPolicy(IApplicationLifecycle applicationLifecycle) 
                : base(applicationLifecycle)
            {
            }

            public new int? MaxSenderCapacity
            {
                get { return base.MaxSenderCapacity; }
                set { base.MaxSenderCapacity = value; }
            }
        }
    }
}
