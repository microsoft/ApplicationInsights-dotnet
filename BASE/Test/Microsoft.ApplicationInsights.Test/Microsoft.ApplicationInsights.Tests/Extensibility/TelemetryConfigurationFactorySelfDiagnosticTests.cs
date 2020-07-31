namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
#if !NETCOREAPP1_1
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.TestFramework;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("SelfDiagnostics")]
    public class TelemetryConfigurationFactorySelfDiagnosticTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            PlatformSingleton.Current = null; // Force reinitialization in future tests so that new environment variables will be loaded.
        }


        [TestMethod]
        public void VerifyConfigCanStillSetFileDiagnosticsModule()
        {
            string testLogFilePath1 = "C:\\Temp\\111";

            // SETUP
            string configFileContents = Configuration(
                @"<TelemetryModules>
                    <Add Type = """ + typeof(FileDiagnosticsTelemetryModule).AssemblyQualifiedName + @"""  >
                        <LogFilePath>" + testLogFilePath1 + @"</LogFilePath>
                    </Add>
                  </TelemetryModules>");

            using (var modules = new TestableTelemetryModules())
            {
                new TestableTelemetryConfigurationFactory().Initialize(new TelemetryConfiguration(), modules, configFileContents);

                var module = modules.Modules.OfType<FileDiagnosticsTelemetryModule>().Single();
                Assert.AreEqual(testLogFilePath1, module.LogFilePath, "the environment variable should take precedence to enable DevOps to have control over troubleshooting scenarios");
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
