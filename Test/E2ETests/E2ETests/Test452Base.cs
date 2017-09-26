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

namespace E2ETests
{
    public abstract class Test452Base
    {
        internal const string WebAppInstrumentationKey = "e45209bb-49ab-41a0-8065-793acb3acc56";
        internal const string WebApiInstrumentationKey = "0786419e-d901-4373-902a-136921b63fb2";
        internal const string ContainerNameWebApp = "e2etests_e2etestwebapp_1";
        internal const string ContainerNameIngestionService = "e2etests_ingestionservice_1";
        internal static string testappip;
        internal static string ingestionServiceIp;
        internal static string DockerComposeFileName = "docker-compose.yml";
        internal static string DockerComposeBaseCommandFormat = "/c docker-compose";
        internal static string DockerComposeFileNameFormat;


        internal static DataEndpointClient dataendpointClient;
        internal static ProcessStartInfo DockerPSProcessInfo = new ProcessStartInfo("cmd", "/c docker ps -a");

        public static void MyClassInitializeBase()
        {
            Trace.WriteLine("Starting ClassInitialize:" + DateTime.UtcNow.ToLongTimeString());
            Assert.IsTrue(File.Exists(".\\" + DockerComposeFileName));

            DockerComposeFileNameFormat = string.Format("-f {0}", DockerComposeFileName);
            DockerComposeGenericCommandExecute("up -d --build");

            PrintDockerProcessStats("ClassInitialize...");

            // Inspect Docker containers to get IP addresses
            testappip = DockerInspectIPAddress(ContainerNameWebApp);
            ingestionServiceIp = DockerInspectIPAddress(ContainerNameIngestionService);

            string url = "http://" + testappip + "/Default";
            Trace.WriteLine("Warmup request fired against WebApp under test:" + url);
            var response = new HttpClient().GetAsync(url);
            Trace.WriteLine("Response for warm up request: " + response.Result.StatusCode);
            

            dataendpointClient = new DataEndpointClient(new Uri("http://" + ingestionServiceIp));

            Thread.Sleep(10000);
            Trace.WriteLine("Completed ClassInitialize:" + DateTime.UtcNow.ToLongTimeString());
        }
        
        public static void MyClassCleanupBase()
        {
            Trace.WriteLine("Started Class Cleanup:" + DateTime.UtcNow.ToLongTimeString());
            DockerComposeGenericCommandExecute("down");
            Trace.WriteLine("Completed Class Cleanup:" + DateTime.UtcNow.ToLongTimeString());
        }

        public void MyTestInitialize()
        {
            Trace.WriteLine("Started Test Initialize:" + DateTime.UtcNow.ToLongTimeString());
            RemoveIngestionItems();
            PrintDockerProcessStats("After MyTestInitialize" + DateTime.UtcNow.ToLongTimeString());
            Trace.WriteLine("Completed Test Initialize:" + DateTime.UtcNow.ToLongTimeString());
        }
        
        public void MyTestCleanup()
        {
            Trace.WriteLine("Started Test Cleanup:" + DateTime.UtcNow.ToLongTimeString());
            PrintDockerProcessStats("After MyTestCleanup" + DateTime.UtcNow.ToLongTimeString());
            Trace.WriteLine("Completed Test Cleanup:" + DateTime.UtcNow.ToLongTimeString());
        }
        
        public void TestBasicRequestWebApp()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.ResponseCode = "200";

            ValidateBasicRequestAsync(testappip, "/Default", expectedRequestTelemetry).Wait();
        }

        public void TestBasicHttpDependencyWebApp()
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(testappip, "/Dependencies.aspx?type=http", expectedDependencyTelemetry).Wait();
        }

        public void TestBasicSqlDependencyWebApp()
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(testappip, "/Dependencies.aspx?type=sql", expectedDependencyTelemetry).Wait();
        }

        private async Task ValidateBasicRequestAsync(string targetInstanceIp, string targetPath, RequestTelemetry expectedRequestTelemetry)
        {
            HttpClient client = new HttpClient();
            string url = "http://" + targetInstanceIp + targetPath;
            Trace.WriteLine("Hitting the target url:" + url);
            var response = await client.GetAsync(url);
            Trace.WriteLine("Actual Response code: " + response.StatusCode);
            Thread.Sleep(2000);
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
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
            }
            Thread.Sleep(2000);
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
            foreach (var deps in dependencies)
            {
                Trace.WriteLine("Dependency Item Details");
                Trace.WriteLine("deps.time: " + deps.time);
                Trace.WriteLine("deps.iKey: " + deps.iKey);
                Trace.WriteLine("deps.data.baseData.name:" + deps.data.baseData.name);
                Trace.WriteLine("deps.data.baseData.type:" + deps.data.baseData.type);
                Trace.WriteLine("deps.data.baseData.target:" + deps.data.baseData.target);
                Trace.WriteLine("--------------------------------------");
            }
        }

        private static void PrintDockerProcessStats(string message)
        {
            Trace.WriteLine("Docker PS Stats at " + message);
            Process process = new Process { StartInfo = DockerPSProcessInfo };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Docker ps -a" + output);
            process.WaitForExit();
        }

        private static void DockerComposeGenericCommandExecute(string action)
        {
            string dockerComposeFullCommandFormat = string.Format("{0} {1} {2}", DockerComposeBaseCommandFormat, DockerComposeFileNameFormat, action);
            Trace.WriteLine("Docker compose using command: " + dockerComposeFullCommandFormat);
            ProcessStartInfo DockerComposeStop = new ProcessStartInfo("cmd", dockerComposeFullCommandFormat);

            Process process = new Process { StartInfo = DockerComposeStop };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Docker Compose output:" + output);
            process.WaitForExit();
        }

        private static string DockerInspectIPAddress(string containerName)
        {
            string dockerInspectIpBaseCommand = "/c docker inspect -f \"{{.NetworkSettings.Networks.nat.IPAddress}}\" ";
            string dockerInspectIpCommand = dockerInspectIpBaseCommand + containerName;
            ProcessStartInfo DockerInspectIp = new ProcessStartInfo("cmd", dockerInspectIpCommand);
            Trace.WriteLine("DockerInspectIp done using command:" + dockerInspectIpCommand);

            Process process = new Process { StartInfo = DockerInspectIp };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string ip = output.Trim();
            Trace.WriteLine(string.Format("DockerInspect IP for {0} is {1}", containerName, ip));
            process.WaitForExit();
            return ip;
        }

        private void RemoveIngestionItems()
        {
            Trace.WriteLine("Deleting items started:" + DateTime.UtcNow.ToLongTimeString());
            dataendpointClient.DeleteItems(WebAppInstrumentationKey);
            dataendpointClient.DeleteItems(WebApiInstrumentationKey);
            Trace.WriteLine("Deleting items completed:" + DateTime.UtcNow.ToLongTimeString());
        }
    }
}
