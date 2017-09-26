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
        public void Test452OnNet462_TestBasicHttpDependencyWebApp()
        {
            base.TestBasicHttpDependencyWebApp();
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
