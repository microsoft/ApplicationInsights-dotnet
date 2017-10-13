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
    public class DeployedApp
    {
        public string containerName;
        public string imageName;
        public string ipAddress;
        public string ikey;
        public string healthCheckPath;
    }
    public abstract class Test452Base
    {
        internal const string WebAppInstrumentationKey = "e45209bb-49ab-41a0-8065-793acb3acc56";
        internal const string WebApiInstrumentationKey = "0786419e-d901-4373-902a-136921b63fb2";
        internal const string WebAppName = "WebApp";
        internal const string WebApiName = "WebApi";
        internal const string IngestionName = "Ingestion";

        internal static Dictionary<string, DeployedApp> Apps = new Dictionary<string, DeployedApp>()
        {
            {
                WebAppName,
                new DeployedApp
                    {
                        ikey = WebAppInstrumentationKey,
                        containerName = "e2etests_e2etestwebapp_1",
                        imageName = "e2etests_e2etestwebapp",
                        healthCheckPath = "/Dependencies?type=httpsync"
                    }
            },

            {
                WebApiName,
                new DeployedApp
                    {
                        ikey = WebApiInstrumentationKey,
                        containerName = "e2etests_e2etestwebapi_1",
                        imageName = "e2etests_e2etestwebapi",
                        healthCheckPath = "/api/values"
                    }
            },

            {
                IngestionName,
                new DeployedApp
                    {
                        containerName = "e2etests_ingestionservice_1",
                        imageName = "e2etests_ingestionservice",
                        healthCheckPath = "/api/Data/HealthCheck?name=cijo"
                    }
            } 
        };
        
        internal const int AISDKBufferFlushTime = 2000;        
        internal static string DockerComposeFileName = "docker-compose.yml";
        internal static string VersionPrefix = "rdddsd";

        internal static DataEndpointClient dataendpointClient;
        internal static ProcessStartInfo DockerPSProcessInfo = new ProcessStartInfo("cmd", "/c docker ps -a");

        public static void MyClassInitializeBase()
        {
            Trace.WriteLine("Starting ClassInitialize:" + DateTime.UtcNow.ToLongTimeString());
            Assert.IsTrue(File.Exists(".\\" + DockerComposeFileName));

            //DockerUtils.RemoveDockerImage(Apps[WebAppName].imageName, true);

            // Deploy the docker cluster using Docker-Compose
            //DockerUtils.ExecuteDockerComposeCommand("up -d --force-recreate --build", DockerComposeFileName);
            DockerUtils.ExecuteDockerComposeCommand("up -d --build", DockerComposeFileName);
            DockerUtils.PrintDockerProcessStats("Docker-Compose -build");
            Thread.Sleep(1000);
            
            // Populate dynamic properties of Deployed Apps like ip address.
            PopulateIPAddresses();

            bool webAppHealthy = HealthCheckAndRestartIfNeeded(Apps[WebAppName]);
            bool webApiHealthy = HealthCheckAndRestartIfNeeded(Apps[WebApiName]);
            bool ingestionHealthy = HealthCheckAndRestartIfNeeded(Apps[IngestionName]);


            Assert.IsTrue(webAppHealthy, "Web App is unhealthy");
            Assert.IsTrue(webApiHealthy, "Web Api is unhealthy");
            Assert.IsTrue(ingestionHealthy, "Ingestion is unhealthy");
            
            dataendpointClient = new DataEndpointClient(new Uri("http://" + Apps[IngestionName].ipAddress));
          
            Thread.Sleep(5000);
            Trace.WriteLine("Completed ClassInitialize:" + DateTime.UtcNow.ToLongTimeString());
        }

        private static void PopulateIPAddresses()
        {
            // Inspect Docker containers to get IP addresses
            Apps[WebAppName].ipAddress = DockerUtils.FindIpDockerContainer(Apps[WebAppName].containerName);
            Apps[WebApiName].ipAddress = DockerUtils.FindIpDockerContainer(Apps[WebApiName].containerName);
            Apps[IngestionName].ipAddress = DockerUtils.FindIpDockerContainer(Apps[IngestionName].containerName);            
        }
        
        private static void RestartAllTestAppContainers()
        {
            foreach(var app in Apps.Values)
            {
                DockerUtils.RestartDockerContainer(app.containerName);
            }            
        }

        public static void MyClassCleanupBase()
        {
            Trace.WriteLine("Started Class Cleanup:" + DateTime.UtcNow.ToLongTimeString());
            DockerUtils.ExecuteDockerComposeCommand("down", DockerComposeFileName);
            Trace.WriteLine("Completed Class Cleanup:" + DateTime.UtcNow.ToLongTimeString());

            DockerUtils.PrintDockerProcessStats("Docker-Compose down");
        }

        public void MyTestInitialize()
        {
            Trace.WriteLine("Started Test Initialize:" + DateTime.UtcNow.ToLongTimeString());
            RemoveIngestionItems();
            DockerUtils.PrintDockerProcessStats("After MyTestInitialize" + DateTime.UtcNow.ToLongTimeString());
            Trace.WriteLine("Completed Test Initialize:" + DateTime.UtcNow.ToLongTimeString());
        }
        
        public void MyTestCleanup()
        {
            Trace.WriteLine("Started Test Cleanup:" + DateTime.UtcNow.ToLongTimeString());
            DockerUtils.PrintDockerProcessStats("After MyTestCleanup" + DateTime.UtcNow.ToLongTimeString());
            Trace.WriteLine("Completed Test Cleanup:" + DateTime.UtcNow.ToLongTimeString());
        }
        
        public void TestBasicRequestWebApp()
        {
            var expectedRequestTelemetry = new RequestTelemetry();
            expectedRequestTelemetry.ResponseCode = "200";            
            ValidateBasicRequestAsync(Apps[WebAppName].ipAddress, "/About", expectedRequestTelemetry, Apps[WebAppName].ikey).Wait();
        }

        public void TestXComponentWebAppToWebApi()
        {
            var expectedRequestTelemetryWebApp = new RequestTelemetry();
            expectedRequestTelemetryWebApp.ResponseCode = "200";            

            var expectedDependencyTelemetryWebApp = new DependencyTelemetry();
            expectedDependencyTelemetryWebApp.Type = "Http";
            expectedDependencyTelemetryWebApp.Success = true;

            var expectedRequestTelemetryWebApi = new RequestTelemetry();
            expectedRequestTelemetryWebApi.ResponseCode = "200";

            ValidateXComponentWebAppToWebApi(Apps[WebAppName].ipAddress, "/Dependencies?type=httpsync", 
                expectedRequestTelemetryWebApp, expectedDependencyTelemetryWebApp, expectedRequestTelemetryWebApi,
                Apps[WebAppName].ikey, Apps[WebApiName].ikey).Wait();
        }

        public void TestSyncHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpsync", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncWithHttpClientHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpasynchttpclient", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestPostCallHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;            

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httppost", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestFailedHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpfailedwithexception", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestFailedAtDnsHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpfailedwithinvaliddns", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncHttpDependency1(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;            

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpasync1", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncFailedHttpDependency1(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=failedhttpasync1", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncHttpDependency2(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpasync2", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncFailedHttpDependency2(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=failedhttpasync2", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncHttpDependency3(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpasync3", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncFailedHttpDependency3(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=failedhttpasync3", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncHttpDependency4(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpasync4", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncFailedHttpDependency4(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=failedhttpasync4", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAsyncAwaitCallHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=httpasyncawait1", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestFailedAsyncAwaitCallHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=failedhttpasyncawait1", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        public void TestAzureTableDependencyWebApp(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();

            // Expected type is http instead of AzureTable as type is based on the target url which
            // will be a local url in case of emulator.
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            // 2 dependency item is expected.
            // 1 from creating table, and 1 from writing data to it.
            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=azuresdktable", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 4, expectedPrefix).Wait();
        }

        public void TestAzureQueueDependencyWebApp(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();

            // Expected type is http instead of AzureTable as type is based on the target url which
            // will be a local url in case of emulator.
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            // 2 dependency item is expected.
            // 1 from creating table, and 1 from writing data to it.
            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=azuresdkqueue", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 2, expectedPrefix).Wait();
        }

        public void TestAzureBlobDependencyWebApp(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();

            // Expected type is http instead of AzureTable as type is based on the target url which
            // will be a local url in case of emulator.
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;

            // 2 dependency item is expected.
            // 1 from creating table, and 1 from writing data to it.
            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=azuresdkblob", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 2, expectedPrefix).Wait();
        }

        public void TestBasicSqlDependencyWebApp(string expectedPrefix = "rddf")
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;

            ValidateBasicDependencyAsync(Apps[WebAppName].ipAddress, "/Dependencies.aspx?type=sql", expectedDependencyTelemetry,
                Apps[WebAppName].ikey, 1, expectedPrefix).Wait();
        }

        private async Task ValidateXComponentWebAppToWebApi(string sourceInstanceIp, string sourcePath,
            RequestTelemetry expectedRequestTelemetrySource,
            DependencyTelemetry expectedDependencyTelemetrySource,
            RequestTelemetry expectedRequestTelemetryTarget,
            string sourceIKey, string targetIKey)
        {
            HttpClient client = new HttpClient();
            string url = "http://" + sourceInstanceIp + sourcePath;
            Trace.WriteLine("Hitting the target url:" + url);
            var response = await client.GetAsync(url);
            Trace.WriteLine("Actual Response code: " + response.StatusCode);
            Thread.Sleep(2 * AISDKBufferFlushTime);
            var requestsSource = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(sourceIKey);
            var dependenciesSource = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>(sourceIKey);
            var requestsTarget = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(targetIKey);

            Trace.WriteLine("RequestCount for Source:" + requestsSource.Count);
            Assert.IsTrue(requestsSource.Count == 1);

            Trace.WriteLine("RequestCount for Target:" + requestsTarget.Count);
            Assert.IsTrue(requestsTarget.Count == 1);

            Trace.WriteLine("Dependencies count for Source:" + dependenciesSource.Count);
            Assert.IsTrue(dependenciesSource.Count == 1);

            var requestSource = requestsSource[0];
            var requestTarget = requestsTarget[0];
            var dependencySource = dependenciesSource[0];

            Assert.IsTrue(requestSource.tags["ai.operation.id"].Equals(requestTarget.tags["ai.operation.id"]), 
                "Operation id for request telemetry in source and target must be same.");

            Assert.IsTrue(requestSource.tags["ai.operation.id"].Equals(dependencySource.tags["ai.operation.id"]),
                "Operation id for request telemetry dependency telemetry in source must be same.");

        }

        private async Task ValidateBasicRequestAsync(string targetInstanceIp, string targetPath,
            RequestTelemetry expectedRequestTelemetry, string ikey)
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
            Thread.Sleep(AISDKBufferFlushTime);
            var requestsWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(ikey);

            Trace.WriteLine("RequestCount for WebApp:" + requestsWebApp.Count);
            Assert.IsTrue(requestsWebApp.Count == 1);
            var request = requestsWebApp[0];
            Assert.AreEqual(expectedRequestTelemetry.ResponseCode, request.data.baseData.responseCode, "Response code is incorrect");
        }

        private async Task ValidateBasicDependencyAsync(string targetInstanceIp, string targetPath,
            DependencyTelemetry expectedDependencyTelemetry, string ikey, int count, string expectedPrefix)
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
            Thread.Sleep(AISDKBufferFlushTime);
            var dependenciesWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>(ikey);
            PrintDependencies(dependenciesWebApp);

            Trace.WriteLine("Dependencies count for WebApp:" + dependenciesWebApp.Count);
            Assert.IsTrue(dependenciesWebApp.Count == count, string.Format("Dependeny count is incorrect. Actual {0} Expected {1}", dependenciesWebApp.Count, count));
            var dependency = dependenciesWebApp[0];
            Assert.AreEqual(expectedDependencyTelemetry.Type, dependency.data.baseData.type, "Dependency Type is incorrect");
            Assert.AreEqual(expectedDependencyTelemetry.Success, dependency.data.baseData.success, "Dependency success is incorrect");

            string actualSdkVersion = dependency.tags[new ContextTagKeys().InternalSdkVersion];
            Assert.IsTrue(actualSdkVersion.Contains(expectedPrefix), "Actual version:" + actualSdkVersion);
        }

        private void PrintDependencies(IList<TelemetryItem<AI.RemoteDependencyData>> dependencies)
        {
            foreach (var deps in dependencies)
            {
                Trace.WriteLine("Dependency Item Details");
                Trace.WriteLine("deps.time: " + deps.time);
                Trace.WriteLine("deps.iKey: " + deps.iKey);
                Trace.WriteLine("deps.name: " + deps.name);
                Trace.WriteLine("deps.data.baseData.name:" + deps.data.baseData.name);
                Trace.WriteLine("deps.data.baseData.type:" + deps.data.baseData.type);
                Trace.WriteLine("deps.data.baseData.success:" + deps.data.baseData.success);
                Trace.WriteLine("deps.data.baseData.duration:" + deps.data.baseData.duration);
                Trace.WriteLine("deps.data.baseData.resultCode:" + deps.data.baseData.resultCode);
                Trace.WriteLine("deps.data.baseData.id:" + deps.data.baseData.id);
                Trace.WriteLine("deps.data.baseData.target:" + deps.data.baseData.target);
                Trace.WriteLine("InternalSdkVersion:" + deps.tags[new ContextTagKeys().InternalSdkVersion]);
                Trace.WriteLine("--------------------------------------");
            }
        }        
        
        private void RemoveIngestionItems()
        {
            Trace.WriteLine("Deleting items started:" + DateTime.UtcNow.ToLongTimeString());
            dataendpointClient.DeleteItems(WebAppInstrumentationKey);
            dataendpointClient.DeleteItems(WebApiInstrumentationKey);
            Trace.WriteLine("Deleting items completed:" + DateTime.UtcNow.ToLongTimeString());
        }

        private static bool HealthCheckAndRestartIfNeeded(DeployedApp app)
        {
            bool isAppHealthy = HealthCheck(app);
            if(!isAppHealthy)
            {
                RestartApp(app);
                isAppHealthy = HealthCheck(app);
            }

            return isAppHealthy;
        }

        private static void RestartApp(DeployedApp app)
        {
            DockerUtils.RestartDockerContainer(app.containerName);
            app.ipAddress = DockerUtils.FindIpDockerContainer(app.containerName);
        }

        private static bool HealthCheck(DeployedApp app)
        {
            bool isHealthy = true;
            Trace.WriteLine("Docker Stats for: " + app.containerName);
            Trace.WriteLine("Status" + DockerUtils.GetDockerStateStatus(app.containerName));
            Trace.WriteLine("ExitCode" + DockerUtils.GetDockerStateExitCode(app.containerName));
            string url = "";
            try
            {
                url = "http://" + app.ipAddress + app.healthCheckPath;            
                Trace.WriteLine(string.Format("Request fired against {0} using url: {1}", app.containerName, url));            
                var response = new HttpClient().GetAsync(url);
                Trace.WriteLine(string.Format("Response from {0} : {1}", url, response.Result.StatusCode));
                if (response.Result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    isHealthy = false;                    
                }
            }
            catch (Exception ex)
            {
                isHealthy = false;
                Trace.WriteLine(string.Format("Exception occuring hitting {0} : {1}", url, ex));
            }
            return isHealthy;
        }
    }
}
