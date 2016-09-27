namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Web;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;

    [TestClass]
    public class ExceptionTrackingTelemetryModuleTests
    {
        private TelemetryConfiguration configuration;
        private IList<ITelemetry> sendItems;

        [TestInitialize]
        public void TestInit()
        {
            this.sendItems = new List<ITelemetry>();
            var stubTelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration = new TelemetryConfiguration
            {
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryChannel = stubTelemetryChannel
            };
        }

        [TestMethod]
        public void OnErrorTracksExceptionsThatArePresentInHttpContext()
        {
            var platformContext = HttpModuleHelper.GetFakeHttpContext();
            var exception1 = new Exception("1");
            platformContext.AddError(exception1);
            platformContext.AddError(new Exception("2"));

            using (var module = new TestableExceptionTrackingTelemetryModule(platformContext))
            {
                module.Initialize(this.configuration);
                module.OnError(null);
            }

            Assert.Equal(2, this.sendItems.Count);
            Assert.Equal(exception1, ((ExceptionTelemetry)this.sendItems[0]).Exception);
        }

        [TestMethod]
        public void OnErrorSetsSeverityToCriticalForRequestWithStatusCode500()
        {
            var platformContext = HttpModuleHelper.GetFakeHttpContext();
            platformContext.Response.StatusCode = 500;
            platformContext.AddError(new Exception());

            using (var module = new TestableExceptionTrackingTelemetryModule(platformContext))
            {
                module.Initialize(this.configuration);
                module.OnError(null);
            }

            Assert.Equal(SeverityLevel.Critical, ((ExceptionTelemetry)this.sendItems[0]).SeverityLevel);
        }

        [TestMethod]
        public void OnErrorDoesNotThrowOnNullContext()
        {
            using (var module = new TestableExceptionTrackingTelemetryModule(null))
            {
                module.Initialize(this.configuration);
                module.OnError(null); // Expected to not throw.
            }
        }

        [TestMethod]
        public void ConstructorSetsOnErrorAsAHanderForEvent3()
        {
            var platformContext = HttpModuleHelper.GetFakeHttpContext();
            platformContext.AddError(new Exception());

            using (var module = new TestableExceptionTrackingTelemetryModule(platformContext))
            {
                module.Initialize(this.configuration);
                WebEventsPublisher.Log.OnError();
            }

            Assert.Equal(1, this.sendItems.Count);
        }

        internal class TestableExceptionTrackingTelemetryModule : ExceptionTrackingTelemetryModule
        {
            private readonly HttpContext context;

            public TestableExceptionTrackingTelemetryModule(HttpContext context)
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
