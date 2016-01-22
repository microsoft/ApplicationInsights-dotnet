#if !NET40
namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
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
            this.httpProcessingFramework = new FrameworkHttpProcessing(this.configuration, new CacheBasedOperationHolder());
        }

        [TestCleanup]
        public void Cleanup()
        {        
        }
        #endregion //TestInitiliaze

        #region BeginEndCallBacks

        /// <summary>
        /// Validates HttpProcessingFramework returns correct operation for OnBeginHttpCallback.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework returns correct operation for OnBeginHttpCallback.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
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
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingFrameworkOnEndHttpCallbackSucess()
        {
            var id = 100;            
            this.httpProcessingFramework.OnBeginHttpCallback(id, TestUrl);  
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id, true, false, 200);  
            
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, TestUrl, RemoteDependencyKind.Http, true, true, 1, this.sleepTimeMsecBetweenBeginAndEnd, "200");
        }

        /// <summary>
        /// Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback for failure.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback for failure.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingFrameworkOnEndHttpCallbackFailure()
        {
            var id = 100;
            this.httpProcessingFramework.OnBeginHttpCallback(id, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id, false, false, 500);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, TestUrl, RemoteDependencyKind.Http, false, true, 1, this.sleepTimeMsecBetweenBeginAndEnd, "500");
        }

        /// <summary>
        /// Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback with no success flag passed.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework sends correct telemetry on calling OnEndHttpCallback with no success flag passed.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingFrameworkOnEndHttpCallbackEmptySuccessFlag()
        {
            var id = 100;
            this.httpProcessingFramework.OnBeginHttpCallback(id, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id, null, false, null);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, TestUrl, RemoteDependencyKind.Http, null, true, 1, this.sleepTimeMsecBetweenBeginAndEnd, string.Empty);
        }

        /// <summary>
        /// Validates HttpProcessingFramework does not sent telemetry on calling OnEndHttpCallback with an id which do not exist.
        /// </summary>
        [TestMethod]
        [Description("Validates HttpProcessingFramework does not sent telemetry on calling OnEndHttpCallback with an id which do not exist.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
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
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestHttpProcessingFrameworkStartTimeFromGetRequestStreamAsync()
        {
            var id1 = 100;            
            this.httpProcessingFramework.OnBeginHttpCallback(id1, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            this.httpProcessingFramework.OnBeginHttpCallback(id1, TestUrl);
            Thread.Sleep(this.sleepTimeMsecBetweenBeginAndEnd);
            Assert.AreEqual(0, this.sendItems.Count, "No telemetry item should be processed without calling End");
            this.httpProcessingFramework.OnEndHttpCallback(id1, true, false, 200);                        

            Assert.AreEqual(1, this.sendItems.Count, "Exactly one telemetry item should be sent");
            ValidateTelemetryPacket(this.sendItems[0] as DependencyTelemetry, TestUrl, RemoteDependencyKind.Http, true, true, 1, 2 * this.sleepTimeMsecBetweenBeginAndEnd, "200");
        }        

        #endregion AsyncScenarios
               
        #region Disposable
        public void Dispose()
        {
            this.httpProcessingFramework.Dispose();
            this.configuration.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Disposable

        #region Helpers
        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, string name, RemoteDependencyKind kind, bool? success, bool async, int count, double valueMin, string statusCode)
        {
            Assert.AreEqual(name, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            Assert.AreEqual(kind.ToString(), remoteDependencyTelemetryActual.DependencyKind, "DependencyKind in the sent telemetry is wrong");
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
        }
        #endregion Helpers
    }
}
#endif