using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;

namespace E2ETests
{
    [TestClass]
    public class UnitTest1
    {
        internal static string testappip;
        static string outputs = "";

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Assert.IsTrue(File.Exists(".\\docker-compose.yml"));

            ProcessStartInfo DockerComposeUp = new ProcessStartInfo("cmd", "/c docker-compose up -d");
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
        }

        [TestMethod]        
        public async Task TestMethod1()
        {
            HttpClient client = new HttpClient();
            string url = "http://" + testappip + "/Default.aspx";
            Trace.WriteLine(url);
            var response =await client.GetAsync(url);           
            Trace.WriteLine(response.StatusCode);
            response = await client.GetAsync(url);
            Trace.WriteLine(response.StatusCode);

            Assert.IsTrue(true);
        }


        [ClassCleanup]
        public static void MyClassCleanup()
        {
            Assert.IsTrue(File.Exists(".\\docker-compose.yml"));
            
            ProcessStartInfo DockerComposeDown = new ProcessStartInfo("cmd", "/c docker-compose down");

            Process process = new Process { StartInfo = DockerComposeDown };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();            
        }       
    }
}
