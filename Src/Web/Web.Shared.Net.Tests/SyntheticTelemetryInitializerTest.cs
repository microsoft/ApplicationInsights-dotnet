namespace Microsoft.ApplicationInsights.Web
{
    using System.Collections.Generic;
    using System.Web;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SyntheticTelemetryInitializerTest
    {
        [TestMethod]
        public void SyntheticSourceIsNotSetIfUserProvidedValue()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            metricTelemetry.Context.Operation.SyntheticSource = "SOURCE";
            var source = new TestableSyntheticTelemetryInitializer(new Dictionary<string, string>
                {
                    { "synthetictest-runid", "ID" }
                });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("SOURCE", metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void SyntheticSourceIsSetToWellKnownValue()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticTelemetryInitializer(new Dictionary<string, string>
                {
                    { "SyntheticTest-RunId", "ID" }
                });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("Application Insights Availability Monitoring", metricTelemetry.Context.Operation.SyntheticSource);
        }

        [TestMethod]
        public void UserIdIsSetToLocation()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticTelemetryInitializer(new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOCATION" },
                { "SyntheticTest-RunId", "ID" }
            });
            
            source.Initialize(metricTelemetry);

            Assert.AreEqual("LOCATION", metricTelemetry.Context.User.Id);
        }

        [TestMethod]
        public void UserIdIsNotOverriden()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            metricTelemetry.Context.User.Id = "UserId";
            var source = new TestableSyntheticTelemetryInitializer(new Dictionary<string, string>
            {
                { "SyntheticTest-Location", "LOCATION" },
                { "SyntheticTest-RunId", "ID" }
            });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("UserId", metricTelemetry.Context.User.Id);
        }

        [TestMethod]
        public void SessionIdIsNotOverriden()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            metricTelemetry.Context.Session.Id = "SessionId";
            var source = new TestableSyntheticTelemetryInitializer(new Dictionary<string, string> { { "SyntheticTest-RunId", "ID" } });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("SessionId", metricTelemetry.Context.Session.Id);
        }

        [TestMethod]
        public void SessionIdIsSetToRunId()
        {
            var metricTelemetry = new MetricTelemetry("name", 0);
            var source = new TestableSyntheticTelemetryInitializer(new Dictionary<string, string> { { "SyntheticTest-RunId", "ID" } });

            source.Initialize(metricTelemetry);

            Assert.AreEqual("ID", metricTelemetry.Context.Session.Id);
        }

        private class TestableSyntheticTelemetryInitializer : SyntheticTelemetryInitializer
        {
            private readonly HttpContext fakeContext;

            public TestableSyntheticTelemetryInitializer(IDictionary<string, string> headers = null)
            {
                this.fakeContext = HttpModuleHelper.GetFakeHttpContext(headers);
            }

            protected override HttpContext ResolvePlatformContext()
            {
                return this.fakeContext;
            }
        }
    }
}
