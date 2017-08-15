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
            var dependencyId = RunTestApplication(false, "guid");
            Assert.IsFalse(dependencyId.Contains("guid"));
        }

        [TestMethod]
        public void AppInsightsUsesActivityWhenDiagnosticSourceIsAvailable()
        {
            var dependencyId = RunTestApplication(true, "guid");
            Assert.IsTrue(dependencyId.StartsWith("|guid."));
        }

        private string RunTestApplication(bool withDiagnosticSource, string operationId)
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
                    Arguments = operationId
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
                class ActivityTest
                {
                    static void Main(string[] args)
                    {
                        var tc = new TelemetryClient();
                        using (var requestOperation = tc.StartOperation<RequestTelemetry>(""request"", args[0]))
                        {
                            using (var dependencyOperation = tc.StartOperation<DependencyTelemetry>(""dependency""))
                            {
                                Console.Write(dependencyOperation.Telemetry.Id);
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