#if !NET40
namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;        
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Common;
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
    public sealed class FrameworkHttpProcessingTest : IDisposable
    {
#region Fields
        private const string RandomAppIdEndpoint = "http://app.id.endpoint"; // appIdEndpoint - this really won't be used for tests because of the app id provider override.
        private const int TimeAccuracyMilliseconds = 50;
        private Uri testUrl = new Uri("http://www.microsoft.com/");
        private Uri testUrlNonStandardPort = new Uri("http://www.microsoft.com:911/");
        private int sleepTimeMsecBetweenBeginAndEnd = 100;       
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sendItems;
        private FrameworkHttpProcessing httpProcessingFramework;
        private CacheBasedOperationHolder cache = new CacheBasedOperationHolder("testCache", 100 * 1000);
        #endregion //Fields

        #region TestInitialize

        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>(); 
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.httpProcessingFramework = new FrameworkHttpProcessing(this.configuration, this.cache, /*setCorrelationHeaders*/ true, new List<string>(), RandomAppIdEndpoint);
            this.httpProcessingFramework.OverrideCorrelationIdLookupHelper(new CorrelationIdLookupHelper(new Dictionary<string, string> { { this.configuration.InstrumentationKey, "cid-v1:" + this.configuration.InstrumentationKey } }));
            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = false;
        }
#endregion //TestInitiliaze

#region BeginEndCallBacks

        [TestMethod]
        public void OnBeginDoesNotThrowForIncorrectUrl()
        {
            this.httpProcessingFramework.OnBeginHttpCallback(100, "BadUrl"); // Should not throw
        }

        /// <summary>
        /// Validates HttpProcessingFramework returns correct operation for OnBeginHttpCallback.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework returns correct operation for OnBeginHttpCallback.")]
        public void RddTestHttpProcessingFrameworkOnBeginHttpCallback()
        {
            var id = 100;
            this.httpProcessingFramework.OnBeginHttpCallback(id, this.testUrl.OriginalString);            
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");         
        }

        /// <summary>
        /// Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback for success.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback for success.")]
        public void RddTestHttpProcessingFrameworkOnEndHttpCallbackSucess()
        {
            var id = 100;
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.httpProcessingFramework.OnBeginHttpCallback(id, this.testUrl.OriginalString);  
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id, true, false, 200);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacketForOnBeginHttpCallback(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, stopwatch.Elapsed.TotalMilliseconds, "200");
        }

        /// <summary>
        /// Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback for failure.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback for failure.")]
        public void RddTestHttpProcessingFrameworkOnEndHttpCallbackFailure()
        {
            var id = 100;
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.httpProcessingFramework.OnBeginHttpCallback(id, this.testUrl.OriginalString);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id, false, false, 500);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacketForOnBeginHttpCallback(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, false, stopwatch.Elapsed.TotalMilliseconds, "500");
        }

        [TestMethod]
        public void IfNoStatusCodeItemIsNotTracked()
        {
            int? statusCode = null;

            this.httpProcessingFramework.OnBeginHttpCallback(100, this.testUrl.OriginalString);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, statusCode);

            Assert.AreEqual(0, this.sendItems.Count);
        }

        [TestMethod]
        public void IfNegativeStatusCodeSuccessIsFalse()
        {
            int? statusCode = -1;

            this.httpProcessingFramework.OnBeginHttpCallback(100, this.testUrl.OriginalString);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, statusCode);

            var dependency = this.sendItems[0] as DependencyTelemetry;
            Assert.IsFalse(dependency.Success.Value);
            Assert.AreEqual(string.Empty, dependency.ResultCode);
        }

        [TestMethod]
        public void ForCorrectStatusCodeSuccessIsSetOnBaseOfIt()
        {
            int? statusCode = 200;
            bool success = false;

            this.httpProcessingFramework.OnBeginHttpCallback(100, this.testUrl.OriginalString);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingFramework.OnEndHttpCallback(100, success, false, statusCode);

            var dependency = this.sendItems[0] as DependencyTelemetry;
            Assert.IsTrue(dependency.Success.Value);
            Assert.AreEqual("200", dependency.ResultCode);
        }

        /// <summary>
        /// Validates HttpProcessingFramework does not sent telemetry on calling OnEndHttpCallback with an id which do not exist.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework does not sent telemetry on calling OnEndHttpCallback with an id which do not exist.")]
        public void RddTestHttpProcessingFrameworkOnEndHttpCallbackInvalidId()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);
            var id1 = ClientServerDependencyTracker.GetIdForRequestObject(request);
            var id2 = 200;
            this.httpProcessingFramework.OnBeginHttpCallback(id1, this.testUrl.ToString());  
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");

            this.httpProcessingFramework.OnEndHttpCallback(id2, true, true, null);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed as invalid id is passed");
        }

        [TestMethod]
        public void OnEndHttpCallbackSetsSuccessToFalseForNegativeStatusCode()
        {
            // -1 StatusCode is returned in case of no response
            int statusCode = -1;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);
            var id = ClientServerDependencyTracker.GetIdForRequestObject(request);
            this.httpProcessingFramework.OnBeginHttpCallback(id, this.testUrl.ToString());  
            this.httpProcessingFramework.OnEndHttpCallback(id, null, false, statusCode);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            var actual = this.sendItems[0] as DependencyTelemetry;

            Assert.IsFalse(actual.Success.Value);
        }

        [TestMethod]
        public void OnEndHttpCallbackSetsSuccessToTrueForLessThan400()
        {
            int statusCode = 399;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);
            var id = ClientServerDependencyTracker.GetIdForRequestObject(request);
            this.httpProcessingFramework.OnBeginHttpCallback(id, this.testUrl.ToString());  
            this.httpProcessingFramework.OnEndHttpCallback(id, null, false, statusCode);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            var actual = this.sendItems[0] as DependencyTelemetry;

            Assert.IsTrue(actual.Success.Value);
        }

        [TestMethod]
        public void FrameworkHttpProcessingIsDisabledWhenHttpDesktopDiagSourceIsEnabled()
        {
            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = true;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);
            var id = ClientServerDependencyTracker.GetIdForRequestObject(request);
            this.httpProcessingFramework.OnBeginHttpCallback(id, this.testUrl.ToString());
            this.httpProcessingFramework.OnEndHttpCallback(id, null, false, 200);

            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be sent");
        }

        [TestMethod]
        public void OnEndHttpCallbackSetsSuccessToFalseForMoreThan400()
        {
            int statusCode = 400;

            this.httpProcessingFramework.OnBeginHttpCallback(100, this.testUrl.OriginalString);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, statusCode);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            var actual = this.sendItems[0] as DependencyTelemetry;

            Assert.IsFalse(actual.Success.Value);
        }

        [TestMethod]
        public void OnEndHttpCallbackWithoutStatusCodeRemovesTelemetryFromCache()
        {
            this.httpProcessingFramework.OnBeginHttpCallback(100, this.testUrl.OriginalString);
            Assert.IsNotNull(this.cache.Get(100));

            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, null);

            Assert.AreEqual(0, this.sendItems.Count, "Telemetry item should not be sent");
            Assert.IsNull(DependencyTableStore.Instance.WebRequestCacheHolder.Get(100));
        }

        [TestMethod]
        public void OnEndHttpCallbackWithoutStatusCodeDoesNotRemoveTelemetryFromCacheWhenDiagnosticSourceIsActivated()
        {
            this.httpProcessingFramework.OnBeginHttpCallback(100, this.testUrl.OriginalString);
            Assert.IsNotNull(this.cache.Get(100));

            DependencyTableStore.IsDesktopHttpDiagnosticSourceActivated = true;
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, null);

            Assert.AreEqual(0, this.sendItems.Count, "Telemetry item should not be sent");
            Assert.IsNotNull(this.cache.Get(100));
        }

        [TestMethod]
        public void HttpProcessorSetsTargetForNonStandardPort()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrlNonStandardPort);
            var id = ClientServerDependencyTracker.GetIdForRequestObject(request);
            this.httpProcessingFramework.OnBeginHttpCallback(id, this.testUrlNonStandardPort.ToString());  
            this.httpProcessingFramework.OnEndHttpCallback(id, null, false, 500);

            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            DependencyTelemetry receivedItem = (DependencyTelemetry)this.sendItems[0];
            string expectedTarget = this.testUrlNonStandardPort.Host + ":" + this.testUrlNonStandardPort.Port;
            Assert.AreEqual(expectedTarget, receivedItem.Target, "HttpProcessingFramework returned incorrect target for non standard port.");
        }

#endregion //BeginEndCallBacks

#region AsyncScenarios

        /// <summary>
        /// Validates HttpProcessingFramework calculates startTime from the start of very first OnRequestSend if any
        /// 1.create request
        /// 2.request.OnRequestSend
        /// 3.request.OnRequestSend
        /// 4.request.OnEndHttpCallback
        /// The expected time is the time between 2 and 4.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework calculates startTime from the start of very first OnRequestSend if any")]
        public void RddTestHttpProcessingFrameworkStartTimeFromGetRequestStreamAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.testUrl);
            var id1 = ClientServerDependencyTracker.GetIdForRequestObject(request);
            this.httpProcessingFramework.OnBeginHttpCallback(id1, this.testUrl.ToString());  
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingFramework.OnBeginHttpCallback(id1, this.testUrl.ToString());
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id1, true, false, 200);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            ValidateTelemetryPacketForOnBeginHttpCallback(this.sendItems[0] as DependencyTelemetry, this.testUrl, RemoteDependencyConstants.HTTP, true, stopwatch.Elapsed.TotalMilliseconds, "200");
        }        

#endregion AsyncScenarios
               
#region Disposable
        public void Dispose()
        {            
            this.configuration.Dispose();
            GC.SuppressFinalize(this);
        }
#endregion Disposable

#region Helpers
        private static void ValidateTelemetryPacketForOnBeginHttpCallback(
            DependencyTelemetry remoteDependencyTelemetryActual, Uri url, string kind, bool? success, double valueMin, string statusCode)
        {
            Assert.AreEqual(url.AbsolutePath, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), prefix: "rddf:");
            ValidateTelemetryPacket(remoteDependencyTelemetryActual, url, kind, success, valueMin, statusCode, expectedVersion);
        }

        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, Uri url, string kind, bool? success, double valueMin, string statusCode, string expectedVersion)
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

#endregion Helpers
    }
}
#endif