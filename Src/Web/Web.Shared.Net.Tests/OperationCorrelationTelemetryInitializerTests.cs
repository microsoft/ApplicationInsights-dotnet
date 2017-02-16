namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Web;    
    using Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;    
    
    [TestClass]
    public class OperationCorrelationTelemetryInitializerTests
    {
        [TestMethod]
        public void InitializeDoesNotThrowWhenHttpContextIsNull()
        {
            var source = new OperationCorrelationTelemetryInitializer();
            source.Initialize(new RequestTelemetry());
        }

        [TestMethod]
        public void InitializeSetsParentIdForTelemetryUsingIdFromRequestTelemetry()
        {
            var exceptionTelemetry = new ExceptionTelemetry();
            var source = new TestableOperationCorrelationTelemetryInitializer(null);
            var requestTelemetry = source.FakeContext.CreateRequestTelemetryPrivate();

            source.Initialize(exceptionTelemetry);

            Assert.AreEqual(requestTelemetry.Id, exceptionTelemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void InitializeDoesNotOverrideCustomerParentOperationId()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(null);

            var customerTelemetry = new TraceTelemetry("Text");
            customerTelemetry.Context.Operation.ParentId = "CustomId";

            source.Initialize(customerTelemetry);

            Assert.AreEqual("CustomId", customerTelemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void InitializeSetsRootOperationIdForTelemetryUsingIdFromRequestTelemetry()
        {
            var exceptionTelemetry = new ExceptionTelemetry();
            var source = new TestableOperationCorrelationTelemetryInitializer(null);
            var requestTelemetry = source.FakeContext.CreateRequestTelemetryPrivate();
            requestTelemetry.Context.Operation.Id = "RootId";

            source.Initialize(exceptionTelemetry);

            Assert.AreEqual(requestTelemetry.Context.Operation.Id, exceptionTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void InitializeDoesNotOverrideCustomerRootOperationId()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(null);
            var requestTelemetry = source.FakeContext.CreateRequestTelemetryPrivate();
            requestTelemetry.Context.Operation.Id = "RootId";

            var customerTelemetry = new TraceTelemetry("Text");
            customerTelemetry.Context.Operation.Id = "CustomId";

            source.Initialize(customerTelemetry);

            Assert.AreEqual("CustomId", customerTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void InitializeSetsRequestTelemetryRootOperaitonIdToOepraitonId()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(null);
            var requestTelemetry = source.FakeContext.CreateRequestTelemetryPrivate();

            var customerTelemetry = new TraceTelemetry("Text");

            source.Initialize(customerTelemetry);

            Assert.AreEqual(requestTelemetry.Id, requestTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void InitializeReadsParentIdFromCustomHeader()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(new Dictionary<string, string>() { { "headerName", "ParentId" } });
            source.ParentOperationIdHeaderName = "headerName";

            var customerTelemetry = new TraceTelemetry("Text");

            source.Initialize(customerTelemetry);

            var requestTelemetry = source.FakeContext.ReadOrCreateRequestTelemetryPrivate();
            Assert.AreEqual("ParentId", requestTelemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void InitializeReadsRootIdFromCustomHeader()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(new Dictionary<string, string>() { { "headerName", "RootId" } });
            source.RootOperationIdHeaderName = "headerName";

            var customerTelemetry = new TraceTelemetry("Text");

            source.Initialize(customerTelemetry);
            Assert.AreEqual("RootId", customerTelemetry.Context.Operation.Id);

            var requestTelemetry = source.FakeContext.ReadOrCreateRequestTelemetryPrivate();
            Assert.AreEqual("RootId", requestTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void InitializeDoNotMakeRequestAParentOfItself()
        {
            var source = new TestableOperationCorrelationTelemetryInitializer(null);
            var requestTelemetry = source.FakeContext.ReadOrCreateRequestTelemetryPrivate();

            source.Initialize(requestTelemetry);
            Assert.AreEqual(null, requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Id, requestTelemetry.Context.Operation.Id);
        }

        [TestMethod]
        public void InitializeDoesNotAddSourceFieldForRequestForSameComponent()
        {
            // ARRANGE
            string instrumentationKey = "b3eb14d6-bb32-4542-9b93-473cd94aaedf";

            // Here is the equivalent generated IKey Hash
            string hashedIkey = "o05HMrc4Og8W1Jyy60JPDPxxQy3bOKyuaj6HudZHTjE=";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, hashedIkey);

            var source = new TestableOperationCorrelationTelemetryInitializer(headers);
            var requestTelemetry = source.FakeContext.ReadOrCreateRequestTelemetryPrivate();
            requestTelemetry.Context.InstrumentationKey = instrumentationKey;

            // ACT
            source.Initialize(requestTelemetry);

            // VALIDATE
            if (!string.IsNullOrEmpty(requestTelemetry.Source))
            {
                Assert.Fail("OperationCorrelationTelemetryInitializer should not set source for same ikey as itself.");
            }
        }

        [TestMethod]
        public void InitializeAddsSourceFieldForRequestWithSourceIkey()
        {
            // ARRANGE                       
            string hashedIkey = "vwuSMCFBLdIHSdeEXvFnmiXPO5ilQRqw9kO/SE5ino4=";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, hashedIkey);

            var source = new TestableOperationCorrelationTelemetryInitializer(headers);
            var requestTelemetry = source.FakeContext.ReadOrCreateRequestTelemetryPrivate();
            requestTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();

            // ACT
            source.Initialize(requestTelemetry);

            // VALIDATE
            Assert.AreEqual(hashedIkey, requestTelemetry.Source);            
        }

        [TestMethod]
        public void InitializeDoesNotAddSourceFieldForRequestWithOutSourceIkeyHeader()
        {
            // ARRANGE                                   
            // do not add any sourceikey header.
            Dictionary<string, string> headers = new Dictionary<string, string>();
            
            var source = new TestableOperationCorrelationTelemetryInitializer(headers);
            var requestTelemetry = source.FakeContext.ReadOrCreateRequestTelemetryPrivate();
            requestTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();

            // ACT
            source.Initialize(requestTelemetry);

            // VALIDATE
            if (!string.IsNullOrEmpty(requestTelemetry.Source))
            {
                Assert.Fail("OperationCorrelationTelemetryInitializer should not set source if not sourceikey found in header");
            }
        }

        [TestMethod]
        public void InitializeDoesNotOverrideSourceField()
        {
            // ARRANGE                       
            string hashedIkeyInHeader = "o05HMrc4Og8W1Jyy60JPDPxxQy3bOKyuaj6HudZHTjE=";
            string hashedIkeySetInSource = "vwuSMCFBLdIHSdeEXvFnmiXPO5ilQRqw9kO=";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, hashedIkeyInHeader);

            var source = new TestableOperationCorrelationTelemetryInitializer(headers);
            var requestTelemetry = source.FakeContext.ReadOrCreateRequestTelemetryPrivate();
            requestTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();
            requestTelemetry.Source = hashedIkeySetInSource;

            // ACT
            source.Initialize(requestTelemetry);

            // VALIDATE
            Assert.AreEqual(hashedIkeySetInSource, requestTelemetry.Source);
        }

        private class TestableOperationCorrelationTelemetryInitializer : OperationCorrelationTelemetryInitializer
        {
            private readonly HttpContext fakeContext;

            public TestableOperationCorrelationTelemetryInitializer(IDictionary<string, string> headers)
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