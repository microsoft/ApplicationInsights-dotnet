#if NET45
namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DesktopDiagnosticSourceHttpProcessingTests
    {
        #region Fields
        private const int TimeAccuracyMilliseconds = 50;
        private const string TestInstrumentationKey = nameof(TestInstrumentationKey);
        private const string TestApplicationId = nameof(TestApplicationId);
        private Uri testUrl = new Uri("http://www.microsoft.com/");
        private int sleepTimeMsecBetweenBeginAndEnd = 100;
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sendItems = new List<ITelemetry>();
        private DesktopDiagnosticSourceHttpProcessing httpDesktopProcessingFramework;
        #endregion //Fields

        #region TestInitialize

        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration()
            {
                TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) },
                InstrumentationKey = TestInstrumentationKey,
                ApplicationIdProvider = new MockApplicationIdProvider(TestInstrumentationKey, TestApplicationId)
            };

            this.httpDesktopProcessingFramework = new DesktopDiagnosticSourceHttpProcessing(this.configuration, new CacheBasedOperationHolder("testCache", 100 * 1000), /*setCorrelationHeaders*/ true, new List<string>());
            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = false;
        }
        #endregion //TestInitiliaze

        /// <summary>
        /// Validates that OnRequestSend and OnResponseReceive sends valid telemetry.
        /// </summary>
        [TestMethod]
        public void RddTestHttpDesktopProcessingFrameworkUpdateTelemetryName()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);

            var stopwatch = Stopwatch.StartNew();
            this.httpDesktopProcessingFramework.OnBegin(request);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            var response = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK);
            this.httpDesktopProcessingFramework.OnEndResponse(request, response);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacketForOnRequestSend(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, stopwatch.Elapsed.TotalMilliseconds, "200");
        }

         /// <summary>
         /// Validates that OnBegin does not inject headers when called with injectCorrelationHeadersFlag = false.
         /// </summary>
         [TestMethod]
         public void RddTestHttpProcessingFrameworkDoNotInjectHeadersWhenFlagIsSet()
         {
             var activity = new Activity("parent").AddBaggage("k", "v").Start();
             HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);
             this.httpDesktopProcessingFramework.OnBegin(request, false);
             Assert.IsNull(request.Headers[RequestResponseHeaders.RequestIdHeader]);
             Assert.IsNull(request.Headers[RequestResponseHeaders.CorrelationContextHeader]);
             Assert.IsNotNull(request.Headers[RequestResponseHeaders.StandardRootIdHeader]);
             Assert.IsNotNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);
             activity.Stop();
         }

        /// <summary>
        /// Validates that even if multiple events have fired, as long as there is only
        /// one HttpWebRequest, only one event should be written, during the first call
        /// to OnResponseReceive.
        /// </summary>
        [TestMethod]
        public void RddTestHttpDesktopProcessingFrameworkNoDuplication()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);
            var redirectResponse = TestUtils.GenerateHttpWebResponse(HttpStatusCode.Redirect);
            var successResponse = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK);

            Stopwatch stopwatch = Stopwatch.StartNew();
            this.httpDesktopProcessingFramework.OnBegin(request);  
            this.httpDesktopProcessingFramework.OnBegin(request);  
            this.httpDesktopProcessingFramework.OnBegin(request);  
            this.httpDesktopProcessingFramework.OnBegin(request);  
            this.httpDesktopProcessingFramework.OnBegin(request);  
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpDesktopProcessingFramework.OnEndResponse(request, redirectResponse);
            stopwatch.Stop();
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");

            this.httpDesktopProcessingFramework.OnEndResponse(request, redirectResponse);
            this.httpDesktopProcessingFramework.OnEndResponse(request, successResponse);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacketForOnRequestSend(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, stopwatch.Elapsed.TotalMilliseconds, "302");
        }

        /// <summary>
        /// Validates if DependencyTelemetry sent contains the cross component correlation ID.
        /// </summary>
        [TestMethod]
        [Description("Validates if DependencyTelemetry sent contains the cross component correlation ID.")]
        public void RddTestHttpDesktopProcessingFrameworkOnEndAddsAppIdToTargetField()
        {
            // Here is a sample App ID, since the test initialize method adds a random ikey and our mock getAppId method pretends that the appId for a given ikey is the same as the ikey.
            // This will not match the current component's App ID. Hence represents an external component.
            string appId = "0935FC42-FE1A-4C67-975C-0C9D5CBDEE8E";

            var request = WebRequest.Create(this.testUrl);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { RequestResponseHeaders.RequestContextHeader, this.GetCorrelationIdHeaderValue(appId) }
            };

            var response = TestUtils.GenerateHttpWebResponse(HttpStatusCode.OK, headers);

            this.httpDesktopProcessingFramework.OnBegin(request);
            this.httpDesktopProcessingFramework.OnEndResponse(request, response);
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            Assert.AreEqual(this.testUrl.Host + " | " + this.GetCorrelationIdValue(appId), ((DependencyTelemetry)this.sendItems[0]).Target);
        }

        /// <summary>
        /// Ensures that the source request header is added when request is sent.
        /// </summary>
        [TestMethod]
        [Description("Ensures that the source request header is added when request is sent.")]
        public void RddTestHttpDesktopProcessingFrameworkOnBeginAddsSourceHeader()
        {
            var request = WebRequest.Create(this.testUrl);

            Assert.IsNull(request.Headers[RequestResponseHeaders.RequestContextHeader]);

            this.httpDesktopProcessingFramework.OnBegin(request);
            Assert.IsNotNull(request.Headers.GetNameValueHeaderValue(RequestResponseHeaders.RequestContextHeader, RequestResponseHeaders.RequestContextCorrelationSourceKey));
        }

        /// <summary>
        /// Ensures that the parent id header is added when request is sent.
        /// </summary>
        [TestMethod]
        public void RddTestHttpDesktopProcessingFrameworkOnBeginAddsParentIdHeader()
        {
            var request = WebRequest.Create(this.testUrl);

            Assert.IsNull(request.Headers[RequestResponseHeaders.StandardParentIdHeader]);

            var client = new TelemetryClient(this.configuration);
            using (var op = client.StartOperation<RequestTelemetry>("request"))
            {
                this.httpDesktopProcessingFramework.OnBegin(request);

                var actualParentIdHeader = request.Headers[RequestResponseHeaders.StandardParentIdHeader];
                var actualRequestIdHeader = request.Headers[RequestResponseHeaders.RequestIdHeader];
                Assert.IsNotNull(actualParentIdHeader);
                Assert.AreNotEqual(actualParentIdHeader, op.Telemetry.Context.Operation.Id);

                Assert.AreEqual(actualParentIdHeader, actualRequestIdHeader);
                Assert.IsTrue(actualRequestIdHeader.StartsWith(Activity.Current.Id, StringComparison.Ordinal));
                Assert.AreNotEqual(Activity.Current.Id, actualRequestIdHeader);

                // This code should go away when Activity is fixed: https://github.com/dotnet/corefx/issues/18418
                // check that Ids are not generated by Activity
                // so they look like OperationTelemetry.Id
                var operationId = op.Telemetry.Context.Operation.Id;

                // length is like default RequestTelemetry.Id length
                Assert.AreEqual(new DependencyTelemetry().Id.Length, operationId.Length);

                // operationId is ulong base64 encoded
                byte[] data = Convert.FromBase64String(operationId);
                Assert.AreEqual(8, data.Length);
                BitConverter.ToUInt64(data, 0);

                // does not look like root Id generated by Activity
                Assert.AreEqual(1, operationId.Split('-').Length);

                //// end of workaround test
            }
        }

        /// <summary>
        /// Ensures that the source request header is not added, as per the config, when request is sent.
        /// </summary>
        [TestMethod]
        [Description("Ensures that the source request header is not added when the config commands as such")]
        public void RddTestHttpDesktopProcessingFrameworkOnBeginSkipsAddingSourceHeaderPerConfig()
        {
            string hostnamepart = "partofhostname";
            string url = string.Format(CultureInfo.InvariantCulture, "http://hostnamestart{0}hostnameend.com/path/to/something?param=1", hostnamepart);
            var request = WebRequest.Create(new Uri(url));

            Assert.IsNull(request.Headers[RequestResponseHeaders.RequestContextHeader]);
            Assert.AreEqual(0, request.Headers.Keys.Cast<string>().Where((x) => { return x.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase); }).Count());

            var localHttpProcessingFramework = new DesktopDiagnosticSourceHttpProcessing(
                this.configuration, 
                new CacheBasedOperationHolder("testCache", 100 * 1000),  
                false, 
                new List<string>());

            localHttpProcessingFramework.OnBegin(request);
            Assert.IsNull(request.Headers[RequestResponseHeaders.RequestContextHeader]);
            Assert.AreEqual(0, request.Headers.Keys.Cast<string>().Count(x => x.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase)));

            ICollection<string> exclusionList = new SanitizedHostList() { "randomstringtoexclude", hostnamepart };
            localHttpProcessingFramework = new DesktopDiagnosticSourceHttpProcessing(
                this.configuration,
                new CacheBasedOperationHolder("testCache", 100 * 1000), 
                true, 
                exclusionList);

            localHttpProcessingFramework.OnBegin(request);
            Assert.IsNull(request.Headers[RequestResponseHeaders.RequestContextHeader]);
            Assert.AreEqual(0, request.Headers.Keys.Cast<string>().Count(x => x.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Ensures that the source request header is not overwritten if already provided by the user.
        /// </summary>
        [TestMethod]
        [Description("Ensures that the source request header is not overwritten if already provided by the user.")]
        public void RddTestHttpDesktopProcessingFrameworkOnBeginDoesNotOverwriteExistingSource()
        {
            string sampleHeaderValueWithAppId = RequestResponseHeaders.RequestContextCorrelationSourceKey + "=HelloWorld";
            var request = WebRequest.Create(this.testUrl);

            request.Headers.Add(RequestResponseHeaders.RequestContextHeader, sampleHeaderValueWithAppId);

            this.httpDesktopProcessingFramework.OnBegin(request);
            var actualHeaderValue = request.Headers[RequestResponseHeaders.RequestContextHeader];

            Assert.IsNotNull(actualHeaderValue);
            Assert.AreEqual(sampleHeaderValueWithAppId, actualHeaderValue);

            string sampleHeaderValueWithoutAppId = "helloWorld";
            request = WebRequest.Create(this.testUrl);

            request.Headers.Add(RequestResponseHeaders.RequestContextHeader, sampleHeaderValueWithoutAppId);

            this.httpDesktopProcessingFramework.OnBegin(request);
            actualHeaderValue = request.Headers[RequestResponseHeaders.RequestContextHeader];

            Assert.IsNotNull(actualHeaderValue);
            Assert.AreNotEqual(sampleHeaderValueWithAppId, actualHeaderValue);
        }

        private static void ValidateTelemetryPacketForOnRequestSend(DependencyTelemetry remoteDependencyTelemetryActual, Uri url, string kind, bool? success, double valueMin, string statusCode)
        {
            Assert.AreEqual("GET " + url.AbsolutePath, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            string expectedVersion =
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), prefix: "rdddsd:");
            ValidateTelemetryPacket(remoteDependencyTelemetryActual, url, kind, success, valueMin, statusCode, expectedVersion);
        }

        private static void ValidateTelemetryPacket(DependencyTelemetry remoteDependencyTelemetryActual, Uri url, string kind, bool? success, double valueMin, string statusCode, string expectedVersion)
        {
            Assert.AreEqual(url.Host, remoteDependencyTelemetryActual.Target, true, "Resource target in the sent telemetry is wrong");
            Assert.AreEqual(url.OriginalString, remoteDependencyTelemetryActual.Data, true, "Resource data in the sent telemetry is wrong");
            Assert.AreEqual(kind.ToString(), remoteDependencyTelemetryActual.Type, "DependencyKind in the sent telemetry is wrong");
            Assert.AreEqual(success, remoteDependencyTelemetryActual.Success, "Success in the sent telemetry is wrong");
            Assert.AreEqual(statusCode, remoteDependencyTelemetryActual.ResultCode, "ResultCode in the sent telemetry is wrong");

            var valueMinRelaxed = valueMin - TimeAccuracyMilliseconds;
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration >= TimeSpan.FromMilliseconds(valueMinRelaxed),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should be equal or more than the time duration between start and end", remoteDependencyTelemetryActual.Duration));

            var valueMax = valueMin + TimeAccuracyMilliseconds;
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration <= TimeSpan.FromMilliseconds(valueMax),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should not be signigficantly bigger then the time duration between start and end", remoteDependencyTelemetryActual.Duration));

            Assert.AreEqual(expectedVersion, remoteDependencyTelemetryActual.Context.GetInternalContext().SdkVersion);
        }

        private string GetCorrelationIdValue(string appId)
        {
            return string.Format(CultureInfo.InvariantCulture, "cid-v1:{0}", appId);
        }

        private string GetCorrelationIdHeaderValue(string appId)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}=cid-v1:{1}", RequestResponseHeaders.RequestContextCorrelationTargetKey, appId);
        }
    }
}
#endif