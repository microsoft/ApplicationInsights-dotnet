// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsTraceFilterTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Tracing;
using Microsoft.ApplicationInsights.Tracing.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights.TraceListener.Tests
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposing the object on the TestCleanup method")]
    public class ApplicationInsightsTraceFilterTests
    {
        private AdapterHelper adapterHelper;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            this.adapterHelper = new AdapterHelper();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.adapterHelper.Dispose();
        }

        [TestMethod]
        [TestCategory("TraceListener")]
        public void RespectFilterForWrite()
        {
            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.Write("message"),
                    false,
                    SourceLevels.Warning);
        }

        [TestMethod]
        [TestCategory("TraceListener")]
        public void TreatWriteAsVerbose()
        {
            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.Write("message"),
                    true,
                    SourceLevels.Verbose);
        }

        [TestMethod]
        [TestCategory("TraceListener")]
        public void RespectFilterForWriteLine()
        {
            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.WriteLine("message"),
                    false,
                    SourceLevels.Warning);
        }

        [TestMethod]
        [TestCategory("TraceListener")]
        public void TreatWriteLineAsVerbose()
        {
            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.WriteLine("message"),
                    true,
                    SourceLevels.Verbose);
        }

        [TestMethod, Ignore]
        public void RespectFilterForTraceEventWithMessage()
        {
            Tuple<TraceEventType, bool> expect = this.LookupTraceFiterExpections();

            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.TraceEvent(shimTraceEventCache, "hello", expect.Item1, 0, "test"),
                    expect.Item2);
        }

        [TestMethod, Ignore]
        public void RespectFilterForTraceEventWithoutMessage()
        {
            Tuple<TraceEventType, bool> expect = this.LookupTraceFiterExpections(false);

            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.TraceEvent(shimTraceEventCache, "hello", expect.Item1, 0),
                    expect.Item2,
                    SourceLevels.Error);
        }

        [TestMethod, Ignore]
        public void RespectFilterForTraceEventWithFormat()
        {
            Tuple<TraceEventType, bool> expect = this.LookupTraceFiterExpections();

            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.TraceEvent(shimTraceEventCache, "hello", expect.Item1, 0, "{0} event", 1),
                    expect.Item2);
        }

        [TestMethod, Ignore]
        public void RespectFilterForTraceDataSingleObject()
        {
            Tuple<TraceEventType, bool> expect = this.LookupTraceFiterExpections();

            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.TraceData(shimTraceEventCache, "hello", expect.Item1, 0, "data"),
                    expect.Item2);
        }

        [TestMethod, Ignore]
        public void RespectFilterForTraceDataMultipleObjects()
        {
            Tuple<TraceEventType, bool> expect = this.LookupTraceFiterExpections();

            string[] data = new[] { "data", "data2" };
            this.TraceFilterTestHelper(
                (ApplicationInsightsTraceListener traceListener, TraceEventCache shimTraceEventCache) =>
                    traceListener.TraceData(shimTraceEventCache, "hello", expect.Item1, 0, data),
                    expect.Item2);
        }

        private void TraceFilterTestHelper(
            Action<ApplicationInsightsTraceListener, TraceEventCache> callTraceEent,
            bool shouldTrace,
            SourceLevels filterLevel = SourceLevels.Warning)
        {
            TraceEventCache shimTraceEventCache = new TraceEventCache();

            using (var traceListener = new ApplicationInsightsTraceListener(this.adapterHelper.InstrumentationKey))
            {
                var telemetryConfiguration = new TelemetryConfiguration
                {
                    InstrumentationKey = Guid.NewGuid().ToString(),
                    TelemetryChannel = this.adapterHelper.Channel
                };

                traceListener.TelemetryClient = new TelemetryClient(telemetryConfiguration);

                var traceFilter = new EventTypeFilter(filterLevel);
                traceListener.Filter = traceFilter;

                callTraceEent(traceListener, shimTraceEventCache);

                Assert.AreEqual(shouldTrace, this.adapterHelper.Channel.SentItems.Length == 1);
            }
        }

        private Tuple<TraceEventType, bool> LookupTraceFiterExpections(bool? expectationOverride = null)
        {
            TraceEventType eventType = (TraceEventType)Enum.Parse(typeof(TraceEventType), (string)this.TestContext.DataRow["TraceEventType"]);
            bool expect = bool.Parse((string)this.TestContext.DataRow["Expect"]);
            return new Tuple<TraceEventType, bool>(eventType, expectationOverride.HasValue == true ? expectationOverride.Value : expect);
        }
    }
}
