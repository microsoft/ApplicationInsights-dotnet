namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    internal interface IApplicationInsightDiagnosticListener
    {
        string ListenerName { get; }
    }
}