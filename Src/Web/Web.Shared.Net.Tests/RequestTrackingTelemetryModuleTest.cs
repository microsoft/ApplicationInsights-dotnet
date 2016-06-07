namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Web;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;

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

                Assert.Equal(startTime, requestTelemetry.Timestamp);
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

                Assert.NotEqual(default(DateTimeOffset), requestTelemetry.Timestamp);
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

                Assert.True(!string.IsNullOrEmpty(requestTelemetry.Id));
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

            Assert.True(context.GetRequestTelemetry().Duration.TotalMilliseconds >= 0);
        }

        [TestMethod]
        public void OnEndCreatesRequestTelemetryIfBeginWasNotCalled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnEndRequest(null);

                Assert.NotNull(context.GetRequestTelemetry());
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

                Assert.Equal(0, context.GetRequestTelemetry().Duration.Ticks);
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

                Assert.Equal("Test", requestTelemetry.ResponseCode);
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

                Assert.Equal("http://test/", requestTelemetry.Url.OriginalString);
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

                Assert.Equal("Test", requestTelemetry.HttpMethod);
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

                Assert.Equal("401", context.GetRequestTelemetry().ResponseCode);
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

                Assert.Equal(context.Request.Url, context.GetRequestTelemetry().Url);
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

            Assert.Equal(context.Request.HttpMethod, context.GetRequestTelemetry().HttpMethod);
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

                Assert.Equal(1, sendItems.Count);
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

            Assert.False(module.NeedProcessRequest(context));
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

                Assert.True(module.NeedProcessRequest(context));
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

                Assert.False(module.NeedProcessRequest(context));
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

                Assert.True(module.NeedProcessRequest(context));
            }
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseOnNullHttpContext()
        {
            using (var module = new RequestTrackingTelemetryModule())
            {
                Assert.False(module.NeedProcessRequest(null));
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

                Assert.NotNull(context.GetRequestTelemetry());
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

                Assert.NotNull(context.GetRequestTelemetry());
            }
        }

        [TestMethod]
        public void SdkVersionHasCorrectFormat()
        {
            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(RequestTrackingTelemetryModule), prefix: "web:");
            
            var context = HttpModuleHelper.GetFakeHttpContext();

            using (var module = new TestableRequestTrackingTelemetryModule(context))
            {
                module.Initialize(TelemetryConfiguration.CreateDefault());
                module.OnBeginRequest(null);
                module.OnEndRequest(null);

                Assert.AreEqual(expectedVersion, context.GetRequestTelemetry().Context.GetInternalContext().SdkVersion);
            }
        }

        internal class FakeHttpHandler : IHttpHandler
        {
            bool IHttpHandler.IsReusable
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
