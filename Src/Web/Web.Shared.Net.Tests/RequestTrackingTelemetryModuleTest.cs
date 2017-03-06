namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Web;

    using Common;
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
        [TestInitialize]
        public void TestInitialize()
        {
            CorrelationIdLookupHelper.OverrideAppIdProvider((string endpoint, string ikey) => {

                // Pretend App Id is the same as Ikey
                var tcs = new TaskCompletionSource<string>();
                tcs.SetResult(ikey);
                return tcs.Task;
            });
        }

        [TestMethod]
        public void OnBeginRequestDoesNotSetTimeIfItWasAssignedBefore()
        {
            var startTime = DateTimeOffset.UtcNow;

            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Timestamp = startTime;

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);

            Assert.Equal(startTime, requestTelemetry.Timestamp);
        }

        [TestMethod]
        public void OnBeginRequestSetsTimeIfItWasNotAssignedBefore()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Timestamp = default(DateTimeOffset);

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);

            Assert.NotEqual(default(DateTimeOffset), requestTelemetry.Timestamp);
        }

        [TestMethod]
        public void RequestIdIsAvailableAfterOnBegin()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();

            var module = new RequestTrackingTelemetryModule();

            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);

            Assert.True(!string.IsNullOrEmpty(requestTelemetry.Id));
        }

        [TestMethod]
        public void OnEndSetsDurationToPositiveValue()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            
            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.True(context.GetRequestTelemetry().Duration.TotalMilliseconds >= 0);
        }

        [TestMethod]
        public void OnEndCreatesRequestTelemetryIfBeginWasNotCalled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnEndRequest(context);

            Assert.NotNull(context.GetRequestTelemetry());
        }

        [TestMethod]
        public void OnEndSetsDurationToZeroIfBeginWasNotCalled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnEndRequest(context);

            Assert.Equal(0, context.GetRequestTelemetry().Duration.Ticks);
        }

        [TestMethod]
        public void OnEndDoesNotOverrideResponseCode()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 300;

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            requestTelemetry.ResponseCode = "Test";

            module.OnEndRequest(context);

            Assert.Equal("Test", requestTelemetry.ResponseCode);
        }

        [TestMethod]
        public void OnEndDoesNotOverrideUrl()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            requestTelemetry.Url = new Uri("http://test/");

            module.OnEndRequest(context);

            Assert.Equal("http://test/", requestTelemetry.Url.OriginalString);
        }

        [TestMethod]
        public void OnEndSetsResponseCode()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 401;

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal("401", context.GetRequestTelemetry().ResponseCode);
        }

        [TestMethod]
        public void OnEndSetsUrl()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(context.Request.Url, context.GetRequestTelemetry().Url);
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

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(configuration);
            module.OnBeginRequest(null);
            module.OnEndRequest(null);

            Assert.Equal(1, sendItems.Count);
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

            var module = new RequestTrackingTelemetryModule();
            var configuration = TelemetryConfiguration.CreateDefault();
            module.Initialize(configuration);

            Assert.True(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseForCustomHandler()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;
            context.Handler = new FakeHttpHandler();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            var module = new RequestTrackingTelemetryModule();
            module.Handlers.Add("Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest+FakeHttpHandler");
            var configuration = TelemetryConfiguration.CreateDefault();
            module.Initialize(configuration);

            Assert.False(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsTrueForNon200()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 500;
            context.Handler = new System.Web.Handlers.AssemblyResourceLoader();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            var module = new RequestTrackingTelemetryModule();
            var configuration = TelemetryConfiguration.CreateDefault();
            module.Initialize(configuration);

            Assert.True(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseOnNullHttpContext()
        {
            var module = new RequestTrackingTelemetryModule();
            {
                Assert.False(module.NeedProcessRequest(null));
            }
        }

        [TestMethod]
        public void SdkVersionHasCorrectFormat()
        {
            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(RequestTrackingTelemetryModule), prefix: "web:");

            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = new RequestTrackingTelemetryModule();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(expectedVersion, context.GetRequestTelemetry().Context.GetInternalContext().SdkVersion);
        }

        [TestMethod]
        public void OnEndDoesNotAddSourceFieldForRequestForSameComponent()
        {
            // ARRANGE
            string ikey = "b3eb14d6-bb32-4542-9b93-473cd94aaedf";
            string appIdHeader = GetCorrelationIdHeaderValue(ikey); // since per our mock appId = ikey


            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.SourceAppIdHeader, appIdHeader);

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = new RequestTrackingTelemetryModule();
            var config = TelemetryConfiguration.CreateDefault();
            config.InstrumentationKey = ikey;

            // ACT
            module.Initialize(config);
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            // VALIDATE
            Assert.True(string.IsNullOrEmpty(context.GetRequestTelemetry().Source), "RequestTrackingTelemetryModule should not set source for same ikey as itself.");
        }

        [TestMethod]
        public void OnEndAddsSourceFieldForRequestWithSourceIkey()
        {
            // ARRANGE                       
            string appId = "b3eb14d6-bb32-4542-9b93-473cd94aaedf";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.SourceAppIdHeader, GetCorrelationIdHeaderValue(appId));

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = new RequestTrackingTelemetryModule();
            var config = TelemetryConfiguration.CreateDefault();

            // My instrumentation key and hence app id is random / newly generated. The appId header is different - hence a different component.
            config.InstrumentationKey = Guid.NewGuid().ToString();

            // ACT
            module.Initialize(config);
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            // VALIDATE
            Assert.Equal(GetCorrelationIdHeaderValue(appId), context.GetRequestTelemetry().Source);
        }

        [TestMethod]
        public void OnEndDoesNotAddSourceFieldForRequestWithOutSourceIkeyHeader()
        {
            // ARRANGE                                   
            // do not add any sourceikey header.
            Dictionary<string, string> headers = new Dictionary<string, string>();

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = new RequestTrackingTelemetryModule();
            var config = TelemetryConfiguration.CreateDefault();
            config.InstrumentationKey = Guid.NewGuid().ToString();

            // ACT
            module.Initialize(config);
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            // VALIDATE
            Assert.True(string.IsNullOrEmpty(context.GetRequestTelemetry().Source), "RequestTrackingTelemetryModule should not set source if not sourceikey found in header");
        }

        [TestMethod]
        public void OnEndDoesNotOverrideSourceField()
        {
            // ARRANGE                       
            string appIdInHeader = GetCorrelationIdHeaderValue("b3eb14d6-bb32-4542-9b93-473cd94aaedf");
            string appIdInSourceField = "9AB8EDCB-21D2-44BB-A64A-C33BB4515F20";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.SourceAppIdHeader, appIdInHeader);

            
            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = new RequestTrackingTelemetryModule();
            var config = TelemetryConfiguration.CreateDefault();
            config.InstrumentationKey = Guid.NewGuid().ToString();

            module.Initialize(config);
            module.OnBeginRequest(context);
            context.GetRequestTelemetry().Source = appIdInSourceField;

            // ACT
            module.OnEndRequest(context);

            // VALIDATE
            Assert.Equal(appIdInSourceField, context.GetRequestTelemetry().Source);
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

        private string GetCorrelationIdHeaderValue(string appId)
        {
            return string.Format("aid-v1:{0}", appId, CultureInfo.InvariantCulture);
        }

    }
}
