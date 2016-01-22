namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Web;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class UserAgentTelemetryProcessorTests
    {
        [TestMethod]
        public void ConstructorThrowsArgumentNullExceptionIfNextIsNull()
        {
            try
            {
                var source = new UserAgentTelemetryProcessor(null);
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

            var source = new UserAgentTelemetryProcessor(spy);

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessCallsNextIfRequestFailed()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContextForFailedRequest();

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new UserAgentTelemetryProcessor(spy);

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessCallsNextIfSuccessfulRequestUserAgentIsNotFiltered()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "a" } });

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new UserAgentTelemetryProcessor(spy);

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void UserAgentIsFilteredIfItIsWhiteSpaceAndWhitespaceExplicitlySpecified()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "    " } });

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new UserAgentTelemetryProcessor(spy);

            source.UserAgents.Add(new FilterRequest { Value = "\n " });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(0, spy.ReceivedCalls);
        }

        [TestMethod]
        public void UserAgentIsNotFilteredIfItIsNullAndWhitespaceSpecified()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", null } });

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new UserAgentTelemetryProcessor(spy);

            source.UserAgents.Add(new FilterRequest { Value = "\n " });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void UserAgentIsFilteredIfItIsNullAndNullExplicitlySpecified()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", null } });

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new UserAgentTelemetryProcessor(spy);

            source.UserAgents.Add(new FilterRequest { Value = null });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(0, spy.ReceivedCalls);
        }

        [TestMethod]
        public void UserAgentIsFilteredIfItIsEmptyAndEmptyExplictlySpecified()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", string.Empty } });

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new UserAgentTelemetryProcessor(spy);

            source.UserAgents.Add(new FilterRequest { Value = string.Empty });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(0, spy.ReceivedCalls);
        }

        [TestMethod]
        public void ProcessReturnsIfSuccessfulRequestUserAgentIsFiltered()
        {
            HttpContext.Current = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "User-Agent", "a" } });

            var spy = new SimpleTelemetryProcessorSpy();

            var source = new UserAgentTelemetryProcessor(spy);

            source.UserAgents.Add(new FilterRequest { Value = "As" });

            source.Process(new RequestTelemetry());

            Assert.AreEqual(0, spy.ReceivedCalls);
        }
    }
}
