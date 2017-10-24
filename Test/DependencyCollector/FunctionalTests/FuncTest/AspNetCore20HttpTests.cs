namespace FuncTest
{
    using System;    
    using System.Linq;
    using AI;
    using FuncTest.Helpers;
    using FuncTest.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Tests Dependency Collector Functionality for a WebApplication written in Dotnet Core.
    /// </summary>
    [TestClass]
    public class AspNetCore20HttpTests
    {
        internal const string AspxCoreAppFolder = ".\\AspxCore20";
        internal const string AspxCoreTestAppFolder = "..\\TestApps\\AspxCore20\\";
        internal static DotNetCoreTestWebApplication AspxCoreTestWebApplication { get; private set; }

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            AspxCoreTestWebApplication = new DotNetCoreTestWebApplication
            {
                AppName = "AspxCore20",
                ExternalCallPath = "external/calls",
                Port = DeploymentAndValidationTools.AspxCore20Port,
                PublishFolder = "netcoreapp2.0"
            };

            AspxCoreTestWebApplication.Deploy();
            DeploymentAndValidationTools.Initialize();

            Trace.TraceInformation("Aspnet core HttpTests class initialized");
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {            
            DeploymentAndValidationTools.CleanUp();
            Trace.TraceInformation("Aspnet core HttpTests class cleaned up");
            AspxCoreTestWebApplication.Remove();

        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            DeploymentAndValidationTools.SdkEventListener.Start();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {            
            DeploymentAndValidationTools.SdkEventListener.Stop();
        }               

        private static void EnsureDotNetCoreInstalled()
        {
            string output = "";
            string error = "";

            if (!DotNetCoreProcess.HasDotNetExe())
            {
                Assert.Inconclusive(".Net Core is not installed");
            }
            else
            {
                DotNetCoreProcess process = new DotNetCoreProcess("--version")
                    .RedirectStandardOutputTo((string outputMessage) => output += outputMessage)
                    .RedirectStandardErrorTo((string errorMessage) => error += errorMessage)
                    .Run();
                
                if (process.ExitCode.Value != 0 || !string.IsNullOrEmpty(error))
                {
                    Assert.Inconclusive(".Net Core is not installed");
                }
                else
                {
                    Trace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "Dotnet.exe process output: {0}", output));

                    // We now know dotnet core is installed. Attemping to validate version is flaky as --version sometimes dont redirect all 
                    // messages. Catch the exception and move on for now.
                    try
                    {
                        // Look for first dash to get semantic version. (for example: 1.0.0-preview2-003156)
                        int dashIndex = output.IndexOf('-');
                        Version version = new Version(dashIndex == -1 ? output : output.Substring(0, dashIndex));

                        Version minVersion = new Version("1.0.0");
                        if (version < minVersion)
                        {
                            Assert.Inconclusive($".Net Core version ({output}) must be greater than or equal to {minVersion}.");
                        }
                    } catch(Exception ex)
                    {
                        Trace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "DotNetCore version check failed with exception : {0}. Test will still continue.", ex.Message));
                    }
                }
            }
        }

        private static IDisposable DotNetCoreTestSetup()
        {
            EnsureDotNetCoreInstalled();

            return new ExpectedSDKPrefixChanger("rdddsc", "rdddsc");
        }
        

        #region Tests

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSyncHttpAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSyncHttpTests(AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, "200", HttpTestConstants.QueryStringOutboundHttp);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSyncHttpPostCallAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSyncHttpPostTests(AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, "200", HttpTestConstants.QueryStringOutboundHttpPost);
            }
        }

        [TestMethod]
        [Ignore] // Don't run this test until .NET Core writes diagnostic events for failed HTTP requests
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSyncHttpFailedAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which fails.            
                HttpTestHelper.ExecuteSyncHttpTests(AspxCoreTestWebApplication, false, 1, HttpTestConstants.AccessTimeMaxHttpInitial, "200", HttpTestConstants.QueryStringOutboundHttpFailed);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSqlExecuteReaderAsyncAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundExecuteReaderAsync);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSqlExecuteScalarAsyncAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundExecuteScalarAsync);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForExecuteReaderStoredProcedureAsyncAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundExecuteReaderStoredProcedureAsync);
            }
        }

        [TestMethod]
        [Ignore] // Can't run this until race condition in SqlCommandHelper.AsyncExecuteReaderInTasks is fixed.
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForTestExecuteReaderTwiceWithTasksAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundTestExecuteReaderTwiceWithTasks);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForTestExecuteNonQueryAsyncAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundExecuteNonQueryAsync);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForTestExecuteXmlReaderAsyncAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundExecuteXmlReaderAsync);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForTestSqlCommandExecuteScalarAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundSqlCommandExecuteScalar);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore20)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForTestSqlCommandExecuteScalarErrorAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSqlTest(
                    AspxCoreTestWebApplication, false, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundSqlCommandExecuteScalarError);
            }
        }

        #endregion        

        private class ExpectedSDKPrefixChanger : IDisposable
        {
            private readonly string previousExpectedSqlSDKPrefix;
            private readonly string previousExpectedHttpSDKPrefix;

            public ExpectedSDKPrefixChanger(string expectedHttpSDKPrefix, string expectedSqlSDKPrefix)
            {
                previousExpectedSqlSDKPrefix = DeploymentAndValidationTools.ExpectedSqlSDKPrefix;
                DeploymentAndValidationTools.ExpectedSqlSDKPrefix = expectedSqlSDKPrefix;

                previousExpectedHttpSDKPrefix = DeploymentAndValidationTools.ExpectedHttpSDKPrefix;
                DeploymentAndValidationTools.ExpectedHttpSDKPrefix = expectedHttpSDKPrefix;
            }

            public void Dispose()
            {
                DeploymentAndValidationTools.ExpectedSqlSDKPrefix = previousExpectedSqlSDKPrefix;
                DeploymentAndValidationTools.ExpectedHttpSDKPrefix = previousExpectedHttpSDKPrefix;
            }
        }
    }
}