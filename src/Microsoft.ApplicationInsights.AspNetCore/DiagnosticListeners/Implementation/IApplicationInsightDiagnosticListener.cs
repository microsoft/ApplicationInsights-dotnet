namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;

    /// <summary>
    /// Base diagnostic listener type for Application Insight
    /// </summary>
    internal interface IApplicationInsightDiagnosticListener : IDisposable
    {
        /// <summary>
        /// Gets a value indicating which listener this instance should be subscribed to
        /// </summary>
        string ListenerName { get; }

        /// <summary>
        /// Notifies listener that it is subscribed to DiagnosticSource
        /// </summary>
        void OnSubscribe();
    }
}