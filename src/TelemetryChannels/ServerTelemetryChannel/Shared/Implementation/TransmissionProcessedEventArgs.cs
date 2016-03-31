namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    
    internal class TransmissionProcessedEventArgs : EventArgs
    {
        public TransmissionProcessedEventArgs(Transmission transmission, Exception exception = null, string responseContent = null)
        {
            this.Transmission = transmission;
            this.Exception = exception;
            this.ResponseContent = responseContent;
        }

        public Transmission Transmission { get; }

        public Exception Exception { get; }

        public string ResponseContent { get; }
    }
}
