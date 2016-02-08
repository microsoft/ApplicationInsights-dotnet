namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Web;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RequestTrackingTelemetryModuleTest
    {
        [TestMethod]
        public void OnBeginRequestDoesNotSetTimeIfItWasAssignedBefore()
        {
            var startTime = DateTimeOffset.UtcNow;

            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.StartTime = startTime;

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);

                Assert.AreEqual(startTime, requestTelemetry.Timestamp);
            }
        }

        [TestMethod]
        public void OnBeginRequestSetsTimeIfItWasNotAssignedBefore()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.StartTime = default(DateTimeOffset);

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);

                Assert.AreNotEqual(default(DateTimeOffset), requestTelemetry.Timestamp);
            }
        }

        [TestMethod]
        public void RequestIdIsAvailableAfterOnBegin()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            
            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);

                Assert.IsTrue(!string.IsNullOrEmpty(requestTelemetry.Id));
            }
        }

        [TestMethod]
        public void OnEndSetsDurationToPositiveValue()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            
            var module = new TestableRequestTrackingTelemetryModule(context);
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(null);
            module.OnEndRequest(null);

            Assert.IsTrue(context.GetRequestTelemetry().Duration.TotalMilliseconds >= 0);
        }

        [TestMethod]
        public void OnEndCreatesRequestTelemetryIfBeginWasNotCalled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnEndRequest(null);

                Assert.IsNotNull(context.GetRequestTelemetry());
            }
        }

        [TestMethod]
        public void OnEndSetsDurationToZeroIfBeginWasNotCalled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnEndRequest(null);

                Assert.AreEqual(0, context.GetRequestTelemetry().Duration.Ticks);
            }
        }

        [TestMethod]
        public void OnEndDoesNotOverrideResponseCode()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 300;

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());

                module.OnBeginRequest(null);
                var requestTelemetry = context.GetRequestTelemetry();
                requestTelemetry.ResponseCode = "Test";

                module.OnEndRequest(null);

                Assert.AreEqual("Test", requestTelemetry.ResponseCode);
            }
        }

        [TestMethod]
        public void OnEndDoesNotOverrideUrl()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);
                var requestTelemetry = context.GetRequestTelemetry();
                requestTelemetry.Url = new Uri("http://test/");

                module.OnEndRequest(null);

                Assert.AreEqual("http://test/", requestTelemetry.Url.OriginalString);
            }
        }

        [TestMethod]
        public void OnEndDoesNotOverrideHttpMethod()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);
                var requestTelemetry = context.GetRequestTelemetry();
                requestTelemetry.HttpMethod = "Test";

                module.OnEndRequest(null);

                Assert.AreEqual("Test", requestTelemetry.HttpMethod);
            }
        }

        [TestMethod]
        public void OnEndSetsResponseCode()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 401;

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);
                module.OnEndRequest(null);

                Assert.AreEqual("401", context.GetRequestTelemetry().ResponseCode);
            }
        }

        [TestMethod]
        public void OnEndSetsUrl()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);
                module.OnEndRequest(null);

                Assert.AreEqual(context.Request.Url, context.GetRequestTelemetry().Url);
            }
        }

        [TestMethod]
        public void OnEndSetsHttpMethod()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            
            var module = new TestableRequestTrackingTelemetryModule(context);
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(null);
            module.OnEndRequest(null);

            Assert.AreEqual(context.Request.HttpMethod, context.GetRequestTelemetry().HttpMethod);
        }

        public void OnEndTracksRequest()
        {
            var sendItems = new List<ITelemetry>();
            var stubTelemetryChannel = new StubTelemetryChannel { OnSend = item => sendItems.Add(item) };
            var configuration = new TelemetryConfiguration
            {
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryChannel = stubTelemetryChannel
            };

            using (var module = new RequestTrackingTelemetryModule())
            {
                module.Initialize(configuration);
                module.OnBeginRequest(null);
                module.OnEndRequest(null);

                Assert.AreEqual(1, sendItems.Count);
            }
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseForDefaultHandler()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;
            context.Handler = new System.Web.Handlers.AssemblyResourceLoader();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            var module = new RequestTrackingTelemetryModule();
            module.Handlers.Add("System.Web.Handlers.AssemblyResourceLoader");
            var configuration = TelemetryConfiguration.CreateDefault();
            module.Initialize(configuration);

            Assert.IsFalse(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsTrueForUnknownHandler()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;
            context.Handler = new FakeHttpHandler();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            using (var module = new RequestTrackingTelemetryModule())
            {
                var configuration = TelemetryConfiguration.CreateDefault();
                module.Initialize(configuration);

                Assert.IsTrue(module.NeedProcessRequest(context));
            }
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseForCustomHandler()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;
            context.Handler = new FakeHttpHandler();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            using (var module = new RequestTrackingTelemetryModule())
            {
                module.Handlers.Add("Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest+FakeHttpHandler");
                var configuration = TelemetryConfiguration.CreateDefault();
                module.Initialize(configuration);

                Assert.IsFalse(module.NeedProcessRequest(context));
            }
        }

        [TestMethod]
        public void NeedProcessRequestReturnsTrueForNon200()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 500;
            context.Handler = new System.Web.Handlers.AssemblyResourceLoader();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            using (var module = new RequestTrackingTelemetryModule())
            {
                var configuration = TelemetryConfiguration.CreateDefault();
                module.Initialize(configuration);

                Assert.IsTrue(module.NeedProcessRequest(context));
            }
        }

        [TestMethod]
        public void ConstructorSetsOnBeginAsAHanderForEvent1()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                WebEventsPublisher.Log.OnBegin();

                Assert.IsNotNull(context.GetRequestTelemetry());
            }
        }

        [TestMethod]
        public void ConstructorSetsOnBeginAsAHanderForEvent2()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                WebEventsPublisher.Log.OnEnd();

                Assert.IsNotNull(context.GetRequestTelemetry());
            }
        }

        internal class FakeHttpHandler : IHttpHandler
        {
            public bool IsReusable
            {
                get { return false; }
            }

            public void ProcessRequest(System.Web.HttpContext context)
            {
            }
        }

        internal class TestableRequestTrackingTelemetryModule : RequestTrackingTelemetryModule
        {
            private readonly HttpContext context;

            public TestableRequestTrackingTelemetryModule(HttpContext context)
            {
                this.context = context;
            }

            protected override HttpContext ResolvePlatformContext()
            {
                return this.context;
            }
        }
    }
}
