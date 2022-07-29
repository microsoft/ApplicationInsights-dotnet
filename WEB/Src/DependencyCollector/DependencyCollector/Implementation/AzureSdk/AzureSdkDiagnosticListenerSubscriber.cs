namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal sealed class AzureSdkDiagnosticListenerSubscriber : DiagnosticSourceListenerBase<object>
    {
        public const string DiagnosticListenerName = "Azure.";

        public AzureSdkDiagnosticListenerSubscriber(TelemetryConfiguration configuration) : base(configuration)
        {
            this.Client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rdd" + RddSource.DiagnosticSourceListenerAzure + ":");
        }

        internal override bool IsSourceEnabled(DiagnosticListener diagnosticListener)
        {
            return diagnosticListener.Name.StartsWith(DiagnosticListenerName, StringComparison.Ordinal);
        }

        internal override bool IsActivityEnabled(string evnt, object context)
        {
            return true;
        }

        protected override IDiagnosticEventHandler GetEventHandler(string diagnosticListenerName)
        {
            return new AzureSdkDiagnosticsEventHandler(this.Client);
        }
    }
}