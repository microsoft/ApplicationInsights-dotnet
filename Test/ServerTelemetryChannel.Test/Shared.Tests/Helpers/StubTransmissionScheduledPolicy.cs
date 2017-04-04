namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers
{
    using System;
    using System.Threading;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    internal class StubTransmissionScheduledPolicy : StubTransmissionPolicy
    {
        public ManualResetEventSlim ActionInvoked = new ManualResetEventSlim();

        public override void Initialize(Transmitter transmitter)
        {
            base.OnInitialize(transmitter);
            this.Transmitter.TransmissionSent += this.HandleTransmissionSentEvent;
        }

        public void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs e)
        {
            if (e.Response != null && e.Response.StatusCode == 500)
            {
                this.MaxSenderCapacity = 0;
                this.MaxBufferCapacity = 0;
                this.Apply();

                this.Transmitter.Enqueue(e.Transmission);

                this.Transmitter.BackoffLogicManager.ScheduleRestore(
                   DateTimeOffset.UtcNow.AddSeconds(2).ToString(),
                   () =>
                   {
                       this.MaxBufferCapacity = null;
                       this.MaxSenderCapacity = null;
                       this.Apply();

                       this.ActionInvoked.Set();

                       return null;
                   });
            }
        }
    }
}
