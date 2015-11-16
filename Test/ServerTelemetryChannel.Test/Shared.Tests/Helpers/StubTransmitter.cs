namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers
{
    using System;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class StubTransmitter : Transmitter
    {
        public Action OnApplyPolicies = () => { };
        public Action<Transmission> OnEnqueue = transmission => { };

        public StubTransmitter()
            : base(new StubTransmissionSender(), new StubTransmissionBuffer(), new StubTransmissionStorage(), Enumerable.Empty<TransmissionPolicy>())
        {
        }

        internal override void Enqueue(Transmission transmission)
        {
            this.OnEnqueue(transmission);
        }
        
        internal new void OnTransmissionSent(TransmissionProcessedEventArgs e)
        {
            base.OnTransmissionSent(e);
        }

        internal override void ApplyPolicies()
        {
            this.OnApplyPolicies();
        }
    }
}
