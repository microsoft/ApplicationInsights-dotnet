using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtils.TestConstants;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Helpers;
using AI;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace E2ETests.netcore20
{
    [TestClass]
    public class TestCore20OnNetCore20 : Test452Base
    {
        private static string VersionPrefix;
        private static string VersionPrefixSql;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DockerComposeFileName = "docker-composeNet20AppOn20.yml";
            VersionPrefix = "rdddsc";
            VersionPrefixSql = "rdddsc";
            AppNameBeingTested = TestConstants.WebAppCore20Name;
            if(!Apps.ContainsKey(AppNameBeingTested))
            {
                Apps.Add(AppNameBeingTested, new DeployedApp
                {
                    ikey = TestConstants.WebAppCore20NameInstrumentationKey,
                    containerName = TestConstants.WebAppCore20ContainerName,
                    imageName = TestConstants.WebAppCore20ImageName,
                    healthCheckPath = TestConstants.WebAppCore20HealthCheckPath,
                    flushPath = TestConstants.WebAppCore20FlushPath
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
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_HttpDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=http", TestConstants.WebAppUrlToWebApiSuccess, TestConstants.WebAppTargetNameToWebApi, "200", true);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_HttpPostDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=httppost", TestConstants.WebAppUrlToWebApiSuccess, TestConstants.WebAppTargetNameToWebApi, "204", true);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_500HttpDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=http500", TestConstants.WebAppUrlToWebApiException, TestConstants.WebAppTargetNameToWebApi, "500", false);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_ExceptionHttpDependency()
        {
            base.TestHttpDependency(VersionPrefix, AppNameBeingTested, "/external/calls?type=httpexception", TestConstants.WebAppUrlToInvalidHost, TestConstants.WebAppTargetNameToInvalidHost, null, false);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteReaderAsync()
        {            
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteReaderAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteScalarAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteScalarAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteNonQueryAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteNonQueryAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteXmlReaderAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlSuccessXML;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=ExecuteXmlReaderAsync&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlCommandExecuteScalarAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryCountToSqlSuccess;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=SqlCommandExecuteScalar&success=true", dataExpected);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlCommandExecuteScalarFailedAsync()
        {
            var dataExpected = TestConstants.WebAppFullQueryToSqlException;
            base.TestSqlDependency(VersionPrefixSql, AppNameBeingTested, "/external/calls?type=SqlCommandExecuteScalar&success=false", dataExpected, false);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlCommandExecuteReaderStoredProcedureAsync()
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
