using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Helpers;
using AI;

namespace E2ETests.Net462
{
    [TestClass]
    public class UnitTest1
    {
        internal static string testappip;
        internal static string ingestionServiceIp;
        internal static string dockerComposeFileName = "docker-compose462.yml";
        internal static string dockerComposeBaseCommandFormat = "/c docker-compose";
        internal static string dockerComposeFileNameFormat = string.Format("-f {0}", dockerComposeFileName);
        internal static DataEndpointClient dataendpointClient;

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Assert.IsTrue(File.Exists(".\\"+dockerComposeFileName));
            string dockerComposeActionCommand = "up -d";
            string dockerComposeFullCommandFormat = string.Format("{0} {1} {2}", dockerComposeBaseCommandFormat, dockerComposeFileNameFormat, dockerComposeActionCommand);
            Trace.WriteLine("Docker compose done using command: " + dockerComposeFullCommandFormat);
            ProcessStartInfo DockerComposeUp = new ProcessStartInfo("cmd", dockerComposeFullCommandFormat);
            ProcessStartInfo DockerInspectIp = new ProcessStartInfo("cmd", "/c docker inspect -f \"{{.NetworkSettings.Networks.e2etests_default.IPAddress}}\" e2etests_e2etestwebapp_1");
            ProcessStartInfo DockerInspectIpIngestion = new ProcessStartInfo("cmd", "/c docker inspect -f \"{{.NetworkSettings.Networks.e2etests_default.IPAddress}}\" e2etests_ingestionservice_1");

            Process process = new Process { StartInfo = DockerComposeUp };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Docker Compose Console output:" + output);
            process.WaitForExit();

            process = new Process { StartInfo = DockerInspectIp };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            testappip = output.Trim();
            Trace.WriteLine("DockerInspect WebApp IP" + output);
            process.WaitForExit();

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

            dataendpointClient = new DataEndpointClient(new Uri("http://" +ingestionServiceIp));
        }

        [TestMethod]        
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

            var requestsWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>("e45209bb-49ab-41a0-8065-793acb3acc56");
            var dependenciesWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>("e45209bb-49ab-41a0-8065-793acb3acc56");
            var requestsWebApi = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>("0786419e-d901-4373-902a-136921b63fb2");

            Trace.WriteLine("RequestCount for WebApp:"+ requestsWebApp.Count);
            Assert.IsTrue(requestsWebApp.Count == 2);

            Trace.WriteLine("DependenciesCount for WebApp:" + dependenciesWebApp.Count);
            Assert.IsTrue(dependenciesWebApp.Count >= 2);

            Trace.WriteLine("RequestCount for WebApi:" + requestsWebApi.Count);
            Assert.IsTrue(requestsWebApi.Count == 1);
        }


        [ClassCleanup]
        public static void MyClassCleanup()
        {                        
            string dockerComposeActionCommand = "down";
            string dockerComposeFullCommandFormat = string.Format("{0} {1} {2}", dockerComposeBaseCommandFormat, dockerComposeFileNameFormat, dockerComposeActionCommand);
            ProcessStartInfo DockerComposeDown = new ProcessStartInfo("cmd", dockerComposeFullCommandFormat);
      
            Process process = new Process { StartInfo = DockerComposeDown };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            Trace.WriteLine("Docker Compose Down Console output:" + output);
            process.WaitForExit();
        }       
    }
}
