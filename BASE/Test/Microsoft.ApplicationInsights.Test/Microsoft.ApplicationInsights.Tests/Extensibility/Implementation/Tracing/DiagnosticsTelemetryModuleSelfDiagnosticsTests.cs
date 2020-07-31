namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsModule;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class DiagnosticsTelemetryModuleSelfDiagnosticsTests
    {
        private readonly string ExpectedDefaultFileLogDirectory = Environment.ExpandEnvironmentVariables("%TEMP%");

        [TestCleanup]
        public void TestCleanup()
        {
            PlatformSingleton.Current = null; // Force reinitialization in future tests so that new environment variables will be loaded.
        }

        [TestMethod]
        public void VerifyDefaultConfiguration()
        {
            var diagnosticsTelemetryModule = new DiagnosticsTelemetryModule();

            // ASSERT
            Assert.AreEqual(this.ExpectedDefaultFileLogDirectory, diagnosticsTelemetryModule.FileLogDirectory);
            Assert.AreEqual(false, diagnosticsTelemetryModule.IsFileLogEnabled);
        }

        [TestMethod]
        public void VerifyCanSetFileDiagnosticsViaModule()
        {
            string testLogDirectory = "C:\\Temp";

            var diagnosticsTelemetryModule = new DiagnosticsTelemetryModule
            {
                FileLogDirectory = testLogDirectory,
                IsFileLogEnabled = true
            };

            // ASSERT
            Assert.AreEqual(true, diagnosticsTelemetryModule.IsFileLogEnabled);
            Assert.AreEqual(testLogDirectory, diagnosticsTelemetryModule.FileLogDirectory);
        }

        [TestMethod]
        public void VerifyCanSetFileDiagnosticsViaEnvironmentVariable()
        {
            string testLogDirectory = "C:\\Temp";

            // SETUP
            this.SetEnvironmentVariable(testLogDirectory);

            // ACT
            var diagnosticsTelemetryModule = new DiagnosticsTelemetryModule();

            // ASSERT
            Assert.AreEqual(true, diagnosticsTelemetryModule.IsFileLogEnabled);
            Assert.AreEqual(testLogDirectory, diagnosticsTelemetryModule.FileLogDirectory);
        }

        [TestMethod]
        public void VerifyCanSetFileDiagnosticsViaConfigXml()
        {
            string testLogDirectory = "C:\\Temp";

            string configFileContents = Configuration(testLogDirectory, isEnabled: true);

            using (var modules = new TestableTelemetryModules())
            {
                new TestableTelemetryConfigurationFactory().Initialize(new TelemetryConfiguration(), modules, configFileContents);

                var diagnosticsTelemetryModule = modules.Modules.OfType<DiagnosticsTelemetryModule>().Single();
                Assert.AreEqual(true, diagnosticsTelemetryModule.IsFileLogEnabled);
                Assert.AreEqual(testLogDirectory, diagnosticsTelemetryModule.FileLogDirectory);
            }
        }

        [TestMethod]
        public void VerifyConfigIsOverridenByEnvironmentVariable()
        {
            string testLogDirectory1 = "C:\\Temp\\111";
            string testLogDirectory2 = "C:\\Temp\\222";

            // SETUP
            this.SetEnvironmentVariable(testLogDirectory2);

            string configFileContents = Configuration(testLogDirectory1, isEnabled: false);

            using (var modules = new TestableTelemetryModules())
            {
                new TestableTelemetryConfigurationFactory().Initialize(new TelemetryConfiguration(), modules, configFileContents);

                var diagnosticsTelemetryModule = modules.Modules.OfType<DiagnosticsTelemetryModule>().Single();
                Assert.AreEqual(true, diagnosticsTelemetryModule.IsFileLogEnabled, "the environment variable should take precedence to enable DevOps to have control over troubleshooting scenarios");
                Assert.AreEqual(testLogDirectory2, diagnosticsTelemetryModule.FileLogDirectory, "the environment variable should take precedence to enable DevOps to have control over troubleshooting scenarios");
            }
        }

        /// <summary>
        /// Writes a string like "Destination=File;Directory=C:\\Temp;";
        /// </summary>
        /// <param name="logDirectory"></param>
        private void SetEnvironmentVariable(string logDirectory)
        {
            var platform = new StubEnvironmentVariablePlatform();
            platform.SetEnvironmentVariable(DiagnosticsTelemetryModule.SelfDiagnosticsEnvironmentVariable, $"{SelfDiagnosticsProvider.KeyDestination}={SelfDiagnosticsProvider.ValueDestinationFile};{SelfDiagnosticsProvider.KeyFilePath}={logDirectory}");
            PlatformSingleton.Current = platform;
        }

        private static string Configuration(string logDirectory, bool isEnabled)
        {
            return
              @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                <ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
                    <TelemetryModules>
                        <Add Type=""" + typeof(DiagnosticsTelemetryModule).AssemblyQualifiedName + @""">
                         " + $"<{nameof(DiagnosticsTelemetryModule.FileLogDirectory)}>{logDirectory}</{nameof(DiagnosticsTelemetryModule.FileLogDirectory)}>" + @"
                        " + $"<{nameof(DiagnosticsTelemetryModule.IsFileLogEnabled)}>{isEnabled}</{nameof(DiagnosticsTelemetryModule.IsFileLogEnabled)}>" + @"
                        </Add>
                    </TelemetryModules>
                </ApplicationInsights>";
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
}
