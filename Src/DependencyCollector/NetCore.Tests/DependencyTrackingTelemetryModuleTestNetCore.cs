namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// .NET Core specific tests that verify Http Dependencies are collected for outgoing request
    /// </summary>
    [TestClass]
    public class DependencyTrackingTelemetryModuleTestNetCore
    {
        private const string IKey = "F8474271-D231-45B6-8DD4-D344C309AE69";
        private const string FakeProfileApiEndpoint = ApplicationInsightsUrlFilter.TelemetryServiceEndpoint;

        /// <summary>
        /// Cleans up.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        /// <summary>
        /// Tests that dependency is collected properly when there is no parent activity.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionNoParentActivity()
        {
            ITelemetry sentTelemetry = null;

            var channel = new StubTelemetryChannel
            {
                OnSend = telemetry =>
                {
                    // The correlation id lookup service also makes http call, just make sure we skip that
                    DependencyTelemetry depTelemetry = telemetry as DependencyTelemetry;
                    if (depTelemetry != null &&
                        !depTelemetry.Data.StartsWith(FakeProfileApiEndpoint, StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.IsNull(sentTelemetry);
                        sentTelemetry = telemetry;
                    }
                },
                EndpointAddress = FakeProfileApiEndpoint
            };

            var config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = channel
            };

            config.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(config);

                var url = new Uri("http://bing.com");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await new HttpClient().SendAsync(request);

                // on netcoreapp1.0 DiagnosticSource event is fired asycronously, let's wait for it 
                Assert.IsTrue(SpinWait.SpinUntil(() => sentTelemetry != null, TimeSpan.FromSeconds(1)));

                var item = (DependencyTelemetry)sentTelemetry;
                Assert.AreEqual(url, item.Data);
                Assert.AreEqual(url.Host, item.Target);
                Assert.AreEqual("GET " + url.AbsolutePath, item.Name);
                Assert.IsTrue(item.Duration > TimeSpan.FromMilliseconds(0), "Duration has to be positive");
                Assert.AreEqual(RemoteDependencyConstants.HTTP, item.Type, "HttpAny has to be dependency kind as it includes http and azure calls");
                Assert.IsTrue(
                    DateTime.UtcNow.Subtract(item.Timestamp.UtcDateTime).TotalMilliseconds <
                    TimeSpan.FromMinutes(1).TotalMilliseconds,
                    "timestamp < now");
                Assert.IsTrue(
                    item.Timestamp.Subtract(DateTime.UtcNow).TotalMilliseconds >
                    -TimeSpan.FromMinutes(1).TotalMilliseconds,
                    "now - 1 min < timestamp");
                Assert.AreEqual("200", item.ResultCode);

                var requestId = item.Id;
                Assert.AreEqual(requestId, request.Headers.GetValues("Request-Id").Single());
                Assert.AreEqual(requestId, request.Headers.GetValues("x-ms-request-id").Single());
                Assert.IsTrue(requestId.StartsWith('|' + item.Context.Operation.Id + '.'));
            }
        }

        /// <summary>
        /// Tests that dependency is collected properly when there is parent activity.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionWithParentActivity()
        {
            ITelemetry sentTelemetry = null;

            var channel = new StubTelemetryChannel
            {
                OnSend = telemetry =>
                {
                    // The correlation id lookup service also makes http call, just make sure we skip that
                    DependencyTelemetry depTelemetry = telemetry as DependencyTelemetry;
                    if (depTelemetry != null &&
                        !depTelemetry.Data.StartsWith(FakeProfileApiEndpoint, StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.IsNull(sentTelemetry);
                        sentTelemetry = telemetry;
                    }
                },
                EndpointAddress = FakeProfileApiEndpoint
            };

            var config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = channel
            };

            config.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(config);

                var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();

                var url = new Uri("http://bing.com");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await new HttpClient().SendAsync(request);

                // on netcoreapp1.0 DiagnosticSource event is fired asycronously, let's wait for it 
                Assert.IsTrue(SpinWait.SpinUntil(() => sentTelemetry != null, TimeSpan.FromSeconds(1)));

                parent.Stop();

                var item = (DependencyTelemetry)sentTelemetry;
                Assert.AreEqual("200", item.ResultCode);

                var requestId = item.Id;
                Assert.AreEqual(requestId, request.Headers.GetValues("Request-Id").Single());
                Assert.AreEqual(requestId, request.Headers.GetValues("x-ms-request-id").Single());
                Assert.IsTrue(requestId.StartsWith(parent.Id));
                Assert.AreNotEqual(parent.Id, requestId);
                Assert.AreEqual("v", item.Context.Properties["k"]);

                Assert.AreEqual("k=v", request.Headers.GetValues("Correlation-Context").Single());
            }
        }
    }
}
