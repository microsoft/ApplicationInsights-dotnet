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
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DockerComposeFileName = "docker-compose452AppOn462.yml";
            VersionPrefix = "rdddsd";
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
        public void Test452OnNet462_TestXComponentWebAppToWebApi()
        {
            base.TestXComponentWebAppToWebApi();
        }

        [TestMethod]
        public void Test452OnNet462_TestBasicRequestWebApp()
        {            
            base.TestBasicRequestWebApp();
        }

        [TestMethod]
        public void Test452OnNet462_TestSyncHttpDependency()
        {
            base.TestSyncHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncWithHttpClientHttpDependency()
        {
            base.TestAsyncWithHttpClientHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestPostCallHttpDependency()
        {
            base.TestPostCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestFailedHttpDependency()
        {
            base.TestFailedHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestFailedAtDnsHttpDependency()
        {
            base.TestFailedAtDnsHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncHttpDependency1()
        {
            base.TestAsyncHttpDependency1(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncFailedHttpDependency1()
        {
            base.TestAsyncFailedHttpDependency1(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncHttpDependency2()
        {
            base.TestAsyncHttpDependency2(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncFailedHttpDependency2()
        {
            base.TestAsyncFailedHttpDependency2(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncHttpDependency3()
        {
            base.TestAsyncHttpDependency3(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncFailedHttpDependency3()
        {
            base.TestAsyncFailedHttpDependency3(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncHttpDependency4()
        {
            base.TestAsyncHttpDependency4(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncFailedHttpDependency4()
        {
            base.TestAsyncFailedHttpDependency4(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAsyncAwaitCallHttpDependency()
        {
            base.TestAsyncAwaitCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestFailedAsyncAwaitCallHttpDependency()
        {
            base.TestFailedAsyncAwaitCallHttpDependency(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestAzureTableDependencyWebApp()
        {
            base.TestAzureTableDependencyWebApp(VersionPrefix);
        }

        [TestMethod]
        public void Test452OnNet462_TestBasicSqlDependencyWebApp()
        {
            base.TestBasicSqlDependencyWebApp();
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            MyClassCleanupBase();
        }
    }
}
