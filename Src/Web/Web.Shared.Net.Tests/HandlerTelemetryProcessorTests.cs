namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.Handlers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
   
    [TestClass]
    public class HandlerTelemetryProcessorTests
    {
        [TestMethod]
        public void ConstructorThrowsArgumentNullExceptionIfNextIsNull()
        {
            try
            {
                var source = new HandlerTelemetryProcessor(null);
                Assert.Fail("Expected ArgumentNullException to be thrown");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("next", e.ParamName);
            }
        }

        [TestMethod]
        public void ProcessCallsNextIfHttpContextIsNull()
        {
            HttpContext.Current = null;

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new HandlerTelemetryProcessor(spy);

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessCallsNextIfRequestFailed()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContextForFailedRequest();

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new HandlerTelemetryProcessor(spy);

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessCallsNextIfSuccessfulRequestHandlerIsNotFiltered()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "a" } });
            HttpContext.Current.Handler = new AssemblyResourceLoader();

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new HandlerTelemetryProcessor(spy);
            source.Handlers.Add(new FilterRequest { Value = "Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest+FakeHttpHandler" });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessReturnsIfSuccessfulRequestHandlerIsFilteredByDefault()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "a" } });
            HttpContext.Current.Handler = new AssemblyResourceLoader();

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new HandlerTelemetryProcessor(spy);
            source.Handlers.Add(new FilterRequest { Value = "System.Web.Handlers.AssemblyResourceLoader" });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(0, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessReturnsIfSuccessfulRequestHandlerIsFiltered()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "a" } });
            HttpContext.Current.Handler = new Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest.FakeHttpHandler();

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new HandlerTelemetryProcessor(spy);
            source.Handlers.Add(new FilterRequest { Value = "Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest+FakeHttpHandler" });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(0, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessCallsNextIfHandlerMatchesButNotNotification()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "a" } });
            HttpContext.Current.Handler = new Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest.FakeHttpHandler();

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new HandlerTelemetryProcessor(spy);

            var filter = new FilterRequest
            {
                Value = "Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest+FakeHttpHandler"
            };
            filter.RequestNotifications.Add(new RequestNotificationFilter
            {
                Value = "EndRequest"
            });

            source.Handlers.Add(filter);

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }
    }
}
