namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    internal interface ISelfDiagnosticsFileWriter
    {
        void Initialize(string level, string fileDirectory);
    }
}
