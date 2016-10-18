namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Net;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class TransmissionProcessedEventArgs : EventArgs
    {
        public TransmissionProcessedEventArgs(Transmission transmission, Exception exception = null, HttpWebResponseWrapper response = null)
        {
            // Fix missing arguments if not passed in
            if (exception != null && response == null && exception is WebException &&
                ((WebException)exception).Response is HttpWebResponse)
            {
                var excResponse = (HttpWebResponse)((WebException)exception).Response;
                response = new HttpWebResponseWrapper()
                {
                    StatusCode = (int)excResponse.StatusCode,
                    StatusDescription = excResponse.StatusDescription,
                    RetryAfterHeader = excResponse.Headers?.Get("Retry-After")
                };
            }

            this.Transmission = transmission;
            this.Exception = exception;
            this.Response = response;
        }

        public Transmission Transmission { get; }

        public Exception Exception { get; }

        public HttpWebResponseWrapper Response { get; protected set; }
    }
}