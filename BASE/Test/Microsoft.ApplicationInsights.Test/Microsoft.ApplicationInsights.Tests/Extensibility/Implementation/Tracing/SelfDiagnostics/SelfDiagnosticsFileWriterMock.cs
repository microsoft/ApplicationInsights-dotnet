using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics;

namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    public class SelfDiagnosticsFileWriterMock : ISelfDiagnosticsFileWriter
    {
        public string Level { get; set; }
        public string FileDirectory { get; set; }

        public void Initialize(string level, string fileDirectory)
        {
            this.Level = level;
            this.FileDirectory = fileDirectory;
        }
    }
}
