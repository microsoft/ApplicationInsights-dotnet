namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
    using Microsoft.ApplicationInsights.Extensibility.W3C;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// .NET Core specific tests that verify Http Dependencies are collected for outgoing request
    /// </summary>
    [TestClass]
    public class DependencyTrackingTelemetryModuleTestNetCore
    {
        private const string IKey = "F8474271-D231-45B6-8DD4-D344C309AE69";
        private const string FakeProfileApiEndpoint = "https://dc.services.visualstudio.com/v2/track";
        private const string localhostUrl = "http://localhost:5050";
        private const string expectedAppId = "cid-v1:someAppId";

        private readonly OperationDetailsInitializer operationDetailsInitializer = new OperationDetailsInitializer();
        private readonly DictionaryApplicationIdProvider appIdProvider = new DictionaryApplicationIdProvider();
        private StubTelemetryChannel channel;
        private TelemetryConfiguration config;
        private List<DependencyTelemetry> sentTelemetry;

        /// <summary>
        /// Initialize.
        /// </summary>
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

            this.appIdProvider.Defined = new Dictionary<string, string>
            {
                [IKey] = expectedAppId
            };

            this.config = new TelemetryConfiguration
            {
                InstrumentationKey = IKey,
                TelemetryChannel = this.channel,
                ApplicationIdProvider = this.appIdProvider
            };

            this.config.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.config.TelemetryInitializers.Add(this.operationDetailsInitializer);
        }

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
        public async Task TestDependencyCollectionWithLegacyHeaders()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableLegacyCorrelationHeadersInjection = true;
                module.Initialize(this.config);

                var url = new Uri(localhostUrl);
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                using (new LocalServer(localhostUrl))
                {
                    await new HttpClient().SendAsync(request);
                }

                // DiagnosticSource Response event is fired after SendAsync returns on netcoreapp1.*
                // let's wait until dependency is collected
                Assert.IsTrue(SpinWait.SpinUntil(() => this.sentTelemetry != null, TimeSpan.FromSeconds(1)));

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, request, true, "200", true);
            }
        }

        /// <summary>
        /// Tests that dependency is collected properly when there is no parent activity.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionNoParentActivity()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.config);

                var url = new Uri(localhostUrl);
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                using (new LocalServer(localhostUrl))
                {
                    await new HttpClient().SendAsync(request);
                }

                // DiagnosticSource Response event is fired after SendAsync returns on netcoreapp1.*
                // let's wait until dependency is collected
                Assert.IsTrue(SpinWait.SpinUntil(() => this.sentTelemetry != null, TimeSpan.FromSeconds(1)));

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, request, true, "200", false);
            }
        }

        /// <summary>
        /// Tests that dependency is collected properly when there is parent activity.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionWithParentActivity()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.config);

                var parent = new Activity("parent").AddBaggage("k", "v").SetParentId("|guid.").Start();

                var url = new Uri(localhostUrl);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                using (new LocalServer(localhostUrl))
                {
                    await new HttpClient().SendAsync(request);
                }

                // DiagnosticSource Response event is fired after SendAsync returns on netcoreapp1.*
                // let's wait until dependency is collected
                Assert.IsTrue(SpinWait.SpinUntil(() => this.sentTelemetry != null, TimeSpan.FromSeconds(1)));

                parent.Stop();

                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), url, request, true, "200", false, true, parent);

                Assert.AreEqual("k=v", request.Headers.GetValues(RequestResponseHeaders.CorrelationContextHeader).Single());
            }
        }

        /// <summary>
        /// Tests dependency collection when request procession causes exception (DNS issue).              
        /// </summary>
        [TestMethod]
        
        public async Task TestDependencyCollectionDnsIssue()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(this.config);

                var url = $"http://{Guid.NewGuid()}:5050";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await new HttpClient().SendAsync(request).ContinueWith(t => { });
                // As DNS Resolution itself failed, no response expected
                this.ValidateTelemetryForDiagnosticSource(this.sentTelemetry.Single(), new Uri(url), request, false, "Faulted", false, responseExpected: false);
            }
        }

        /// <summary>
        /// Tests that dependency is collected properly when there is parent activity.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionWithW3CHeadersAndRequestId()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableW3CHeadersInjection = true;
                this.config.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
                module.Initialize(this.config);

                var parent = new Activity("parent")
                    .AddBaggage("k", "v")
                    .SetParentId("|guid.")
                    .Start()
                    .GenerateW3CContext();
                parent.SetTracestate("state=some");
                var url = new Uri(localhostUrl);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                using (new LocalServer(localhostUrl))
                {
                    await new HttpClient().SendAsync(request);
                }

                // DiagnosticSource Response event is fired after SendAsync returns on netcoreapp1.*
                // let's wait until dependency is collected
                Assert.IsTrue(SpinWait.SpinUntil(() => this.sentTelemetry.Count > 0, TimeSpan.FromSeconds(1)));

                parent.Stop();

                string expectedTraceId = parent.GetTraceId();
                string expectedParentId = parent.GetSpanId();

                DependencyTelemetry dependency = this.sentTelemetry.Single();
                Assert.AreEqual(expectedTraceId, dependency.Context.Operation.Id);
                Assert.AreEqual($"|{expectedTraceId}.{expectedParentId}.", dependency.Context.Operation.ParentId);

                Assert.IsTrue(request.Headers.Contains(W3C.W3CConstants.TraceParentHeader));

                var dependencyIdParts = dependency.Id.Split('.', '|');
                Assert.AreEqual(4, dependencyIdParts.Length);

                Assert.AreEqual(expectedTraceId, dependencyIdParts[1]);
                Assert.AreEqual($"00-{expectedTraceId}-{dependencyIdParts[2]}-02", request.Headers.GetValues(W3C.W3CConstants.TraceParentHeader).Single());

                Assert.IsTrue(request.Headers.Contains(W3C.W3CConstants.TraceStateHeader));
                Assert.AreEqual($"{W3C.W3CConstants.AzureTracestateNamespace}={expectedAppId},state=some", request.Headers.GetValues(W3C.W3CConstants.TraceStateHeader).Single());

                Assert.IsTrue(request.Headers.Contains(RequestResponseHeaders.CorrelationContextHeader));
                Assert.AreEqual("k=v", request.Headers.GetValues(RequestResponseHeaders.CorrelationContextHeader).Single());

                Assert.AreEqual("v", dependency.Properties["k"]);
                Assert.AreEqual("state=some", dependency.Properties[W3C.W3CConstants.TracestateTag]);

                Assert.IsTrue(dependency.Properties.ContainsKey(W3C.W3CConstants.LegacyRequestIdProperty));
                Assert.IsTrue(dependency.Properties[W3C.W3CConstants.LegacyRequestIdProperty].StartsWith("|guid."));
            }
        }

        /// <summary>
        /// Tests that dependency is collected properly when there is parent activity.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionWithW3CHeadersAndNoParentContext()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableW3CHeadersInjection = true;
                this.config.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
                module.Initialize(this.config);

                var parent = new Activity("parent")
                    .Start();

                var url = new Uri(localhostUrl);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                using (new LocalServer(localhostUrl))
                {
                    await new HttpClient().SendAsync(request);
                }

                // DiagnosticSource Response event is fired after SendAsync returns on netcoreapp1.*
                // let's wait until dependency is collected
                Assert.IsTrue(SpinWait.SpinUntil(() => this.sentTelemetry != null, TimeSpan.FromSeconds(1)));

                parent.Stop();

                string expectedTraceId = parent.GetTraceId();
                string expectedParentId = parent.GetSpanId();

                DependencyTelemetry dependency = this.sentTelemetry.Single();
                Assert.AreEqual(expectedTraceId, dependency.Context.Operation.Id);
                Assert.AreEqual($"|{expectedTraceId}.{expectedParentId}.", dependency.Context.Operation.ParentId);

                Assert.IsTrue(request.Headers.Contains(W3C.W3CConstants.TraceParentHeader));

                var dependencyIdParts = dependency.Id.Split('.', '|');
                Assert.AreEqual(4, dependencyIdParts.Length);

                Assert.AreEqual(expectedTraceId, dependencyIdParts[1]);
                Assert.AreEqual($"00-{expectedTraceId}-{dependencyIdParts[2]}-02", request.Headers.GetValues(W3C.W3CConstants.TraceParentHeader).Single());

                Assert.IsTrue(request.Headers.Contains(W3C.W3CConstants.TraceStateHeader));
                Assert.AreEqual($"{W3C.W3CConstants.AzureTracestateNamespace}={expectedAppId}", request.Headers.GetValues(W3C.W3CConstants.TraceStateHeader).Single());

                Assert.IsTrue(dependency.Properties.ContainsKey(W3C.W3CConstants.LegacyRequestIdProperty));
                Assert.IsTrue(dependency.Properties[W3C.W3CConstants.LegacyRequestIdProperty].StartsWith(parent.Id));
            }
        }

        /// <summary>
        /// Tests that dependency is collected properly when there is parent activity.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public async Task TestDependencyCollectionWithW3CHeadersWithState()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.EnableW3CHeadersInjection = true;
                this.config.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
                module.Initialize(this.config);

                var parent = new Activity("parent")
                    .Start()
                    .GenerateW3CContext();

                parent.SetTracestate("some=state");

                var url = new Uri(localhostUrl);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                using (new LocalServer(localhostUrl))
                {
                    await new HttpClient().SendAsync(request);
                }

                // DiagnosticSource Response event is fired after SendAsync returns on netcoreapp1.*
                // let's wait until dependency is collected
                Assert.IsTrue(SpinWait.SpinUntil(() => this.sentTelemetry != null, TimeSpan.FromSeconds(1)));

                parent.Stop();

                var traceState = HttpHeadersUtilities.GetHeaderValues(request.Headers, W3C.W3CConstants.TraceStateHeader).First();
                Assert.AreEqual($"{W3C.W3CConstants.AzureTracestateNamespace}={expectedAppId},some=state", traceState);
            }
        }

        private void ValidateTelemetryForDiagnosticSource(DependencyTelemetry item, Uri url, HttpRequestMessage request, bool success, string resultCode, bool expectLegacyHeaders, bool responseExpected = true, Activity parent = null)
        {
            Assert.AreEqual(url, item.Data);
            Assert.AreEqual($"{url.Host}:{url.Port}", item.Target);
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
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), "rdddsc:"),
                item.Context.GetInternalContext().SdkVersion);

            var requestId = item.Id;
            Assert.IsTrue(requestId.StartsWith('|' + item.Context.Operation.Id + '.'));

            if (parent == null)
            {
                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331 TODO)
                Assert.AreEqual(32, item.Context.Operation.Id.Length);
                Assert.IsTrue(Regex.Match(item.Context.Operation.Id, @"[a-z][0-9]").Success);
                // end of workaround test
            }
            else
            {
                Assert.AreEqual(parent.RootId, item.Context.Operation.Id);
            }

            if (request != null)
            {
                Assert.AreEqual(requestId, request.Headers.GetValues(RequestResponseHeaders.RequestIdHeader).Single());
                if (expectLegacyHeaders)
                {
                    Assert.AreEqual(item.Context.Operation.Id, request.Headers.GetValues(RequestResponseHeaders.StandardRootIdHeader).Single());
                    Assert.AreEqual(requestId, request.Headers.GetValues(RequestResponseHeaders.StandardParentIdHeader).Single());
                }
                else
                {
                    Assert.IsFalse(request.Headers.Contains(RequestResponseHeaders.StandardRootIdHeader));
                    Assert.IsFalse(request.Headers.Contains(RequestResponseHeaders.StandardParentIdHeader));   
                }
            }

            // Validate the http request was captured
            this.operationDetailsInitializer.ValidateOperationDetailsCore(item, responseExpected);
        }

        private sealed class LocalServer : IDisposable
        {
            private readonly IWebHost host;
            private readonly CancellationTokenSource cts;

            public LocalServer(string url)
            {
                this.cts = new CancellationTokenSource();
                this.host = new WebHostBuilder()
                    .UseKestrel()
                    .UseStartup<Startup>()
                    .UseUrls(url)
                    .Build();

                Task.Run(() => this.host.Run(this.cts.Token));
            }

            public void Dispose()
            {
                this.cts.Cancel(false);
                try
                {
                    this.host.Dispose();
                }
                catch (Exception)
                {
                    // ignored, see https://github.com/aspnet/KestrelHttpServer/issues/1513
                    // Kestrel 2.0.0 should have fix it, but it does not seem important for our tests
                }
            }

            private class Startup
            {
                public void Configure(IApplicationBuilder app)
                {
                    app.Run(async (context) =>
                    {
                        await context.Response.WriteAsync("Hello World!");
                    });
                }
            }
        }
    }
}
