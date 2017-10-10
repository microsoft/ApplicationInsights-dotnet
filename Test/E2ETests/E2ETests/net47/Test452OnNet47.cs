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

namespace E2ETests.Net47
{
    [TestClass]
    public class Test452OnNet47 : Test452Base
    {
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DockerComposeFileName = "docker-compose452AppOn47.yml";

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
        [Ignore]
        public void Test452OnNet47_TestXComponentWebAppToWebApi()
        {
            base.TestXComponentWebAppToWebApi();
        }

        [TestMethod]
        [Ignore]
        public void Test452OnNet47_TestBasicRequestWebApp()
        {
            base.TestBasicRequestWebApp();
        }

        [TestMethod]
        [Ignore]
        public void Test452OnNet47_TestBasicHttpDependencyWebApp()
        {
            base.TestBasicHttpDependencyWebApp();
        }

        [TestMethod]
        [Ignore]
        public void Test452OnNet47_TestAzureTableDependencyWebApp()
        {
            base.TestAzureTableDependencyWebApp();
        }

        [TestMethod]
        [Ignore]
        public void Test452OnNet47_TestBasicSqlDependencyWebApp()
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
