using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Helpers;
using TestUtils.TestConstants;
using AI;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Linq;

namespace E2ETests
{
    public class DeployedApp
    {
        public string containerName;
        public string imageName;
        public string ipAddress;
        public string ikey;
        public string healthCheckPath;
        public string flushPath;
    }
    public abstract class Test452Base
    {

        internal static Dictionary<string, DeployedApp> Apps = new Dictionary<string, DeployedApp>()
        {
            {
                TestConstants.WebApiName,
                new DeployedApp
                    {
                        ikey = TestConstants.WebApiInstrumentationKey,
                        containerName = TestConstants.WebApiContainerName,
                        imageName = TestConstants.WebApiImageName,
                        healthCheckPath = TestConstants.WebApiHealthCheckPath,
                        flushPath = TestConstants.WebApiFlushPath
                    }
            },

            {
                TestConstants.IngestionName,
                new DeployedApp
                    {
                        ikey = "dummy",
                        containerName = TestConstants.IngestionContainerName,
                        imageName = TestConstants.IngestionImageName,
                        healthCheckPath = TestConstants.IngestionHealthCheckPath,
                        flushPath = TestConstants.IngestionFlushPath
                    }
            }
        };

        internal const int AISDKBufferFlushTime = 1000;
        internal static string DockerComposeFileName = "docker-compose.yml";
        internal static string AppNameBeingTested = "none";

        internal static DataEndpointClient dataendpointClient;
        internal static ProcessStartInfo DockerPSProcessInfo = new ProcessStartInfo("cmd", "/c docker ps -a");

        public static void MyClassInitializeBase()
        {
            Trace.WriteLine("Starting ClassInitialize:" + DateTime.UtcNow.ToLongTimeString());
            Assert.IsTrue(File.Exists(".\\" + DockerComposeFileName));
            Trace.WriteLine("DockerComposeFileName:" + DockerComposeFileName);

            // Windows Server Machines dont have docker-compose installed.
            Trace.WriteLine("Getting docker-compose.exe if required.");
            GetDockerCompose();
            Trace.WriteLine("Getting docker-compose.exe completed.");

            DockerUtils.RemoveDockerImage(Apps[AppNameBeingTested].imageName, true);
            DockerUtils.RemoveDockerContainer(Apps[AppNameBeingTested].containerName, true);
            // Deploy the docker cluster using Docker-Compose
            DockerUtils.ExecuteDockerComposeCommand("up -d --force-recreate --build", DockerComposeFileName);

            //DockerUtils.ExecuteDockerComposeCommand("up -d --build", DockerComposeFileName);

            Thread.Sleep(5000);
            DockerUtils.PrintDockerProcessStats("Docker-Compose -build");

            // Populate dynamic properties of Deployed Apps like ip address.
            PopulateIPAddresses();

            bool allAppsHealthy = HealthCheckAndRemoveImageIfNeededAllApp();

            if (!allAppsHealthy)
            {
                DockerUtils.ExecuteDockerComposeCommand("up -d --build", DockerComposeFileName);
                Thread.Sleep(5000);
                DockerUtils.PrintDockerProcessStats("Docker-Compose -build retry");
                PopulateIPAddresses();
                allAppsHealthy = HealthCheckAndRemoveImageIfNeededAllApp();
            }

            Assert.IsTrue(allAppsHealthy, "All Apps are not unhealthy.");

            dataendpointClient = new DataEndpointClient(new Uri("http://" + Apps[TestConstants.IngestionName].ipAddress));

            InitializeDatabase();

            Thread.Sleep(2000);

            // Wait long enough for first telemetry items from health check requests.
            // These could arrive several seconds after first request to app is made.
            Trace.WriteLine("Waiting to receive request telemetry from health check for WebApp");
            var requestsWebApp = WaitForReceiveRequestItemsFromDataIngestion(Apps[AppNameBeingTested].ipAddress, Apps[AppNameBeingTested].ikey, 30, false);
            Trace.WriteLine("Waiting to receive request telemetry from health check for WebApp completed. Item received count:" + requestsWebApp.Count);

            Trace.WriteLine("Waiting to receive request telemetry from health check for WebApi");
            var requestsWebApi = WaitForReceiveRequestItemsFromDataIngestion(Apps[TestConstants.WebApiName].ipAddress, Apps[TestConstants.WebApiName].ikey, 30, false);
            Trace.WriteLine("Waiting to receive request telemetry from health check for WebApi completed. Item received count:" + requestsWebApi.Count);

            Trace.WriteLine("Completed ClassInitialize:" + DateTime.UtcNow.ToLongTimeString());
        }

        private static void GetDockerCompose()
        {
            HttpClient client = new HttpClient();
            var stream = client.GetStreamAsync("https://github.com/docker/compose/releases/download/1.24.1/docker-compose-Windows-x86_64.exe").Result;
            FileStream fs = null;
            try
            {
                fs = new FileStream(".\\docker-compose.exe", FileMode.Create, FileAccess.Write, FileShare.None);
                stream.CopyToAsync(fs).Wait();
            }
            finally
            {
                fs.Close();
            }
        }

        private static void InitializeDatabase()
        {
            var ip = DockerUtils.FindIpDockerContainer("e2etests_sql-server_1");
            LocalDbHelper localDbHelper = new LocalDbHelper(ip);
            if (!localDbHelper.CheckDatabaseExists("dependencytest"))
            {
                Trace.WriteLine(DateTime.UtcNow.ToLongTimeString() + "Database not exist, will be created.");
                localDbHelper.CreateDatabase("dependencytest", "c:\\dependencytest.mdf");
                Trace.WriteLine(DateTime.UtcNow.ToLongTimeString() + "Database created.");
            }
            Trace.WriteLine(DateTime.UtcNow.ToLongTimeString() + "Database table creation begin..");
            localDbHelper.ExecuteScript("dependencytest", "Helpers\\TestDatabase.sql");
            Trace.WriteLine(DateTime.UtcNow.ToLongTimeString() + "Database table creation end..");


            if (!localDbHelper.CheckDatabaseExists("dependencytest"))
            {
                throw new Exception($"Failed to create database: 'dependencytest'");
            }
            Trace.WriteLine(DateTime.UtcNow.ToLongTimeString() + "Database successfully created");
        }

        private static bool HealthCheckAndRemoveImageIfNeededAllApp()
        {
            bool healthy = true;
            foreach (var app in Apps)
            {
                healthy = healthy && HealthCheckAndRemoveImageIfNeeded(app.Value);
            }

            return healthy;
        }

        private static void PopulateIPAddresses()
        {
            // Inspect Docker containers to get IP addresses      
            Trace.WriteLine("Inspecting Docker containers to get IP addresses");
            foreach (var app in Apps)
            {
                app.Value.ipAddress = DockerUtils.FindIpDockerContainer(app.Value.containerName);
            }
        }

        private static void RestartAllTestAppContainers()
        {
            foreach (var app in Apps.Values)
            {
                DockerUtils.RestartDockerContainer(app.containerName);
            }
        }

        public static void MyClassCleanupBase()
        {
            Trace.WriteLine("Started Class Cleanup:" + DateTime.UtcNow.ToLongTimeString());
            RemoveIngestionItems();
            // Not doing cleanup intentional for fast re-runs in local.
            //DockerUtils.ExecuteDockerComposeCommand("down", DockerComposeFileName);            
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
            ValidateBasicRequestAsync(Apps[AppNameBeingTested].ipAddress, "/About", expectedRequestTelemetry, Apps[AppNameBeingTested].ikey).Wait();
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

            ValidateXComponentWebAppToWebApi(Apps[AppNameBeingTested].ipAddress, Apps[TestConstants.WebApiName].ipAddress,
                "/Dependencies?type=httpsync",
                expectedRequestTelemetryWebApp, expectedDependencyTelemetryWebApp, expectedRequestTelemetryWebApi,
                Apps[AppNameBeingTested].ikey, Apps[TestConstants.WebApiName].ikey).Wait();
        }

        public void TestHttpDependency(string expectedPrefix, string appname, string path,
            string data, string target, string resultCodeExpected, bool successExpected)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = successExpected;
            expectedDependencyTelemetry.ResultCode = resultCodeExpected;
            expectedDependencyTelemetry.Target = target;
            expectedDependencyTelemetry.Data = data;

            ValidateBasicDependency(Apps[appname].ipAddress, path, expectedDependencyTelemetry,
                Apps[appname].ikey, 1, expectedPrefix);
        }

        public void TestSyncHttpDependency(string expectedPrefix)
        {
            TestHttpDependency(expectedPrefix,
                AppNameBeingTested,
                "/Dependencies.aspx?type=httpsync",
                TestConstants.WebAppUrlToWebApiSuccess,
                TestConstants.WebAppTargetNameToWebApi,
                "200",
                true);
        }

        /// <summary>
        /// Tests correlation between POST request and depdendency call that is done from the controller.
        /// </summary>
        /// <returns></returns>
        public async Task TestHttpDependencyCorrelationInPostRequest()
        {
            var operationId = ActivityTraceId.CreateRandom().ToHexString();
            bool supportsOnRequestExecute = false;
            string restoredActivityId = null;
            using (var httpClient = new HttpClient())
            {
                // The POST controller method will manually track dependency through the StartOperation 
                var request = new HttpRequestMessage(HttpMethod.Post, string.Format($"http://{Apps[TestConstants.WebApiName].ipAddress}/api/values"));
                request.Headers.Add("traceparent", $"00-{operationId}-{ActivitySpanId.CreateRandom()}-01");

                request.Content = new StringContent($"\"{new string('1', 1024 * 1024)}\"", Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);

                Trace.WriteLine("Response Headers: ");
                foreach (var header in response.Headers)
                {
                    Trace.WriteLine($"\t{header.Key} = {header.Value.First()}");
                }

                supportsOnRequestExecute = bool.TrueString == response.Headers.GetValues("OnExecuteRequestStep").First();
                if (response.Headers.TryGetValues("RestoredActivityId", out var ids))
                {
                    restoredActivityId = ids.First();
                }

                Assert.AreNotEqual(0, Int32.Parse(response.Headers.GetValues("BodyLength").First()));
            }

            var dependencies = WaitForReceiveDependencyItemsFromDataIngestion(Apps[TestConstants.WebApiName].ipAddress, Apps[TestConstants.WebApiName].ikey);
            Trace.WriteLine("Dependencies count for WebApp:" + dependencies.Count);
            PrintDependencies(dependencies);
            Assert.AreEqual(1, dependencies.Count);

            var requests = WaitForReceiveRequestItemsFromDataIngestion(Apps[TestConstants.WebApiName].ipAddress, Apps[TestConstants.WebApiName].ikey, expectNumberOfItems: 1);
            Trace.WriteLine("Requests count for WebApp:" + requests.Count);
            PrintRequests(requests);
            Assert.AreEqual(1, requests.Count);

            var dependency = dependencies[0];

            // if the App runs on ASP.NET 4.7.1+ version that supports OnExecuteRequestStep
            // dependency should be correlated to the request, false otherwise
            if (supportsOnRequestExecute)
            {
                var spanId = restoredActivityId.Split('-')[2];
                Assert.AreEqual(operationId, dependency.tags["ai.operation.id"]);
                Assert.AreEqual(spanId, dependency.tags["ai.operation.parentId"]);
                Assert.AreEqual(requests[0].data.baseData.id, dependency.tags["ai.operation.parentId"]);
            }
            else
            {
                Assert.AreNotEqual(operationId, dependency.tags["ai.operation.id"]);
            }
        }

        public void TestAsyncWithHttpClientHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.ResultCode = "200";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiSuccess;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpasynchttpclient", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestPostCallHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiSuccess;

            // 204 as the webapi does not return anything
            expectedDependencyTelemetry.ResultCode = "204";

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httppost", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestFailedHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "500";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiException;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpfailedwithexception", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestFailedAtDnsHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToInvalidHost;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToInvalidHost;
            // Failed at DNS means status code not collected
            //expectedDependencyTelemetry.ResultCode = "200";

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpfailedwithinvaliddns", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncHttpDependency1(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.ResultCode = "200";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiSuccess;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpasync1", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncFailedHttpDependency1(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "500";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiException;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=failedhttpasync1", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncHttpDependency2(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.ResultCode = "200";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiSuccess;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpasync2", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncFailedHttpDependency2(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "500";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiException;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=failedhttpasync2", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncHttpDependency3(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.ResultCode = "200";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiSuccess;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpasync3", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncFailedHttpDependency3(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "500";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiException;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=failedhttpasync3", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncHttpDependency4(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.ResultCode = "200";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiSuccess;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpasync4", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncFailedHttpDependency4(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "500";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiException;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=failedhttpasync4", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAsyncAwaitCallHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.ResultCode = "200";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiSuccess;
            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=httpasyncawait1", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestFailedAsyncAwaitCallHttpDependency(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "500";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToWebApi;
            expectedDependencyTelemetry.Data = TestConstants.WebAppUrlToWebApiException;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=failedhttpasyncawait1", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestAzureTableDependencyWebApp(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();

            // Expected type is http instead of AzureTable as type is based on the target url which
            // will be a local url in case of emulator.
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetToEmulatorTable;

            // 2 dependency item is expected.
            // 1 from creating table, and 1 from writing data to it.
            ValidateAzureDependencyAsync(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=azuresdktable&tablename=people" + expectedPrefix, expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 2, expectedPrefix, 2000);
        }

        public void TestAzureQueueDependencyWebApp(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();

            // Expected type is http instead of AzureTable as type is based on the target url which
            // will be a local url in case of emulator.
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetToEmulatorQueue;

            // 2 dependency item is expected.
            // 1 from creating queue, and 1 from writing data to it.
            ValidateAzureDependencyAsync(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=azuresdkqueue", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 2, expectedPrefix, 2000);
        }

        public void TestAzureBlobDependencyWebApp(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();

            // Expected type is http instead of AzureTable as type is based on the target url which
            // will be a local url in case of emulator.
            expectedDependencyTelemetry.Type = "Http";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetToEmulatorBlob;

            // 2 dependency item is expected.
            // 1 from creating table, and 1 from writing data to it.
            ValidateAzureDependencyAsync(Apps[AppNameBeingTested].ipAddress,
                "/Dependencies.aspx?type=azuresdkblob&containerName=" + expectedPrefix + "&blobname=" + expectedPrefix,
                expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 2, expectedPrefix, 2000);
        }


        public void TestSqlDependency(string expectedPrefix, string appname, string path, string expectedData, bool success = true)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = success;
            if (!success)
            {
                expectedDependencyTelemetry.ResultCode = "208";
            }
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = expectedData;

            ValidateBasicDependency(Apps[appname].ipAddress, path, expectedDependencyTelemetry,
                Apps[appname].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteReaderSuccessAsync(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;

            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteReaderAsync&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteReaderFailedAsync(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteReaderAsync&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader0Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader0&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader0Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader0&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader1Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader1&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader1Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader1&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader2Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader2&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader2Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader2&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader3Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader3&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteReader3Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteReader3&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteReader0Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteReader1&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteReader0Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteReader1&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteReader1Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteReader1&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteReader1Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteReader1&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteScalarAsyncSuccess(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteScalarAsync&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteScalarAsyncFailed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteScalarAsync&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteScalarSuccess(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteScalar&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteScalarFailed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteScalar&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteNonQuerySuccess(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteNonQuery&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteNonQueryFailed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteNonQuery&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteNonQueryAsyncSuccess(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteNonQueryAsync&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteNonQueryAsyncFailed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteNonQueryAsync&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteNonQuery0Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteNonQuery0&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteNonQuery0Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteNonQuery0&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteNonQuery2Success(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccess : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteNonQuery2&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteNonQuery2Failed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteNonQuery2&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteXmlReaderAsyncSuccess(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccessXML : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteXmlReaderAsync&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyExecuteXmlReaderAsyncFailed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteXmlReaderAsync&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteXmlReaderSuccess(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccessXML : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteXmlReader&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyBeginExecuteXmlReaderFailed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=BeginExecuteXmlReader&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteXmlReaderSuccess(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlSuccessXML : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteXmlReader&success=true", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencySqlCommandExecuteXmlReaderFailed(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = false;
            expectedDependencyTelemetry.ResultCode = "208";
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = (expectedPrefix != "rddf") ? TestConstants.WebAppFullQueryToSqlException : string.Empty;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=SqlCommandExecuteXmlReader&success=false", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        public void TestSqlDependencyStoredProcedureName(string expectedPrefix)
        {
            var expectedDependencyTelemetry = new DependencyTelemetry();
            expectedDependencyTelemetry.Type = "SQL";
            expectedDependencyTelemetry.Success = true;
            expectedDependencyTelemetry.Target = TestConstants.WebAppTargetNameToSql;
            expectedDependencyTelemetry.Data = TestConstants.WebAppStoredProcedureNameToSql;

            ValidateBasicDependency(Apps[AppNameBeingTested].ipAddress, "/Dependencies.aspx?type=ExecuteReaderStoredProcedureAsync&storedProcedureName=GetTopTenMessages", expectedDependencyTelemetry,
                Apps[AppNameBeingTested].ikey, 1, expectedPrefix);
        }

        private async Task ValidateXComponentWebAppToWebApi(string sourceInstanceIp, string targetInstanceIp, string sourcePath,
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
            Thread.Sleep(5 * AISDKBufferFlushTime);

            var requestsSource = WaitForReceiveRequestItemsFromDataIngestion(sourceInstanceIp, sourceIKey);
            var dependenciesSource = WaitForReceiveDependencyItemsFromDataIngestion(sourceInstanceIp, sourceIKey);
            var requestsTarget = WaitForReceiveRequestItemsFromDataIngestion(targetInstanceIp, targetIKey);

            PrintDependencies(dependenciesSource);
            PrintRequests(requestsSource);
            PrintRequests(requestsTarget);

            ReadApplicationTraces(sourceInstanceIp, "/Dependencies.aspx?type=etwlogs");
            ReadApplicationTraces(targetInstanceIp, "/Dependencies.aspx?type=etwlogs");

            Trace.WriteLine("RequestCount for Source:" + requestsSource.Count);
            // There could be 1 additional request here coming from the health check.
            // In profiler cases, this request telemetry may arrive quite late
            Assert.IsTrue(requestsSource.Count >= 1);

            Trace.WriteLine("RequestCount for Target:" + requestsTarget.Count);
            Assert.IsTrue(requestsTarget.Count == 1);

            Trace.WriteLine("Dependencies count for Source:" + dependenciesSource.Count);
            Assert.IsTrue(dependenciesSource.Count == 1);

            var requestTarget = requestsTarget[0];
            var dependencySource = dependenciesSource[0];
            Assert.IsTrue(requestTarget.tags["ai.operation.id"].Equals(dependencySource.tags["ai.operation.id"]),
                "Operation id for request telemetry dependency telemetry in source must be same.");

            var requestSource = requestsSource.SingleOrDefault(rd => rd.data.baseData.url.Contains(sourcePath));
            if (requestSource != null)
            {
                Assert.IsTrue(requestSource.tags["ai.operation.id"].Equals(requestTarget.tags["ai.operation.id"]),
                    "Operation id for request telemetry in source and target must be same.");
                Assert.IsTrue(requestSource.tags["ai.operation.id"].Equals(dependencySource.tags["ai.operation.id"]),
                    "Operation id for request telemetry dependency telemetry in source must be same.");
            }
            else
            {
                Assert.Inconclusive("Source request was not received");
            }
        }

        private async Task ValidateBasicRequestAsync(string targetInstanceIp, string targetPath,
            RequestTelemetry expectedRequestTelemetry, string ikey)
        {
            await ExecuteWebRequestToTarget(targetInstanceIp, targetPath);
            //Thread.Sleep(AISDKBufferFlushTime);
            //var requestsWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(ikey);
            var requestsWebApp = WaitForReceiveRequestItemsFromDataIngestion(targetInstanceIp, ikey);

            Trace.WriteLine("RequestCount for WebApp:" + requestsWebApp.Count);
            PrintRequests(requestsWebApp);

            PrintApplicationTraces(ikey);

            // we may receive several requests, including aux /flush and /Dependencies requests
            Assert.IsTrue(requestsWebApp.Count >= 1);
            var targetRequests = requestsWebApp.Where(r => r.data.baseData.url.Contains(targetPath)).ToList();
            Assert.AreEqual(1, targetRequests.Count);
            var request = targetRequests[0];

            Assert.AreEqual(expectedRequestTelemetry.ResponseCode, request.data.baseData.responseCode, "Response code is incorrect");
        }

        private void ValidateBasicDependency(string targetInstanceIp, string targetPath,
            DependencyTelemetry expectedDependencyTelemetry, string ikey, int count,
            string expectedPrefix, int additionalSleepTimeMsec = 0)
        {
            var success = ExecuteWebRequestToTarget(targetInstanceIp, targetPath).Result;
            Assert.IsTrue(success, "Web App did not respond with success. Failing test. Check exception from logs.");


            //Thread.Sleep(AISDKBufferFlushTime + additionalSleepTimeMsec);
            //var dependenciesWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>(ikey);
            var dependenciesWebApp = WaitForReceiveDependencyItemsFromDataIngestion(targetInstanceIp, ikey);
            Trace.WriteLine("Dependencies count for WebApp:" + dependenciesWebApp.Count);
            PrintDependencies(dependenciesWebApp);

            ReadApplicationTraces(targetInstanceIp, "/Dependencies.aspx?type=etwlogs");
            Assert.IsTrue(dependenciesWebApp.Count == count, string.Format("Dependeny count is incorrect. Actual: {0} Expected: {1}", dependenciesWebApp.Count, count));
            var dependency = dependenciesWebApp[0];

            ValidateDependency(expectedDependencyTelemetry, dependency, expectedPrefix);
        }

        private static IList<TelemetryItem<RemoteDependencyData>> WaitForReceiveDependencyItemsFromDataIngestion(string targetInstanceIp, string ikey, int maxRetryCount = 5, bool flushChannel = true)
        {
            int receivedItemCount = 0;
            int iteration = 0;
            IList<TelemetryItem<RemoteDependencyData>> items = new List<TelemetryItem<RemoteDependencyData>>();

            while (iteration < maxRetryCount && receivedItemCount < 1)
            {
                Thread.Sleep(AISDKBufferFlushTime);
                items = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>(ikey);
                receivedItemCount = items.Count;
                iteration++;
                if (receivedItemCount == 0 && flushChannel)
                {
                    ExecuteWebRequestToTarget(targetInstanceIp, Apps[AppNameBeingTested].flushPath).Wait();
                }
            }

            Trace.WriteLine("Items received in iteration: " + iteration);
            return items;
        }

        private static IList<TelemetryItem<RequestData>> WaitForReceiveRequestItemsFromDataIngestion(string targetInstanceIp, string ikey, int maxRetryCount = 5, bool flushChannel = true, int expectNumberOfItems = 1)
        {
            int receivedItemCount = 0;
            int iteration = 0;
            IList<TelemetryItem<RequestData>> items = new List<TelemetryItem<RequestData>>();

            while (iteration < maxRetryCount && receivedItemCount < expectNumberOfItems)
            {
                Thread.Sleep(AISDKBufferFlushTime);
                items = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RequestData>>(ikey);
                receivedItemCount = items.Count;
                iteration++;
                if (receivedItemCount == 0 && flushChannel)
                {
                    ExecuteWebRequestToTarget(targetInstanceIp, Apps[AppNameBeingTested].flushPath).Wait();
                }
            }

            Trace.WriteLine($"{receivedItemCount} items received in iteration {iteration}");
            return items;
        }

        private void PrintApplicationTraces(string ikey)
        {
            try
            {
                var messages = dataendpointClient.GetItemsOfType<TelemetryItem<AI.MessageData>>(ikey);
                Trace.WriteLine("Begin Application Traces for ikey:" + ikey + "----------------------------------------------------------------------------------------");
                foreach (var message in messages)
                {
                    Trace.WriteLine("Application Trace:" + message.data.baseData.message);
                }
                Trace.WriteLine("End Application Traces for ikey:" + ikey + "----------------------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception printing application traces:" + ex.Message);
            }
        }

        private void ReadApplicationTraces(string targetInstanceIp, string targetPath)
        {
            try
            {
                Trace.WriteLine("Begin Application Traces----------------------------------------------------------------------------------------");


                HttpClient client = new HttpClient();
                string url = "http://" + targetInstanceIp + targetPath;
                Trace.WriteLine("Hitting url to get traces: " + url);
                try
                {
                    var response = client.GetStringAsync(url).Result;
                    Trace.WriteLine("Actual Response text: " + response.ToString());
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Exception occured:" + ex.Message);
                }

                Trace.WriteLine("End Application Traces----------------------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception printing application traces:" + ex.Message);
            }
        }

        private void ValidateAzureDependencyAsync(string targetInstanceIp, string targetPath,
            DependencyTelemetry expectedDependencyTelemetry, string ikey, int minCount, string expectedPrefix, int additionalSleepTimeMsec = 0)
        {
            var success = ExecuteWebRequestToTarget(targetInstanceIp, targetPath).Result;
            Assert.IsTrue(success, "Web App did not respond with success. Failing test. Check exception from logs.");
            Thread.Sleep(AISDKBufferFlushTime + additionalSleepTimeMsec);

            var dependenciesWebApp = dataendpointClient.GetItemsOfType<TelemetryItem<AI.RemoteDependencyData>>(ikey);
            Trace.WriteLine("Dependencies count for WebApp:" + dependenciesWebApp.Count);
            PrintDependencies(dependenciesWebApp);

            ReadApplicationTraces(targetInstanceIp, "/Dependencies.aspx?type=etwlogs");

            Assert.IsTrue(dependenciesWebApp.Count >= minCount, string.Format("Dependency count is incorrect. Actual: {0} Expected minimum: {1}", dependenciesWebApp.Count, minCount));
            var dependency = dependenciesWebApp[0];

            ValidateDependency(expectedDependencyTelemetry, dependency, expectedPrefix);
        }

        private static async Task<bool> ExecuteWebRequestToTarget(string targetInstanceIp, string targetPath)
        {
            bool success = false;
            HttpClient client = new HttpClient();
            string url = "http://" + targetInstanceIp + targetPath;
            Trace.WriteLine("Hitting the target url:" + url);
            try
            {
                var response = await client.GetStringAsync(url);
                Trace.WriteLine("Actual Response text: " + response.ToString());
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                Trace.WriteLine("Exception occured:" + ex);
            }

            return success;
        }

        private void ValidateDependency(DependencyTelemetry expectedDependencyTelemetry,
            TelemetryItem<AI.RemoteDependencyData> actualDependencyTelemetry,
            string expectedPrefix)
        {
            Assert.AreEqual(expectedDependencyTelemetry.Type, actualDependencyTelemetry.data.baseData.type, "Dependency Type is incorrect");
            Assert.AreEqual(expectedDependencyTelemetry.Success, actualDependencyTelemetry.data.baseData.success, "Dependency success is incorrect");

            string actualSdkVersion = actualDependencyTelemetry.tags[new ContextTagKeys().InternalSdkVersion];
            Assert.IsTrue(actualSdkVersion.Contains(expectedPrefix), "Actual version:" + actualSdkVersion);

            if (!string.IsNullOrEmpty(expectedDependencyTelemetry.ResultCode))
            {
                Assert.AreEqual(expectedDependencyTelemetry.ResultCode, actualDependencyTelemetry.data.baseData.resultCode);
            }

            //Assert.AreEqual(verb + " " + expectedUrl.AbsolutePath, actualDependencyTelemetry.data.baseData.name);
            if (!string.IsNullOrEmpty(expectedDependencyTelemetry.Target))
            {
                Assert.AreEqual(expectedDependencyTelemetry.Target, actualDependencyTelemetry.data.baseData.target);
            }

            if (!string.IsNullOrEmpty(expectedDependencyTelemetry.Data))
            {
                Assert.AreEqual(expectedDependencyTelemetry.Data, actualDependencyTelemetry.data.baseData.data);
            }

            var depTime = TimeSpan.Parse(actualDependencyTelemetry.data.baseData.duration, CultureInfo.InvariantCulture);
            if (expectedDependencyTelemetry.Success.HasValue)
            {
                if (expectedDependencyTelemetry.Success.Value == true)
                {
                    Assert.IsTrue(depTime.TotalMilliseconds > 0, "Access time should be above zero");
                }
                else
                {
                    Assert.IsTrue(depTime.TotalMilliseconds >= 0, "Access time should be zero or above zero if success is false.");
                }
            }
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
                Trace.WriteLine("deps.tags[ai.operation.id]:" + deps.tags["ai.operation.id"]);
                Trace.WriteLine("deps.data.baseData.type:" + deps.data.baseData.type);
                Trace.WriteLine("deps.data.baseData.data:" + deps.data.baseData.data);
                Trace.WriteLine("deps.data.baseData.success:" + deps.data.baseData.success);
                Trace.WriteLine("deps.data.baseData.duration:" + deps.data.baseData.duration);
                Trace.WriteLine("deps.data.baseData.resultCode:" + deps.data.baseData.resultCode);
                Trace.WriteLine("deps.data.baseData.id:" + deps.data.baseData.id);
                Trace.WriteLine("deps.data.baseData.target:" + deps.data.baseData.target);
                Trace.WriteLine("InternalSdkVersion:" + deps.tags[new ContextTagKeys().InternalSdkVersion]);
                Trace.WriteLine("--------------------------------------");
            }
        }

        private void PrintRequests(IList<TelemetryItem<AI.RequestData>> requests)
        {
            foreach (var req in requests)
            {
                Trace.WriteLine("Request Item Details");
                Trace.WriteLine("req.time: " + req.time);
                Trace.WriteLine("req.iKey: " + req.iKey);
                Trace.WriteLine("req.name: " + req.name);
                Trace.WriteLine("req.data.baseData.name:" + req.data.baseData.name);
                Trace.WriteLine("req.tags[ai.operation.id]:" + req.tags["ai.operation.id"]);
                Trace.WriteLine("req.data.baseData.responseCode:" + req.data.baseData.responseCode);
                Trace.WriteLine("req.data.baseData.success:" + req.data.baseData.success);
                Trace.WriteLine("req.data.baseData.duration:" + req.data.baseData.duration);
                Trace.WriteLine("req.data.baseData.id:" + req.data.baseData.id);
                Trace.WriteLine("req.data.baseData.url:" + req.data.baseData.url);
                Trace.WriteLine("InternalSdkVersion:" + req.tags[new ContextTagKeys().InternalSdkVersion]);
                Trace.WriteLine("--------------------------------------");
            }
        }

        private static void RemoveIngestionItems()
        {
            Trace.WriteLine("Deleting items started:" + DateTime.UtcNow.ToLongTimeString());
            foreach (var app in Apps)
            {
                Trace.WriteLine("Deleting items for ikey:" + app.Value.ikey);
                dataendpointClient.DeleteItems(app.Value.ikey);
            }
            Trace.WriteLine("Deleting items completed:" + DateTime.UtcNow.ToLongTimeString());
        }

        private static bool HealthCheckAndRemoveImageIfNeeded(DeployedApp app)
        {
            Trace.WriteLine("Starting health check for :" + app.imageName);

            bool isAppHealthy = HealthCheck(app);
            if (!isAppHealthy)
            {
                DockerUtils.RemoveDockerContainer(app.containerName, true);
            }

            Trace.WriteLine(app.imageName + " healthy:" + isAppHealthy);
            return isAppHealthy;
        }

        private static void RemoveApp(DeployedApp app)
        {
            DockerUtils.RestartDockerContainer(app.containerName);
            app.ipAddress = DockerUtils.FindIpDockerContainer(app.containerName);
        }

        private static void RestartApp(DeployedApp app)
        {
            DockerUtils.RestartDockerContainer(app.containerName);
            app.ipAddress = DockerUtils.FindIpDockerContainer(app.containerName);
        }

        private static bool HealthCheck(DeployedApp app)
        {
            bool isHealthy = true;
            string url = "";
            try
            {
                url = "http://" + app.ipAddress + app.healthCheckPath;
                Stopwatch sw = Stopwatch.StartNew();
                Trace.WriteLine(string.Format("{2}:Request fired against {0} using url: {1}", app.containerName, url, DateTime.UtcNow.ToLongTimeString()));
                var response = new HttpClient().GetAsync(url);
                Trace.WriteLine(string.Format("Response from {0} : {1}", url, response.Result.StatusCode));
                Trace.WriteLine("Health check took " + sw.ElapsedMilliseconds + " msec.");
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
