namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// DependencyTrackingTelemetryModule .Net 4.6 specific tests. 
    /// </summary>
    [TestClass]
    public class DependencyTrackingTelemetryModuleTestNet46
    {
        private const string IKey = "F8474271-D231-45B6-8DD4-D344C309AE69";
        private const string FakeProfileApiEndpoint = "http://www.microsoft.com";

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionNoParentActivity()
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
                }
            };

            var config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = channel
            };

            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(config);

                var url = new Uri("http://bing.com");
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.GetResponse();

                Assert.IsNotNull(sentTelemetry);
                var item = (DependencyTelemetry)sentTelemetry;
                Assert.AreEqual(url, item.Data);
                Assert.AreEqual(url.Host, item.Target);
                Assert.AreEqual("GET " + url.AbsolutePath, item.Name);
                Assert.IsTrue(item.Duration > TimeSpan.FromMilliseconds(0), "Duration has to be positive");
                Assert.AreEqual(RemoteDependencyConstants.HTTP, item.Type,  "HttpAny has to be dependency kind as it includes http and azure calls");
                Assert.IsTrue(
                    DateTime.UtcNow.Subtract(item.Timestamp.UtcDateTime).TotalMilliseconds <
                    TimeSpan.FromMinutes(1).TotalMilliseconds, 
                    "timestamp < now");
                Assert.IsTrue(
                    item.Timestamp.Subtract(DateTime.UtcNow).TotalMilliseconds >
                    -TimeSpan.FromMinutes(1).TotalMilliseconds, 
                    "now - 1 min < timestamp");

                var requestId = item.Id;
                Assert.AreEqual(requestId, request.Headers["Request-Id"]);
                Assert.AreEqual(requestId, request.Headers["x-ms-request-id"]);
                Assert.IsTrue(requestId.StartsWith('|' + item.Context.Operation.Id + '.'));

                Assert.IsNull(request.Headers["Correlation-Context"]);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionWithParentActivity()
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
                }
            };

            var config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = channel
            };

            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(config);

                var url = new Uri("http://bing.com");
                HttpWebRequest request = WebRequest.CreateHttp(url);

                var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();
                request.GetResponse();
                parent.Stop();

                Assert.IsNotNull(sentTelemetry);
                var item = (DependencyTelemetry)sentTelemetry;

                var requestId = item.Id;
                Assert.AreEqual(requestId, request.Headers["Request-Id"]);
                Assert.AreEqual(requestId, request.Headers["x-ms-request-id"]);
                Assert.IsTrue(requestId.StartsWith(parent.Id));
                Assert.AreNotEqual(parent.Id, requestId);

                Assert.AreEqual("k=v", request.Headers["Correlation-Context"]);
            }
        }
    }
}