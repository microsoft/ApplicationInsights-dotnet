using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace E2ETests.Net462
{
    [TestClass]
    public class Test452OnNet462 : Test452Base
    {
        private static string VersionPrefix;
        private static string VersionPrefixSql;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DockerComposeFileName = "docker-compose452AppOn462.yml";
            VersionPrefix = "rdddsd";
            VersionPrefixSql = "rddf";
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
        public void Test452OnNet462_XComponentWebAppToWebApi()
        {
            base.TestXComponentWebAppToWebApi();
        }

        [TestMethod]
        public void Test452OnNet462_BasicRequestWebApp()
        {            
            base.TestBasicRequestWebApp();
        }

        [TestMethod]
        public void Test452OnNet462_SyncHttpDependency()
        {
            base.TestSyncHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncWithHttpClientHttpDependency()
        {
            base.TestAsyncWithHttpClientHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_PostCallHttpDependency()
        {
            base.TestPostCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_FailedHttpDependency()
        {
            base.TestFailedHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_FailedAtDnsHttpDependency()
        {
            base.TestFailedAtDnsHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncHttpDependency1()
        {
            base.TestAsyncHttpDependency1(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncFailedHttpDependency1()
        {
            base.TestAsyncFailedHttpDependency1(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncHttpDependency2()
        {
            base.TestAsyncHttpDependency2(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncFailedHttpDependency2()
        {
            base.TestAsyncFailedHttpDependency2(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncHttpDependency3()
        {
            base.TestAsyncHttpDependency3(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncFailedHttpDependency3()
        {
            base.TestAsyncFailedHttpDependency3(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncHttpDependency4()
        {
            base.TestAsyncHttpDependency4(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncFailedHttpDependency4()
        {
            base.TestAsyncFailedHttpDependency4(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AsyncAwaitCallHttpDependency()
        {
            base.TestAsyncAwaitCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_FailedAsyncAwaitCallHttpDependency()
        {
            base.TestFailedAsyncAwaitCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AzureTableDependencyWebApp()
        {
            base.TestAzureTableDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AzureQueueDependencyWebApp()
        {
            base.TestAzureQueueDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_AzureBlobDependencyWebApp()
        {
            base.TestAzureBlobDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteReaderSuccess()
        {
            base.TestSqlDependencyExecuteReaderSuccessAsync(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteReaderFailed()
        {
            base.TestSqlDependencyExecuteReaderFailedAsync(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteReader0Success()
        {
            base.TestSqlDependencyBeginExecuteReader0Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteReader0Failed()
        {
            base.TestSqlDependencyBeginExecuteReader0Failed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteReader1Success()
        {
            base.TestSqlDependencyBeginExecuteReader1Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteReader1Failed()
        {
            base.TestSqlDependencyBeginExecuteReader1Failed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteReader2Success()
        {
            base.TestSqlDependencyBeginExecuteReader2Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteReader3Success()
        {
            base.TestSqlDependencyBeginExecuteReader3Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteReader3Failed()
        {
            base.TestSqlDependencyBeginExecuteReader3Failed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteReader0Success()
        {
            base.TestSqlDependencySqlCommandExecuteReader0Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteReader0Failed()
        {
            base.TestSqlDependencySqlCommandExecuteReader0Failed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteReader1Success()
        {
            base.TestSqlDependencySqlCommandExecuteReader1Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteReader1Failed()
        {
            base.TestSqlDependencySqlCommandExecuteReader1Failed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteScalarAsyncSuccess()
        {
            base.TestSqlDependencyExecuteScalarAsyncSuccess(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteScalarAsyncFailed()
        {
            base.TestSqlDependencyExecuteScalarAsyncFailed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteScalarSuccess()
        {
            base.TestSqlDependencySqlCommandExecuteScalarSuccess(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteScalarFailed()
        {
            base.TestSqlDependencySqlCommandExecuteScalarFailed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteNonQuerySuccess()
        {
            base.TestSqlDependencyExecuteNonQuerySuccess(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteNonQueryFailed()
        {
            base.TestSqlDependencyExecuteNonQueryFailed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteNonQueryAsyncSuccess()
        {
            base.TestSqlDependencyExecuteNonQueryAsyncSuccess(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteNonQueryAsyncFailed()
        {
            base.TestSqlDependencyExecuteNonQueryAsyncFailed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteNonQuery0Success()
        {
            base.TestSqlDependencyBeginExecuteNonQuery0Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteNonQuery0Failed()
        {
            base.TestSqlDependencyBeginExecuteNonQuery0Failed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteNonQuery2Success()
        {
            base.TestSqlDependencyBeginExecuteNonQuery2Success(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteNonQuery2Failed()
        {
            base.TestSqlDependencyBeginExecuteNonQuery2Failed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteXmlReaderAsyncSuccess()
        {
            base.TestSqlDependencyExecuteXmlReaderAsyncSuccess(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyExecuteXmlReaderAsyncFailed()
        {
            base.TestSqlDependencyExecuteXmlReaderAsyncFailed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteXmlReaderSuccess()
        {
            base.TestSqlDependencyBeginExecuteXmlReaderSuccess(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencyBeginExecuteXmlReaderFailed()
        {
            base.TestSqlDependencyBeginExecuteXmlReaderFailed(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteXmlReaderSuccess()
        {
            base.TestSqlDependencySqlCommandExecuteXmlReaderSuccess(VersionPrefixSql);
        }

        [TestMethod]
        public void Test452OnNet462_SqlDependencySqlCommandExecuteXmlReaderFailed()
        {
            base.TestSqlDependencySqlCommandExecuteXmlReaderFailed(VersionPrefixSql);
        }       

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            MyClassCleanupBase();
        }
    }
}
