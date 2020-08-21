namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    internal interface ISelfDiagnostics
    {
        void Initialize(string level, string fileDirectory);
    }
}
