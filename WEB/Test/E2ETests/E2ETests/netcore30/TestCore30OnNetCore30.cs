using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtils.TestConstants;

namespace E2ETests.netcore30
{
    [TestClass]
    public class TestCore30OnNetCore30 : Test452Base
    {
        private static string VersionPrefix;
        private static string VersionPrefixSql;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DockerComposeFileName = "docker-composeNet30AppOn30.yml";
            VersionPrefix = "rdddsc";
            VersionPrefixSql = "rdddsc";
            AppNameBeingTested = TestConstants.WebAppCore30Name;
            if(!Apps.ContainsKey(AppNameBeingTested))
            {
                Apps.Add(AppNameBeingTested, new DeployedApp
                {
                    ikey = TestConstants.WebAppCore30NameInstrumentationKey,
                    containerName = TestConstants.WebAppCore30ContainerName,
                    imageName = TestConstants.WebAppCore30ImageName,
                    healthCheckPath = TestConstants.WebAppCore30HealthCheckPath,
                    flushPath = TestConstants.WebAppCore30FlushPath
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
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_HttpDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=http", TestConstants.WebAppUrlToWebApiSuccess, TestConstants.WebAppTargetNameToWebApi, "200", true);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_HttpPostDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=httppost", TestConstants.WebAppUrlToWebApiSuccess, TestConstants.WebAppTargetNameToWebApi, "204", true);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_500HttpDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=http500", TestConstants.WebAppUrlToWebApiException, TestConstants.WebAppTargetNameToWebApi, "500", false);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_ExceptionHttpDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=httpexception", TestConstants.WebAppUrlToInvalidHost, TestConstants.WebAppTargetNameToInvalidHost, null, false);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_SqlExecuteReaderAsync()
        {            
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteReaderAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_SqlExecuteScalarAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteScalarAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_SqlExecuteNonQueryAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteNonQueryAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore20OnNetCore30_SqlExecuteXmlReaderAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccessXML;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteXmlReaderAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_SqlCommandExecuteScalarAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryCountToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=SqlCommandExecuteScalar&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_SqlCommandExecuteScalarFailedAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlException;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=SqlCommandExecuteScalar&success=false", dataExpected, false);
        }

        [TestMethod]
        [TestCategory("Core30")]
        public void TestCore30OnNetCore30_SqlCommandExecuteReaderStoredProcedureAsync()
        {
            var dataExpected = TestConstants.WebAppStoredProcedureNameToSql;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteReaderStoredProcedureAsync&storedProcedureName=GetTopTenMessages&success=true", dataExpected);
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            MyClassCleanupBase();
        }
    }
}
