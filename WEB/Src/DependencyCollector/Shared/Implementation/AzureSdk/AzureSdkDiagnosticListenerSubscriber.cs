namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Extensibility;

    internal sealed class AzureSdkDiagnosticListenerSubscriber : DiagnosticSourceListenerBase<object>
    {
        public const string DiagnosticListenerName = "Azure.";

        public AzureSdkDiagnosticListenerSubscriber(TelemetryConfiguration configuration) : base(configuration)
        {
        }

        internal override bool IsSourceEnabled(DiagnosticListener diagnosticListener)
        {
            return diagnosticListener.Name.StartsWith(DiagnosticListenerName);
        }

        internal override bool IsActivityEnabled(string evnt, object context)
        {
            return true;
        }

        protected override IDiagnosticEventHandler GetEventHandler(string diagnosticListenerName)
        {
            return new AzureSdkDiagnosticsEventHandler(this.Configuration);
        }
    }
}