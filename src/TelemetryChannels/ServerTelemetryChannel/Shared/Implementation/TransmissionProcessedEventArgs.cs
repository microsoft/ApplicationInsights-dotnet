namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    
    internal class TransmissionProcessedEventArgs : EventArgs
    {
        private readonly Transmission transmission;
        private readonly Exception exception;

        public TransmissionProcessedEventArgs(Transmission transmission, Exception exception = null)
        {
            this.transmission = transmission;
            this.exception = exception;
        }

        public Transmission Transmission
        {
            get { return this.transmission; }
        }

        public Exception Exception
        {
            get { return this.exception; }
        }
    }
}
