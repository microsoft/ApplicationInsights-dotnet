namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
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
    public class DependencyTrackingTelemetryModuleHttpTest
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
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }

            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = false;
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestBasicDependencyCollectionDiagnosticSource()
        {
            this.TestCollectionSuccessfulResponse(true, new Uri(LocalhostUrl), 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceWithParentActivity()
        {
            var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();
            this.TestCollectionSuccessfulResponse(true, new Uri(LocalhostUrl), 200);
            parent.Stop();
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSourceWithParentActivity()
        {
            var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();
            this.TestCollectionSuccessfulResponse(false, new Uri(LocalhostUrl), 200);
            parent.Stop();
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestBasicDependencyCollectionEventSource()
        {
            this.TestCollectionSuccessfulResponse(false, new Uri(LocalhostUrl), 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSourceNonSuccessStatusCode()
        {
            this.TestCollectionSuccessfulResponse(false, new Uri(LocalhostUrl), 404);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceNonSuccessStatusCode()
        {
            this.TestCollectionSuccessfulResponse(true, new Uri(LocalhostUrl), 404);
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
        public async Task TestDependencyCollectionDnsIssueRequestDiagnosticSource()
        {
            await this.TestCollectionDnsIssue(true);
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionDnsIssueRequestEventSource()
        {
            await this.TestCollectionDnsIssue(false);
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionCanceledRequestDiagnosticSource()
        {
            await this.TestCollectionCanceledRequest(true);
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionCanceledRequestEventSource()
        {
            await this.TestCollectionCanceledRequest(false);
        }

        [TestMethod]
        [Timeout(5000)]
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

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectorPostRequestsAreCollectedDiagnosticSource()
        {
            this.TestCollectionPostRequests(true);
        }

        [TestMethod]
        [Timeout(500000)]
        public void TestDependencyCollectorPostRequestsAreCollectedEventSource()
        {
            this.TestCollectionPostRequests(false);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestHttpRequestsWithQueryStringAreCollectedDiagnosticSource()
        {
            this.TestCollectionSuccessfulResponse(true, new Uri(LocalhostUrl + "123?q=123"), 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestHttpRequestsWithQueryStringAreCollectedEventSource()
        {
            this.TestCollectionSuccessfulResponse(false, new Uri(LocalhostUrl + "123?q=123"), 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceRedirect()
        {
            this.TestCollectionResponseWithRedirects(true);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSourceRedirect()
        {
            this.TestCollectionResponseWithRedirects(false);
        }

        private void TestCollectionPostRequests(bool enableDiagnosticSource)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                var url = new Uri(LocalhostUrl);
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "POST";
                request.ContentLength = 1;

                using (new LocalServer(LocalhostUrl))
                {
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(new byte[1], 0, 1);
                    }

                    using (request.GetResponse())
                    {
                    }
                }

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), url, request, true, "200");
            }
        }

        private void TestCollectionResponseWithRedirects(bool enableDiagnosticSource)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                var url = new Uri(LocalhostUrl);
                HttpWebRequest request = WebRequest.CreateHttp(url);

                int count = 0;
                Action<HttpListenerContext> onRequest = (context) =>
                {
                    if (count == 0)
                    {
                        context.Response.StatusCode = 302;
                        context.Response.RedirectLocation = LocalhostUrl;
                    }
                    else
                    {
                        context.Response.StatusCode = 200;
                    }

                    count++;
                };

                using (new LocalServer(LocalhostUrl, onRequest))
                {
                    using (request.GetResponse())
                    {
                    }
                }

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), url, request, true, "200");
            }
        }

        private void TestCollectionSuccessfulResponse(bool enableDiagnosticSource, Uri url, int statusCode)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);

                using (new LocalServer(
                    LocalhostUrl,
                    ctx =>
                    {
                        if (!enableDiagnosticSource && statusCode != 200)
                        {
                            // https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/548
                            // for quick unsuccesfull response OnEnd is fired too fast after begin (before it's completed)
                            // first begin may take a while because of lazy initializations and jit compiling
                            // let's wait a bit here.
                            Thread.Sleep(20);
                        }

                        ctx.Response.StatusCode = statusCode;
                    }))
                {
                    try
                    {
                        using (request.GetResponse())
                        {
                        }
                    }
                    catch (WebException)
                    {
                        // ignore and let ValidateTelemetry method check status code
                    }
                }

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), url, request, statusCode >= 200 && statusCode < 300, statusCode.ToString(CultureInfo.InvariantCulture));
            }
        }

        private async Task TestCollectionCanceledRequest(bool enableDiagnosticSource)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                HttpClient httpClient = new HttpClient();

                var url = new Uri(LocalhostUrl);
                using (new LocalServer(
                    LocalhostUrl,
                    ctx =>
                    {
                        if (!enableDiagnosticSource)
                        {
                            // https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/548
                            // for quick unsuccesfull response OnEnd is fired too fast after begin (before it's completed)
                            // first begin may take a while because of lazy initializations and jit compiling
                            // let's wait a bit here.
                            Thread.Sleep(20);
                        }

                        cts.Cancel();
                    }))
                {
                    await httpClient.GetAsync(url, cts.Token).ContinueWith(t => { });
                }

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), url, null, false, string.Empty);
            }
        }

        private async Task TestCollectionDnsIssue(bool enableDiagnosticSource)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                var url = new Uri($"http://{Guid.NewGuid()}/");
                HttpClient client = new HttpClient();
                await client.GetAsync(url).ContinueWith(t => { });

                if (enableDiagnosticSource)
                {
                    // here the start of dependency is tracked with HttpDesktopDiagnosticSourceListener, 
                    // so the expected SDK version should have DiagnosticSource 'rdddsd' prefix. 
                    // however the end is tracked by FrameworkHttpEventListener
                    this.ValidateTelemetry(true, this.sentTelemetry.Single(), url, null, false, string.Empty);
                }
                else
                {
                    // https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/548
                    // End is fired before Begin, so EventSource doesn't track telemetry
                    Assert.IsFalse(this.sentTelemetry.Any());
                }
            }
        }

        private void ValidateTelemetry(bool diagnosticSource, DependencyTelemetry item, Uri url, WebRequest request, bool success, string resultCode)
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

            Assert.AreEqual(Activity.Current?.Id, item.Context.Operation.ParentId);
            Assert.IsTrue(item.Id.StartsWith('|' + item.Context.Operation.Id + '.'));

            if (diagnosticSource)
            {
                this.ValidateTelemetryForDiagnosticSource(item, url, request);
            }
            else
            {
                this.ValidateTelemetryForEventSource(item, url, request);
            }
        }

        private void ValidateTelemetryForDiagnosticSource(DependencyTelemetry item, Uri url, WebRequest request)
        {
            var expectedMethod = request != null ? request.Method : "GET";
            Assert.AreEqual(expectedMethod + " " + url.AbsolutePath, item.Name);

            Assert.AreEqual(
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), "rdddsd:"),
                item.Context.GetInternalContext().SdkVersion);

            var requestId = item.Id;

            if (request != null)
            {
                Assert.AreEqual(requestId, request.Headers[RequestResponseHeaders.RequestIdHeader]);
                Assert.AreEqual(requestId, request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                Assert.AreEqual(item.Context.Operation.Id, request.Headers[RequestResponseHeaders.StandardRootIdHeader]);

                if (Activity.Current != null)
                {
                    var correlationContextHeader = request.Headers[RequestResponseHeaders.CorrelationContextHeader]
                        .Split(',');

                    var baggage = Activity.Current.Baggage.Select(kvp => $"{kvp.Key}={kvp.Value}").ToArray();
                    Assert.AreEqual(baggage.Length, correlationContextHeader.Length);

                    foreach (var baggageItem in baggage)
                    {
                        Assert.IsTrue(correlationContextHeader.Contains(baggageItem));
                    }
                }
                else
                {
                    Assert.IsNull(request.Headers[RequestResponseHeaders.CorrelationContextHeader]);
                }
            }
        }

        private void ValidateTelemetryForEventSource(DependencyTelemetry item, Uri url, WebRequest request)
        {
            Assert.AreEqual(url.AbsolutePath, item.Name);

            Assert.AreEqual(
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), "rddf:"),
                item.Context.GetInternalContext().SdkVersion);

            if (request != null)
            {
                Assert.IsNull(request.Headers[RequestResponseHeaders.RequestIdHeader]);
                Assert.IsNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                Assert.IsNull(request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
                Assert.IsNull(request.Headers[RequestResponseHeaders.CorrelationContextHeader]);
            }
        }

        private DependencyTrackingTelemetryModule CreateDependencyTrackingModule(bool enableDiagnosticSource)
        {
            var module = new DependencyTrackingTelemetryModule();

            if (!enableDiagnosticSource)
            {
                module.DisableDiagnosticSourceInstrumentation = true;
            }

            module.ProfileQueryEndpoint = FakeProfileApiEndpoint;
            module.Initialize(this.config);
            Assert.AreEqual(enableDiagnosticSource, DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated);

            return module;
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