namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Common;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal sealed class AzureSdkDiagnosticListenerSubscriber : DiagnosticSourceListenerBase<object>, IDisposable
    {
        public const string DiagnosticListenerName = "Azure.";
        private readonly IDisposable logsListener;

        public AzureSdkDiagnosticListenerSubscriber(TelemetryConfiguration configuration) : base(configuration)
        {
            // listen to Cosmos EventSource only - other logs can be sent using ILogger
            this.logsListener = new AzureSdkEventListener(this.Client, EventLevel.Warning, "Azure.Cosmos");
            this.Client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rdd" + RddSource.DiagnosticSourceListenerAzure + ":");
        }

        public override void Dispose()
        {
            this.logsListener?.Dispose();
            base.Dispose();
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