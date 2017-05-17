namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

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
        private const string LocalhostUrl = "http://localhost:8088/";

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

                var url = new Uri(LocalhostUrl);
                HttpWebRequest request = WebRequest.CreateHttp(url);

                using (new LocalServer(LocalhostUrl))
                {
                    using (request.GetResponse())
                    {
                    }
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

                var url = new Uri(LocalhostUrl);
                HttpWebRequest request = WebRequest.CreateHttp(url);

                using (new LocalServer(LocalhostUrl))
                {
                    using (request.GetResponse())
                    {
                    }
                }

                this.ValidateTelemetryForEventSource(this.sentTelemetry.Single(), url, request, true, "200");
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionEventSourceNonSuccessStatusCode()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.DisableDiagnosticSourceInstrumentation = true;
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);
                Assert.IsFalse(DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated);

                HttpClient httpClient = new HttpClient();
                using (new LocalServer(
                    LocalhostUrl, 
                    ctx => 
                    {
                        // https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/548
                        // for quick unsuccesfull response OnEnd is fired too fast after begin (before it's completed)
                        // first begin may take a while because of lazy initializations and jit compiling
                        // let's wait a bit here.
                        Thread.Sleep(20);
                        ctx.Response.StatusCode = 404;
                    }))
                {
                    await httpClient.GetAsync(LocalhostUrl).ContinueWith(t => { });
                }

                this.ValidateTelemetryForEventSource(this.sentTelemetry.Single(), new Uri(LocalhostUrl), null, false, "404");
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionDiagnosticSourceNonSuccessStatusCode()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                HttpClient httpClient = new HttpClient();
                using (new LocalServer(LocalhostUrl, ctx => ctx.Response.StatusCode = 404))
                {
                    await httpClient.GetStringAsync(LocalhostUrl).ContinueWith(t => { });
                }

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), new Uri(LocalhostUrl), null, false, "404");
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

                var url = new Uri(LocalhostUrl);
                HttpWebRequest request = WebRequest.CreateHttp(url);

                using (new LocalServer(LocalhostUrl))
                {
                    request.GetResponse();
                }

                // HttpDesktopDiagnosticListener cannot collect dependencies if HttpWebResponse was not closed/disposed
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
                var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();

                var url = new Uri(LocalhostUrl);
                HttpWebRequest request = WebRequest.CreateHttp(url);

                using (new LocalServer(LocalhostUrl))
                {
                    using (request.GetResponse())
                    {
                    }
                }

                parent.Stop();

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, request, true, "200");

                Assert.AreEqual("k=v", request.Headers[RequestResponseHeaders.CorrelationContextHeader]);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionDnsIssueRequestDiagnosticSource()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri($"http://{Guid.NewGuid()}/");
                HttpClient client = new HttpClient();
                await client.GetAsync(url).ContinueWith(t => { });

                // here the start of dependency is tracked with HttpDesktopDiagnosticSourceListener, 
                // so the expected SDK version should have DiagnosticSource 'rdddsd' prefix. 
                // however the end is tracked by FrameworkHttpEventListener
                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, null, false, string.Empty);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionDnsIssueRequestEventSource()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.DisableDiagnosticSourceInstrumentation = true;
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri($"http://{Guid.NewGuid()}/");
                HttpClient client = new HttpClient();
                await client.GetAsync(url).ContinueWith(t => { });

                // https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/548
                // End is fired before Begin, so EventSource doesn't track telemetry
                Assert.IsFalse(this.sentTelemetry.Any());
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionCanceledRequestDiagnosticSource()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri(LocalhostUrl);
                CancellationTokenSource cts = new CancellationTokenSource();
                HttpClient httpClient = new HttpClient();
                using (new LocalServer(LocalhostUrl, ctx => cts.Cancel()))
                {
                    await httpClient.GetAsync(url, cts.Token).ContinueWith(t => { });
                }

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, null, false, string.Empty);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionCanceledRequestEventSource()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.DisableDiagnosticSourceInstrumentation = true;
                module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
                module.Initialize(this.config);

                var url = new Uri(LocalhostUrl);
                CancellationTokenSource cts = new CancellationTokenSource();
                HttpClient httpClient = new HttpClient();
                using (new LocalServer(
                    LocalhostUrl,
                    ctx =>
                    {
                        // https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/548
                        // for quick unsuccesfull response OnEnd is fired too fast after begin (before it's completed)
                        // first begin may take a while because of lazy initializations and jit compiling
                        // let's wait a bit here.
                        Thread.Sleep(20);
                        cts.Cancel();
                    }))
                {
                    await httpClient.GetAsync(LocalhostUrl, cts.Token).ContinueWith(t => { });
                }

                this.ValidateTelemetryForEventSource(this.sentTelemetry.Single(), url, null, false, string.Empty);
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

            if (url.Port == 80 || url.Port == 443)
            {
                Assert.AreEqual($"{url.Host}", item.Target);
            }
            else
            {
                Assert.AreEqual($"{url.Host}:{url.Port}", item.Target);
            }

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

            if (url.Port == 80 || url.Port == 443)
            {
                Assert.AreEqual($"{url.Host}", item.Target);
            }
            else
            {
                Assert.AreEqual($"{url.Host}:{url.Port}", item.Target);
            }

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

        private class LocalServer : IDisposable
        {
            private readonly HttpListener listener;
            private readonly CancellationTokenSource cts;

            public LocalServer(string url, Action<HttpListenerContext> onRequest = null)
            {
                this.listener = new HttpListener();
                this.listener.Prefixes.Add(url);
                this.listener.Start();
                this.cts = new CancellationTokenSource();

                Task.Run(
                    () =>
                    {
                        while (!this.cts.IsCancellationRequested)
                        {
                            HttpListenerContext context = this.listener.GetContext();
                            if (onRequest != null)
                            {
                                onRequest(context);
                            }
                            else
                            {
                                context.Response.StatusCode = 200;
                            }

                            context.Response.Close();
                        }
                    },
                    this.cts.Token);
            }

            public void Dispose()
            {
                this.cts.Cancel(false);
                this.listener.Stop();
            }
        }
    }
}