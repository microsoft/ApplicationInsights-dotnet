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
        private const string LocalhostUrlDiagSource = "http://localhost:8088/";
        private const string LocalhostUrlEventSource = "http://localhost:8089/";

        private StubTelemetryChannel channel;
        private TelemetryConfiguration config;
        private List<DependencyTelemetry> sentTelemetry;

        [TestInitialize]
        public void Initialize()
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
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
            GC.Collect();
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestBasicDependencyCollectionDiagnosticSource()
        {
            this.TestCollectionSuccessfulResponse(true, LocalhostUrlDiagSource, 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestBasicDependencyCollectionDiagnosticSourceLegacyHeaders()
        {
            this.TestCollectionSuccessfulResponse(true, LocalhostUrlDiagSource, 200, true);
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestZeroContentResponseDiagnosticSource()
        {
            await this.TestCollectionHttpClientSuccessfulResponse(LocalhostUrlDiagSource, 200, 0);
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestZeroAndNonZeroContentResponseDiagnosticSource()
        {
            await this.TestZeroContentResponseAfterNonZeroResponse(LocalhostUrlDiagSource, 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceWithParentActivity()
        {
            var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();
            this.TestCollectionSuccessfulResponse(true, LocalhostUrlDiagSource, 200);
            parent.Stop();
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSourceWithParentActivity()
        {
            var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();
            this.TestCollectionSuccessfulResponse(false, LocalhostUrlEventSource, 200);
            parent.Stop();
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestBasicDependencyCollectionEventSource()
        {
            this.TestCollectionSuccessfulResponse(false, LocalhostUrlEventSource, 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSourceNonSuccessStatusCode()
        {
            this.TestCollectionSuccessfulResponse(false, LocalhostUrlEventSource, 404);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceNonSuccessStatusCode()
        {
            this.TestCollectionSuccessfulResponse(true, LocalhostUrlDiagSource, 404);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestNoDependencyCollectionDiagnosticSourceNoResponseClose()
        {
            using (this.CreateDependencyTrackingModule(true))
            {
                HttpWebRequest request = WebRequest.CreateHttp(LocalhostUrlDiagSource);

                using (new LocalServer(LocalhostUrlDiagSource))
                {
                    request.GetResponse();
                }

                // HttpDesktopDiagnosticListener cannot collect dependencies if HttpWebResponse was not closed/disposed
                Assert.IsFalse(this.sentTelemetry.Any());
                var requestId = request.Headers[RequestResponseHeaders.RequestIdHeader];
                Assert.IsNotNull(requestId);
                Assert.IsNull(request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
                Assert.IsNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
            }
        }

        // https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/769
        [TestMethod]
        [Timeout(5000)]
        public void TestNoDependencyCollectionDiagnosticSourceInitializedAfterEndpointCached()
        {
            // first time call endpoint before Dependency Collector initialization
            // Use event source Url to make sure endpoint was not hooked by prev tests
            HttpWebRequest request1 = WebRequest.CreateHttp(LocalhostUrlEventSource);
            using (new LocalServer(LocalhostUrlEventSource))
            {
                request1.GetResponse().Dispose();
            }

            // initialize dependency collector
            using (this.CreateDependencyTrackingModule(true))
            {
                HttpWebRequest request2 = WebRequest.CreateHttp(LocalhostUrlEventSource);

                using (new LocalServer(LocalhostUrlEventSource))
                {
                    request2.GetResponse().Dispose();
                }

                // HttpDesktopDiagnosticListener may not collect dependencies if endpoint was recently called before 
                // dependency collection was initialized
                Assert.Inconclusive("At this point dependency may or may not be collected");
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task TestDependencyCollectionDnsIssueRequestDiagnosticSource()
        {
            await this.TestCollectionDnsIssue(true);
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task TestDependencyCollectionDnsIssueRequestEventSource()
        {
            await this.TestCollectionDnsIssue(false);
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionCanceledRequestDiagnosticSource()
        {
            await this.TestCollectionCanceledRequest(true, LocalhostUrlDiagSource);
        }

        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionCanceledRequestEventSource()
        {
            await this.TestCollectionCanceledRequest(false, LocalhostUrlEventSource);
        }

        [TestMethod]
        [Timeout(5000)]
        public void OnBeginOnEndAreNotCalledForAppInsightsUrl()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
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
            this.TestCollectionPostRequests(true, LocalhostUrlDiagSource);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectorPostRequestsAreCollectedEventSource()
        {
            this.TestCollectionPostRequests(false, LocalhostUrlEventSource);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestHttpRequestsWithQueryStringAreCollectedDiagnosticSource()
        {
            this.TestCollectionSuccessfulResponse(true, LocalhostUrlDiagSource + "123?q=123", 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestHttpRequestsWithQueryStringAreCollectedEventSource()
        {
            this.TestCollectionSuccessfulResponse(false, LocalhostUrlEventSource + "123?q=123", 200);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionDiagnosticSourceRedirect()
        {
            this.TestCollectionResponseWithRedirects(true, LocalhostUrlDiagSource);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestDependencyCollectionEventSourceRedirect()
        {
            this.TestCollectionResponseWithRedirects(false, LocalhostUrlEventSource);
        }

        private void TestCollectionPostRequests(bool enableDiagnosticSource, string url)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "POST";
                request.ContentLength = 1;

                using (new LocalServer(url))
                {
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(new byte[1], 0, 1);
                        stream.Close();
                    }

                    using (request.GetResponse())
                    {
                    }
                }

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), new Uri(url), request, true, "200");
            }
        }

        private void TestCollectionResponseWithRedirects(bool enableDiagnosticSource, string url)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);

                int count = 0;
                Action<HttpListenerContext> onRequest = (context) =>
                {
                    if (count == 0)
                    {
                        context.Response.StatusCode = 302;
                        context.Response.RedirectLocation = url;
                    }
                    else
                    {
                        context.Response.StatusCode = 200;
                    }

                    count++;
                };

                using (new LocalServer(url, onRequest))
                {
                    using (request.GetResponse())
                    {
                    }
                }

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), new Uri(url), request, true, "200");
            }
        }

        private void TestCollectionSuccessfulResponse(bool enableDiagnosticSource, string url, int statusCode, bool injectLegacyHeaders = false)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource, injectLegacyHeaders))
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);

                using (new LocalServer(
                    new Uri(url).GetLeftPart(UriPartial.Authority) + "/",
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

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), new Uri(url), request, statusCode >= 200 && statusCode < 300, statusCode.ToString(CultureInfo.InvariantCulture), injectLegacyHeaders);
            }
        }

        private async Task TestCollectionHttpClientSuccessfulResponse(string url, int statusCode, int contentLength, bool injectLegacyHeaders = false)
        {
            using (this.CreateDependencyTrackingModule(true))
            {
                using (HttpClient client = new HttpClient())
                using (new LocalServer(
                    url, 
                    context =>
                    {
                        context.Response.ContentLength64 = contentLength;
                        context.Response.StatusCode = statusCode;
                    }))
                {
                    try
                    {
                        using (HttpResponseMessage response = await client.GetAsync(url))
                        {
                            Assert.AreEqual(0, response.Content.Headers.ContentLength);
                        }
                    }
                    catch (WebException)
                    {
                        // ignore and let ValidateTelemetry method check status code
                    }
                }

                this.ValidateTelemetry(true, this.sentTelemetry.Single(), new Uri(url), null, statusCode >= 200 && statusCode < 300, statusCode.ToString(CultureInfo.InvariantCulture));
            }
        }

        private async Task TestZeroContentResponseAfterNonZeroResponse(string url, int statusCode)
        {
            using (this.CreateDependencyTrackingModule(true))
            {
                using (HttpClient client = new HttpClient())
                {
                    using (new LocalServer(
                        url,
                        context =>
                        {
                            context.Response.ContentLength64 = 1;
                            context.Response.StatusCode = statusCode;
                            context.Response.OutputStream.WriteByte(0x1);
                            context.Response.OutputStream.Close();
                        }))
                    {
                        try
                        {
                            using (HttpResponseMessage response = await client.GetAsync(url))
                            {
                                Assert.AreEqual(1, response.Content.Headers.ContentLength);
                            }
                        }
                        catch (WebException)
                        {
                            // ignore and let ValidateTelemetry method check status code
                        }
                    }

                    using (new LocalServer(
                        url,
                        context =>
                        {
                            context.Response.ContentLength64 = 0;
                            context.Response.StatusCode = statusCode;
                        }))
                    {
                        try
                        {
                            using (HttpResponseMessage response = await client.GetAsync(url))
                            {
                                Assert.AreEqual(0, response.Content.Headers.ContentLength);
                            }
                        }
                        catch (WebException)
                        {
                            // ignore and let ValidateTelemetry method check status code
                        }
                    }
                }

                Assert.AreEqual(2, this.sentTelemetry.Count);
                this.ValidateTelemetry(true, this.sentTelemetry.Last(), new Uri(url), null, statusCode >= 200 && statusCode < 300, statusCode.ToString(CultureInfo.InvariantCulture));
            }
        }

        private async Task TestCollectionCanceledRequest(bool enableDiagnosticSource, string url)
        {
            using (this.CreateDependencyTrackingModule(enableDiagnosticSource))
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                HttpClient httpClient = new HttpClient();

                using (new LocalServer(
                    url,
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

                this.ValidateTelemetry(enableDiagnosticSource, this.sentTelemetry.Single(), new Uri(url), null, false, string.Empty);
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

        private void ValidateTelemetry(bool diagnosticSource, DependencyTelemetry item, Uri url, WebRequest request, bool success, string resultCode, bool expectLegacyHeaders = false)
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
                this.ValidateTelemetryForDiagnosticSource(item, url, request, expectLegacyHeaders);
            }
            else
            {
                this.ValidateTelemetryForEventSource(item, url, request);
            }
        }

        private void ValidateTelemetryForDiagnosticSource(DependencyTelemetry item, Uri url, WebRequest request, bool expectLegacyHeaders)
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

                if (expectLegacyHeaders)
                {
                    Assert.AreEqual(requestId, request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                    Assert.AreEqual(item.Context.Operation.Id, request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
                }
                else
                {
                    Assert.IsNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                    Assert.IsNull(request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
                }

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

        private DependencyTrackingTelemetryModule CreateDependencyTrackingModule(bool enableDiagnosticSource, bool injectLegacyHeaders = false)
        {
            var module = new DependencyTrackingTelemetryModule();

            if (!enableDiagnosticSource)
            {
                module.DisableDiagnosticSourceInstrumentation = true;
            }

            module.EnableLegacyCorrelationHeadersInjection = injectLegacyHeaders;

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

                            context.Response.OutputStream.Close();
                            context.Response.Close();
                        }
                    },
                    this.cts.Token);
            }

            public void Dispose()
            {
                this.cts.Cancel(false);
                this.listener.Abort();
                ((IDisposable)this.listener).Dispose();
                this.cts.Dispose();
            }
        }
    }
}