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
            AppNameBeingTested = "WebAppCore20";
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
        public void TestCore20OnNetCore20_SyncHttpDependency()
        {
            base.TestSyncHttpDependency(VersionPrefix, Test452Base.WebAppCore20Name, "/external/calls?type=http");
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SyncHttpPostDependency()
        {
            base.TestSyncHttpDependency(VersionPrefix, Test452Base.WebAppCore20Name, "/external/calls?type=httppost");
        }


        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteReaderAsync()
        {
            base.TestSqlDependency(VersionPrefixSql, Test452Base.WebAppCore20Name, "/external/calls?type=ExecuteReaderAsync&success=true");
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteScalarAsync()
        {
            base.TestSqlDependency(VersionPrefixSql, Test452Base.WebAppCore20Name, "/external/calls?type=ExecuteScalarAsync&success=true");
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteNonQueryAsync()
        {
            base.TestSqlDependency(VersionPrefixSql, Test452Base.WebAppCore20Name, "/external/calls?type=ExecuteNonQueryAsync&success=true");
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlExecuteXmlReaderAsync()
        {
            base.TestSqlDependency(VersionPrefixSql, Test452Base.WebAppCore20Name, "/external/calls?type=ExecuteXmlReaderAsync&success=true");
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlCommandExecuteScalarAsync()
        {
            base.TestSqlDependency(VersionPrefixSql, Test452Base.WebAppCore20Name, "/external/calls?type=SqlCommandExecuteScalar&success=true");
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlCommandExecuteScalarFailedAsync()
        {
            base.TestSqlDependency(VersionPrefixSql, Test452Base.WebAppCore20Name, "/external/calls?type=SqlCommandExecuteScalar&success=false", false);
        }

        [TestMethod]
        [TestCategory("Core20")]
        public void TestCore20OnNetCore20_SqlCommandExecuteReaderStoredProcedureAsync()
        {
            base.TestSqlDependency(VersionPrefixSql, Test452Base.WebAppCore20Name, "/external/calls?type=ExecuteReaderStoredProcedureAsync&storedProcedureName=GetTopTenMessages&success=true", true);
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            MyClassCleanupBase();
        }
    }
}
