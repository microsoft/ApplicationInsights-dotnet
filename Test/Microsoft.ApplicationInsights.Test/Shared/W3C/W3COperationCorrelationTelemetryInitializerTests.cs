using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights.W3C
{
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.W3C;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class W3COperationCorrelationTelemetryInitializerTests
    {
        [TestInitialize]
        public void Init()
        {
            //TODO
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        [TestMethod]
        public void InitializerCreatesNewW3CContext()
        {
            Activity a = new Activity("dummy").Start();

            RequestTelemetry request = new RequestTelemetry();

            new OperationCorrelationTelemetryInitializer().Initialize(request);

            Assert.IsNotNull(request.Context.Operation.Id);
            Assert.AreEqual($"|{a.GetTraceId()}.{a.GetSpanId()}.", request.Context.Operation.ParentId);

            Assert.AreEqual(0, request.Properties.Count);

            /*Assert.IsTrue(request.Properties.ContainsKey(W3CConstants.LegacyRequestIdProperty));
            Assert.AreEqual(a.Id, request.Properties[W3CConstants.LegacyRequestIdProperty]);

            Assert.IsTrue(request.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
            Assert.AreEqual(a.RootId, request.Properties[W3CConstants.LegacyRootIdProperty]);*/
        }

        [TestMethod]
        public void InitializerSetsCorrelationIdsOnTraceTelemetry()
        {
            Activity a = new Activity("dummy")
                .Start();
            
            string expectedTrace = a.GetTraceId();
            string expectedParent = a.GetSpanId();

            TraceTelemetry trace = new TraceTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(trace);

            Assert.AreEqual(expectedTrace, trace.Context.Operation.Id);
            Assert.AreEqual($"|{expectedTrace}.{expectedParent}.", trace.Context.Operation.ParentId);

            Assert.IsFalse(trace.Properties.Any());
        }

        [TestMethod]
        public void InitializerSetsCorrelationIdsOnRequestTelemetry()
        {
            Activity p = new Activity("parent").Start();
            Activity a = new Activity("dummy")
                .Start();

            string expectedTrace = a.GetTraceId();
            string expectedSpanId = a.GetSpanId();

            string expectedParent = a.ParentSpanId.AsHexString;

            RequestTelemetry request = new RequestTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(request);

            Assert.AreEqual(expectedTrace, request.Context.Operation.Id);
            Assert.AreEqual($"|{expectedTrace}.{expectedParent}.", request.Context.Operation.ParentId);
            Assert.AreEqual($"|{expectedTrace}.{expectedSpanId}.", request.Id);

            Assert.AreEqual(2, request.Properties.Count);

            /*Assert.IsTrue(request.Properties.ContainsKey(W3CConstants.LegacyRequestIdProperty));
            Assert.AreEqual(a.Id, request.Properties[W3CConstants.LegacyRequestIdProperty]);

            Assert.IsTrue(request.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
            Assert.AreEqual(a.RootId, request.Properties[W3CConstants.LegacyRootIdProperty]);*/
        }

        [TestMethod]
        public void InitializerSetsCorrelationIdsOnRequestTelemetryNoParent()
        {
            Activity a = new Activity("dummy")
                .Start();

            string expectedTrace = a.GetTraceId();
            string expectedSpanId = a.GetSpanId();

            RequestTelemetry request = new RequestTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(request);

            Assert.AreEqual(expectedTrace, request.Context.Operation.Id);
            Assert.IsNull(request.Context.Operation.ParentId);
            Assert.AreEqual($"|{expectedTrace}.{expectedSpanId}.", request.Id);

            Assert.AreEqual(2, request.Properties.Count);

            /*Assert.IsTrue(request.Properties.ContainsKey(W3CConstants.LegacyRequestIdProperty));
            Assert.AreEqual(a.Id, request.Properties[W3CConstants.LegacyRequestIdProperty]);

            Assert.IsTrue(request.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
            Assert.AreEqual(a.RootId, request.Properties[W3CConstants.LegacyRootIdProperty]);*/
        }

        [TestMethod]
        public void InitializerNoopWithoutActivity()
        {
            RequestTelemetry request = new RequestTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(request);

            Assert.IsNull(request.Context.Operation.Id);
            Assert.IsNull(request.Context.Operation.ParentId);

            Assert.IsFalse(request.Properties.Any());
        }

        [TestMethod]
        public void InitializerPopulatesTraceStateOnRequestAndDependencyTelemetry()
        {
            Activity a = new Activity("dummy")
                .Start();
                
            a.TraceStateString = "key=value";

            string expectedTrace = a.GetTraceId();
            string expectedSpanId = a.GetSpanId();

            RequestTelemetry request = new RequestTelemetry();
            DependencyTelemetry dependency = new DependencyTelemetry();
            TraceTelemetry trace = new TraceTelemetry();
            var initializer = new OperationCorrelationTelemetryInitializer();
            initializer.Initialize(request);
            initializer.Initialize(dependency);
            initializer.Initialize(trace);

            Assert.AreEqual(expectedTrace, request.Context.Operation.Id);
            Assert.AreEqual($"|{expectedTrace}.{expectedSpanId}.", request.Id);

            Assert.AreEqual("key=value", request.Properties["tracestate"]);
            Assert.AreEqual("key=value", dependency.Properties["tracestate"]);
            Assert.IsFalse(trace.Properties.Any());
        }

        [TestMethod]
        public void InitializerOnNestedActivitities()
        {
            Activity requestActivity = new Activity("request")
                .Start();

            RequestTelemetry request = new RequestTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(request);

            Activity nested1 = new Activity("nested1").Start();
            Activity nested2 = new Activity("nested1").Start();

            DependencyTelemetry dependency2 = new DependencyTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(dependency2);

            Assert.AreEqual(request.Context.Operation.Id, nested2.GetTraceId());
            Assert.AreEqual(request.Context.Operation.Id, nested1.GetTraceId());

            Assert.AreEqual(request.Id, $"|{nested1.GetTraceId()}.{nested1.GetParentSpanId()}.");
            Assert.AreEqual(nested1.GetSpanId(), nested2.GetParentSpanId());

            Assert.AreEqual(request.Context.Operation.Id, dependency2.Context.Operation.Id);

            nested2.Stop();

            DependencyTelemetry dependency1 = new DependencyTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(dependency1);

            Assert.AreEqual(request.Id, $"|{nested1.GetTraceId()}.{nested1.GetParentSpanId()}.");
            Assert.AreEqual(dependency2.Context.Operation.ParentId, dependency1.Id);
            Assert.AreEqual(request.Context.Operation.Id, dependency1.Context.Operation.Id);
            Assert.AreEqual(request.Id, dependency1.Context.Operation.ParentId);
        }

        [TestMethod]
        public void InitializerOnSqlDependency()
        {
            Activity requestActivity = new Activity("request")
                .Start()
                .GenerateW3CContext();

            RequestTelemetry request = new RequestTelemetry();
            DependencyTelemetry sqlDependency = new DependencyTelemetry()
            {
                Type = "SQL"
            };
            sqlDependency.Context.GetInternalContext().SdkVersion = "rdddsc:12345";
            string expectedId = sqlDependency.Id;

            new OperationCorrelationTelemetryInitializer().Initialize(sqlDependency);
            new OperationCorrelationTelemetryInitializer().Initialize(request);

            Assert.AreEqual(request.Context.Operation.Id, sqlDependency.Context.Operation.Id);
            Assert.AreEqual(request.Id, sqlDependency.Context.Operation.ParentId);
            Assert.AreEqual(expectedId, sqlDependency.Id);
        }

        [TestMethod]
        public void InitializerOnActivityWithParentWithoutW3CTags()
        {
            Activity parentActivity = new Activity("parent")
                .Start();
            Activity childActivity = new Activity("child")
                .Start();

            RequestTelemetry request = new RequestTelemetry();
            new OperationCorrelationTelemetryInitializer().Initialize(request);

            Assert.AreEqual(request.Context.Operation.Id, parentActivity.GetTraceId());
            Assert.AreEqual(request.Context.Operation.Id, childActivity.GetTraceId());
            Assert.AreEqual(request.Id, $"|{childActivity.GetTraceId()}.{childActivity.GetSpanId()}.");
            Assert.AreEqual(request.Context.Operation.ParentId, $"|{childActivity.GetTraceId()}.{parentActivity.GetSpanId()}.");
        }
    }
}
