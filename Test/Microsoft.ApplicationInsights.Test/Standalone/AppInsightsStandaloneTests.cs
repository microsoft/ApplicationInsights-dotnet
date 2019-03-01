namespace Microsoft.ApplicationInsights
{
    using System.Diagnostics;
    using System.IO;
    
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class AppInsightsStandaloneTests
    {
        private string tempPath;

        [TestInitialize]
        public void Initialize()
        {
            this.tempPath = Path.Combine(Path.GetTempPath(), "ApplicationInsightsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(this.tempPath);
            File.Copy("Microsoft.ApplicationInsights.dll", $"{tempPath}\\Microsoft.ApplicationInsights.dll", true);
            File.Delete($"{tempPath}\\System.Diagnostics.DiagnosticSource.dll");
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(this.tempPath, true);
        }

        [TestMethod]
        public void AppInsightsDllCouldRunStandalone()
        {
            var traceId = Guid.NewGuid().ToString("n");
            var spanId = Guid.NewGuid().ToString("n").Substring(0, 16);
            var dependencyId = RunTestApplication(false, traceId, spanId);
            Assert.IsFalse(dependencyId.Contains(traceId));
        }

        [TestMethod]
        public void AppInsightsUsesActivityWhenDiagnosticSourceIsAvailable()
        {
            var traceId = Guid.NewGuid().ToString("n");
            var spanId = Guid.NewGuid().ToString("n").Substring(0, 16);

            var dependencyId = RunTestApplication(true, traceId, spanId);
            Assert.IsTrue(dependencyId.StartsWith($"|{traceId}."));
        }

        private string RunTestApplication(bool withDiagnosticSource, string traceId, string spanId)
        {
            if (withDiagnosticSource)
            {
                File.Copy("System.Diagnostics.DiagnosticSource.dll", $"{this.tempPath}\\System.Diagnostics.DiagnosticSource.dll");
            }

            var fileName = $"{this.tempPath}\\ActivityTest.exe";

            Assert.IsTrue(CreateTestApplication(fileName));

            Process p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = fileName,
                    Arguments = $"00-{traceId}-{spanId}-01"
                }
            };
            
            p.Start();

            Assert.IsTrue(p.WaitForExit(10000));
            Assert.AreEqual(0, p.ExitCode);

            return p.StandardOutput.ReadToEnd();
        }

        private static bool CreateTestApplication(string fileName)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@"
                using System;
                using Microsoft.ApplicationInsights;
                using Microsoft.ApplicationInsights.DataContracts;
                using Microsoft.ApplicationInsights.Extensibility;

                class ActivityTest
                {
                    static void Main(string[] args)
                    {
                        var config = new TelemetryConfiguration();
                        config.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
                        var tc = new TelemetryClient(config);
                        using (var requestOperation = tc.StartOperation<RequestTelemetry>(""request"", args[0]))
                        {
                            using (var dependencyOperation = tc.StartOperation<DependencyTelemetry>(""dependency"", args[0]))
                            {
                                Console.Write(dependencyOperation.Telemetry.Id);
                                tc.TrackTrace(""Hello World!"");
                            }
                        }
                    }
                }");

            MetadataReference[] references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(TelemetryClient).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                "ActivityTest",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            return compilation.Emit(fileName).Success;
        }
    }
}