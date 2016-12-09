namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;
#if !NET40
    using System.Web;
#endif

    using Common;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ProfilerHttpProcessingTest : IDisposable
    {
        #region Fields
        private const int TimeAccuracyMilliseconds = 150; // this may be big number when under debugger
        private TelemetryConfiguration configuration;
        private Uri testUrl = new Uri("http://www.microsoft.com/");
        private List<ITelemetry> sendItems;
        private int sleepTimeMsecBetweenBeginAndEnd = 100;
        private Exception ex;
        private ProfilerHttpProcessing httpProcessingProfiler;        
        #endregion //Fields

        #region TestInitialize

        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.sendItems = new List<ITelemetry>();
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.httpProcessingProfiler = new ProfilerHttpProcessing(this.configuration, null, new ObjectInstanceBasedOperationHolder(), /*setCorrelationHeaders*/ true, new List<string>());
            this.ex = new Exception();
        }

        [TestCleanup]
        public void Cleanup()
        {        
        }
        #endregion //TestgInitiliaze

        #region GetResponse

        /// <summary>
        /// Validates HttpProcessingProfiler returns correct operation for OnBeginForGetResponse.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler returns correct operation for OnBeginForGetResponse.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnBeginForGetResponse()
        {                        
            var request = WebRequest.Create(this.testUrl);
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Assert.IsNull(operationReturned, "Operation returned should be null as all context is maintained internally");
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
        }

        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnEndForGetResponse.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry on calling OnEndForGetResponse.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnEndForGetResponse()
        {            
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK);
            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            var objectReturned = this.httpProcessingProfiler.OnEndForGetResponse(null, returnObjectPassed, request);
            
            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForGetResponse processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, this.sleepTimeMsecBetweenBeginAndEnd, "200");
        }

        /// <summary>
        /// Validates if DependencyTelemetry sent contains the cross component instrumentation key hash.
        /// </summary>
        [TestMethod]
        [Description("Validates if DependencyTelemetry sent contains the cross component IKey hash.")]
        public void RddTestHttpProcessingProfilerOnEndAddsIkeyToTargetField()
        {
            string hashedIkey = "vwuSMCFBLdIHSdeEXvFnmiXPO5ilQRqw9kO/SE5ino4=";

            var request = WebRequest.Create(this.testUrl);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.TargetInstrumentationKeyHeader, hashedIkey);

            var returnObjectPassed = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK, headers);
            returnObjectPassed.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader] = hashedIkey;

            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            var objectReturned = this.httpProcessingProfiler.OnEndForGetResponse(null, returnObjectPassed, request);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            Assert.AreEqual(this.testUrl.Host + " | " + hashedIkey, ((DependencyTelemetry)this.sendItems[0]).Target);
        }

        /// <summary>
        /// Ensures that the source request header is added when request is sent.
        /// </summary>
        [TestMethod]
        [Description("Ensures that the source request header is added when request is sent.")]
        public void RddTestHttpProcessingProfilerOnBeginAddsSourceHeader()
        {
            var request = WebRequest.Create(this.testUrl);

            Assert.IsNull(request.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader]);

            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Assert.IsNotNull(request.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader]);
        }

        /// <summary>
        /// Ensures that the source request header is added when request is sent.
        /// </summary>
        [TestMethod]
        public void RddTestHttpProcessingProfilerOnBeginAddsRootIdHeader()
        {
            var request = WebRequest.Create(this.testUrl);

            Assert.IsNull(request.Headers[RequestResponseHeaders.StandardRootIdHeader]);

            var client = new TelemetryClient(this.configuration);
            using (var op = client.StartOperation<RequestTelemetry>("request"))
            {
                this.httpProcessingProfiler.OnBeginForGetResponse(request);
                Assert.IsNotNull(request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
                Assert.AreEqual(request.Headers[RequestResponseHeaders.StandardRootIdHeader], op.Telemetry.Context.Operation.Id);
            }
        }

        /// <summary>
        /// Ensures that the parent id header is added when request is sent.
        /// </summary>
        [TestMethod]
        public void RddTestHttpProcessingProfilerOnBeginAddsParentIdHeader()
        {
            var request = WebRequest.Create(this.testUrl);

            Assert.IsNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);

            var client = new TelemetryClient(this.configuration);
            using (var op = client.StartOperation<RequestTelemetry>("request"))
            {
                this.httpProcessingProfiler.OnBeginForGetResponse(request);
                Assert.IsNotNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
                Assert.AreNotEqual(request.Headers[RequestResponseHeaders.StandardParentIdHeader], op.Telemetry.Context.Operation.Id);
            }
        }
        
        /// <summary>
        /// Ensures that the source request header is not added when request is sent as per the config.
        /// </summary>
        [TestMethod]
        [Description("Ensures that the source request header is not added when the config commands as such")]
        public void RddTestHttpProcessingProfilerOnBeginSkipsAddingSourceHeaderPerConfig()
        {
            var request = WebRequest.Create(this.testUrl);

            Assert.IsNull(request.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader]);

            var httpProcessingProfiler = new ProfilerHttpProcessing(this.configuration, null, new ObjectInstanceBasedOperationHolder(), /*setCorrelationHeaders*/ false, new List<string>());
            httpProcessingProfiler.OnBeginForGetResponse(request);
            Assert.IsNull(request.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader]);

            httpProcessingProfiler = new ProfilerHttpProcessing(this.configuration, null, new ObjectInstanceBasedOperationHolder(), /*setCorrelationHeaders*/ true, new List<string> { this.testUrl.Host });
            httpProcessingProfiler.OnBeginForGetResponse(request);
            Assert.IsNull(request.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader]);
        }

        /// <summary>
        /// Ensures that the source request header is not overwritten if already provided by the user.
        /// </summary>
        [TestMethod]
        [Description("Ensures that the source request header is not overwritten if already provided by the user.")]
        public void RddTestHttpProcessingProfilerOnBeginDoesNotOverwriteExistingSourceHeader()
        {
            string sampleHeaderValue = "helloWorld";
            var request = WebRequest.Create(this.testUrl);

            request.Headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, sampleHeaderValue);

            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            var actualHeaderValue = request.Headers[RequestResponseHeaders.SourceInstrumentationKeyHeader];

            Assert.IsNotNull(actualHeaderValue);
            Assert.AreEqual(sampleHeaderValue, actualHeaderValue);
        }

        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForGetResponse.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForGetResponse.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnExceptionForGetResponse()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = new object();
            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Exception exc = new Exception();
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            this.httpProcessingProfiler.OnExceptionForGetResponse(null, exc, request);
            
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, this.sleepTimeMsecBetweenBeginAndEnd, string.Empty);
        }

        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry including response code on calling OnExceptionForGetResponse for WebException.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry including response code on calling OnExceptionForGetResponse for WebException.")]
        [Owner("mafletch")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnWebExceptionForGetResponse()
        {
            var request = WebRequest.Create(this.testUrl);
            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            var returnObjectPassed = TestUtils.GenerateHttpWebResponse(HttpStatusCode.NotFound);
            Exception exc = new WebException("exception message", null, WebExceptionStatus.ProtocolError, returnObjectPassed);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingProfiler.OnExceptionForGetResponse(null, exc, request);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, this.sleepTimeMsecBetweenBeginAndEnd, "404");
        }

#if !NET40
        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry including response code on calling OnExceptionForGetResponse for HttpException.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry including response code on calling OnExceptionForGetResponse for HttpException.")]
        [Owner("mafletch")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnHttpExceptionForGetResponse()
        {
            var request = WebRequest.Create(this.testUrl);
            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Exception exc = new HttpException(404, "exception message");
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingProfiler.OnExceptionForGetResponse(null, exc, request);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, this.sleepTimeMsecBetweenBeginAndEnd, "404");
        }
#endif
        /// <summary>
        /// Validates HttpProcessingProfiler OnBegin logs error into EventLog when passed invalid thisObject.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler OnBegin logs error into EventLog when passed invalid thisObject")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnBeginForGetResponseFailed()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                
                var request = WebRequest.Create(this.testUrl);
                DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForGetResponse(null);
                Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
                
                TestUtils.ValidateEventLogMessage(listener, "will not run for id", EventLevel.Warning);
            }
        }

        /// <summary>
        /// Validates HttpProcessingProfiler OnEnd logs error into EventLog when passed invalid thisObject.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler OnEnd logs error into EventLog when passed invalid thisObject")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnEndForGetResponseFailed()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                
                var returnObjectPassed = new object();
                var request = WebRequest.Create(this.testUrl);
                DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForGetResponse(request);
                var objectReturned = this.httpProcessingProfiler.OnEndForGetResponse(null, returnObjectPassed, null);
                Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForGetResponse processor is not the same as expected return object");
                Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
                
                var message = listener.Messages.First(item => item.EventId == 14);
                Assert.IsNotNull(message);  
            }
        }

        #endregion //GetResponse

        #region GetRequestStream

        /// <summary>
        /// Validates HttpProcessingProfiler returns correct operation for OnBeginForGetRequestStream.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler returns correct operation for OnBeginForGetRequestStream.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnBeginForGetRequestStream()
        {
            var request = WebRequest.Create(this.testUrl);
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForGetRequestStream(request, null);
            Assert.IsNull(operationReturned, "Operation returned should be null as all context is maintained internally");
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
        }
        
        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForGetRequestStream.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForGetRequestStream.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnExceptionForGetRequestStream()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = new object();
            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            Exception exc = new Exception();
            this.httpProcessingProfiler.OnExceptionForGetResponse(null, exc, request);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, this.sleepTimeMsecBetweenBeginAndEnd, string.Empty);
        }

        #endregion //GetRequestStream

        #region BeginGetResponse-EndGetResponse

        /// <summary>
        /// Validates HttpProcessingProfiler returns correct operation for OnBeginForBeginGetResponse.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler returns correct operation for OnBeginForBeginGetResponse.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnBeginForBeginGetResponse()
        {
            var request = WebRequest.Create(this.testUrl);
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForBeginGetResponse(request, null, null);
            Assert.IsNull(operationReturned, "For async methods, operation returned should be null as correlation is done internally using WeakTables.");
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
        }

        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnEndForGetResponse.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry on calling OnEndForEndGetResponse.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnEndForEndGetResponse()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK);
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForBeginGetResponse(request, null, null);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            var objectReturned = this.httpProcessingProfiler.OnEndForEndGetResponse(operationReturned, returnObjectPassed, request, null);

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForEndGetResponse processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, this.sleepTimeMsecBetweenBeginAndEnd, "200");
        }

        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnEndForGetResponse when returned object has been disposed.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry on calling OnEndForEndGetResponse when returned object has been disposed.")]
        [Owner("mafletch")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnEndForEndGetResponseWithDisposedResponse()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = TestUtils.GenerateDisposedHttpWebResponse(HttpStatusCode.OK);
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForBeginGetResponse(request, null, null);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            var objectReturned = this.httpProcessingProfiler.OnEndForEndGetResponse(operationReturned, returnObjectPassed, request, null);

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForEndGetResponse processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, this.sleepTimeMsecBetweenBeginAndEnd, string.Empty);
        }

        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForEndGetResponse.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForGetResponse.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnExceptionForEndGetResponse()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = new object();
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForBeginGetResponse(request, null, null);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            Exception exc = new Exception();
            this.httpProcessingProfiler.OnExceptionForEndGetResponse(operationReturned, exc, request, null);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, this.sleepTimeMsecBetweenBeginAndEnd, string.Empty);
        }

        #endregion //BeginGetResponse-EndGetResponse

        #region BeginGetRequestStream-EndGetRequestStream

        /// <summary>
        /// Validates HttpProcessingProfiler returns correct operation for OnBeginForBeginGetRequestStream.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler returns correct operation for OnBeginForBeginGetRequestStream.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnBeginForBeginGetRequestStream()
        {
            var request = WebRequest.Create(this.testUrl);
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForBeginGetRequestStream(request, null, null);
            Assert.IsNull(operationReturned, "For async methods, operation returned should be null as correlation is done internally using WeakTables.");
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
        }

        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForEndGetRequestStream.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler sends correct telemetry on calling OnExceptionForEndGetRequestStream.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerOnExceptionForEndGetRequestStream()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = new object();
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForBeginGetRequestStream(request, null, null);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            Exception exc = new Exception();
            this.httpProcessingProfiler.OnExceptionForEndGetRequestStream(operationReturned, exc, request, null, null);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, this.sleepTimeMsecBetweenBeginAndEnd, string.Empty);
        }

        #endregion //BeginGetRequestStream-EndGetRequestStream

        #region SyncScenarios

        /// <summary>
        /// Validates HttpProcessingProfiler calculates startTime from the start of very first GetRequestStream
        /// 1.create request
        /// 2.request.GetRequestStream
        /// 3.request.GetRequestStream
        /// 4.request.GetRequestStream
        /// 5.request.GetResponse
        /// The expected time is the time between 2 and 5.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler calculates startTime from the start of very first GetRequestStream")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerStartTimeFromGetRequestStream()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK);

            this.httpProcessingProfiler.OnBeginForGetRequestStream(request, null);
            this.httpProcessingProfiler.OnBeginForGetRequestStream(request, null);            
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingProfiler.OnBeginForGetRequestStream(request, null);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);

            this.httpProcessingProfiler.OnBeginForGetResponse(request);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingProfiler.OnEndForGetResponse(null, returnObjectPassed, request);

            // These times should not be calculated as dependency times
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);

            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, 3 * this.sleepTimeMsecBetweenBeginAndEnd, "200");
        }

        /// <summary>        
        /// Validates that HttpProcessingProfiler will sent RDD telemetry when GetRequestStream fails and GetResponse is not invoked
        /// 1.create request
        /// 2.request.GetRequestStream  fails.                
        /// </summary>
        [TestMethod]
        [Description("Validates that HttpProcessingProfiler will sent RDD telemetry when GetRequestStream fails and GetResponse is not invoked.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerFailedGetRequestStream()
        {
            var request = WebRequest.Create(this.testUrl);
            
            this.httpProcessingProfiler.OnBeginForGetRequestStream(request, null);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            this.httpProcessingProfiler.OnExceptionForGetRequestStream(null, this.ex, request, null);

            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, 0, string.Empty);
        }
        #endregion //SyncScenarios

        #region AsyncScenarios

        /// <summary>
        /// Validates HttpProcessingProfiler calculates startTime from the start of very first BeginGetRequestStream if any
        /// 1.create request
        /// 2.request.BeginGetRequestStream
        /// 3.request.BeginGetResponse
        /// 4.request.EndGetResponse        
        /// The expected time is the time between 2 and 4.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler calculates startTime from the start of very first BeginGetRequestStream if any")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerStartTimeFromGetRequestStreamAsync()
        {
            var request = WebRequest.Create(this.testUrl);
            var returnObjectPassed = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK);

            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);

            this.httpProcessingProfiler.OnBeginForBeginGetRequestStream(request, null, null);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingProfiler.OnBeginForBeginGetResponse(request, null, null);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
            this.httpProcessingProfiler.OnEndForEndGetResponse(null, returnObjectPassed, request, null);
                        
            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, 2 * this.sleepTimeMsecBetweenBeginAndEnd, "200");
        }        

        #endregion AsyncScenarios

        #region ProfilerCorrectlyPreventsRecursion
        
        /// <summary>
        /// Validates HttpProcessingProfiler sends correct telemetry on calling OnEndForGetResponse.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler filters out custom ApplicationInsights resource.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerGetResponseIgnoreCustomAppInsightsUrl()
        {
            Uri specificEndpointAddress = new Uri("http://localhost:8989");
            var currentChannel = this.configuration.TelemetryChannel;
            string currentEndpointAddress = null;

            if (currentChannel is InMemoryChannel)
            {
                currentEndpointAddress = currentChannel.EndpointAddress;
                currentChannel.EndpointAddress = specificEndpointAddress.ToString();
            }
            else
            {
                this.configuration.TelemetryChannel = new InMemoryChannel
                {
                    EndpointAddress = specificEndpointAddress.ToString()
                };
            }

            try
            {
                var request = WebRequest.Create(specificEndpointAddress);
                var returnObjectPassed = new object();
                this.httpProcessingProfiler.OnBeginForGetResponse(request);
                Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
                var objectReturned = this.httpProcessingProfiler.OnEndForGetResponse(null, returnObjectPassed, request);

                Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForGetResponse processor is not the same as expected return object");
                Assert.AreEqual(0, this.sendItems.Count, "No RDD packets should be created for AI urls.");
            }
            finally
            {
                this.configuration.TelemetryChannel = currentChannel;
                if (currentEndpointAddress != null)
                {
                    this.configuration.TelemetryChannel.EndpointAddress = currentEndpointAddress;
                }
            }
        }

        #endregion

        #region Misc

        /// <summary>
        /// Validates HttpProcessingProfiler determines resource name correctly for simple url.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler determines resource name correctly for simple url.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerResourceNameTestForSimpleUrl()
        {
            var request = WebRequest.Create(this.testUrl);
            var expectedName = this.testUrl;
            var actualResourceName = this.httpProcessingProfiler.GetUrl(request);
            Assert.AreEqual(expectedName, actualResourceName, "HttpProcessingProfiler returned incorrect resource name");

            Assert.AreEqual(null, this.httpProcessingProfiler.GetUrl(null));
        }

        /// <summary>
        /// Validates HttpProcessingProfiler determines resource name correctly for url with query string.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler determines resource name correctly for url with query string.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerResourceNameTestForUrlWithQueryString()
        {
            UriBuilder ub = new UriBuilder(this.testUrl);
            ub.Query = "querystring=1";
            var request = WebRequest.Create(ub.Uri);
            var expectedName = ub.Uri.ToString();
            var actualResourceName = this.httpProcessingProfiler.GetUrl(request);
            Assert.AreEqual(expectedName, actualResourceName.ToString(), "HttpProcessingProfiler returned incorrect resource name");
        }

        /// <summary>
        /// Validates HttpProcessingProfiler determines resource name correctly for url with paths.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler determines resource name correctly for url with path.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerResourceNameTestForUrlWithPaths()
        {
            UriBuilder ub = new UriBuilder(this.testUrl);
            ub.Path = "/rewards";
            var request = WebRequest.Create(ub.Uri);
            var expectedName = ub.Uri.ToString();
            var actualResourceName = this.httpProcessingProfiler.GetUrl(request);
            Assert.AreEqual(expectedName, actualResourceName.ToString(), "HttpProcessingProfiler returned incorrect resource name");
        }

        #endregion //Misc       

        #region LoggingTests

        /// <summary>
        /// Validates HttpProcessingProfiler logs to event log when resource name is null or empty.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingProfiler logs to event log when resource name is null or empty.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingProfilerLogsWhenResourceNameIsNullOrEmpty()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                
                // pass any object other than WebRequest so that Processor will fail to extract any url/name
                var request = new object();
                DependencyTelemetry operationReturned = (DependencyTelemetry)this.httpProcessingProfiler.OnBeginForGetResponse(request);
                
                Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
                TestUtils.ValidateEventLogMessage(listener, "UnexpectedCallbackParameter", EventLevel.Warning);                
            }
        }

        #endregion //LoggingTests

        #region Disposable
        public void Dispose()
        {
            this.configuration.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Disposable

        #region Helpers
        
        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, Uri uri, string type, bool success, double expectedValue, string resultCode)
        {
            Assert.AreEqual("GET " + uri.AbsolutePath, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            Assert.AreEqual(uri.Host, remoteDependencyTelemetryActual.Target, true, "Resource target in the sent telemetry is wrong");
            Assert.AreEqual(uri.OriginalString, remoteDependencyTelemetryActual.Data, true, "Resource data in the sent telemetry is wrong");
            Assert.AreEqual(type.ToString(), remoteDependencyTelemetryActual.Type, "DependencyKind in the sent telemetry is wrong");
            Assert.AreEqual(success, remoteDependencyTelemetryActual.Success, "Success in the sent telemetry is wrong");
            Assert.AreEqual(resultCode, remoteDependencyTelemetryActual.ResultCode, "ResultCode in the sent telemetry is wrong");

            var valueMinRelaxed = expectedValue - TimeAccuracyMilliseconds;
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration >= TimeSpan.FromMilliseconds(valueMinRelaxed),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should be equal or more than the time duration between start and end", remoteDependencyTelemetryActual.Duration));

            var valueMax = expectedValue + TimeAccuracyMilliseconds;
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration <= TimeSpan.FromMilliseconds(valueMax),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should not be significantly bigger than the time duration between start and end", remoteDependencyTelemetryActual.Duration));

            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModuleTest), prefix: "rddp:");
            Assert.AreEqual(expectedVersion, remoteDependencyTelemetryActual.Context.GetInternalContext().SdkVersion);
        }
        #endregion Helpers
    }
}
