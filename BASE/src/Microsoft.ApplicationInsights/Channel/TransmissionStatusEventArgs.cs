namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Event argument to track response from ingestion endpoint.
    /// </summary>
    public class TransmissionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransmissionStatusEventArgs"/> class.
        /// </summary>
        /// <param name="response">Response from ingestion endpoint.</param>
        public TransmissionStatusEventArgs(HttpResponseMessage response = null)
        {
            this.Response = response;
        }

        /// <summary>
        /// Gets the response from ingestion endpoint.
        /// </summary>
        public HttpResponseMessage Response { get; }
    }
}
