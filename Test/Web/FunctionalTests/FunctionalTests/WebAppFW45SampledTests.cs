namespace FunctionalTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Runtime.ExceptionServices;

    using Functional.Asmx;
    using Functional.Helpers;
    using Functional.IisExpress;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;

    [TestClass]
    [Ignore] // Sampling is out in Core 1.2
    public class WebAppFw45SampledTests : RequestTelemetryTestBase
    {
        private const int TimeoutInMs = 10000;
        private const int TestListenerWaitTimeInMs = 60000;

        private const string TestWebApplicationSourcePath = @"..\TestApps\WebAppFW45Sampled\App";
        private const string TestWebApplicationDestPath = @"..\TestApps\WebAppFW45Sampled\App";

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                TestWebApplicationDestPath);
            applicationDirectory = Path.GetFullPath(applicationDirectory);
            Trace.WriteLine("Application directory:" + applicationDirectory);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 4321,
                    })
                {
                    TelemetryListenerPort = 4008,
                    // AttachDebugger = Debugger.IsAttached,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
                });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.StopWebAppHost();
        }

        [TestMethod]        
        public void TestWebApiRequestWithExceptionSampledCustomErrorsOff()
        {
            const string requestPath = "api/products/5";
            
            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            // create a numer of requests
            const int RequestsToGenerate = 25;

            for (int i = 0; i < RequestsToGenerate; i++)
            {
                Trace.WriteLine("Executing request " + i.ToString(CultureInfo.InvariantCulture) + "...");

                var asyncTask = HttpClient.GetStringAsync(requestPath);

                try
                {
                    // wait a lot longer on first request giving IIS time to warm up the application
                    bool completed = asyncTask.Wait(TimeoutInMs);
                    Assert.Fail("Task was supposed to fail with 500. 'Task completed' flag is set to " + completed.ToString());
                }
                catch (AggregateException exp)
                {
                    Trace.WriteLine(exp.InnerException.Message);
                }

                Trace.WriteLine("Request " + i.ToString(CultureInfo.InvariantCulture) + " completed");
            }

            Envelope[] items = null;

            items = Listener.ReceiveAllItemsDuringTime(TestListenerWaitTimeInMs);

            // split items into requests and exceptions
            Envelope[] requests = items.Where(item => (item is TelemetryItem<RequestData>)).ToArray();
            Envelope[] exceptions = items.Where(item => (item is TelemetryItem<ExceptionData>)).ToArray();

            // must capture the same number of requests and exceptions
            // which is lower than # of requests produced due to sampling
            Assert.IsTrue(requests.Length > 0, "Number of request telemetry items captured must be > 0");
            Assert.IsTrue(requests.Length < RequestsToGenerate, "Number of request telemetry items captured must be < actual requests generated");

            Assert.AreEqual(requests.Length, exceptions.Length, "Number of exceptions captured must be equal to number of requests");

            // check each request has corresponding exception
            foreach (var request in requests)
            {
                Assert.IsTrue(exceptions.Any(ex => ex.tags[new ContextTagKeys().OperationId] == request.tags[new ContextTagKeys().OperationId]));
            }

            // check each exception has corresponding request
            foreach (var exception in exceptions)
            {
                Assert.IsTrue(requests.Any(r => r.tags[new ContextTagKeys().OperationId] == exception.tags[new ContextTagKeys().OperationId]));
            }

            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);
        }
    }
}