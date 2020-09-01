namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Event argument to track response from ingestion endpoint.
    /// </summary>
    public class TransmissionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransmissionStatusEventArgs"/> class.
        /// </summary>
        /// <param name="response">Response from ingestion endpoint.</param>
        public TransmissionStatusEventArgs(HttpWebResponseWrapper response)
        {
            this.Response = response;
        }

        /// <summary>
        /// Gets the response from ingestion endpoint.
        /// </summary>
        public HttpWebResponseWrapper Response { get; }
    }
}
