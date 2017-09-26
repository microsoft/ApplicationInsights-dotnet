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
        [Ignore]
        public async Task TestBasicFlow()
        {
            var startTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            Thread.Sleep(5000);

            HttpClient client = new HttpClient();
            string url = "http://" + testappip + "/Default";
            Trace.WriteLine(url);
            var response =await client.GetAsync(url);           
            Trace.WriteLine(response.StatusCode);
            response = await client.GetAsync(url);
            Trace.WriteLine(response.StatusCode);            

            var requestsWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(WebAppInstrumentationKey);
            var dependenciesWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>(WebAppInstrumentationKey);
            var requestsWebApi = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(WebApiInstrumentationKey);

            Trace.WriteLine("RequestCount for WebApp:"+ requestsWebApp.Count);
            Assert.IsTrue(requestsWebApp.Count >= 2);

            Trace.WriteLine("DependenciesCount for WebApp:" + dependenciesWebApp.Count);
            Assert.IsTrue(dependenciesWebApp.Count >= 2);

            Trace.WriteLine("RequestCount for WebApi:" + requestsWebApi.Count);
            Assert.IsTrue(requestsWebApi.Count >= 1);
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
