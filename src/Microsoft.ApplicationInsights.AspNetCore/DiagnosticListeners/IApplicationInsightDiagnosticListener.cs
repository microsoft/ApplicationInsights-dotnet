namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    internal interface IApplicationInsightDiagnosticListener
    {
        string ListenerName { get; }
    }
}