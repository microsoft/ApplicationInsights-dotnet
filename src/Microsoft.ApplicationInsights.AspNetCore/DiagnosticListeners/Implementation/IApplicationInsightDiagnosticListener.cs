namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    /// <summary>
    /// Base diagnostic listener type for Application Insight
    /// </summary>
    internal interface IApplicationInsightDiagnosticListener
    {
        /// <summary>
        /// Gets a value indicating which listener this instance should be subscribed to
        /// </summary>
        string ListenerName { get; }
    }
}