namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Platform;

    /// <summary>
    /// An <see cref="ITelemetryProcessor"/> that act as a proxy to the Transmission of telemetry"/>.
    /// The <see cref="ITelemetryChannel"/>, passed at construction time, will be used for transmission.
    /// This processor is always appended as the last processor in the chain.
    /// </summary>
    internal class TransmissionProcessor : ITelemetryProcessor
    {        
        private readonly ITelemetryChannel channel;
        private readonly IDebugOutput debugOutput;     

        /// <summary>
        /// Initializes a new instance of the <see cref="TransmissionProcessor"/> class.
        /// </summary>        
        /// <param name="channel">The <see cref="ITelemetryChannel"/> to use for sending telemetry.</param>
        internal TransmissionProcessor(ITelemetryChannel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            this.channel = channel;
            this.debugOutput = PlatformSingleton.Current.GetDebugOutput();
        }

        /// <summary>
        /// Process the given <see cref="ITelemetry"/> item. Here processing is sending the item through the channel/>.
        /// </summary>
        public void Process(ITelemetry item)
        {
            TelemetryDebugWriter.WriteTelemetry(item);

            this.channel.Send(item);
        }
    }
}
