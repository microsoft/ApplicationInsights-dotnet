namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// DependencyTrackingTelemetryModule .Net 4.6 specific tests. 
    /// </summary>
    [TestClass]
    public class DependencyTrackingTelemetryModuleTestNet46
    {
        private const string IKey = "F8474271-D231-45B6-8DD4-D344C309AE69";
        private const string FakeProfileApiEndpoint = "https://dc.services.visualstudio.com/v2/track";
        private StubTelemetryChannel channel;
        private TelemetryConfiguration config;
        private List<DependencyTelemetry> sentTelemetry;

        [TestInitialize]
        public void Initialize()
        {
            this.sentTelemetry = new List<DependencyTelemetry>();

            this.channel = new StubTelemetryChannel
            {
                OnSend = telemetry =>
                {
                    // The correlation id lookup service also makes http call, just make sure we skip that
                    DependencyTelemetry depTelemetry = telemetry as DependencyTelemetry;
                    if (depTelemetry != null)
                    {
                        this.sentTelemetry.Add(depTelemetry);
                    }
                },
                EndpointAddress = FakeProfileApiEndpoint
            };

            this.config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = this.channel
            };

            this.config.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = false;
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceNoParentActivity()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);
                Assert.IsTrue(DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated);

                var url = new Uri("https://www.bing.com/");
                HttpWebRequest request = WebRequest.CreateHttp(url);
                using (request.GetResponse())
                {
                }

                var dependency = this.sentTelemetry.Single();
                this.ValidateTelemetryForDiagnosticSource(dependency, url, request, true, "200");
                Assert.IsNull(request.Headers[RequestResponseHeaders.CorrelationContextHeader]);
                Assert.AreEqual(null, dependency.Context.Operation.ParentId);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSource()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.DisableDiagnosticSourceInstrumentation = true;
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);
                Assert.IsFalse(DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated);

                var url = new Uri("https://www.bing.com/");
                HttpWebRequest request = WebRequest.CreateHttp(url);
                using (request.GetResponse())
                {
                }

                this.ValidateTelemetryForEventSource(this.sentTelemetry.Single(), url, request, true, "200");
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSource404()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.DisableDiagnosticSourceInstrumentation = true;
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);
                Assert.IsFalse(DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated);

                var url = new Uri("http://google.com/404");
                HttpClient httpClient = new HttpClient();
                httpClient.GetStringAsync(url).ContinueWith(t => { }).Wait();

                this.ValidateTelemetryForEventSource(this.sentTelemetry.Single(), url, null, false, "404");
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSource404()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri("http://google.com/404");
                HttpClient httpClient = new HttpClient();
                httpClient.GetStringAsync(url).ContinueWith(t => { }).Wait();

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, null, false, "404");
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestNoDependencyCollectionDiagnosticSourceNoResponseClose()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri("https://www.bing.com/");
                HttpWebRequest request = WebRequest.CreateHttp(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Assert.IsFalse(this.sentTelemetry.Any());
                var requestId = request.Headers[RequestResponseHeaders.RequestIdHeader];
                var rootId = request.Headers[RequestResponseHeaders.StandardRootIdHeader];
                Assert.IsNotNull(requestId);
                Assert.IsNotNull(rootId);
                Assert.AreEqual(requestId, request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                Assert.IsTrue(requestId.StartsWith('|' + rootId + '.'));
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceWithParentActivity()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri("https://www.bing.com/");
                HttpWebRequest request = WebRequest.CreateHttp(url);

                var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();
                using (request.GetResponse())
                {
                }

                parent.Stop();

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, request, true, "200");

                Assert.AreEqual("k=v", request.Headers[RequestResponseHeaders.CorrelationContextHeader]);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionFailedRequestDiagnosticSource()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri($"http://{Guid.NewGuid()}/");
                HttpWebRequest request = WebRequest.CreateHttp(url);
                try
                {
                    using (request.GetResponse())
                    {
                    }
                }
                catch (WebException)
                {
                }

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, request, false, string.Empty);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionCanceledRequestDiagnosticSource()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri("https://bing.com/");
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Timeout = 5;
                try
                {
                    using (request.GetResponse())
                    {
                    }
                }
                catch (WebException)
                {
                }

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, request, false, string.Empty);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        [Ignore] // enable with DiagnosticSource version 4.4.0-preview2*
        public void OnBeginOnEndAreNotCalledForAppInsightsUrl()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                using (var listener = new TestEventListener())
                {
                    listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, DependencyCollectorEventSource.Keywords.RddEventKeywords);

                    new HttpClient().GetAsync(FakeProfileApiEndpoint).ContinueWith(t => { }).Wait();

                    foreach (var message in listener.Messages)
                    {
                        Assert.IsFalse(message.EventId == 27 || message.EventId == 28);
                        Assert.IsFalse(message.Message.Contains("HttpDesktopDiagnosticSourceListener: Begin callback called for id"));
                        Assert.IsFalse(message.Message.Contains("HttpDesktopDiagnosticSourceListener: End callback called for id"));
                    }
                }
            }
        }

        private void ValidateTelemetryForDiagnosticSource(DependencyTelemetry item, Uri url, WebRequest request, bool success, string resultCode)
        {
            Assert.AreEqual(url, item.Data);
            Assert.AreEqual(url.Host, item.Target);
            Assert.AreEqual("GET " + url.AbsolutePath, item.Name);
            Assert.IsTrue(item.Duration > TimeSpan.FromMilliseconds(0), "Duration has to be positive");
            Assert.AreEqual(RemoteDependencyConstants.HTTP, item.Type, "HttpAny has to be dependency kind as it includes http and azure calls");
            Assert.IsTrue(
                item.Timestamp.UtcDateTime < DateTime.UtcNow.AddMilliseconds(20), // DateTime.UtcNow precesion is ~16ms
                "timestamp < now");
            Assert.IsTrue(
                item.Timestamp.UtcDateTime > DateTime.UtcNow.AddSeconds(-5), 
                "timestamp > now - 5 sec");

            Assert.AreEqual(resultCode, item.ResultCode);
            Assert.AreEqual(success, item.Success);
            Assert.AreEqual(
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), "rdddsd:"),
                item.Context.GetInternalContext().SdkVersion);

            var requestId = item.Id;
            Assert.IsTrue(requestId.StartsWith('|' + item.Context.Operation.Id + '.'));
            if (request != null)
            {
                Assert.AreEqual(requestId, request.Headers[RequestResponseHeaders.RequestIdHeader]);
                Assert.AreEqual(requestId, request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                Assert.AreEqual(item.Context.Operation.Id, request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
            }
        }

        private void ValidateTelemetryForEventSource(DependencyTelemetry item, Uri url, WebRequest request, bool success, string resultCode)
        {
            Assert.AreEqual(url, item.Data);
            Assert.AreEqual(url.Host, item.Target);
            Assert.AreEqual(url.AbsolutePath, item.Name);
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
            Assert.AreEqual(resultCode, item.ResultCode);
            Assert.AreEqual(success, item.Success);
            Assert.AreEqual(
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), "rddf:"),
                item.Context.GetInternalContext().SdkVersion);

            var requestId = item.Id;
            Assert.IsTrue(requestId.StartsWith('|' + item.Context.Operation.Id + '.'));
            if (request != null)
            {
                Assert.IsNull(request.Headers[RequestResponseHeaders.RequestIdHeader]);
                Assert.IsNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                Assert.IsNull(request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
            }
        }
    }
}