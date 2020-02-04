namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
#if !NETCOREAPP1_1
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Xml.Linq;
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

        [TestMethod]
        public void WhatHappensWithMultipleModules()
        {
            string testLogFilePath1 = "C:\\Temp\\111";
            string testLogFilePath2 = "C:\\Temp\\222";

            // SETUP
            var platform = new StubEnvironmentVariablePlatform();
            platform.SetEnvironmentVariable(TelemetryConfigurationFactory.DiagnosticsEnvironmentVariable, "true");
            platform.SetEnvironmentVariable(TelemetryConfigurationFactory.DiagnosticsLogDirectoryEnvironmentVariable, testLogFilePath2);
            PlatformSingleton.Current = platform;

            string configFileContents = Configuration(
                @"<TelemetryModules>
                    <Add Type = """ + typeof(FileDiagnosticsTelemetryModule).AssemblyQualifiedName + @"""  >
                        <LogFilePath>" + testLogFilePath1 + @"</LogFilePath>
                    </Add>
                    <Add Type = """ + typeof(FileDiagnosticsTelemetryModule).AssemblyQualifiedName + @"""  >
                        <LogFilePath>xxx</LogFilePath>
                    </Add>
                  </TelemetryModules>");

            using (var modules = new TestableTelemetryModules())
            {
                new TestableTelemetryConfigurationFactory().Initialize(new TelemetryConfiguration(), modules, configFileContents);

                var module = modules.Modules.OfType<FileDiagnosticsTelemetryModule>().Single();
                Assert.AreEqual(testLogFilePath2, module.LogFilePath, "We want the config from the environment variable to take precedence to enable DevOps to have control over troubleshooting scenarios");
            }
        }

        private static string Configuration(string innerXml)
        {
            return
              @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                <ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
" + innerXml + @"
                </ApplicationInsights>";
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

        private class TestableTelemetryConfigurationFactory : TelemetryConfigurationFactory
        {
            public static object CreateInstance(Type interfaceType, string typeName)
            {
                return TelemetryConfigurationFactory.CreateInstance(interfaceType, typeName);
            }

            public static new void LoadFromXml(TelemetryConfiguration configuration, TelemetryModules modules, XDocument xml)
            {
                TelemetryConfigurationFactory.LoadFromXml(configuration, modules, xml);
            }

            public static object LoadInstance(XElement definition, Type expectedType, object instance, TelemetryModules modules)
            {
                return TelemetryConfigurationFactory.LoadInstance(definition, expectedType, instance, null, modules);
            }

            [SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Justification = "This method allows calling protected base method in this test class.")]
            public static new void LoadInstances<T>(XElement definition, ICollection<T> instances, TelemetryModules modules)
            {
                TelemetryConfigurationFactory.LoadInstances(definition, instances, modules);
            }

            public static new void LoadProperties(XElement definition, object instance, TelemetryModules modules)
            {
                TelemetryConfigurationFactory.LoadProperties(definition, instance, modules);
            }
        }
    }
#endif
}
