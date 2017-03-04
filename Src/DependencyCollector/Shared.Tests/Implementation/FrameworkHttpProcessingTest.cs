#if !NET40
namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;        
    using System.Threading;
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
    public sealed class FrameworkHttpProcessingTest : IDisposable
    {
        #region Fields
        private const string TestUrl = "http://www.microsoft.com/";
        private const string TestUrlNonStandardPort = "http://www.microsoft.com:911/";
        private const int TimeAccuracyMilliseconds = 50;
        private int sleepTimeMsecBetweenBeginAndEnd = 100;       
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sendItems;
        private FrameworkHttpProcessing httpProcessingFramework;        
        #endregion //Fields

        #region TestInitialize

        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>(); 
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.httpProcessingFramework = new FrameworkHttpProcessing(this.configuration, new CacheBasedOperationHolder("testCache", 100 * 1000));
        }

        [TestCleanup]
        public void Cleanup()
        {        
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
            this.httpProcessingFramework.OnBeginHttpCallback(id, TestUrl);            
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
            this.httpProcessingFramework.OnBeginHttpCallback(id, TestUrl);  
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id, true, false, 200);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, new Uri(TestUrl), RemoteDependencyConstants.HTTP, true, stopwatch.Elapsed.TotalMilliseconds, "200");
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
            this.httpProcessingFramework.OnBeginHttpCallback(id, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id, false, false, 500);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, new Uri(TestUrl), RemoteDependencyConstants.HTTP, false, stopwatch.Elapsed.TotalMilliseconds, "500");
        }

        [TestMethod]
        public void IfNoStatusCodeItemIsNotTracked()
        {
            int? statusCode = null;

            this.httpProcessingFramework.OnBeginHttpCallback(100, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, statusCode);

            Assert.AreEqual(0, this.sendItems.Count);
        }

        [TestMethod]
        public void IfNegativeStatusCodeSuccessIsFalse()
        {
            int? statusCode = -1;

            this.httpProcessingFramework.OnBeginHttpCallback(100, TestUrl);
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

            this.httpProcessingFramework.OnBeginHttpCallback(100, TestUrl);
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
            var id1 = 100;
            var id2 = 200;
            this.httpProcessingFramework.OnBeginHttpCallback(id1, TestUrl);
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

            this.httpProcessingFramework.OnBeginHttpCallback(100, TestUrl);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, statusCode);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            var actual = this.sendItems[0] as DependencyTelemetry;

            Assert.IsFalse(actual.Success.Value);
        }

        [TestMethod]
        public void OnEndHttpCallbackSetsSuccessToTrueForLessThan400()
        {
            int statusCode = 399;

            this.httpProcessingFramework.OnBeginHttpCallback(100, TestUrl);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, statusCode);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            var actual = this.sendItems[0] as DependencyTelemetry;

            Assert.IsTrue(actual.Success.Value);
        }

        [TestMethod]
        public void OnEndHttpCallbackSetsSuccessToFalseForMoreThan400()
        {
            int statusCode = 400;

            this.httpProcessingFramework.OnBeginHttpCallback(100, TestUrl);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, statusCode);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            var actual = this.sendItems[0] as DependencyTelemetry;

            Assert.IsFalse(actual.Success.Value);
        }

        [TestMethod]
        public void HttpProcessorSetsTargetForNonStandardPort()
        {
            Uri testUrl = new Uri(TestUrlNonStandardPort);
            this.httpProcessingFramework.OnBeginHttpCallback(100, TestUrlNonStandardPort);
            this.httpProcessingFramework.OnEndHttpCallback(100, null, false, 500);

            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            DependencyTelemetry receivedItem = (DependencyTelemetry)this.sendItems[0];
            string expectedTarget = testUrl.Host + ":" + testUrl.Port;
            Assert.AreEqual(expectedTarget, receivedItem.Target, "HttpProcessingFramework returned incorrect target for non standard port.");
        }

        #endregion //BeginEndCallBacks

        #region AsyncScenarios

        /// <summary>
        /// Validates HttpProcessingFramework calculates startTime from the start of very first BeginGetRequestStream if any
        /// 1.create request
        /// 2.request.BeginGetRequestStream
        /// 3.request.BeginGetResponse
        /// 4.request.EndGetResponse        
        /// The expected time is the time between 2 and 4.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework calculates startTime from the start of very first BeginGetRequestStream if any")]
        public void RddTestHttpProcessingFrameworkStartTimeFromGetRequestStreamAsync()
        {
            var id1 = 100;
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.httpProcessingFramework.OnBeginHttpCallback(id1, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingFramework.OnBeginHttpCallback(id1, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id1, true, false, 200);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, new Uri(TestUrl), RemoteDependencyConstants.HTTP, true, stopwatch.Elapsed.TotalMilliseconds, "200");
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
        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, Uri url, string kind, bool? success, double valueMin, string statusCode)
        {
            Assert.AreEqual(url.AbsolutePath, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
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

            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModuleTest), prefix: "rddf:");
            Assert.AreEqual(expectedVersion, remoteDependencyTelemetryActual.Context.GetInternalContext().SdkVersion);
        }
        #endregion Helpers
    }
}
#endif