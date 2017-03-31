namespace Microsoft.ApplicationInsights
{
    using System.Diagnostics;
    using System.IO;

    using Assert = Xunit.Assert;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AppInsightsStandaloneTests
    {
        private static string tempPath = Path.GetTempPath();

        [TestInitialize]
        public void Initialize()
        {
            File.Copy("Microsoft.ApplicationInsights.dll", $"{tempPath}\\Microsoft.ApplicationInsights.dll", true);
            File.Delete($"{tempPath}\\System.Diagnostics.DiagnosticSource.dll");
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete($"{tempPath}\\Microsoft.ApplicationInsights.dll");
            File.Delete($"{tempPath}\\System.Diagnostics.DiagnosticSource.dll");
        }

        [TestMethod]
        public void AppInsightsDllCouldRunStandalone()
        {
            var dependencyId = RunTestApplication(false, "guid");
            Assert.False(dependencyId.Contains("guid"));
        }

        [TestMethod]
        public void AppInsightsUsesdActivityWhenDiagnosticSourceIsAvailable()
        {
            var dependencyId = RunTestApplication(true, "guid");
            Assert.True(dependencyId.StartsWith("|guid."));
        }

        private static string RunTestApplication(bool withDiagnosticSource, string operationId)
        {
            if (withDiagnosticSource)
            {
                File.Copy("System.Diagnostics.DiagnosticSource.dll", $"{tempPath}\\System.Diagnostics.DiagnosticSource.dll");
            }

            var fileName = $"{tempPath}\\ActivityTest.exe";

            Assert.True(CreateTestApplication(fileName));

            Process p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = fileName,
                    Arguments = operationId
                }
            };

            p.Start();

            Assert.True(p.WaitForExit(1000));
            Assert.Equal(0, p.ExitCode);

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