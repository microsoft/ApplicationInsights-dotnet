using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using TestUtils.TestConstants;

namespace E2ETests.net471
{
    [TestClass]
    public class Test452OnNet471 : Test452Base
    {
        private const string VersionPrefix = "rdddsd";
        private const string VersionPrefixSql = "rddf";

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DockerComposeFileName = "docker-compose452AppOn471.yml";
            AppNameBeingTested = TestConstants.WebAppName;

            if (!Apps.ContainsKey(AppNameBeingTested))
            {
                Apps.Add(AppNameBeingTested, new DeployedApp
                {
                    ikey = TestConstants.WebAppInstrumentationKey,
                    containerName = TestConstants.WebAppContainerName,
                    imageName = TestConstants.WebAppImageName,
                    healthCheckPath = TestConstants.WebAppHealthCheckPath,
                    flushPath = TestConstants.WebAppFlushPath
                });
            }
            MyClassInitializeBase();
        }

        [TestInitialize]
        public new void MyTestInitialize()
        {
            base.MyTestInitialize();
        }

        [TestCleanup]
        public new void MyTestCleanup()
        {
            base.MyTestCleanup();
        }

        [TestMethod]
        [TestCategory("Net452OnNet471")]
        public async Task Test452OnNet471_HttpDependencyCorrelationInPostRequest()
        {
            await base.TestHttpDependencyCorrelationInPostRequest();
        }

        [TestMethod]
        [TestCategory("Net452OnNet471")]
        public void Test452OnNet471_TestXComponentWebAppToWebApi()
        {
            base.TestXComponentWebAppToWebApi();
        }

        [TestMethod]
        [TestCategory("Net452OnNet471")]
        public void Test452OnNet471_TestBasicRequestWebApp()
        {
            base.TestBasicRequestWebApp();
        }

        [TestMethod]
        [TestCategory("Net452OnNet471")]
        public void Test452OnNet47_TestSyncHttpDependencyWebApp()
        {
            base.TestSyncHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet471")]
        public void Test452OnNet471_AzureTableDependencyWebApp()
        {
            base.TestAzureTableDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet471")]
        public void Test452OnNet471_SqlDependencyExecuteReaderSuccess()
        {
            base.TestSqlDependencyExecuteReaderSuccessAsync(VersionPrefixSql);
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            MyClassCleanupBase();
        }
    }
}
