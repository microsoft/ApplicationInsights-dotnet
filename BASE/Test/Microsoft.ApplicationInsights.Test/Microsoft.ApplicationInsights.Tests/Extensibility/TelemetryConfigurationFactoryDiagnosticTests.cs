namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
#if !NETCOREAPP1_1
    using System;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics;
    using Microsoft.ApplicationInsights.TestFramework;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Diagnostics")]
    public class TelemetryConfigurationFactoryDiagnosticTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            PlatformSingleton.Current = null; // Force reinitialization in future tests so that new environment variables will be loaded.
        }

        [TestMethod]
        public void VerifyDiagnosticsIsEnabledViaEnvironmentVariable()
        {
            this.RunBasicTest(environmentVariableValue: "true", shouldInitializeModule: true);
            this.RunBasicTest(environmentVariableValue: "TRUE", shouldInitializeModule: true);
            this.RunBasicTest(environmentVariableValue: "True", shouldInitializeModule: true);
        }

        [TestMethod]
        public void VerifyDiagnosticsIsNotEnabled()
        {
            this.RunBasicTest(environmentVariableValue: "false", shouldInitializeModule: false);
            this.RunBasicTest(environmentVariableValue: "False", shouldInitializeModule: false);
            this.RunBasicTest(environmentVariableValue: "FALSE", shouldInitializeModule: false);
            this.RunBasicTest(environmentVariableValue: null, shouldInitializeModule: false);
        }

        [TestMethod]
        public void VerifyFileDiagnosticsFolderPathCanBeSetViaEnvironmentVariable()
        {
            string testLogFilePath = "C:\\Temp";

            // SETUP
            var platform = new StubEnvironmentVariablePlatform();
            platform.SetEnvironmentVariable(TelemetryConfigurationFactory.DiagnosticsEnvironmentVariable, "true");
            platform.SetEnvironmentVariable(TelemetryConfigurationFactory.DiagnosticsLogDirectoryEnvironmentVariable, testLogFilePath);
            PlatformSingleton.Current = platform;

            // ACT
            using (var modules = new TestableTelemetryModules())
            {
                TelemetryConfigurationFactory.Instance.EvaluateDiagnosticsMode(modules);

                var module = (FileDiagnosticsTelemetryModule)modules.Modules.Single();
                Assert.AreEqual(testLogFilePath, module.LogFilePath);
            }
        }

        [TestMethod]
        public void IfFileDiagnosticsModuleAlreadyExistVerifyWeDoNothing()
        {
            string testLogFilePath1 = "C:\\Temp\\111";
            string testLogFilePath2 = "C:\\Temp\\222";

            // SETUP
            var platform = new StubEnvironmentVariablePlatform();
            platform.SetEnvironmentVariable(TelemetryConfigurationFactory.DiagnosticsEnvironmentVariable, "true");
            platform.SetEnvironmentVariable(TelemetryConfigurationFactory.DiagnosticsLogDirectoryEnvironmentVariable, testLogFilePath2);
            PlatformSingleton.Current = platform;

            // ACT
            using (var modules = new TestableTelemetryModules())
            {
                modules.Modules.Add(new FileDiagnosticsTelemetryModule { LogFilePath = testLogFilePath1 });

                TelemetryConfigurationFactory.Instance.EvaluateDiagnosticsMode(modules);

                var module = (FileDiagnosticsTelemetryModule)modules.Modules.Single();
                Assert.AreEqual(testLogFilePath1, module.LogFilePath, "although path #2 was set in the environment variable, the previous module with path #1 was pre-set and should not be overwritten");
            }
        }

        private void RunBasicTest(string environmentVariableValue, bool shouldInitializeModule)
        {
            // SETUP
            var platform = new StubEnvironmentVariablePlatform();
            platform.SetEnvironmentVariable(TelemetryConfigurationFactory.DiagnosticsEnvironmentVariable, environmentVariableValue);
            PlatformSingleton.Current = platform;

            // ACT
            using (var modules = new TestableTelemetryModules())
            {
                Assert.AreEqual(0, modules.Modules.Count());

                TelemetryConfigurationFactory.Instance.EvaluateDiagnosticsMode(modules);

                Assert.AreEqual(shouldInitializeModule ? 1 : 0, modules.Modules.Count());
            }
        }

        private class TestableTelemetryModules : TelemetryModules, IDisposable
        {
            public void Dispose()
            {
                foreach (var module in this.Modules)
                {
                    (module as IDisposable)?.Dispose();
                }
            }
        }
    }
#endif
}
