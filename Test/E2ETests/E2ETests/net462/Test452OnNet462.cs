using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Helpers;

namespace E2ETests.Net462
{
    [TestClass]
    public class UnitTest1
    {
        internal static string testappip;
        internal static string dockerComposeFileName = "docker-compose462.yml";
        internal static string dockerComposeBaseCommandFormat = "/c docker-compose";
        internal static string dockerComposeFileNameFormat = string.Format("-f {0}", dockerComposeFileName);                
        
        internal static AppInsightsRestClient aiClientForWebApp;
        internal static AppInsightsRestClient aiClientForWebApi;        

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Assert.IsTrue(File.Exists(".\\net462\\"+dockerComposeFileName));
            string dockerComposeActionCommand = "up -d";
            string dockerComposeFullCommandFormat = string.Format("{0} {1} {2}", dockerComposeBaseCommandFormat, dockerComposeFileNameFormat, dockerComposeActionCommand);
            ProcessStartInfo DockerComposeUp = new ProcessStartInfo("cmd", dockerComposeFullCommandFormat);
            ProcessStartInfo DockerInspectIp = new ProcessStartInfo("cmd", "/c docker inspect -f \"{{.NetworkSettings.Networks.e2etests_default.IPAddress}}\" e2etests_e2etestwebapp_1");

            Process process = new Process { StartInfo = DockerComposeUp };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;            
            process.Start();            
            process.WaitForExit();

            process = new Process { StartInfo = DockerInspectIp };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            testappip = output.Trim();
            Trace.WriteLine(output);
            process.WaitForExit();

            aiClientForWebApp = new AppInsightsRestClient("0d6d8edc-754f-46d9-9ca3-4d5c7ae8346d", "feftgddgxfvxmrhxi1etwjjo1z2k01lc1qgi3p8s");
            aiClientForWebApi = new AppInsightsRestClient("d376981c-e63a-4701-ac97-3773ad9d67a6", "ed41a1npbikkpawi82yeztynnzzgsjn3kgbecx21");
        }

        [TestMethod]        
        public async Task TestBasicRequest()
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
            var endTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");


            Thread.Sleep(300000);
            string queryWindow = string.Format("{0}/{1}", startTime, endTime);
            Trace.WriteLine("QueryWindow:" + queryWindow);
            var requests = aiClientForWebApp.GetRequests(queryWindow);

            Trace.WriteLine("RequestCount:"+requests.Count);
            Assert.IsTrue(requests.Count == 2);

            Assert.IsTrue(true);
        }


        [ClassCleanup]
        public static void MyClassCleanup()
        {                        
            string dockerComposeActionCommand = "down";
            string dockerComposeFullCommandFormat = string.Format("{0} {1} {2}", dockerComposeBaseCommandFormat, dockerComposeFileNameFormat, dockerComposeActionCommand);
            ProcessStartInfo DockerComposeDown = new ProcessStartInfo("cmd", dockerComposeFullCommandFormat);

            Process process = new Process { StartInfo = DockerComposeDown };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();            
        }       
    }
}
