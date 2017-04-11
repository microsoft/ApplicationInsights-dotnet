namespace Microsoft.ApplicationInsights.Web
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OperationCorrelationTelemetryInitializerTests
    {
        [TestCleanup]
        public void Cleanup()
        {
#if NET45
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
#else
            ActivityHelpers.CleanOperationContext();
#endif
        }

        [TestMethod]
        public void InitializeDoesNotThrowWhenHttpContextIsNull()
        {
            var source = new OperationCorrelationTelemetryInitializer();
            source.Initialize(new RequestTelemetry());
        }

        [TestMethod]
        public void DefaultHeadersOperationCorrelationTelemetryInitializerAreSet()
        {
            var initializer = new OperationCorrelationTelemetryInitializer();
            Assert.AreEqual(RequestResponseHeaders.StandardParentIdHeader, initializer.ParentOperationIdHeaderName);
            Assert.AreEqual(RequestResponseHeaders.StandardRootIdHeader, initializer.RootOperationIdHeaderName);
        }

        [TestMethod]
        public void CustomHeadersOperationCorrelationTelemetryInitializerAreSetProperly()
        {
            var initializer = new OperationCorrelationTelemetryInitializer();
            initializer.ParentOperationIdHeaderName = "myParentHeader";
            initializer.RootOperationIdHeaderName = "myRootHeader";

            Assert.AreEqual("myParentHeader", ActivityHelpers.ParentOperationIdHeaderName);
            Assert.AreEqual("myRootHeader", ActivityHelpers.RootOperationIdHeaderName);

            Assert.AreEqual("myParentHeader", initializer.ParentOperationIdHeaderName);
            Assert.AreEqual("myRootHeader", initializer.RootOperationIdHeaderName);
        }

#if NET40
        [TestMethod]
        public void OperationContextIsSetForNonRequestTelemetry()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid.1",
                ["Correlation-Context"] = "k1=v1,k2=v2,k1=v3"
            });

            // simulate OnBegin behavior:
            // create telemetry and start activity for children
            var requestTelemetry = source.FakeContext.CreateRequestTelemetryPrivate();
            
            // lost Acitivity / call context
            ActivityHelpers.CleanOperationContext();

            var exceptionTelemetry = new ExceptionTelemetry();
            source.Initialize(exceptionTelemetry);

            Assert.AreEqual(requestTelemetry.Context.Operation.Id, exceptionTelemetry.Context.Operation.Id);
            Assert.AreEqual(requestTelemetry.Id, exceptionTelemetry.Context.Operation.ParentId);

            Assert.AreEqual(2, exceptionTelemetry.Context.Properties.Count);
            
            // undefined behavior for duplicates
            Assert.IsTrue(exceptionTelemetry.Context.Properties["k1"] == "v3" || exceptionTelemetry.Context.Properties["k1"] == "v1");
            Assert.AreEqual("v2", exceptionTelemetry.Context.Properties["k2"]);
        }

        [TestMethod]
        public void OperationContextIsNotUpdatedIfOperationIdIsSet()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid.1",
                ["Correaltion-Context"] = "k1=v1"
            });

            // create telemetry and immediately clean call context/activity
            source.FakeContext.CreateRequestTelemetryPrivate();
            ActivityHelpers.CleanOperationContext();

            var exceptionTelemetry = new ExceptionTelemetry();
            exceptionTelemetry.Context.Operation.Id = "guid";
            source.Initialize(exceptionTelemetry);

            Assert.IsNull(exceptionTelemetry.Context.Operation.ParentId);

            Assert.AreEqual(0, exceptionTelemetry.Context.Properties.Count);
        }
#else
        [TestMethod]
        public void OperationContextIsSetForNonRequestTelemetry()
        {
            var activity = new Activity("request")
                .SetParentId("|guid.1")
                .AddBaggage("k1", "v1")
                .AddBaggage("k2", "v2")
                .AddBaggage("k1", "v3")
                .Start();

            var source = new TestableOperationCorrelationTelemetryInitializer();

            // simulate OnBegin behavior:
            // create telemetry and start activity for children
            var requestTelemetry = source.FakeContext.CreateRequestTelemetryPrivate();
            
            // lost Acitivity / call context
            activity.Stop();

            var exceptionTelemetry = new ExceptionTelemetry();
            source.Initialize(exceptionTelemetry);

            Assert.AreEqual(requestTelemetry.Context.Operation.Id, exceptionTelemetry.Context.Operation.Id);
            Assert.AreEqual(requestTelemetry.Id, exceptionTelemetry.Context.Operation.ParentId);

            Assert.AreEqual(2, exceptionTelemetry.Context.Properties.Count);

            // undefined behavior for duplicates
            Assert.IsTrue(exceptionTelemetry.Context.Properties["k1"] == "v3" || exceptionTelemetry.Context.Properties["k1"] == "v1");
            Assert.AreEqual("v2", exceptionTelemetry.Context.Properties["k2"]);
        }

        [TestMethod]
        public void OperationContextIsNotUpdatedIfOperationIdIsSet()
        {
            var activity = new Activity("request")
                .SetParentId("|guid.1")
                .AddBaggage("k1", "v1")
                .Start();

            var source = new TestableOperationCorrelationTelemetryInitializer();

            // create telemetry and immediately clean call context/activity
            source.FakeContext.CreateRequestTelemetryPrivate();

            activity.Stop();

            var exceptionTelemetry = new ExceptionTelemetry();
            exceptionTelemetry.Context.Operation.Id = "guid";
            source.Initialize(exceptionTelemetry);

            Assert.IsNull(exceptionTelemetry.Context.Operation.ParentId);

            Assert.AreEqual(0, exceptionTelemetry.Context.Properties.Count);
        }
#endif

        private class TestableOperationCorrelationTelemetryInitializer : OperationCorrelationTelemetryInitializer
        {
            private readonly HttpContext fakeContext;

            public TestableOperationCorrelationTelemetryInitializer(IDictionary<string, string> headers = null)
            {
                this.fakeContext = HttpModuleHelper.GetFakeHttpContext(headers);
            }

            public HttpContext FakeContext
            {
                get { return this.fakeContext; }
            }

            protected override HttpContext ResolvePlatformContext()
            {
                return this.fakeContext;
            }
        }
    }
}