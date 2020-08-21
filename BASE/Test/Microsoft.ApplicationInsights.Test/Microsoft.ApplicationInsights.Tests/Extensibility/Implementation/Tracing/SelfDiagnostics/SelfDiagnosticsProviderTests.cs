using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    [TestClass]
    public class SelfDiagnosticsProviderTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            PlatformSingleton.Current = null; // Force reinitialization in future tests so that new environment variables will be loaded.
        }

        [TestMethod]
        public void VerifyEvaluateSelfDiagnosticsReturnsNullIfNoEnvironmentVariable()
        {
            var selfDiagnosticsMock = SelfDiagnosticsProvider.EvaluateSelfDiagnosticsConfig<SelfDiagnosticsFileWriterMock>();
            Assert.IsNull(selfDiagnosticsMock);
        }

        /// <remarks>
        /// This test is done twice to confirm that no default values are being set. 
        /// All tested values come from the environment variable.
        /// </remarks>
        [TestMethod]
        public void VerifyEvaluateSelfDiagnosticsConfigWorksAsExpected()
        {
            string testLevel1 = "Error";
            string testDirectory1 = "C:\\TEST1";
            this.SetEnvironmentVariable(testLevel1, testDirectory1);

            var selfDiagnosticsMock1 = SelfDiagnosticsProvider.EvaluateSelfDiagnosticsConfig<SelfDiagnosticsFileWriterMock>();

            Assert.AreEqual(testLevel1, selfDiagnosticsMock1.Level);
            Assert.AreEqual(testDirectory1, selfDiagnosticsMock1.FileDirectory);

            string testLevel2 = "Verbose";
            string testDirectory2 = "C:\\TEST2";
            this.SetEnvironmentVariable(testLevel2, testDirectory2);

            var selfDiagnosticsMock2 = SelfDiagnosticsProvider.EvaluateSelfDiagnosticsConfig<SelfDiagnosticsFileWriterMock>();

            Assert.AreEqual(testLevel2, selfDiagnosticsMock2.Level);
            Assert.AreEqual(testDirectory2, selfDiagnosticsMock2.FileDirectory);
        }

        [TestMethod]
        public void VerifyIsFileDiagnosticsEnabledWorksAsExpected()
        {
            string testLevel = "Error";
            string testDirectory = "C:\\TEST1";

            var testConfigString = BuildConfigurationString(testLevel, testDirectory);

            Assert.IsTrue(SelfDiagnosticsProvider.IsFileDiagnosticsEnabled(testConfigString, out string directory, out string level));
            Assert.AreEqual(testLevel, level);
            Assert.AreEqual(testDirectory, directory);
        }


        private void SetEnvironmentVariable(string level, string fileDirectory)
        {
            var platform = new StubEnvironmentVariablePlatform();
            platform.SetEnvironmentVariable(SelfDiagnosticsProvider.SelfDiagnosticsEnvironmentVariable, this.BuildConfigurationString(level, fileDirectory));
            PlatformSingleton.Current = platform;
        }

        /// <summary>
        /// Builds a string like "Destination=File;Level=Verbose;Directory=C:\\Temp;";
        /// </summary>
        private string BuildConfigurationString(string level, string fileDirectory) => $"{SelfDiagnosticsProvider.KeyDestination}={SelfDiagnosticsProvider.ValueFile};{SelfDiagnosticsProvider.KeyLevel}={level};{SelfDiagnosticsProvider.KeyDirectory}={fileDirectory}";
    }
}
