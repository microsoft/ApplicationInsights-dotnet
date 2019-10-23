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

namespace E2ETests.Net462SMSM
{
    [TestClass]
    public class Test452OnNet462SMSMSM : Test452Base
    {
        private static string VersionPrefix;
        private static string VersionPrefixSql;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DockerComposeFileName = "docker-compose452AppOn462StatusMonitor.yml";
            AppNameBeingTested = TestConstants.WebAppName;
            VersionPrefix = "rddp";
            VersionPrefixSql = "rddp";
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

            // Forcefully remove the image to ensure SM gets installed properly.
            DockerUtils.RemoveDockerImage(Apps[AppNameBeingTested].imageName, true);
            DockerUtils.RemoveDockerContainer(Apps[AppNameBeingTested].containerName, true);

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
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestXComponentWebAppToWebApi()
        {
            base.TestXComponentWebAppToWebApi();
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestBasicRequestWebApp()
        {
            base.TestBasicRequestWebApp();
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestSyncHttpDependency()
        {
            base.TestSyncHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncWithHttpClientHttpDependency()
        {
            base.TestAsyncWithHttpClientHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestPostCallHttpDependency()
        {
            base.TestPostCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestFailedHttpDependency()
        {
            base.TestFailedHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestFailedAtDnsHttpDependency()
        {
            base.TestFailedAtDnsHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncHttpDependency1()
        {
            base.TestAsyncHttpDependency1(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncFailedHttpDependency1()
        {
            base.TestAsyncFailedHttpDependency1(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncHttpDependency2()
        {
            base.TestAsyncHttpDependency2(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncFailedHttpDependency2()
        {
            base.TestAsyncFailedHttpDependency2(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncHttpDependency3()
        {
            base.TestAsyncHttpDependency3(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncFailedHttpDependency3()
        {
            base.TestAsyncFailedHttpDependency3(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncHttpDependency4()
        {
            base.TestAsyncHttpDependency4(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncFailedHttpDependency4()
        {
            base.TestAsyncFailedHttpDependency4(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAsyncAwaitCallHttpDependency()
        {
            base.TestAsyncAwaitCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestFailedAsyncAwaitCallHttpDependency()
        {
            base.TestFailedAsyncAwaitCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAzureTableDependencyWebApp()
        {
            base.TestAzureTableDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAzureQueueDependencyWebApp()
        {
            base.TestAzureQueueDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_TestAzureBlobDependencyWebApp()
        {
            base.TestAzureBlobDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteReaderSuccess()
        {
            base.TestSqlDependencyExecuteReaderSuccessAsync(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteReaderFailed()
        {
            base.TestSqlDependencyExecuteReaderFailedAsync(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteReader0Success()
        {
            base.TestSqlDependencyBeginExecuteReader0Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteReader0Failed()
        {
            base.TestSqlDependencyBeginExecuteReader0Failed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteReader1Success()
        {
            base.TestSqlDependencyBeginExecuteReader1Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteReader1Failed()
        {
            base.TestSqlDependencyBeginExecuteReader1Failed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteReader2Success()
        {
            base.TestSqlDependencyBeginExecuteReader2Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteReader3Success()
        {
            base.TestSqlDependencyBeginExecuteReader3Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteReader3Failed()
        {
            base.TestSqlDependencyBeginExecuteReader3Failed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteReader0Success()
        {
            base.TestSqlDependencySqlCommandExecuteReader0Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteReader0Failed()
        {
            base.TestSqlDependencySqlCommandExecuteReader0Failed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteReader1Success()
        {
            base.TestSqlDependencySqlCommandExecuteReader1Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteReader1Failed()
        {
            base.TestSqlDependencySqlCommandExecuteReader1Failed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteScalarAsyncSuccess()
        {
            base.TestSqlDependencyExecuteScalarAsyncSuccess(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteScalarAsyncFailed()
        {
            base.TestSqlDependencyExecuteScalarAsyncFailed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteScalarSuccess()
        {
            base.TestSqlDependencySqlCommandExecuteScalarSuccess(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteScalarFailed()
        {
            base.TestSqlDependencySqlCommandExecuteScalarFailed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteNonQuerySuccess()
        {
            base.TestSqlDependencyExecuteNonQuerySuccess(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteNonQueryFailed()
        {
            base.TestSqlDependencyExecuteNonQueryFailed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteNonQueryAsyncSuccess()
        {
            base.TestSqlDependencyExecuteNonQueryAsyncSuccess(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteNonQueryAsyncFailed()
        {
            base.TestSqlDependencyExecuteNonQueryAsyncFailed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteNonQuery0Success()
        {
            base.TestSqlDependencyBeginExecuteNonQuery0Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteNonQuery0Failed()
        {
            base.TestSqlDependencyBeginExecuteNonQuery0Failed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteNonQuery2Success()
        {
            base.TestSqlDependencyBeginExecuteNonQuery2Success(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteNonQuery2Failed()
        {
            base.TestSqlDependencyBeginExecuteNonQuery2Failed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteXmlReaderAsyncSuccess()
        {
            base.TestSqlDependencyExecuteXmlReaderAsyncSuccess(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyExecuteXmlReaderAsyncFailed()
        {
            base.TestSqlDependencyExecuteXmlReaderAsyncFailed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteXmlReaderSuccess()
        {
            base.TestSqlDependencyBeginExecuteXmlReaderSuccess(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyBeginExecuteXmlReaderFailed()
        {
            base.TestSqlDependencyBeginExecuteXmlReaderFailed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteXmlReaderSuccess()
        {
            base.TestSqlDependencySqlCommandExecuteXmlReaderSuccess(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencySqlCommandExecuteXmlReaderFailed()
        {
            base.TestSqlDependencySqlCommandExecuteXmlReaderFailed(VersionPrefixSql);
        }

        [TestMethod]
        [TestCategory("Net452OnNet462SM")]
        public void Test452OnNet462SM_SqlDependencyStoredProcedure()
        {
            base.TestSqlDependencyStoredProcedureName(VersionPrefixSql);
        }

        

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            MyClassCleanupBase();
        }
    }
}