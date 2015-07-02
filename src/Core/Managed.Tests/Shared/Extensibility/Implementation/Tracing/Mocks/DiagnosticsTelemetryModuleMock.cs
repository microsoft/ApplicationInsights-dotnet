namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks
{
    using System.Collections.Generic;

    internal class DiagnosticsTelemetryModuleMock : DiagnosticsTelemetryModule
    {
        public IList<IDiagnosticsSender> ModuleSenders
        {
            get { return this.Senders; }
        }

        public DiagnosticsListener ModuleListener
        {
            get { return this.eventListener; }
        }
    }
}
