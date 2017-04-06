namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.TestFramework;

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

        [TestCleanup]
        public void Cleanup()
        {
            Common.ActivityHelpers.StopRequestActivity();
        }

        [TestMethod]
        public void OnErrorTracksExceptionsThatArePresentInHttpContext()
        {
            var platformContext = HttpModuleHelper.GetFakeHttpContext();
            var exception1 = new Exception("1");
            platformContext.AddError(exception1);
            platformContext.AddError(new Exception("2"));

            var module = new ExceptionTrackingTelemetryModule();
            module.Initialize(this.configuration);
            module.OnError(platformContext);
            
            Assert.Equal(2, this.sendItems.Count);
            Assert.Equal(exception1, ((ExceptionTelemetry)this.sendItems[0]).Exception);
        }

        [TestMethod]
        public void OnErrorSetsSeverityToCriticalForRequestWithStatusCode500()
        {
            var platformContext = HttpModuleHelper.GetFakeHttpContext();
            platformContext.Response.StatusCode = 500;
            platformContext.AddError(new Exception());

            var module = new ExceptionTrackingTelemetryModule();
            module.Initialize(this.configuration);
            module.OnError(platformContext);
            
            Assert.Equal(SeverityLevel.Critical, ((ExceptionTelemetry)this.sendItems[0]).SeverityLevel);
        }

        [TestMethod]
        public void OnErrorDoesNotThrowOnNullContext()
        {
            var module = new ExceptionTrackingTelemetryModule();

            module.Initialize(this.configuration);
            module.OnError(null); // is not supposed to throw
        }
    }
}
