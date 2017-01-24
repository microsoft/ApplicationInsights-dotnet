namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Net.Http;
    using Microsoft.Extensions.DiagnosticAdapter;

    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for HTTP requests from this AspNetCore application.
    /// </summary>
    internal class DependencyCollectorDiagnosticListener : IApplicationInsightDiagnosticListener
    {
        /// <inheritdoc/>
        public string ListenerName { get; } = "HttpHandlerDiagnosticListener"; // This value comes from: https://github.com/dotnet/corefx/blob/bffef76f6af208e2042a2f27bc081ee908bb390b/src/Common/src/System/Net/Http/HttpHandlerLoggingStrings.cs#L12

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Request' event.
        /// </summary>
        [DiagnosticName("System.Net.Http.Request")]
        public void OnBeginRequest(HttpRequestMessage request, Guid loggingRequestId, long timestamp)
        {
        }

        /// <summary>
        /// Diagnostic event handler method for 'System.Net.Http.Response' event.
        /// </summary>
        [DiagnosticName("System.Net.Http.Response")]
        public void OnEndRequest(HttpResponseMessage response, Guid loggingRequestId, long timestamp)
        {
        }
    }
}
