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
    public class Test452OnNet462
    {
        private const string WebAppInstrumentationKey = "e45209bb-49ab-41a0-8065-793acb3acc56";
        private const string WebApiInstrumentationKey = "0786419e-d901-4373-902a-136921b63fb2";
        internal static string testappip;
        internal static string ingestionServiceIp;
        internal static string dockerComposeFileName = "docker-compose462.yml";
        internal static string dockerComposeBaseCommandFormat = "/c docker-compose";
        internal static string dockerComposeFileNameFormat = string.Format("-f {0}", dockerComposeFileName);
        internal static DataEndpointClient dataendpointClient;
        internal static ProcessStartInfo DockerPSProcessInfo = new ProcessStartInfo("cmd", "/c docker ps -a");

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Trace.WriteLine("Starting ClassInitialize:" + DateTime.UtcNow.ToLongTimeString());

            Assert.IsTrue(File.Exists(".\\" + dockerComposeFileName));
            string dockerComposeActionCommand = "up -d --build";
            string dockerComposeFullCommandFormat = string.Format("{0} {1} {2}", dockerComposeBaseCommandFormat, dockerComposeFileNameFormat, dockerComposeActionCommand);
            Trace.WriteLine("Docker compose done using command: " + dockerComposeFullCommandFormat);
            ProcessStartInfo DockerComposeUp = new ProcessStartInfo("cmd", dockerComposeFullCommandFormat);
            ProcessStartInfo DockerInspectIp = new ProcessStartInfo("cmd", "/c docker inspect -f \"{{.NetworkSettings.Networks.nat.IPAddress}}\" e2etests_e2etestwebapp_1");
            ProcessStartInfo DockerInspectIpIngestion = new ProcessStartInfo("cmd", "/c docker inspect -f \"{{.NetworkSettings.Networks.nat.IPAddress}}\" e2etests_ingestionservice_1");
            

            Trace.WriteLine("DockerComposeUp started:" + DateTime.UtcNow.ToLongTimeString());

            Process process = new Process { StartInfo = DockerComposeUp };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Docker Compose Console output:" + output);
            process.WaitForExit();

            Trace.WriteLine("DockerComposeUp completed:" + DateTime.UtcNow.ToLongTimeString());

            Trace.WriteLine("DockerInspect started:" + DateTime.UtcNow.ToLongTimeString());
            process = new Process { StartInfo = DockerInspectIp };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            testappip = output.Trim();
            Trace.WriteLine("DockerInspect WebApp IP" + output);
            process.WaitForExit();
            Trace.WriteLine("DockerInspect completed:" + DateTime.UtcNow.ToLongTimeString());

            process = new Process { StartInfo = DockerInspectIpIngestion };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            ingestionServiceIp = output.Trim();
            Trace.WriteLine("DockerInspect IngestionService IP" + output);
            Trace.WriteLine(output);
            process.WaitForExit();

            PrintDockerProcessStats("ClassInitialize completed");

            dataendpointClient = new DataEndpointClient(new Uri("http://" + ingestionServiceIp));
        }

        private static void PrintDockerProcessStats(string message)
        {
            Trace.WriteLine(message);
            Process process = new Process { StartInfo = DockerPSProcessInfo };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Docker ps -a" + output);
            process.WaitForExit();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {
            Thread.Sleep(5000);
            Trace.WriteLine("Deleting items started:" + DateTime.UtcNow.ToLongTimeString());
            dataendpointClient.DeleteItems(WebAppInstrumentationKey);
            dataendpointClient.DeleteItems(WebApiInstrumentationKey);
            Trace.WriteLine("Deleting items completed:" + DateTime.UtcNow.ToLongTimeString());

            PrintDockerProcessStats("After MyTestCleanup");
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

            Thread.Sleep(5000);

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
        public async Task TestBasicRequestWebApp()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.ResponseCode = "200";

            await ValidateBasicRequestAsync(testappip, "/Default", expectedRequestTelemetry);
        }

        [TestMethod]
        public async Task TestBasicHttpDependencyWebApp()
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            await ValidateBasicDependencyAsync(testappip, "/Dependencies.aspx?type=http", expectedDependencyTelemetry);
        }

        [TestMethod]
        public async Task TestBasicSqlDependencyWebApp()
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;

            await ValidateBasicDependencyAsync(testappip, "/Dependencies.aspx?type=sql", expectedDependencyTelemetry);
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            PrintDockerProcessStats("Start of class cleanup");
            string dockerComposeActionCommand = "down";
            string dockerComposeFullCommandFormat = string.Format("{0} {1} {2}", dockerComposeBaseCommandFormat, dockerComposeFileNameFormat, dockerComposeActionCommand);
            Trace.WriteLine("Docker compose cleanup done using command: " + dockerComposeFullCommandFormat);
            ProcessStartInfo DockerComposeDown = new ProcessStartInfo("cmd", dockerComposeFullCommandFormat);
      
            Process process = new Process { StartInfo = DockerComposeDown };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Docker Compose Down Console output:" + output);
            process.WaitForExit();

            Thread.Sleep(5000);
        }

        private async Task ValidateBasicRequestAsync(string targetInstanceIp, string targetPath, RequestTelemetry expectedRequestTelemetry)
        {
            HttpClient client = new HttpClient();
            string url = "http://" + targetInstanceIp + targetPath;
            Trace.WriteLine("Hitting the target url:" + url);
            var response = await client.GetAsync(url);
            Trace.WriteLine("Actual Response code: " + response.StatusCode);
            Thread.Sleep(1000);
            var requestsWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(WebAppInstrumentationKey);

            Trace.WriteLine("RequestCount for WebApp:" + requestsWebApp.Count);
            Assert.IsTrue(requestsWebApp.Count == 1);
            var request = requestsWebApp[0];
            Assert.AreEqual(expectedRequestTelemetry.ResponseCode, request.data.baseData.responseCode, "Response code is incorrect");
        }

        private async Task ValidateBasicDependencyAsync(string targetInstanceIp, string targetPath, DependencyTelemetry expectedDependencyTelemetry)
        {
            HttpClient client = new HttpClient();
            string url = "http://" + targetInstanceIp + targetPath;
            Trace.WriteLine("Hitting the target url:" + url);
            try
            {
                var response = await client.GetStringAsync(url);                
                Trace.WriteLine("Actual Response text: " + response.ToString());
            }
            catch(Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
            }
            Thread.Sleep(1000);
            var dependenciesWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>(WebAppInstrumentationKey);
            PrintDependencies(dependenciesWebApp);

            Trace.WriteLine("Dependencies count for WebApp:" + dependenciesWebApp.Count);            
            Assert.IsTrue(dependenciesWebApp.Count == 1);
            var dependency = dependenciesWebApp[0];
            Assert.AreEqual(expectedDependencyTelemetry.Type, dependency.data.baseData.type, "Dependency Type is incorrect");
            Assert.AreEqual(expectedDependencyTelemetry.Success, dependency.data.baseData.success, "Dependency success is incorrect");
        }

        private void PrintDependencies(IList<TelemetryItem<AI.RemoteDependencyData>> dependencies)
        {
            foreach(var deps in dependencies)
            {
                Trace.WriteLine("Dependency Item Details");
                Trace.WriteLine("deps.time: "  +  deps.time);
                Trace.WriteLine("deps.iKey: " + deps.iKey);
                Trace.WriteLine("deps.data.baseData.name:" + deps.data.baseData.name);
                Trace.WriteLine("deps.data.baseData.type:" + deps.data.baseData.type);
                Trace.WriteLine("deps.data.baseData.target:" + deps.data.baseData.target);
                Trace.WriteLine("--------------------------------------");
            }
        }
    }
}
